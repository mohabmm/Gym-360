import 'package:gymapp_core/gymapp_core.dart';
import 'package:hive_flutter/hive_flutter.dart';

/// Turns anything thrown by a staff-app action into text that is safe to show
/// in a snackbar: no `Exception:` / `ApiException(403):` prefixes, and Arabic
/// for the backend messages staff hit day to day (the API answers in English
/// regardless of `Accept-Language`).
///
/// [isArabic] defaults to the app's saved locale, for call sites that have no
/// BuildContext left after an await.
String staffErrorText(Object error, {bool? isArabic}) {
  isArabic ??= _savedLocaleIsArabic();
  if (error is ApiException) {
    final message = error.message.trim();
    if (!isArabic) {
      return message.isEmpty ? 'Something went wrong.' : message;
    }
    final translated = _arabicByMessage[message];
    if (translated != null) return translated;
    if (message.isEmpty || message == 'Something went wrong.') {
      return _arabicByStatus[error.statusCode] ?? 'حدث خطأ. يرجى المحاولة مرة اخرى.';
    }
    return message;
  }
  final text = error.toString();
  const prefix = 'Exception: ';
  return text.startsWith(prefix) ? text.substring(prefix.length) : text;
}

bool _savedLocaleIsArabic() {
  try {
    if (Hive.isBoxOpen(StorageKeys.settingsBox)) {
      final saved = Hive.box(StorageKeys.settingsBox)
          .get(StorageKeys.locale, defaultValue: AppConstants.localeAr);
      return saved == AppConstants.localeAr;
    }
  } catch (_) {
    // Fall through to the Arabic-first default.
  }
  return true;
}

const Map<int, String> _arabicByStatus = {
  401: 'انتهت الجلسة. يرجى تسجيل الدخول مرة اخرى.',
  403: 'ليس لديك صلاحية لتنفيذ هذا الاجراء.',
  404: 'العنصر المطلوب غير موجود.',
  408: 'الخادم لا يستجيب. يرجى المحاولة مرة اخرى.',
};

const Map<String, String> _arabicByMessage = {
  'The provided credentials are invalid.':
      'البريد الالكتروني او كلمة المرور غير صحيحة.',
  'The member is already added to this gym.': 'العضو مضاف بالفعل الى هذه الصالة.',
  'The check-in is invalid.':
      'العضو غير موجود. تاكد من الرقم او اسأل الموظف.',
  'A check-in already exists for this member today.':
      'تم تسجيل حضور هذا العضو اليوم بالفعل.',
  "The member's membership is frozen and cannot check in.":
      'عضوية هذا العضو مجمدة ولا يمكن تسجيل حضوره.',
  'Feature requires Business plan': 'هذه الميزة تتطلب باقة Business.',
};
