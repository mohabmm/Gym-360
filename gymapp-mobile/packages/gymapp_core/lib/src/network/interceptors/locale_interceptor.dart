import 'package:dio/dio.dart';
import 'package:hive/hive.dart';

import '../../constants/app_constants.dart';
import '../../constants/storage_keys.dart';

/// Sets `Accept-Language` on every outgoing request to match the app's
/// currently-selected locale. Reads from Hive on each request rather than
/// holding a locale reference so a locale switch propagates immediately
/// without needing to rebuild the ApiClient.
///
/// Falls back to [AppConstants.defaultLocale] when Hive isn't available
/// (pre-init, tests) or the setting hasn't been stored yet.
class LocaleInterceptor extends Interceptor {
  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) {
    // Per-request override wins — leave it alone.
    if (options.headers.containsKey('Accept-Language')) {
      return handler.next(options);
    }

    options.headers['Accept-Language'] = _currentLocale();
    return handler.next(options);
  }

  String _currentLocale() {
    try {
      if (Hive.isBoxOpen(StorageKeys.settingsBox)) {
        final box = Hive.box(StorageKeys.settingsBox);
        final saved = box.get(StorageKeys.locale);
        if (saved is String && saved.isNotEmpty) return saved;
      }
    } catch (_) {
      // Fall through to default.
    }
    return AppConstants.defaultLocale;
  }
}
