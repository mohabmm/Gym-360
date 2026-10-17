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
    final colorScheme = branding.colorScheme(brightness: Brightness.light);
    final textTheme = AppTypography.textTheme(
      isArabic: isArabic,
      isDark: false,
    );
    final primary = branding.primaryColor;

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.backgroundLight,
      cardColor: AppColors.cardLight,
      dividerColor: AppColors.dividerLight,
      appBarTheme: _appBarTheme(
        textTheme,
        background: AppColors.surfaceLight,
        foreground: AppColors.textPrimaryLight,
      ),
      bottomNavigationBarTheme:
          _lightBottomNavigationBarTheme(textTheme, primary),
      navigationBarTheme: _lightNavigationBarTheme(textTheme, primary),
      cardTheme: CardThemeData(
        color: AppColors.cardLight,
        elevation: 0,
        shape: _cardShape(AppColors.dividerLight),
        shadowColor: const Color(0x140F172A),
        surfaceTintColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: _elevatedButtonTheme(
        textTheme,
        primary,
        shadowColor: primary.withOpacity(0.25),
      ),
      outlinedButtonTheme: _lightOutlinedButtonTheme(textTheme, primary),
      textButtonTheme: _lightTextButtonTheme(textTheme, primary),
      inputDecorationTheme: _lightInputDecorationTheme(textTheme, primary),
      chipTheme: _lightChipTheme(textTheme, primary),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        backgroundColor: primary,
        foregroundColor: AppColors.white,
        shape: const CircleBorder(),
      ),
      snackBarTheme: _snackBarTheme,
      dialogTheme: _dialogTheme,
      bottomSheetTheme: _bottomSheetTheme(AppColors.surfaceLight),
      tabBarTheme: _lightTabBarTheme(textTheme, primary),
      dividerTheme: _dividerTheme(AppColors.dividerLight),
      switchTheme: _lightSwitchTheme(primary),
    );
  }

  /// Build the dark theme.
  static ThemeData dark({
    bool isArabic = true,
    GymBrandingTheme branding = GymBrandingTheme.defaultBranding,
  }) {
    final colorScheme = branding.colorScheme(brightness: Brightness.dark);
    final textTheme = AppTypography.textTheme(
      isArabic: isArabic,
      isDark: true,
    );
    final primary = branding.primaryColor;

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.backgroundDark,
      cardColor: AppColors.cardDark,
      dividerColor: AppColors.dividerDark,
      appBarTheme: _appBarTheme(
        textTheme,
        background: AppColors.surfaceDark,
        foreground: AppColors.textPrimaryDark,
      ),
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: AppColors.surfaceDark,
        selectedItemColor: primary,
        unselectedItemColor: AppColors.grey500,
        type: BottomNavigationBarType.fixed,
        elevation: 8,
      ),
      cardTheme: CardThemeData(
        color: AppColors.cardDark,
        elevation: 0,
        shape: _cardShape(AppColors.dividerDark),
        surfaceTintColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
      ),
      elevatedButtonTheme: _elevatedButtonTheme(textTheme, primary),
      inputDecorationTheme: _darkInputDecorationTheme(textTheme, primary),
      snackBarTheme: _snackBarTheme,
      dialogTheme: _dialogTheme,
      bottomSheetTheme: _bottomSheetTheme(AppColors.surfaceDark),
      dividerTheme: _dividerTheme(AppColors.dividerDark),
    );
  }

  // ---------------------------------------------------------------------------
  // Shared component themes
  // ---------------------------------------------------------------------------

  static final BorderRadius _radius =
      BorderRadius.circular(AppConstants.borderRadius);

  static AppBarTheme _appBarTheme(
    TextTheme textTheme, {
    required Color background,
    required Color foreground,
  }) {
    return AppBarTheme(
      backgroundColor: background,
      foregroundColor: foreground,
      elevation: 0,
      centerTitle: true,
      titleTextStyle: textTheme.titleLarge,
      surfaceTintColor: Colors.transparent,
    );
  }

  static RoundedRectangleBorder _cardShape(Color borderColor) {
    return RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
      side: BorderSide(color: borderColor, width: 0.6),
    );
  }

  static ElevatedButtonThemeData _elevatedButtonTheme(
    TextTheme textTheme,
    Color primary, {
    Color? shadowColor,
  }) {
    return ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: primary,
        foregroundColor: AppColors.white,
        minimumSize: const Size(double.infinity, 52),
        elevation: 0,
        shadowColor: shadowColor,
        shape: RoundedRectangleBorder(borderRadius: _radius),
        textStyle: textTheme.labelLarge?.copyWith(
          fontWeight: FontWeight.w700,
          letterSpacing: 0.2,
        ),
      ),
    );
  }

  static OutlineInputBorder _inputBorder(BorderSide side) {
    return OutlineInputBorder(borderRadius: _radius, borderSide: side);
  }

  static final SnackBarThemeData _snackBarTheme = SnackBarThemeData(
    behavior: SnackBarBehavior.floating,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.borderRadiusSmall),
    ),
  );

  static final DialogThemeData _dialogTheme = DialogThemeData(
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(AppConstants.borderRadiusLarge),
    ),
  );

  static BottomSheetThemeData _bottomSheetTheme(Color background) {
    return BottomSheetThemeData(
      showDragHandle: true,
      backgroundColor: background,
      surfaceTintColor: Colors.transparent,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(28),
        ),
      ),
    );
  }

  static DividerThemeData _dividerTheme(Color color) {
    return DividerThemeData(color: color, thickness: 0.6, space: 1);
  }

  /// Resolves [selected] when the widget is selected, [otherwise] when not.
  static WidgetStateProperty<T> _whenSelected<T>(T selected, T otherwise) {
    return WidgetStateProperty.resolveWith(
      (states) => states.contains(WidgetState.selected) ? selected : otherwise,
    );
  }

  // ---------------------------------------------------------------------------
  // Light-only component themes
  // ---------------------------------------------------------------------------

  static BottomNavigationBarThemeData _lightBottomNavigationBarTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    return BottomNavigationBarThemeData(
      backgroundColor: AppColors.surfaceLight,
      selectedItemColor: primary,
      unselectedItemColor: AppColors.grey400,
      type: BottomNavigationBarType.fixed,
      elevation: 8,
      selectedLabelStyle: textTheme.labelSmall?.copyWith(
        fontWeight: FontWeight.w600,
      ),
      unselectedLabelStyle: textTheme.labelSmall,
    );
  }

  static TextButtonThemeData _lightTextButtonTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    return TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: primary,
        textStyle: textTheme.labelLarge?.copyWith(
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  static TabBarThemeData _lightTabBarTheme(TextTheme textTheme, Color primary) {
    return TabBarThemeData(
      indicatorColor: primary,
      indicatorSize: TabBarIndicatorSize.label,
      labelColor: primary,
      unselectedLabelColor: AppColors.grey500,
      labelStyle: textTheme.labelLarge,
      unselectedLabelStyle: textTheme.labelMedium,
    );
  }

  static NavigationBarThemeData _lightNavigationBarTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    return NavigationBarThemeData(
      backgroundColor: AppColors.surfaceLight,
      indicatorColor: primary.withOpacity(0.1),
      surfaceTintColor: Colors.transparent,
      labelTextStyle: _whenSelected(
        textTheme.labelSmall?.copyWith(
          color: primary,
          fontWeight: FontWeight.w600,
        ),
        textTheme.labelSmall?.copyWith(
          color: AppColors.grey500,
        ),
      ),
    );
  }

  static OutlinedButtonThemeData _lightOutlinedButtonTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    return OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: primary,
        minimumSize: const Size(double.infinity, 52),
        shape: RoundedRectangleBorder(borderRadius: _radius),
        side: BorderSide(color: primary, width: 1.4),
        textStyle: textTheme.labelLarge?.copyWith(
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }

  static InputDecorationTheme _lightInputDecorationTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    const idle = BorderSide(color: AppColors.grey200, width: 1);
    return InputDecorationTheme(
      filled: true,
      fillColor: AppColors.grey50,
      contentPadding: const EdgeInsets.symmetric(
        horizontal: 16,
        vertical: 16,
      ),
      border: _inputBorder(idle),
      enabledBorder: _inputBorder(idle),
      focusedBorder: _inputBorder(BorderSide(color: primary, width: 1.8)),
      errorBorder: _inputBorder(
        const BorderSide(color: AppColors.error, width: 1.4),
      ),
      labelStyle: textTheme.bodyMedium?.copyWith(
        color: AppColors.grey500,
      ),
      hintStyle: textTheme.bodyMedium?.copyWith(
        color: AppColors.grey400,
      ),
    );
  }

  static ChipThemeData _lightChipTheme(TextTheme textTheme, Color primary) {
    return ChipThemeData(
      backgroundColor: AppColors.grey100,
      selectedColor: primary.withOpacity(0.12),
      side: BorderSide.none,
      labelStyle: textTheme.labelMedium!,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(999),
      ),
    );
  }

  static SwitchThemeData _lightSwitchTheme(Color primary) {
    return SwitchThemeData(
      thumbColor: _whenSelected<Color?>(primary, AppColors.grey400),
      trackColor: _whenSelected<Color?>(
        primary.withOpacity(0.3),
        AppColors.grey200,
      ),
    );
  }

  // ---------------------------------------------------------------------------
  // Dark-only component themes
  // ---------------------------------------------------------------------------

  static InputDecorationTheme _darkInputDecorationTheme(
    TextTheme textTheme,
    Color primary,
  ) {
    const idle = BorderSide(color: AppColors.grey700);
    return InputDecorationTheme(
      filled: true,
      fillColor: AppColors.grey800,
      contentPadding: const EdgeInsets.symmetric(
        horizontal: 16,
        vertical: 14,
      ),
      border: _inputBorder(idle),
      enabledBorder: _inputBorder(idle),
      focusedBorder: _inputBorder(BorderSide(color: primary, width: 2)),
      errorBorder: _inputBorder(const BorderSide(color: AppColors.error)),
      labelStyle: textTheme.bodyMedium?.copyWith(
        color: AppColors.grey400,
      ),
      hintStyle: textTheme.bodyMedium?.copyWith(
        color: AppColors.grey500,
      ),
    );
  }
}
