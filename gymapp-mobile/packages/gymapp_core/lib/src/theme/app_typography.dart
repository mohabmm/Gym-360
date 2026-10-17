import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'app_colors.dart';

/// Typography definitions supporting Arabic (primary) and English.
/// Arabic: Cairo (body) / Tajawal (display weight).
/// English: Plus Jakarta Sans (headlines — matches web) + Inter (body).
class AppTypography {
  AppTypography._();

  /// Arabic body font family.
  static String get arabicFontFamily => GoogleFonts.cairo().fontFamily!;

  /// Arabic display/headline font family (heavier weight).
  static String get arabicDisplayFamily => GoogleFonts.tajawal().fontFamily!;

  /// English body font family.
  static String get englishFontFamily => GoogleFonts.inter().fontFamily!;

  /// English display/headline font — matches gymapp-web.
  static String get englishDisplayFamily =>
      GoogleFonts.plusJakartaSans().fontFamily!;

  static String _body(bool isArabic) =>
      isArabic ? arabicFontFamily : englishFontFamily;

  static String _display(bool isArabic) =>
      isArabic ? arabicDisplayFamily : englishDisplayFamily;

  /// Build the TextTheme for a given locale + brightness.
  static TextTheme textTheme({
    required bool isArabic,
    required bool isDark,
  }) {
    final textColor =
        isDark ? AppColors.textPrimaryDark : AppColors.textPrimaryLight;
    final secondaryColor =
        isDark ? AppColors.textSecondaryDark : AppColors.textSecondaryLight;
    final display = _Face(_display(isArabic), textColor);
    final body = _Face(_body(isArabic), textColor);
    final bodySecondary = _Face(_body(isArabic), secondaryColor);

    return TextTheme(
      displayLarge: display.style(34, FontWeight.w800, 1.15, letterSpacing: -0.5),
      displayMedium: display.style(28, FontWeight.w800, 1.2, letterSpacing: -0.4),
      displaySmall: display.style(24, FontWeight.w700, 1.25, letterSpacing: -0.3),
      headlineLarge: display.style(22, FontWeight.w700, 1.3, letterSpacing: -0.2),
      headlineMedium: display.style(20, FontWeight.w700, 1.3),
      headlineSmall: display.style(18, FontWeight.w600, 1.4),
      titleLarge: body.style(16, FontWeight.w600, 1.4),
      titleMedium: body.style(14, FontWeight.w600, 1.4),
      titleSmall: body.style(12, FontWeight.w600, 1.4),
      bodyLarge: body.style(16, FontWeight.w400, 1.5),
      bodyMedium: body.style(14, FontWeight.w400, 1.5),
      bodySmall: bodySecondary.style(12, FontWeight.w400, 1.5),
      labelLarge: body.style(14, FontWeight.w600, 1.4, letterSpacing: 0.1),
      labelMedium:
          bodySecondary.style(12, FontWeight.w600, 1.4, letterSpacing: 0.2),
      labelSmall:
          bodySecondary.style(11, FontWeight.w600, 1.4, letterSpacing: 0.6),
    );
  }
}

/// A font family + colour pair from which the individual text styles derive.
class _Face {
  const _Face(this.fontFamily, this.color);

  final String fontFamily;
  final Color color;

  TextStyle style(
    double fontSize,
    FontWeight fontWeight,
    double height, {
    double? letterSpacing,
  }) {
    return TextStyle(
      fontFamily: fontFamily,
      fontSize: fontSize,
      fontWeight: fontWeight,
      color: color,
      height: height,
      letterSpacing: letterSpacing,
    );
  }
}
