import 'package:flutter/widgets.dart';

/// The UI languages GymApp ships. Arabic is the primary language.
///
/// Replaces `bool isArabic` / `String locale` flags on formatting and label
/// helpers so callers cannot pass an unsupported locale string by accident.
enum AppLanguage {
  arabic('ar'),
  english('en');

  const AppLanguage(this.code);

  /// ISO 639-1 language code, as used by `intl` and [Locale].
  final String code;

  bool get isArabic => this == AppLanguage.arabic;

  /// Separator for joining a list of labels ("الصدر، الترايسبس").
  String get listSeparator => isArabic ? '، ' : ', ';

  /// Arabic for an `ar` locale, English for anything else.
  static AppLanguage fromLocale(Locale locale) =>
      locale.languageCode == AppLanguage.arabic.code
          ? AppLanguage.arabic
          : AppLanguage.english;

  /// The language of the nearest [Localizations] ancestor.
  static AppLanguage of(BuildContext context) =>
      fromLocale(Localizations.localeOf(context));
}
