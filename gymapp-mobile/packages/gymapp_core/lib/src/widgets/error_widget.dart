import 'package:flutter/material.dart';
import 'app_button.dart';

/// Error display widget with retry action.
class AppErrorWidget extends StatelessWidget {
  final String message;
  final String? messageAr;
  final VoidCallback? onRetry;
  final String? retryLabel;
  final IconData icon;
  final bool compact;

  const AppErrorWidget({
    super.key,
    required this.message,
    this.messageAr,
    this.onRetry,
    this.retryLabel,
    this.icon = Icons.error_outline,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final raw = isArabic && messageAr != null ? messageAr! : message;
    final displayMessage = _friendlyMessage(raw, isArabic: isArabic);
    final retryText = retryLabel ??
        (isArabic ? 'إعادة المحاولة' : 'Retry');

    if (compact) {
      return Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, color: Colors.red, size: 20),
          const SizedBox(width: 8),
          Flexible(
            child: Text(
              displayMessage,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Colors.red,
                  ),
            ),
          ),
          if (onRetry != null) ...[
            const SizedBox(width: 8),
            TextButton(
              onPressed: onRetry,
              child: Text(retryText),
            ),
          ],
        ],
      );
    }

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 64,
              color: Colors.red.withOpacity(0.5),
            ),
            const SizedBox(height: 16),
            Text(
              displayMessage,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            if (onRetry != null) ...[
              const SizedBox(height: 24),
              AppButton(
                text: retryText,
                onPressed: onRetry,
                width: 200,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Pattern emitted by `ApiException.toString()` — `ApiException(<code>): <msg>`.
final RegExp _apiExceptionPattern =
    RegExp(r'^\.?ApiException\((\d+|null)\):\s*(.*)$', dotAll: true);

/// A handful of generic backend messages that we never want to surface to
/// end users — they're noise that the BE returns when it has nothing better
/// to say. We replace them with a localised, status-aware fallback.
const Set<String> _genericServerMessages = {
  'Something went wrong.',
  'Something went wrong',
  'Request failed.',
  'Request failed',
};

/// Converts a raw error message (often `ApiException(...)`-formatted from
/// `e.toString()`) into a user-friendly localised string. Status-aware so a
/// 405 reads "This action isn't supported here…" instead of leaking the
/// Dart exception class name.
String _friendlyMessage(String raw, {required bool isArabic}) {
  final match = _apiExceptionPattern.firstMatch(raw.trim());
  if (match == null) return raw;

  final codeStr = match.group(1) ?? '';
  final inner = (match.group(2) ?? '').trim();
  final statusCode = int.tryParse(codeStr);
  final hasUsefulMessage =
      inner.isNotEmpty && !_genericServerMessages.contains(inner);

  if (hasUsefulMessage) return inner;
  return _statusFallback(statusCode, isArabic: isArabic);
}

String _statusFallback(int? statusCode, {required bool isArabic}) {
  switch (statusCode) {
    case 400:
      return isArabic ? 'طلب غير صالح.' : 'Invalid request.';
    case 401:
      return isArabic
          ? 'يرجى تسجيل الدخول مرة أخرى.'
          : 'Please sign in again.';
    case 403:
      return isArabic
          ? 'ليس لديك صلاحية لعرض هذا.'
          : "You don't have permission to view this.";
    case 404:
      return isArabic ? 'العنصر غير موجود.' : 'Not found.';
    case 405:
      return isArabic
          ? 'هذا الإجراء غير مدعوم. يرجى تحديث التطبيق.'
          : "This action isn't supported here. Please update the app.";
    case 408:
      return isArabic
          ? 'استغرق الخادم وقتًا طويلاً للرد.'
          : 'The server took too long to respond.';
    case 409:
      return isArabic
          ? 'يتعارض هذا مع بيانات موجودة.'
          : 'This conflicts with existing data.';
    case 500:
    case 502:
    case 503:
    case 504:
      return isArabic
          ? 'خطأ في الخادم. يرجى المحاولة بعد قليل.'
          : 'Server error. Please try again shortly.';
    default:
      return isArabic ? 'حدث خطأ ما.' : 'Something went wrong.';
  }
}

/// Network error widget specifically.
class NetworkErrorWidget extends StatelessWidget {
  final VoidCallback? onRetry;

  const NetworkErrorWidget({super.key, this.onRetry});

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    return AppErrorWidget(
      message: isArabic
          ? 'لا يوجد اتصال بالإنترنت'
          : 'No internet connection',
      icon: Icons.wifi_off,
      onRetry: onRetry,
    );
  }
}
