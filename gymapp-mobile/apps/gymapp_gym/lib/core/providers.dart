import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import 'permissions.dart';

// ---------------------------------------------------------------------------
// Secure Storage
// ---------------------------------------------------------------------------
final secureStorageProvider = Provider<SecureStorageService>((ref) {
  return SecureStorageService();
});

// ---------------------------------------------------------------------------
// Connectivity Service
// ---------------------------------------------------------------------------
final connectivityServiceProvider = Provider<ConnectivityService>((ref) {
  final service = ConnectivityService();
  service.initialize();
  ref.onDispose(service.dispose);
  return service;
});

// ---------------------------------------------------------------------------
// Offline Queue Service
// ---------------------------------------------------------------------------
final offlineQueueServiceProvider = Provider<OfflineQueueService>((ref) {
  final service = OfflineQueueService();
  service.initialize();
  return service;
});

// ---------------------------------------------------------------------------
// Session-invalidated pulse — incremented by ApiClient when refresh-token
// fails so the AuthNotifier can clear state and route the user to /login.
// ---------------------------------------------------------------------------
final sessionInvalidatedProvider = StateProvider<int>((ref) => 0);

// ---------------------------------------------------------------------------
// API Client
// ---------------------------------------------------------------------------
final apiClientProvider = Provider<ApiClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return ApiClient(
    baseUrl: ApiBaseUrl.resolve(),
    storage: storage,
    onAuthInvalidated: () async {
      // Bumping this StateProvider re-triggers AuthNotifier.build (which
      // watches it), and build() flips the state to unauthenticated when
      // there is no token. Router redirect picks that up and navigates to
      // /login automatically.
      final current = ref.read(sessionInvalidatedProvider);
      ref.read(sessionInvalidatedProvider.notifier).state = current + 1;
    },
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
  final String? staffRole;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.user,
    this.currentGymId,
    this.staffRole,
  });

  AuthState copyWith({
    AuthStatus? status,
    UserModel? user,
    String? currentGymId,
    String? staffRole,
  }) {
    return AuthState(
      status: status ?? this.status,
      user: user ?? this.user,
      currentGymId: currentGymId ?? this.currentGymId,
      staffRole: staffRole ?? this.staffRole,
    );
  }

  /// Check if user has permission for a given action.
  bool hasPermission(StaffPermission permission) {
    if (staffRole == null) return false;
    return StaffPermissions.hasPermission(staffRole!, permission);
  }
}

class AuthNotifier extends AsyncNotifier<AuthState> {
  @override
  Future<AuthState> build() async {
    // Re-run when ApiClient signals refresh-token failure so we clear state
    // and the router redirects to /login.
    ref.watch(sessionInvalidatedProvider);
    final storage = ref.watch(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token == null) {
      return const AuthState(status: AuthStatus.unauthenticated);
    }

    final userBox = Hive.box(StorageKeys.userBox);
    final cachedUserJson = userBox.get(StorageKeys.cachedUser);
    final gymId = await storage.getCurrentGymId();
    final role = await storage.getUserRole();

    // Always validate the token with the backend on startup. The AuthInterceptor
    // will transparently refresh on 401 — if refresh also fails, this throws
    // UnauthorizedException and we sign the user out.
    try {
      final apiClient = ref.read(apiClientProvider);
      final user = await apiClient.get<UserModel>(
        ApiEndpoints.profile,
        parser: (data) => UserModel.fromJson(data as Map<String, dynamic>),
      );
      await userBox.put(StorageKeys.cachedUser, user.toJson());

      // If secure storage doesn't yet hold the active gym (first launch after
      // a token-only restore, or storage was cleared), pick the first active
      // membership from the profile so the rest of the app has a gym context.
      var resolvedGymId = gymId;
      if (resolvedGymId == null || resolvedGymId.isEmpty) {
        final memberships = user.gymMemberships ?? const [];
        final active = memberships.firstWhere(
          (m) => m.isActive ?? true,
          orElse: () => memberships.isNotEmpty
              ? memberships.first
              : const GymMembershipModel(gymId: ''),
        );
        if (active.gymId.isNotEmpty) {
          resolvedGymId = active.gymId;
          await storage.saveCurrentGymId(GymId(resolvedGymId));
        }
      }

      _initializeFcm(resolvedGymId);
      return AuthState(
        status: AuthStatus.authenticated,
        user: user,
        currentGymId: resolvedGymId,
        staffRole: role,
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
            staffRole: role,
          );
        } catch (_) {
          // Corrupt cache — drop through to unauthenticated.
        }
      }
      return const AuthState(status: AuthStatus.unauthenticated);
    }
  }

  /// BE role claims admitted by the staff app (GymStaff, GymOwner).
  static const _allowedRoles = {'staff', 'owner'};

  Future<void> login(AuthResponse response, {String? staffRole}) async {
    // App-gate: this is the gym staff app — only GymStaff (Receptionist /
    // Manager) and GymOwner principals are allowed. Reject trainees, trainers,
    // and platform admins at the door so they fail fast with a clear message
    // instead of cascading 403s when staff-only endpoints reject their JWT.
    // BE serializes roles via UserRoleExtensions.ToRoleClaim:
    // GymStaff → "staff", GymOwner → "owner".
    if (!_allowedRoles.contains(response.user.role.toLowerCase())) {
      throw const ApiException(
        message:
            'This app is for gym staff and owners only. Use the role-specific app for your account.',
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

    final userBox = Hive.box(StorageKeys.userBox);
    await userBox.put(StorageKeys.cachedUser, response.user.toJson());

    // Backend login returns a scalar user.gymId for the active membership.
    // Fall back to the gymMemberships list when it's embedded in the response.
    final gymId = response.user.loginGymId;
    if (gymId != null && gymId.isNotEmpty) {
      await storage.saveCurrentGymId(GymId(gymId));
    }

    // The per-gym tier (owner / manager / receptionist) drives every
    // permission check in the app; read it from the token the BE just issued.
    final savedRole = staffRole ??
        StaffPermissions.roleFromAccessToken(
          response.accessToken,
          response.user.role,
        );
    await storage.saveUserRole(savedRole);

    state = AsyncData(AuthState(
      status: AuthStatus.authenticated,
      user: response.user,
      currentGymId: gymId,
      staffRole: savedRole,
    ));

    // Initialize FCM
    _initializeFcm(gymId);
  }

  Future<void> updateUser(UserModel user) async {
    final currentState = state.valueOrNull;
    if (currentState == null) return;

    final userBox = Hive.box(StorageKeys.userBox);
    await userBox.put(StorageKeys.cachedUser, user.toJson());

    state = AsyncData(currentState.copyWith(user: user));
  }

  Future<void> switchGym(String gymId) async {
    final storage = ref.read(secureStorageProvider);
    await storage.saveCurrentGymId(GymId(gymId));

    final currentState = state.valueOrNull;
    if (currentState != null) {
      state = AsyncData(currentState.copyWith(currentGymId: gymId));
    }

    // Re-fetch plan features for new gym
    ref.invalidate(gymPlanFeaturesProvider);
    ref.invalidate(activeGymProvider);
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
      ref.invalidate(gymPlanFeaturesProvider);
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
// Active Gym
// ---------------------------------------------------------------------------
/// BE GymModel response nests contact/address fields the Flutter GymModel
/// expects flat. Normalize so the existing fromJson can consume it.
Map<String, dynamic> _normalizeGymJson(Map<String, dynamic> json) {
  final normalized = Map<String, dynamic>.from(json);
  final contact = json['contact'];
  if (contact is Map<String, dynamic>) {
    normalized['phone'] ??= contact['phone'];
    normalized['email'] ??= contact['email'];
  }
  final address = json['address'];
  if (address is Map<String, dynamic>) {
    final addr = Map<String, dynamic>.from(address);
    addr['governorate'] ??= addr['state'];
    addr['postalCode'] ??= addr['zipCode'];
    normalized['address'] = addr;
  }
  return normalized;
}

final activeGymProvider = FutureProvider<GymModel?>((ref) async {
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return null;

  // Try cache first
  final gymBox = Hive.box(StorageKeys.gymBox);
  final cachedJson = gymBox.get(StorageKeys.cachedGym);
  if (cachedJson != null) {
    try {
      return GymModel.fromJson(
        Map<String, dynamic>.from(cachedJson as Map),
      );
    } catch (_) {
      // Fall through
    }
  }

  try {
    final apiClient = ref.read(apiClientProvider);
    final gym = await apiClient.get<GymModel>(
      ApiEndpoints.gymById(GymId(gymId)),
      parser: (data) =>
          GymModel.fromJson(_normalizeGymJson(data as Map<String, dynamic>)),
    );
    // Cache write must not nullify the live response if Hive trips over a
    // nested non-primitive (freezed toJson does not always recurse).
    try {
      await gymBox.put(StorageKeys.cachedGym, gym.toJson());
    } catch (e) {
      debugPrint('[activeGym] cache write failed: $e');
    }
    return gym;
  } catch (e, st) {
    debugPrint('[activeGym] fetch failed: $e\n$st');
    return null;
  }
});

// ---------------------------------------------------------------------------
// Plan Features -- overrides the core placeholder
// ---------------------------------------------------------------------------
final gymPlanFeaturesProvider = FutureProvider<PlanFeaturesModel>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final authState = ref.watch(authProvider);

  if (authState.valueOrNull?.status != AuthStatus.authenticated) {
    throw Exception('Not authenticated');
  }

  // Network first: an admin Deploy (FCM config_refresh) invalidates this
  // provider and must see the new plan, not the copy cached at login. The
  // cache only serves offline launches.
  final planBox = Hive.box(StorageKeys.planFeaturesBox);
  try {
    final features = await apiClient.get<PlanFeaturesModel>(
      ApiEndpoints.planFeatures,
      parser: (data) =>
          PlanFeaturesModel.fromJson(data as Map<String, dynamic>),
    );
    try {
      // toJson() leaves the nested feature items as objects, which Hive
      // cannot store; round-trip through JSON to get plain maps.
      await planBox.put(
        StorageKeys.cachedPlanFeatures,
        jsonDecode(jsonEncode(features.toJson())),
      );
    } catch (e) {
      debugPrint('[planFeatures] cache write failed: $e');
    }
    return features;
  } catch (_) {
    final cachedJson = planBox.get(StorageKeys.cachedPlanFeatures);
    if (cachedJson != null) {
      return PlanFeaturesModel.fromJson(
        jsonDecode(jsonEncode(cachedJson)) as Map<String, dynamic>,
      );
    }
    rethrow;
  }
});

// ---------------------------------------------------------------------------
// Gym Suspended State
// ---------------------------------------------------------------------------
final gymSuspendedProvider = StateProvider<bool>((ref) => false);

// ---------------------------------------------------------------------------
// Gym Branding
// ---------------------------------------------------------------------------
final gymBrandingProvider = FutureProvider<GymBrandingModel?>((ref) async {
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return null;

  final gymBox = Hive.box(StorageKeys.gymBox);
  final cachedJson = gymBox.get(StorageKeys.gymBranding);
  if (cachedJson != null) {
    try {
      return GymBrandingModel.fromJson(
        Map<String, dynamic>.from(cachedJson as Map),
      );
    } catch (_) {
      // Fall through
    }
  }

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
    return null;
  }
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
// Current Gym Id
// ---------------------------------------------------------------------------
/// Resolves the active gym id with a sensible fallback chain so screens never
/// fire requests against /gyms//... when secure storage hasn't been hydrated.
/// Order: AuthState.currentGymId → UserModel.gymId → first active membership
/// from /identity/profile.
final currentGymIdProvider = Provider<String>((ref) {
  final auth = ref.watch(authProvider).valueOrNull;
  if (auth == null) return '';
  final fromState = auth.currentGymId;
  if (fromState != null && fromState.isNotEmpty) return fromState;
  final user = auth.user;
  if (user == null) return '';
  if (user.gymId != null && user.gymId!.isNotEmpty) return user.gymId!;
  final memberships = user.gymMemberships ?? const [];
  if (memberships.isEmpty) return '';
  final active = memberships.firstWhere(
    (m) => m.isActive ?? true,
    orElse: () => memberships.first,
  );
  return active.gymId;
});

// ---------------------------------------------------------------------------
// Staff Role convenience provider
// ---------------------------------------------------------------------------
final staffRoleProvider = Provider<String?>((ref) {
  final authState = ref.watch(authProvider);
  return authState.valueOrNull?.staffRole;
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
