import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'core/providers.dart';
import 'core/router.dart';

Future<void> main() async {
  final telemetry = TelemetryService(
    appName: 'gymapp-trainee',
    baseUrl: ApiBaseUrl.resolve(),
    storage: SecureStorageService(),
  );

  await runWithTelemetry('gymapp-trainee', telemetry, () async {
    WidgetsFlutterBinding.ensureInitialized();

    // Load locale data for both UI languages so DateFormat('…', 'en') renders
  // Latin digits and English month names instead of falling back to the
  // device locale — fixes BUG-15 where the Next Session card showed Arabic
  // numerals / months while the rest of the UI was English.
  await Future.wait([
    initializeDateFormatting('ar'),
    initializeDateFormatting('en'),
  ]);

  // Initialize Firebase (gracefully skip if not configured in dev).
  // Without google-services.json this throws; FCMService treats that as "FCM off".
  try {
    await Firebase.initializeApp();
  } catch (e) {
    debugPrint('[main] Firebase.initializeApp failed: $e — running without FCM.');
  }

  // Initialize Hive for local cache
  await Hive.initFlutter();
  await Future.wait([
    Hive.openBox(StorageKeys.userBox),
    Hive.openBox(StorageKeys.gymBox),
    Hive.openBox(StorageKeys.planFeaturesBox),
    Hive.openBox(StorageKeys.settingsBox),
    Hive.openBox(StorageKeys.exerciseCacheBox),
    Hive.openBox(StorageKeys.workoutCacheBox),
    Hive.openBox(StorageKeys.offlineQueueBox),
    Hive.openBox(StorageKeys.consentBox),
  ]);

  // Force portrait mode
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);

  await telemetry.init();

  runApp(
    ProviderScope(
      overrides: [
        telemetryServiceProvider.overrideWithValue(telemetry),
        // Override the core placeholder with the actual billing API implementation
        planFeaturesProvider.overrideWith(
          (ref) async {
            final apiClient = ref.watch(apiClientProvider);
            final authState = ref.watch(authProvider);
            if (authState.valueOrNull?.status != AuthStatus.authenticated) {
              throw Exception('Not authenticated');
            }
            // No active gym (newly registered user not yet linked to a gym):
            // return an empty plan so FeatureGate renders fallbacks instead of
            // firing GYM_CONTEXT_REQUIRED 400s against the API.
            final gymId = authState.valueOrNull?.currentGymId;
            if (gymId == null || gymId.isEmpty) {
              return const PlanFeaturesModel(
                gymId: '',
                planCode: '',
                planName: '',
                memberLimit: 0,
                trainerLimit: 0,
                staffLimit: 0,
                features: <PlanFeatureItemModel>[],
              );
            }
            // Try cache first
            final planBox = Hive.box(StorageKeys.planFeaturesBox);
            final cachedJson = planBox.get(StorageKeys.cachedPlanFeatures);
            if (cachedJson != null) {
              try {
                return PlanFeaturesModel.fromJson(
                    Map<String, dynamic>.from(cachedJson as Map));
              } catch (_) {}
            }
            // Pass gymId explicitly as a query param. The GymIdInterceptor
            // also sets the X-Gym-Id header, but sending both makes this
            // resilient to header drops on proxies / stale BE builds that
            // still require the query param.
            final features = await apiClient.get<PlanFeaturesModel>(
              ApiEndpoints.planFeatures,
              queryParameters: {'gymId': gymId},
              parser: (data) =>
                  PlanFeaturesModel.fromJson(data as Map<String, dynamic>),
            );
            await planBox.put(StorageKeys.cachedPlanFeatures, features.toJson());
            return features;
          },
        ),
      ],
      child: const GymAppTrainee(),
    ),
  );
  });
}

class GymAppTrainee extends ConsumerWidget {
  const GymAppTrainee({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final locale = ref.watch(localeProvider);
    final themeMode = ref.watch(themeModeProvider);
    final branding = ref.watch(gymBrandingProvider);
    // Keep the SignalR branding hub alive for the lifetime of the app shell;
    // it auto-connects after login, re-subscribes on reconnect, and invalidates
    // gymBrandingProvider on every server-pushed branding_updated event.
    ref.watch(brandingHubServiceProvider);

    final isArabic = locale.languageCode == 'ar';
    final brandingTheme = branding.when(
      data: (b) => GymBrandingTheme(branding: b),
      loading: () => GymBrandingTheme.defaultBranding,
      error: (_, __) => GymBrandingTheme.defaultBranding,
    );

    return MaterialApp.router(
      title: isArabic ? AppConstants.appNameAr : AppConstants.appName,
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      theme: AppTheme.light(isArabic: isArabic, branding: brandingTheme),
      darkTheme: AppTheme.dark(isArabic: isArabic, branding: brandingTheme),
      themeMode: themeMode,
      locale: locale,
      supportedLocales: const [
        Locale('ar'),
        Locale('en'),
      ],
      localizationsDelegates: const [
        // Overrides Flutter's default English `modalBarrierDismissLabel`
        // ("Scrim"), which otherwise leaks into screen readers and
        // accessibility trees as the literal word "Scrim" over every modal.
        _AppMaterialLocalizationsDelegate(),
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
    );
  }
}

/// Localizations delegate that only overrides the modal-barrier dismiss label
/// so the accessibility tree shows a meaningful word instead of Flutter's
/// default English "Scrim" (see MaterialLocalizationEn.modalBarrierDismissLabel).
class _AppMaterialLocalizationsDelegate
    extends LocalizationsDelegate<MaterialLocalizations> {
  const _AppMaterialLocalizationsDelegate();

  @override
  bool isSupported(Locale locale) =>
      locale.languageCode == 'en' || locale.languageCode == 'ar';

  @override
  Future<MaterialLocalizations> load(Locale locale) async {
    final base = await GlobalMaterialLocalizations.delegate.load(locale);
    return _AppMaterialLocalizations(base);
  }

  @override
  bool shouldReload(_AppMaterialLocalizationsDelegate old) => false;
}

class _AppMaterialLocalizations extends DefaultMaterialLocalizations {
  _AppMaterialLocalizations(this._base);

  final MaterialLocalizations _base;

  @override
  String get modalBarrierDismissLabel {
    final localeName =
        _base.runtimeType.toString().contains('Ar') ? 'ar' : 'en';
    return localeName == 'ar' ? 'إغلاق' : 'Dismiss';
  }
}
