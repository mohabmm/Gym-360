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
  // Build telemetry first so the global error handlers wired by
  // runWithTelemetry can capture failures from the rest of startup too.
  final telemetry = TelemetryService(
    appName: 'gymapp-trainer',
    baseUrl: ApiBaseUrl.resolve(),
    storage: SecureStorageService(),
  );

  await runWithTelemetry('gymapp-trainer', telemetry, () async {
    WidgetsFlutterBinding.ensureInitialized();

    // Load Arabic + English date/time symbols so DateFormat and table_calendar
    // can render weekday/month names in the app locale instead of English.
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');

  // Firebase initialization (gracefully skip if not configured in dev).
  // Without google-services.json this throws; FCMService treats that as "FCM off".
  try {
    await Firebase.initializeApp();
  } catch (e) {
    debugPrint('[main] Firebase.initializeApp failed: $e — running without FCM.');
  }

  // Hive initialization for local caching
  await Hive.initFlutter();
  await Hive.openBox(StorageKeys.userBox);
  await Hive.openBox(StorageKeys.gymBox);
  await Hive.openBox(StorageKeys.planFeaturesBox);
  await Hive.openBox(StorageKeys.offlineQueueBox);
  await Hive.openBox(StorageKeys.settingsBox);
  await Hive.openBox(StorageKeys.workoutCacheBox);

    // Force portrait orientation
    await SystemChrome.setPreferredOrientations([
      DeviceOrientation.portraitUp,
      DeviceOrientation.portraitDown,
    ]);

    // Gather device/app context + start the flush timer + reflush any queued
    // records from a previous session. Never throws.
    await telemetry.init();

    runApp(
      ProviderScope(
        overrides: [
          telemetryServiceProvider.overrideWithValue(telemetry),
        ],
        child: const GymAppTrainer(),
      ),
    );
  });
}

class GymAppTrainer extends ConsumerWidget {
  const GymAppTrainer({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    final locale = ref.watch(localeProvider);
    final themeMode = ref.watch(themeModeProvider);
    final branding = ref.watch(gymBrandingProvider);
    final isArabic = locale.languageCode == 'ar';

    return MaterialApp.router(
      title: isArabic ? AppConstants.appNameAr : AppConstants.appName,
      debugShowCheckedModeBanner: false,
      routerConfig: router,
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
      theme: AppTheme.light(
        isArabic: isArabic,
        branding: branding,
      ),
      darkTheme: AppTheme.dark(
        isArabic: isArabic,
        branding: branding,
      ),
      themeMode: themeMode,
      builder: (context, child) {
        // Wrap with gym suspension overlay
        final isSuspended = ref.watch(gymSuspensionProvider);
        if (isSuspended) {
          return const GymSuspensionOverlay();
        }
        return Directionality(
          textDirection: isArabic ? TextDirection.rtl : TextDirection.ltr,
          child: child ?? const SizedBox.shrink(),
        );
      },
    );
  }
}
