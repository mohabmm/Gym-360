import 'dart:ui';
import 'package:flutter/material.dart';
import '../constants/app_constants.dart';
import '../theme/app_colors.dart';

/// Visual variants available to AppCard.
enum AppCardVariant { flat, elevated, outlined, gradient, glass }

/// Reusable card widget with premium web-aligned styling.
class AppCard extends StatelessWidget {
  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry? padding;
  final EdgeInsetsGeometry? margin;
  final Color? color;
  final double? elevation;
  final double borderRadius;
  final Border? border;
  final AppCardVariant variant;
  final Gradient? gradient;

  const AppCard({
    super.key,
    required this.child,
    this.onTap,
    this.padding,
    this.margin,
    this.color,
    this.elevation,
    this.borderRadius = AppConstants.borderRadiusLarge,
    this.border,
    this.variant = AppCardVariant.elevated,
    this.gradient,
  });

  /// Convenience constructor for the premium gradient variant (primary CTA card).
  const AppCard.gradient({
    super.key,
    required this.child,
    this.onTap,
    this.padding,
    this.margin,
    this.gradient,
    this.borderRadius = AppConstants.borderRadiusLarge,
  })  : color = null,
        elevation = null,
        border = null,
        variant = AppCardVariant.gradient;

  /// Convenience constructor for a subtle outlined card (secondary surfaces).
  const AppCard.outlined({
    super.key,
    required this.child,
    this.onTap,
    this.padding,
    this.margin,
    this.color,
    this.borderRadius = AppConstants.borderRadiusLarge,
  })  : elevation = null,
        border = null,
        gradient = null,
        variant = AppCardVariant.outlined;

  /// Glassmorphism card — great for hero/overlay surfaces.
  const AppCard.glass({
    super.key,
    required this.child,
    this.onTap,
    this.padding,
    this.margin,
    this.borderRadius = AppConstants.borderRadiusLarge,
  })  : color = null,
        elevation = null,
        border = null,
        gradient = null,
        variant = AppCardVariant.glass;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final radius = BorderRadius.circular(borderRadius);
    final effectivePadding = padding ?? const EdgeInsets.all(16);

    Widget content = Padding(padding: effectivePadding, child: child);

    Widget decorated;
    switch (variant) {
      case AppCardVariant.gradient:
        decorated = Container(
          decoration: BoxDecoration(
            gradient: gradient ?? AppColors.primaryGradient,
            borderRadius: radius,
            boxShadow: AppColors.primaryGlow(opacity: 0.3),
          ),
          child: DefaultTextStyle.merge(
            style: const TextStyle(color: AppColors.white),
            child: IconTheme.merge(
              data: const IconThemeData(color: AppColors.white),
              child: content,
            ),
          ),
        );
        break;

      case AppCardVariant.glass:
        decorated = ClipRRect(
          borderRadius: radius,
          child: BackdropFilter(
            filter: ImageFilter.blur(sigmaX: 18, sigmaY: 18),
            child: Container(
              decoration: BoxDecoration(
                color: (isDark ? AppColors.cardDark : AppColors.white)
                    .withOpacity(isDark ? 0.55 : 0.72),
                borderRadius: radius,
                border: Border.all(
                  color: (isDark ? Colors.white : Colors.white)
                      .withOpacity(isDark ? 0.08 : 0.6),
                  width: 0.8,
                ),
              ),
              child: content,
            ),
          ),
        );
        break;

      case AppCardVariant.outlined:
        decorated = Container(
          decoration: BoxDecoration(
            color: color ??
                (isDark ? AppColors.cardDark : AppColors.surfaceLight),
            borderRadius: radius,
            border: border ??
                Border.all(
                  color: isDark
                      ? AppColors.dividerDark
                      : AppColors.dividerLight,
                  width: 1,
                ),
          ),
          child: content,
        );
        break;

      case AppCardVariant.flat:
        decorated = Container(
          decoration: BoxDecoration(
            color: color ??
                (isDark ? AppColors.cardDark : AppColors.surfaceLight),
            borderRadius: radius,
            border: border,
          ),
          child: content,
        );
        break;

      case AppCardVariant.elevated:
        decorated = Container(
          decoration: BoxDecoration(
            color: color ??
                (isDark ? AppColors.cardDark : AppColors.surfaceLight),
            borderRadius: radius,
            border: border ??
                Border.all(
                  color: isDark
                      ? AppColors.dividerDark
                      : AppColors.dividerLight,
                  width: 0.6,
                ),
            boxShadow: AppColors.cardShadow(isDark: isDark),
          ),
          child: content,
        );
        break;
    }

    final withMargin =
        margin != null ? Padding(padding: margin!, child: decorated) : decorated;

    if (onTap == null) return withMargin;

    return Padding(
      padding: margin ?? EdgeInsets.zero,
      child: Material(
        color: Colors.transparent,
        borderRadius: radius,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          splashColor: AppColors.primary.withOpacity(0.08),
          highlightColor: AppColors.primary.withOpacity(0.04),
          child: decorated,
        ),
      ),
    );
  }
}

/// Card specifically for list items with leading, title, subtitle, and trailing.
class AppListCard extends StatelessWidget {
  final Widget? leading;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry? padding;
  final AppCardVariant variant;

  const AppListCard({
    super.key,
    this.leading,
    required this.title,
    this.subtitle,
    this.trailing,
    this.onTap,
    this.padding,
    this.variant = AppCardVariant.elevated,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      variant: variant,
      padding: padding ?? const EdgeInsetsDirectional.fromSTEB(14, 12, 14, 12),
      child: Row(
        children: [
          if (leading != null) ...[
            leading!,
            const SizedBox(width: 12),
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  style: Theme.of(context).textTheme.titleMedium,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (subtitle != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      subtitle!,
                      style: Theme.of(context).textTheme.bodySmall,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
              ],
            ),
          ),
          if (trailing != null) ...[
            const SizedBox(width: 8),
            trailing!,
          ],
        ],
      ),
    );
  }
}
