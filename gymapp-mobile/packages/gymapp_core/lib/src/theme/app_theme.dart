import 'package:flutter/material.dart';
import '../constants/app_constants.dart';
import 'app_colors.dart';
import 'app_typography.dart';
import 'gym_branding.dart';

/// Application theme builder supporting:
/// - Arabic-first RTL layout
/// - Light and dark modes
/// - Dynamic gym branding colors
class AppTheme {
  AppTheme._();

  /// Build the light theme.
  static ThemeData light({
    bool isArabic = true,
    GymBrandingTheme branding = GymBrandingTheme.defaultBranding,
  }) {
    final colorScheme =
        branding.colorScheme(brightness: Brightness.light);
    final textTheme = AppTypography.textTheme(
      isArabic: isArabic,
      isDark: false,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.backgroundLight,
      cardColor: AppColors.cardLight,
      dividerColor: AppColors.dividerLight,
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.surfaceLight,
        foregroundColor: AppColors.textPrimaryLight,
        elevation: 0,
        centerTitle: true,
        titleTextStyle: textTheme.titleLarge,
        surfaceTintColor: Colors.transparent,
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: AppColors.surfaceLight,
        selectedItemColor: branding.primaryColor,
        unselectedItemColor: AppColors.grey400,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
        selectedLabelStyle: textTheme.labelSmall?.copyWith(
          fontWeight: FontWeight.w600,
        ),
        unselectedLabelStyle: textTheme.labelSmall,
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: AppColors.surfaceLight,
        indicatorColor: branding.primaryColor.withOpacity(0.1),
        surfaceTintColor: Colors.transparent,
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return textTheme.labelSmall?.copyWith(
              color: branding.primaryColor,
              fontWeight: FontWeight.w600,
            );
          }
          return textTheme.labelSmall?.copyWith(
            color: AppColors.grey500,
          );
        }),
      ),
      cardTheme: CardTheme(
        color: AppColors.cardLight,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
          side: const BorderSide(color: AppColors.dividerLight, width: 0.6),
        ),
        shadowColor: const Color(0x140F172A),
        surfaceTintColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: branding.primaryColor,
          foregroundColor: AppColors.white,
          minimumSize: const Size(double.infinity, 52),
          elevation: 0,
          shadowColor: branding.primaryColor.withOpacity(0.25),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          ),
          textStyle: textTheme.labelLarge?.copyWith(
            fontWeight: FontWeight.w700,
            letterSpacing: 0.2,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: branding.primaryColor,
          minimumSize: const Size(double.infinity, 52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          ),
          side: BorderSide(color: branding.primaryColor, width: 1.4),
          textStyle: textTheme.labelLarge?.copyWith(
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: branding.primaryColor,
          textStyle: textTheme.labelLarge?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.grey50,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 16,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.grey200, width: 1),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.grey200, width: 1),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: BorderSide(color: branding.primaryColor, width: 1.8),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.error, width: 1.4),
        ),
        labelStyle: textTheme.bodyMedium?.copyWith(
          color: AppColors.grey500,
        ),
        hintStyle: textTheme.bodyMedium?.copyWith(
          color: AppColors.grey400,
        ),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.grey100,
        selectedColor: branding.primaryColor.withOpacity(0.12),
        side: BorderSide.none,
        labelStyle: textTheme.labelMedium!,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(999),
        ),
      ),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        backgroundColor: branding.primaryColor,
        foregroundColor: AppColors.white,
        shape: const CircleBorder(),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusSmall),
        ),
      ),
      dialogTheme: DialogTheme(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
        ),
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        showDragHandle: true,
        backgroundColor: AppColors.surfaceLight,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(28),
          ),
        ),
      ),
      tabBarTheme: TabBarTheme(
        indicatorColor: branding.primaryColor,
        indicatorSize: TabBarIndicatorSize.label,
        labelColor: branding.primaryColor,
        unselectedLabelColor: AppColors.grey500,
        labelStyle: textTheme.labelLarge,
        unselectedLabelStyle: textTheme.labelMedium,
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.dividerLight,
        thickness: 0.6,
        space: 1,
      ),
      switchTheme: SwitchThemeData(
        thumbColor: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return branding.primaryColor;
          }
          return AppColors.grey400;
        }),
        trackColor: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return branding.primaryColor.withOpacity(0.3);
          }
          return AppColors.grey200;
        }),
      ),
    );
  }

  /// Build the dark theme.
  static ThemeData dark({
    bool isArabic = true,
    GymBrandingTheme branding = GymBrandingTheme.defaultBranding,
  }) {
    final colorScheme =
        branding.colorScheme(brightness: Brightness.dark);
    final textTheme = AppTypography.textTheme(
      isArabic: isArabic,
      isDark: true,
    );

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.backgroundDark,
      cardColor: AppColors.cardDark,
      dividerColor: AppColors.dividerDark,
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.surfaceDark,
        foregroundColor: AppColors.textPrimaryDark,
        elevation: 0,
        centerTitle: true,
        titleTextStyle: textTheme.titleLarge,
        surfaceTintColor: Colors.transparent,
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: AppColors.surfaceDark,
        selectedItemColor: branding.primaryColor,
        unselectedItemColor: AppColors.grey500,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
      cardTheme: CardTheme(
        color: AppColors.cardDark,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
          side: const BorderSide(color: AppColors.dividerDark, width: 0.6),
        ),
        surfaceTintColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: branding.primaryColor,
          foregroundColor: AppColors.white,
          minimumSize: const Size(double.infinity, 52),
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          ),
          textStyle: textTheme.labelLarge?.copyWith(
            fontWeight: FontWeight.w700,
            letterSpacing: 0.2,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.grey800,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.grey700),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.grey700),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: BorderSide(color: branding.primaryColor, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadius),
          borderSide: const BorderSide(color: AppColors.error),
        ),
        labelStyle: textTheme.bodyMedium?.copyWith(
          color: AppColors.grey400,
        ),
        hintStyle: textTheme.bodyMedium?.copyWith(
          color: AppColors.grey500,
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusSmall),
        ),
      ),
      dialogTheme: DialogTheme(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
        ),
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        showDragHandle: true,
        backgroundColor: AppColors.surfaceDark,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(28),
          ),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.dividerDark,
        thickness: 0.6,
        space: 1,
      ),
    );
  }
}
