import 'package:flutter/material.dart';

/// Tailwind-aligned breakpoints so mobile layouts match the web design
/// system's adaptive behavior.
///   xs  < 360  — compact phones
///   sm  >= 360 — standard phones
///   md  >= 640 — large phones / small tablets (web `sm:`)
///   lg  >= 768 — tablets (web `md:`)
///   xl  >= 1024 — large tablets / foldables (web `lg:`)
class Breakpoints {
  Breakpoints._();
  static const double xs = 360;
  static const double sm = 640;
  static const double md = 768;
  static const double lg = 1024;
}

enum ScreenSize { xs, sm, md, lg, xl }

extension ResponsiveContext on BuildContext {
  Size get _size => MediaQuery.sizeOf(this);
  double get screenWidth => _size.width;
  double get screenHeight => _size.height;

  ScreenSize get screenSize {
    final w = screenWidth;
    if (w < Breakpoints.xs) return ScreenSize.xs;
    if (w < Breakpoints.sm) return ScreenSize.sm;
    if (w < Breakpoints.md) return ScreenSize.md;
    if (w < Breakpoints.lg) return ScreenSize.lg;
    return ScreenSize.xl;
  }

  bool get isCompactPhone => screenWidth < Breakpoints.xs;
  bool get isPhone => screenWidth < Breakpoints.sm;
  bool get isTablet => screenWidth >= Breakpoints.md;
  bool get isLargeTablet => screenWidth >= Breakpoints.lg;
  bool get isLandscape =>
      MediaQuery.orientationOf(this) == Orientation.landscape;

  /// Responsive horizontal page padding (grows with viewport).
  double get responsivePagePadding {
    if (isLargeTablet) return 32;
    if (isTablet) return 24;
    if (isCompactPhone) return 12;
    return 16;
  }

  /// Max content width for reading comfort on tablets.
  double get maxContentWidth {
    if (isLargeTablet) return 960;
    if (isTablet) return 720;
    return double.infinity;
  }

  /// Pick a value based on screen size with sensible fallthrough.
  T responsive<T>({
    required T phone,
    T? largePhone,
    T? tablet,
    T? largeTablet,
  }) {
    if (isLargeTablet) return largeTablet ?? tablet ?? largePhone ?? phone;
    if (isTablet) return tablet ?? largePhone ?? phone;
    if (screenWidth >= Breakpoints.sm) return largePhone ?? phone;
    return phone;
  }

  /// Grid columns that scale: 1 on phones, 2 on tablets, 3 on large tablets.
  int gridColumns({int phone = 1, int tablet = 2, int largeTablet = 3}) {
    if (isLargeTablet) return largeTablet;
    if (isTablet) return tablet;
    return phone;
  }
}

/// Renders different child widgets depending on screen size.
class ResponsiveBuilder extends StatelessWidget {
  final Widget Function(BuildContext context, ScreenSize size) builder;
  const ResponsiveBuilder({super.key, required this.builder});

  @override
  Widget build(BuildContext context) => builder(context, context.screenSize);
}

/// Centers and caps a child's width on wide viewports so content doesn't
/// stretch edge-to-edge on tablets / foldables.
class ResponsiveCenter extends StatelessWidget {
  final Widget child;
  final double? maxWidth;
  final EdgeInsetsGeometry? padding;

  const ResponsiveCenter({
    super.key,
    required this.child,
    this.maxWidth,
    this.padding,
  });

  @override
  Widget build(BuildContext context) {
    final cap = maxWidth ?? context.maxContentWidth;
    return Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: cap),
        child: Padding(
          padding: padding ??
              EdgeInsets.symmetric(horizontal: context.responsivePagePadding),
          child: child,
        ),
      ),
    );
  }
}
