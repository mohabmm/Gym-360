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
// Telemetry (mobile → SigNoz). Overridden in main() with the live instance.
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
enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  final AuthStatus status;
  final UserModel? user;
  final String? currentGymId;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.user,
    this.currentGymId,
  });

  AuthState copyWith({
    AuthStatus? status,
    UserModel? user,
    String? currentGymId,
  }) {
    return AuthState(
      status: status ?? this.status,
      user: user ?? this.user,
      currentGymId: currentGymId ?? this.currentGymId,
    );
  }
}

class AuthNotifier extends AsyncNotifier<AuthState> {
  @override
  Future<AuthState> build() async {
    final storage = ref.watch(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token == null) {
      return const AuthState(status: AuthStatus.unauthenticated);
    }

    final userBox = Hive.box(StorageKeys.userBox);
    final cachedUserJson = userBox.get(StorageKeys.cachedUser);
    final gymId = await storage.getCurrentGymId();

    // Always validate the token with the backend on startup. The AuthInterceptor
    // will transparently refresh on 401 — if refresh also fails, this throws
    // UnauthorizedException and we sign the user out.
    try {
      final apiClient = ref.read(apiClientProvider);
      final user = await apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
      );
      try {
        await userBox.put(StorageKeys.cachedUser, user.toJson());
      } catch (_) {
        // Cache write failure is non-fatal — continue with the fresh user.
      }
      _initializeFcm(gymId);
      return AuthState(
        status: AuthStatus.authenticated,
        user: user,
        currentGymId: gymId,
      );
    } on UnauthorizedException {
      // Backend rejected the token (and refresh). Force login.
      await storage.clearAll();
      await userBox.clear();
      return const AuthState(status: AuthStatus.unauthenticated);
    } catch (_) {
      // Network / server unreachable — fall back to cached user so the user
      // can keep using the app offline. If we have no cache, require login.
      if (cachedUserJson != null) {
        try {
          final user = UserModel.fromJson(
            Map<String, dynamic>.from(cachedUserJson as Map),
          );
          return AuthState(
            status: AuthStatus.authenticated,
            user: user,
            currentGymId: gymId,
          );
        } catch (_) {
          // Corrupt cache — drop through to unauthenticated.
        }
      }
      return const AuthState(status: AuthStatus.unauthenticated);
    }
  }

  Future<void> login(AuthResponse response) async {
    // App-gate: this is the trainee app — refuse non-Trainee principals at
    // the door so a Trainer / Staff / Owner who types their credentials here
    // gets a clean error instead of cascading 403s (MEMBER_ROLE_REQUIRED)
    // when /me/* handlers reject them downstream.
    final role = response.user.role.toLowerCase();
    if (role != 'trainee') {
      throw const ApiException(
        message:
            'This app is for members only. Use the role-specific app for your account.',
        statusCode: 403,
        errorCode: 'WRONG_APP_FOR_ROLE',
      );
    }

    final storage = ref.read(secureStorageProvider);
    await storage.saveTokens(
      accessToken: AccessToken(response.accessToken),
      refreshToken: RefreshToken(response.refreshToken),
    );
    await storage.saveUserId(UserId(response.user.id));

    // Determine gym ID: backend login returns a scalar user.gymId for the active
    // membership. Fall back to gymMemberships[0] for apps/flows that populate it
    // (some responses embed the full list, e.g. multi-gym trainer).
    final gymId = response.user.loginGymId;
    if (gymId != null && gymId.isNotEmpty) {
      await storage.saveCurrentGymId(GymId(gymId));
    }

    // The login response (UserResponse) omits gymMemberships / memberNumber.
    // Fetch the full profile so those fields are available immediately (e.g. QR
    // check-in screen reads user.gymMemberships to get the member number).
    final user = await _fetchProfileOr(response.user);
    await _cacheUser(user);

    state = AsyncData(AuthState(
      status: AuthStatus.authenticated,
      user: user,
      currentGymId: gymId,
    ));

    // Initialize FCM
    _initializeFcm(gymId);
  }

  /// Full profile from the BE, or [fallback] (the partial login-response
  /// user) when the fetch fails.
  Future<UserModel> _fetchProfileOr(UserModel fallback) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      return await apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
      );
    } catch (_) {
      // Profile fetch failed — continue with partial login-response user.
      return fallback;
    }
  }

  Future<void> _cacheUser(UserModel user) async {
    final userBox = Hive.box(StorageKeys.userBox);
    try {
      await userBox.put(StorageKeys.cachedUser, user.toJson());
    } catch (_) {}
  }

  Future<void> updateUser(UserModel user) async {
    final currentState = state.valueOrNull;
    if (currentState == null) return;

    final userBox = Hive.box(StorageKeys.userBox);
    await userBox.put(StorageKeys.cachedUser, user.toJson());

    state = AsyncData(currentState.copyWith(user: user));
  }

  /// Set the active gym for this user. One-gym rule (FOUNDER_DECISIONS Q3,
  /// revised 2026-05-01): only invoked at initial gym link via the link-gym
  /// QR/code flow. Trainees cannot switch gyms after linking.
  Future<void> setActiveGym(String gymId) async {
    final storage = ref.read(secureStorageProvider);
    await storage.saveCurrentGymId(GymId(gymId));

    final currentState = state.valueOrNull;
    if (currentState != null) {
      state = AsyncData(currentState.copyWith(currentGymId: gymId));
    }

    ref.invalidate(planFeaturesProvider);
  }

  Future<void> logout() async {
    final storage = ref.read(secureStorageProvider);
    await storage.clearAll();

    final userBox = Hive.box(StorageKeys.userBox);
    await userBox.clear();

    final gymBox = Hive.box(StorageKeys.gymBox);
    await gymBox.clear();

    final planBox = Hive.box(StorageKeys.planFeaturesBox);
    await planBox.clear();

    state = const AsyncData(
      AuthState(status: AuthStatus.unauthenticated),
    );
  }

  void _initializeFcm(String? gymId) {
    final fcm = ref.read(fcmServiceProvider);
    fcm.initialize();
    fcm.subscribeToGlobal();
    if (gymId != null) {
      fcm.subscribeToGym(gymId);
    }

    // Listen for config refresh to invalidate plan features
    fcm.onConfigRefresh.listen((_) {
      ref.invalidate(planFeaturesProvider);
    });

    // Listen for gym suspension
    fcm.onGymSuspensionChanged.listen((suspended) {
      ref.read(gymSuspendedProvider.notifier).state = suspended;
    });
  }
}

final authProvider = AsyncNotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);

// ---------------------------------------------------------------------------
// Plan Features — overrides the core placeholder
// ---------------------------------------------------------------------------
final trainePlanFeaturesProvider =
    FutureProvider<PlanFeaturesModel>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final authState = ref.watch(authProvider);

  if (authState.valueOrNull?.status != AuthStatus.authenticated) {
    throw Exception('Not authenticated');
  }

  // Try cache first
  final planBox = Hive.box(StorageKeys.planFeaturesBox);
  final cachedJson = planBox.get(StorageKeys.cachedPlanFeatures);
  if (cachedJson != null) {
    try {
      return PlanFeaturesModel.fromJson(
        Map<String, dynamic>.from(cachedJson as Map),
      );
    } catch (_) {
      // Fall through to API
    }
  }

  final features = await apiClient.get<PlanFeaturesModel>(
    ApiEndpoints.planFeatures,
    parser: (data) => PlanFeaturesModel.fromJson(data as Map<String, dynamic>),
  );

  await planBox.put(StorageKeys.cachedPlanFeatures, features.toJson());
  return features;
});

// ---------------------------------------------------------------------------
// Gym Suspended State
// ---------------------------------------------------------------------------
final gymSuspendedProvider = StateProvider<bool>((ref) => false);

// ---------------------------------------------------------------------------
// Gym Branding
// ---------------------------------------------------------------------------
// Fetch-then-cache. Cache is only an offline fallback — live SignalR events on
// branding_updated invalidate this provider so staff colour changes propagate
// without re-login (see brandingHubServiceProvider).
final gymBrandingProvider = FutureProvider<GymBrandingModel?>((ref) async {
  final authState = ref.watch(authProvider);
  final gymId = authState.valueOrNull?.currentGymId;
  if (gymId == null) return null;

  final gymBox = Hive.box(StorageKeys.gymBox);

  try {
    final apiClient = ref.read(apiClientProvider);
    final branding = await apiClient.get<GymBrandingModel>(
      ApiEndpoints.gymBranding(GymId(gymId)),
      parser: (data) =>
          GymBrandingModel.fromJson(data as Map<String, dynamic>),
    );
    await gymBox.put(StorageKeys.gymBranding, branding.toJson());
    return branding;
  } catch (_) {
    // Offline / API down — fall back to cached copy if any.
    final cachedJson = gymBox.get(StorageKeys.gymBranding);
    if (cachedJson != null) {
      try {
        return GymBrandingModel.fromJson(
          Map<String, dynamic>.from(cachedJson as Map),
        );
      } catch (_) {
        // Fall through to null
      }
    }
    return null;
  }
});

// ---------------------------------------------------------------------------
// Branding Hub (SignalR)
// ---------------------------------------------------------------------------
// Single hub instance per authenticated session. Watches auth + gym scope and
// re-connects / re-subscribes whenever either changes. On `branding_updated`
// it invalidates [gymBrandingProvider] so the UI refetches and rerenders.
final brandingHubServiceProvider = Provider<BrandingHubService?>((ref) {
  final authState = ref.watch(authProvider);
  final state = authState.valueOrNull;
  if (state?.status != AuthStatus.authenticated) return null;
  final gymId = state?.currentGymId;
  if (gymId == null || gymId.isEmpty) return null;

  BrandingHubService? service;
  bool disposed = false;

  () async {
    final storage = ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token == null || disposed) return;

    final svc = BrandingHubService(accessToken: token);
    svc.onBrandingUpdated = (_) {
      // Drop cached copy so the refetch reads fresh colours immediately.
      Hive.box(StorageKeys.gymBox).delete(StorageKeys.gymBranding);
      ref.invalidate(gymBrandingProvider);
    };

    await svc.connect();
    if (disposed) {
      await svc.disconnect();
      return;
    }
    await svc.subscribeToGym(gymId);
    service = svc;
  }();

  ref.onDispose(() {
    disposed = true;
    service?.disconnect();
  });

  return service;
});

// ---------------------------------------------------------------------------
// Locale
// ---------------------------------------------------------------------------
final localeProvider = StateProvider<Locale>((ref) {
  final settingsBox = Hive.box(StorageKeys.settingsBox);
  final savedLocale =
      settingsBox.get(StorageKeys.locale, defaultValue: AppConstants.localeAr);
  return Locale(savedLocale as String);
});

// ---------------------------------------------------------------------------
// Theme Mode
// ---------------------------------------------------------------------------
final themeModeProvider = StateProvider<ThemeMode>((ref) {
  final settingsBox = Hive.box(StorageKeys.settingsBox);
  final savedTheme =
      settingsBox.get(StorageKeys.themeMode, defaultValue: 'system');
  switch (savedTheme) {
    case 'light':
      return ThemeMode.light;
    case 'dark':
      return ThemeMode.dark;
    default:
      return ThemeMode.system;
  }
});

// ---------------------------------------------------------------------------
// Unit System (kg/lbs, cm/inches)
// ---------------------------------------------------------------------------
enum UnitSystem { metric, imperial }

final unitSystemProvider = StateProvider<UnitSystem>((ref) {
  final settingsBox = Hive.box(StorageKeys.settingsBox);
  final saved = settingsBox.get(StorageKeys.unitSystem, defaultValue: 'metric');
  return saved == 'imperial' ? UnitSystem.imperial : UnitSystem.metric;
});

/// Internal "Test Mode" toggle. When ON, time-based gates (e.g. starting a
/// session before its scheduled time) are bypassed for QA. Persisted to Hive
/// so it survives restarts; should be OFF in production.
final testModeProvider = StateProvider<bool>((ref) {
  final settingsBox = Hive.box(StorageKeys.settingsBox);
  return settingsBox.get(StorageKeys.testMode, defaultValue: false) as bool;
});

extension on UserModel {
  /// The scalar `gymId`, falling back to the first membership when blank.
  String? get loginGymId {
    final scalar = gymId;
    if (scalar != null && scalar.isNotEmpty) return scalar;
    final memberships = gymMemberships;
    if (memberships == null || memberships.isEmpty) return scalar;
    return memberships.first.gymId;
  }
}
