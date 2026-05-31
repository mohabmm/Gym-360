import 'package:flutter/material.dart';
import '../models/gym_branding_model.dart';
import 'app_colors.dart';

/// Dynamic gym branding support.
/// Fetched on login and cached. Re-fetched on app open.
/// If gym_branding feature is disabled (Free/Starter), uses default GymApp theme.
class GymBrandingTheme {
  final GymBrandingModel? branding;

  const GymBrandingTheme({this.branding});

  /// Default branding with GymApp green.
  static const GymBrandingTheme defaultBranding = GymBrandingTheme();

  /// Primary color from gym branding or default.
  Color get primaryColor {
    if (branding?.primaryColor != null) {
      return AppColors.fromHex(branding!.primaryColor);
    }
    return AppColors.primary;
  }

  /// Secondary color from gym branding or default.
  Color get secondaryColor {
    if (branding?.secondaryColor != null) {
      return AppColors.fromHex(branding!.secondaryColor);
    }
    return AppColors.secondary;
  }

  /// Gym logo URL if available.
  String? get logoUrl => branding?.logoUrl;

  /// Cover photo URL if available.
  String? get coverPhotoUrl => branding?.coverPhotoUrl;

  /// Whether custom branding is active.
  bool get hasCustomBranding =>
      branding != null &&
      branding!.primaryColor.toUpperCase() != '#C41E3A' &&
      branding!.primaryColor.toUpperCase() != '#10B981';

  /// Generate a ColorScheme from the gym branding.
  /// Tertiary is tuned to the premium gold accent so surfaces picked up
  /// via `colorScheme.tertiary` on the mobile side render consistently
  /// with the web persona.
  ColorScheme colorScheme({required Brightness brightness}) {
    return ColorScheme.fromSeed(
      seedColor: primaryColor,
      brightness: brightness,
      primary: primaryColor,
      secondary: secondaryColor,
      tertiary: AppColors.accentGold,
    );
  }
}
