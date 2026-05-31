import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

import 'core/providers.dart';
import 'core/router.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

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
    Hive.openBox(StorageKeys.offlineQueueBox),
    Hive.openBox(StorageKeys.consentBox),
  ]);

  // Allow landscape for tablet kiosk mode, portrait for phone
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);

  runApp(
    ProviderScope(
      overrides: [
        // The shared FeatureGate widget watches the core `planFeaturesProvider`,
        // which is a placeholder that throws UnimplementedError unless overridden.
        // Without this the gym app's gated screens silently fail open. Delegate
        // to this app's existing plan-features source so both share one cache and
        // the FCM config_refresh invalidation (gymPlanFeaturesProvider) propagates.
        planFeaturesProvider.overrideWith(
          (ref) => ref.watch(gymPlanFeaturesProvider.future),
        ),
      ],
      child: const GymAppGymStaff(),
    ),
  );
}

class GymAppGymStaff extends ConsumerWidget {
  const GymAppGymStaff({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final locale = ref.watch(localeProvider);
    final themeMode = ref.watch(themeModeProvider);
    final branding = ref.watch(gymBrandingProvider);

    final isArabic = locale.languageCode == 'ar';
    final brandingTheme = branding.when(
      data: (b) => b != null
          ? GymBrandingTheme(branding: b)
          : GymBrandingTheme.defaultBranding,
      loading: () => GymBrandingTheme.defaultBranding,
      error: (_, __) => GymBrandingTheme.defaultBranding,
    );

    return MaterialApp.router(
      title: isArabic ? 'جيم 360 - الإدارة' : 'Gym 360 - Staff',
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
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
    );
  }
}
