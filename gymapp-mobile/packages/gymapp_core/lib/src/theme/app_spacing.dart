import 'package:flutter/material.dart';

/// Standard spacing constants for consistent layout across all GymApp screens.
/// Arabic-first design: values apply equally to LTR and RTL layouts.
class AppSpacing {
  AppSpacing._();

  // Base spacing unit: 4dp
  static const double xs = 4.0;
  static const double sm = 8.0;
  static const double md = 12.0;
  static const double lg = 16.0;
  static const double xl = 24.0;
  static const double xxl = 32.0;
  static const double xxxl = 48.0;
  static const double huge = 64.0;

  // Semantic aliases
  static const double pagePadding = lg;
  static const double cardPadding = lg;
  static const double sectionSpacing = xl;
  static const double itemSpacing = sm;
  static const double inputSpacing = md;
  static const double buttonHeight = 48.0;
  static const double iconButtonSize = 40.0;
  static const double appBarHeight = 56.0;
  static const double bottomNavHeight = 60.0;
  static const double listItemHeight = 72.0;
  static const double avatarSmall = 32.0;
  static const double avatarMedium = 48.0;
  static const double avatarLarge = 64.0;

  // Convenience EdgeInsets
  static const EdgeInsets pageInsets = EdgeInsets.all(pagePadding);
  static const EdgeInsets pageInsetsHorizontal =
      EdgeInsets.symmetric(horizontal: pagePadding);
  static const EdgeInsets pageInsetsVertical =
      EdgeInsets.symmetric(vertical: pagePadding);
  static const EdgeInsets cardInsets = EdgeInsets.all(cardPadding);
  static const EdgeInsets listItemInsets =
      EdgeInsets.symmetric(horizontal: lg, vertical: sm);

  // SizedBox helpers
  static const Widget xsGap = SizedBox(height: xs, width: xs);
  static const Widget smGap = SizedBox(height: sm, width: sm);
  static const Widget mdGap = SizedBox(height: md, width: md);
  static const Widget lgGap = SizedBox(height: lg, width: lg);
  static const Widget xlGap = SizedBox(height: xl, width: xl);
  static const Widget xxlGap = SizedBox(height: xxl, width: xxl);

  static const Widget xsVGap = SizedBox(height: xs);
  static const Widget smVGap = SizedBox(height: sm);
  static const Widget mdVGap = SizedBox(height: md);
  static const Widget lgVGap = SizedBox(height: lg);
  static const Widget xlVGap = SizedBox(height: xl);
  static const Widget xxlVGap = SizedBox(height: xxl);

  static const Widget xsHGap = SizedBox(width: xs);
  static const Widget smHGap = SizedBox(width: sm);
  static const Widget mdHGap = SizedBox(width: md);
  static const Widget lgHGap = SizedBox(width: lg);
  static const Widget xlHGap = SizedBox(width: xl);
  static const Widget xxlHGap = SizedBox(width: xxl);
}
