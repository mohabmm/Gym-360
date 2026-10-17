import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

// ---------------------------------------------------------------------------
// Secure Storage
// ---------------------------------------------------------------------------
final secureStorageProvider = Provider<SecureStorageService>((ref) {
  return SecureStorageService();
});

// ---------------------------------------------------------------------------
// API Client
// ---------------------------------------------------------------------------
final apiClientProvider = Provider<ApiClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return ApiClient(
    baseUrl: ApiBaseUrl.resolve(),
    storage: storage,
    telemetry: ref.watch(telemetryServiceProvider),
  );
});

// ---------------------------------------------------------------------------
// Telemetry (mobile → SigNoz). Overridden in main() with the live instance
// built before the ProviderScope; the throwing default guards against running
// without it.
// ---------------------------------------------------------------------------
final telemetryServiceProvider = Provider<TelemetryService>((ref) {
  throw UnimplementedError(
    'telemetryServiceProvider must be overridden in main()',
  );
});

// ---------------------------------------------------------------------------
// FCM Service
// ---------------------------------------------------------------------------
final fcmServiceProvider = Provider<FCMService>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return FCMService(ref: ref, storage: storage);
});

// ---------------------------------------------------------------------------
// Auth State
// ---------------------------------------------------------------------------
enum AuthStatus { initial, authenticated, unauthenticated, pending }

class AuthState {
  final AuthStatus status;
  final UserModel? user;
  final String? error;

  const AuthState({
    this.status = AuthStatus.initial,
    this.user,
    this.error,
  });

  AuthState copyWith({
    AuthStatus? status,
    UserModel? user,
    String? error,
  }) {
    return AuthState(
      status: status ?? this.status,
      user: user ?? this.user,
      error: error ?? this.error,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  final Ref _ref;

  AuthNotifier(this._ref) : super(const AuthState()) {
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    try {
      final storage = _ref.read(secureStorageProvider);
      final token = await storage.getAccessToken();
      if (token == null) {
        state = const AuthState(status: AuthStatus.unauthenticated);
        return;
      }

      // Always validate the token with the backend on startup.
      try {
        final apiClient = _ref.read(apiClientProvider);
        final user = await apiClient.get<UserModel>(
          ApiEndpoints.profile,
          parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
        );
        if (user.status?.toLowerCase() == 'pending') {
          state = AuthState(status: AuthStatus.pending, user: user);
        } else {
          final activeGymId = user.gymMemberships
                  ?.firstWhere(
                    (m) => m.isActive ?? false,
                    orElse: () => user.gymMemberships!.first,
                  )
                  .gymId ??
              user.gymId;
          if (activeGymId != null && activeGymId.isNotEmpty) {
            await _ref
                .read(activeGymIdProvider.notifier)
                .setInitialGym(activeGymId);
          }
          state = AuthState(status: AuthStatus.authenticated, user: user);
          _initializeFcm();
        }
      } on UnauthorizedException {
        await storage.clearAll();
        state = const AuthState(status: AuthStatus.unauthenticated);
      } catch (e, st) {
        debugPrint('auto-login /profile failed: $e\n$st');
        state = const AuthState(status: AuthStatus.unauthenticated);
      }
    } catch (e) {
      // Keychain/storage read failed (e.g. after keychain reset).
      debugPrint('_checkAuth storage error: $e');
      state = const AuthState(status: AuthStatus.unauthenticated);
    }
  }

  Future<void> login(String email, String password) async {
    final apiClient = _ref.read(apiClientProvider);
    final storage = _ref.read(secureStorageProvider);

    final authResponse = await apiClient.post<AuthResponse>(
      ApiEndpoints.login,
      data: LoginRequest(email: email, password: password).toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );

    await _saveSession(storage, authResponse);
    await _rejectNonTrainer(storage, authResponse.user);

    // Persist the trainer's active gym so X-Gym-Id is injected on subsequent
    // requests. Trainers can work at multiple gyms (Q3) and switch later via
    // the gym switcher; on login we seed the header with the backend's
    // chosen active membership.
    final gymId = _loginGymId(authResponse.user);
    if (gymId != null && gymId.isNotEmpty) {
      await _ref.read(activeGymIdProvider.notifier).setInitialGym(gymId);
    }

    _applySignedInUser(authResponse.user);
  }

  Future<void> _saveSession(
    SecureStorageService storage,
    AuthResponse authResponse,
  ) async {
    await storage.saveTokens(
      accessToken: authResponse.accessToken,
      refreshToken: authResponse.refreshToken,
    );
    await storage.saveUserId(authResponse.user.id);
  }

  /// This app is for trainers only: wipe the just-saved session and fail.
  Future<void> _rejectNonTrainer(
    SecureStorageService storage,
    UserModel user,
  ) async {
    final normalizedRole = user.role.toLowerCase();
    if (normalizedRole == 'trainer') return;

    await storage.clearAll();
    state = const AuthState(status: AuthStatus.unauthenticated);
    throw ApiException(
      message: 'This app is for trainers only. '
          'The signed-in account has role "${user.role}".',
      statusCode: 403,
      errorCode: normalizedRole == 'trainee'
          ? 'ROLE_TRAINEE_NOT_ALLOWED'
          : 'ROLE_NOT_ALLOWED',
    );
  }

  /// The scalar `gymId`, falling back to the first membership when blank.
  static String? _loginGymId(UserModel user) {
    final gymId = user.gymId;
    final memberships = user.gymMemberships;
    final hasGymId = gymId != null && gymId.isNotEmpty;
    if (hasGymId || memberships == null || memberships.isEmpty) return gymId;
    return memberships.first.gymId;
  }

  void _applySignedInUser(UserModel user) {
    if (user.status?.toLowerCase() == 'pending') {
      state = AuthState(status: AuthStatus.pending, user: user);
      return;
    }
    state = AuthState(status: AuthStatus.authenticated, user: user);
    _initializeFcm();
  }

  /// Registers a trainer account. The role is always forced to `Trainer`.
  Future<void> register(RegisterRequest request) async {
    final apiClient = _ref.read(apiClientProvider);
    final storage = _ref.read(secureStorageProvider);

    final authResponse = await apiClient.post<AuthResponse>(
      ApiEndpoints.register,
      data: request.copyWith(role: 'Trainer').toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );

    await _saveSession(storage, authResponse);

    // Trainer registration results in Pending status until gym owner approves
    state = AuthState(
      status: AuthStatus.pending,
      user: authResponse.user,
    );
  }

  Future<void> logout() async {
    final storage = _ref.read(secureStorageProvider);
    await storage.clearAll();
    await Hive.box(StorageKeys.userBox).clear();
    await Hive.box(StorageKeys.gymBox).clear();
    await Hive.box(StorageKeys.planFeaturesBox).clear();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void updateUser(UserModel user) {
    state = state.copyWith(user: user);
  }

  Future<void> refreshProfile() async {
    final apiClient = _ref.read(apiClientProvider);
    try {
      final user = await apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
      );
      if (user.status?.toLowerCase() == 'pending') {
        state = AuthState(status: AuthStatus.pending, user: user);
      } else {
        state = AuthState(status: AuthStatus.authenticated, user: user);
      }
    } catch (_) {
      // Keep existing state on error
    }
  }

  void _initializeFcm() {
    final fcm = _ref.read(fcmServiceProvider);
    fcm.initialize();
    fcm.subscribeToGlobal();

    // Subscribe to active gym topic
    final activeGymId = _ref.read(activeGymIdProvider);
    if (activeGymId != null) {
      fcm.subscribeToGym(activeGymId);
    }
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref);
});

// ---------------------------------------------------------------------------
// Active Gym
// ---------------------------------------------------------------------------
class ActiveGymNotifier extends StateNotifier<String?> {
  final Ref _ref;

  ActiveGymNotifier(this._ref) : super(null) {
    _loadSaved();
  }

  Future<void> _loadSaved() async {
    // setInitialGym (called from AuthNotifier.login/_checkAuth) may race with
    // this; don't clobber a value already pushed by the auth path.
    if (state != null) return;
    final storage = _ref.read(secureStorageProvider);
    state = await storage.getCurrentGymId();
  }

  /// Publish a gym id immediately after login/token-validation so every
  /// `ref.watch(activeGymIdProvider)` consumer (clients roster, packages,
  /// conversations, meal plans, home stats) observes it without an app
  /// restart. Persists to storage as well.
  Future<void> setInitialGym(String gymId) async {
    if (gymId.isEmpty) return;
    final storage = _ref.read(secureStorageProvider);
    state = gymId;
    await storage.saveCurrentGymId(gymId);
  }

}

final activeGymIdProvider =
    StateNotifierProvider<ActiveGymNotifier, String?>((ref) {
  return ActiveGymNotifier(ref);
});

// ---------------------------------------------------------------------------
// Plan Features (overrides the core placeholder)
// ---------------------------------------------------------------------------
final trainerPlanFeaturesProvider =
    FutureProvider<PlanFeaturesModel>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (gymId == null) {
    return const PlanFeaturesModel(
      gymId: '',
      planCode: 'free',
      planName: 'Free',
      memberLimit: 50,
      trainerLimit: 1,
      staffLimit: 1,
      features: <PlanFeatureItemModel>[],
    );
  }

  final features = await apiClient.get<PlanFeaturesModel>(
    ApiEndpoints.planFeatures,
    parser: (data) =>
        PlanFeaturesModel.fromJson(data as Map<String, dynamic>),
  );

  // Cache in Hive
  final box = Hive.box(StorageKeys.planFeaturesBox);
  await box.put(StorageKeys.cachedPlanFeatures, features.toJson());

  return features;
});

// ---------------------------------------------------------------------------
// Gym Suspension
// ---------------------------------------------------------------------------
class GymSuspensionNotifier extends StateNotifier<bool> {
  final FCMService _fcm;
  StreamSubscription<bool>? _sub;

  GymSuspensionNotifier(this._fcm) : super(false) {
    _sub = _fcm.onGymSuspensionChanged.listen((suspended) {
      state = suspended;
    });
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}

final gymSuspensionProvider =
    StateNotifierProvider<GymSuspensionNotifier, bool>((ref) {
  final fcm = ref.watch(fcmServiceProvider);
  return GymSuspensionNotifier(fcm);
});

// ---------------------------------------------------------------------------
// Gym Branding
// ---------------------------------------------------------------------------
final gymBrandingProvider = Provider<GymBrandingTheme>((ref) {
  // Branding is loaded from cached gym data
  final box = Hive.box(StorageKeys.gymBox);
  final brandingJson = box.get(StorageKeys.gymBranding);
  if (brandingJson != null) {
    try {
      final branding = GymBrandingModel.fromJson(
        Map<String, dynamic>.from(brandingJson as Map),
      );
      return GymBrandingTheme(branding: branding);
    } catch (_) {
      return GymBrandingTheme.defaultBranding;
    }
  }
  return GymBrandingTheme.defaultBranding;
});

// ---------------------------------------------------------------------------
// Locale
// ---------------------------------------------------------------------------
final localeProvider = StateProvider<Locale>((ref) {
  final box = Hive.box(StorageKeys.settingsBox);
  final savedLocale = box.get(StorageKeys.locale, defaultValue: 'ar') as String;
  return Locale(savedLocale);
});

// ---------------------------------------------------------------------------
// Theme Mode
// ---------------------------------------------------------------------------
final themeModeProvider = StateProvider<ThemeMode>((ref) {
  final box = Hive.box(StorageKeys.settingsBox);
  final savedTheme =
      box.get(StorageKeys.themeMode, defaultValue: 'system') as String;
  switch (savedTheme) {
    case 'light':
      return ThemeMode.light;
    case 'dark':
      return ThemeMode.dark;
    default:
      return ThemeMode.system;
  }
});

/// Internal "Test Mode" toggle. Reflects the gym-level `early_session_start`
/// feature override. Source of truth lives in the BE; trainer app cannot flip
/// it (the staff/owner toggles it from their app). The Hive value seeds the
/// initial state for offline launches; `serverTestingModeProvider` syncs it
/// from the BE on every shell mount and gym switch.
final testModeProvider = StateProvider<bool>((ref) {
  final box = Hive.box(StorageKeys.settingsBox);
  return box.get(StorageKeys.testMode, defaultValue: false) as bool;
});

/// Fetches the BE testing-mode flag for the currently active gym. The trainer
/// app shell watches this and pushes the value into [testModeProvider] +
/// Hive so the existing local readers stay in sync without each caller
/// becoming async.
final serverTestingModeProvider = FutureProvider<bool>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (gymId == null || gymId.isEmpty) {
    return ref.read(testModeProvider);
  }
  try {
    final enabled = await apiClient.get<bool>(
      ApiEndpoints.gymTestingMode(gymId),
      parser: (data) {
        if (data is Map<String, dynamic>) {
          return data['enabled'] == true;
        }
        return false;
      },
    );
    // Push into the local cache so legacy sync readers see the same value.
    ref.read(testModeProvider.notifier).state = enabled;
    await Hive.box(StorageKeys.settingsBox)
        .put(StorageKeys.testMode, enabled);
    return enabled;
  } catch (_) {
    // BE unreachable / 403 — fall back to whatever the cache already has.
    return ref.read(testModeProvider);
  }
});

// ---------------------------------------------------------------------------
// Connectivity
// ---------------------------------------------------------------------------
final connectivityProvider = Provider<ConnectivityService>((ref) {
  return ConnectivityService();
});

// ---------------------------------------------------------------------------
// Trainer Gyms List
// ---------------------------------------------------------------------------
final trainerGymsProvider = FutureProvider<List<GymModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final auth = ref.watch(authProvider);
  if (auth.user == null) return [];

  final memberships = auth.user!.gymMemberships ?? [];
  final gyms = <GymModel>[];
  for (final membership in memberships) {
    try {
      final gym = await apiClient.get<GymModel>(
        ApiEndpoints.gymById(membership.gymId),
        parser: (data) => GymModel.fromJson(data as Map<String, dynamic>),
      );
      gyms.add(gym);
    } catch (_) {
      // Skip gyms that fail to load
    }
  }
  return gyms;
});
