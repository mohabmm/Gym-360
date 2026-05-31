import 'package:flutter/material.dart';

/// Application color palette — aligned with the GymApp web persona.
/// Primary is the warm crimson (#C41E3A) used by the gym-staff web app,
/// paired with a premium gold accent (#D4AF37) and neutral slates.
/// Dynamic gym branding can still override primaryColor per gym.
class AppColors {
  AppColors._();

  // Brand — matches web gym-staff persona
  static const Color primary = Color(0xFFC41E3A);
  static const Color primaryLight = Color(0xFFE23E5B);
  static const Color primaryDark = Color(0xFF8F1026);
  static const Color primarySoft = Color(0xFFFDECEF);

  static const Color secondary = Color(0xFF0F172A);
  static const Color secondaryLight = Color(0xFF1E293B);

  /// Premium accent used for highlights, leaderboards, loyalty.
  static const Color accentGold = Color(0xFFD4AF37);
  static const Color accentGoldLight = Color(0xFFE9CB6B);

  /// Admin-web tone (available for power-user surfaces).
  static const Color adminBlue = Color(0xFF3B82F6);

  // Legacy green — kept for back-compat callers that referenced it.
  static const Color legacyGreen = Color(0xFF10B981);

  // Status
  static const Color success = Color(0xFF16A34A);
  static const Color warning = Color(0xFFF59E0B);
  static const Color error = Color(0xFFEF4444);
  static const Color info = Color(0xFF3B82F6);

  static const Color graceWarning = Color(0xFFFBBF24);
  static const Color gymStaffRed = primary;

  // Neutrals (slate family — matches web)
  static const Color white = Color(0xFFFFFFFF);
  static const Color black = Color(0xFF000000);
  static const Color grey50 = Color(0xFFF8FAFC);
  static const Color grey100 = Color(0xFFF1F5F9);
  static const Color grey200 = Color(0xFFE2E8F0);
  static const Color grey300 = Color(0xFFCBD5E1);
  static const Color grey400 = Color(0xFF94A3B8);
  static const Color grey500 = Color(0xFF64748B);
  static const Color grey600 = Color(0xFF475569);
  static const Color grey700 = Color(0xFF334155);
  static const Color grey800 = Color(0xFF1E293B);
  static const Color grey900 = Color(0xFF0F172A);

  // Backgrounds
  static const Color backgroundLight = Color(0xFFF8FAFC);
  static const Color backgroundDark = Color(0xFF0B1220);
  static const Color surfaceLight = Color(0xFFFFFFFF);
  static const Color surfaceDark = Color(0xFF111827);
  static const Color cardLight = Color(0xFFFFFFFF);
  static const Color cardDark = Color(0xFF1E293B);

  // Text
  static const Color textPrimaryLight = Color(0xFF0F172A);
  static const Color textSecondaryLight = Color(0xFF475569);
  static const Color textTertiaryLight = Color(0xFF94A3B8);
  static const Color textPrimaryDark = Color(0xFFF8FAFC);
  static const Color textSecondaryDark = Color(0xFFCBD5E1);
  static const Color textTertiaryDark = Color(0xFF64748B);

  // Dividers
  static const Color dividerLight = Color(0xFFE2E8F0);
  static const Color dividerDark = Color(0xFF334155);

  // Shimmer
  static const Color shimmerBase = Color(0xFFE2E8F0);
  static const Color shimmerHighlight = Color(0xFFF1F5F9);
  static const Color shimmerBaseDark = Color(0xFF1E293B);
  static const Color shimmerHighlightDark = Color(0xFF334155);

  // Rating star
  static const Color star = accentGold;
  static const Color starEmpty = Color(0xFFCBD5E1);

  // Leaderboard
  static const Color gold = Color(0xFFFFD700);
  static const Color silver = Color(0xFFC0C0C0);
  static const Color bronze = Color(0xFFCD7F32);

  // Kiosk
  static const Color kioskBackground = Color(0xFF0B1220);
  static const Color kioskSurface = Color(0xFF111827);
  static const Color kioskSuccess = Color(0xFF22C55E);
  static const Color kioskError = Color(0xFFEF4444);
  static const Color kioskText = Color(0xFFF8FAFC);

  // ── Gradients (premium web-aligned flourishes) ──
  static const LinearGradient primaryGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFFE23E5B), Color(0xFFC41E3A), Color(0xFF8F1026)],
    stops: [0.0, 0.55, 1.0],
  );

  static const LinearGradient goldGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFFE9CB6B), Color(0xFFD4AF37), Color(0xFFA8862A)],
  );

  static const LinearGradient darkGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF1E293B), Color(0xFF0F172A)],
  );

  static const LinearGradient surfaceGradientLight = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFFFFFFFF), Color(0xFFF8FAFC)],
  );

  /// Soft glow around primary CTAs.
  static List<BoxShadow> primaryGlow({double opacity = 0.25}) => [
        BoxShadow(
          color: primary.withOpacity(opacity),
          blurRadius: 18,
          offset: const Offset(0, 8),
          spreadRadius: -4,
        ),
      ];

  /// Standard elevated-card shadow (web-aligned soft shadow).
  static List<BoxShadow> cardShadow({bool isDark = false}) => [
        BoxShadow(
          color: isDark
              ? Colors.black.withOpacity(0.35)
              : const Color(0xFF0F172A).withOpacity(0.06),
          blurRadius: 16,
          offset: const Offset(0, 4),
          spreadRadius: -2,
        ),
        BoxShadow(
          color: isDark
              ? Colors.black.withOpacity(0.25)
              : const Color(0xFF0F172A).withOpacity(0.04),
          blurRadius: 4,
          offset: const Offset(0, 2),
        ),
      ];

  /// Parse a hex color string to Color. Falls back to [primary] for malformed
  /// input — guards against stale cached branding (e.g. sweep junk in
  /// secure storage that survives DB resets).
  static Color fromHex(String hex) {
    final clean = hex.replaceFirst('#', '').trim();
    if (!RegExp(r'^[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$').hasMatch(clean)) {
      return primary;
    }
    final buffer = StringBuffer();
    if (clean.length == 6) buffer.write('FF');
    buffer.write(clean);
    final parsed = int.tryParse(buffer.toString(), radix: 16);
    return parsed != null ? Color(parsed) : primary;
  }
}
