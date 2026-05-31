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
    final body = _body(isArabic);
    final display = _display(isArabic);
    final textColor =
        isDark ? AppColors.textPrimaryDark : AppColors.textPrimaryLight;
    final secondaryColor =
        isDark ? AppColors.textSecondaryDark : AppColors.textSecondaryLight;

    return TextTheme(
      displayLarge: TextStyle(
        fontFamily: display,
        fontSize: 34,
        fontWeight: FontWeight.w800,
        color: textColor,
        height: 1.15,
        letterSpacing: -0.5,
      ),
      displayMedium: TextStyle(
        fontFamily: display,
        fontSize: 28,
        fontWeight: FontWeight.w800,
        color: textColor,
        height: 1.2,
        letterSpacing: -0.4,
      ),
      displaySmall: TextStyle(
        fontFamily: display,
        fontSize: 24,
        fontWeight: FontWeight.w700,
        color: textColor,
        height: 1.25,
        letterSpacing: -0.3,
      ),
      headlineLarge: TextStyle(
        fontFamily: display,
        fontSize: 22,
        fontWeight: FontWeight.w700,
        color: textColor,
        height: 1.3,
        letterSpacing: -0.2,
      ),
      headlineMedium: TextStyle(
        fontFamily: display,
        fontSize: 20,
        fontWeight: FontWeight.w700,
        color: textColor,
        height: 1.3,
      ),
      headlineSmall: TextStyle(
        fontFamily: display,
        fontSize: 18,
        fontWeight: FontWeight.w600,
        color: textColor,
        height: 1.4,
      ),
      titleLarge: TextStyle(
        fontFamily: body,
        fontSize: 16,
        fontWeight: FontWeight.w600,
        color: textColor,
        height: 1.4,
      ),
      titleMedium: TextStyle(
        fontFamily: body,
        fontSize: 14,
        fontWeight: FontWeight.w600,
        color: textColor,
        height: 1.4,
      ),
      titleSmall: TextStyle(
        fontFamily: body,
        fontSize: 12,
        fontWeight: FontWeight.w600,
        color: textColor,
        height: 1.4,
      ),
      bodyLarge: TextStyle(
        fontFamily: body,
        fontSize: 16,
        fontWeight: FontWeight.w400,
        color: textColor,
        height: 1.5,
      ),
      bodyMedium: TextStyle(
        fontFamily: body,
        fontSize: 14,
        fontWeight: FontWeight.w400,
        color: textColor,
        height: 1.5,
      ),
      bodySmall: TextStyle(
        fontFamily: body,
        fontSize: 12,
        fontWeight: FontWeight.w400,
        color: secondaryColor,
        height: 1.5,
      ),
      labelLarge: TextStyle(
        fontFamily: body,
        fontSize: 14,
        fontWeight: FontWeight.w600,
        color: textColor,
        height: 1.4,
        letterSpacing: 0.1,
      ),
      labelMedium: TextStyle(
        fontFamily: body,
        fontSize: 12,
        fontWeight: FontWeight.w600,
        color: secondaryColor,
        height: 1.4,
        letterSpacing: 0.2,
      ),
      labelSmall: TextStyle(
        fontFamily: body,
        fontSize: 11,
        fontWeight: FontWeight.w600,
        color: secondaryColor,
        height: 1.4,
        letterSpacing: 0.6,
      ),
    );
  }
}
