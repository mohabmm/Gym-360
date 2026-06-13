import 'package:flutter/material.dart';
import '../constants/app_constants.dart';
import '../theme/app_colors.dart';

/// Statistics card showing a label, value, and optional trend.
///
/// Supports three layouts:
/// - [StatCard]          — full card with icon tile and headline value.
/// - [StatCard.compact]  — horizontal row layout for dense lists.
/// - [StatCard.gradient] — premium gradient hero tile (use sparingly).
class StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color? iconColor;
  final Color? backgroundColor;
  final String? trend;
  final bool trendUp;
  final VoidCallback? onTap;
  final bool compact;
  final bool gradient;
  final Gradient? gradientOverride;

  const StatCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    this.iconColor,
    this.backgroundColor,
    this.trend,
    this.trendUp = true,
    this.onTap,
    this.compact = false,
  })  : gradient = false,
        gradientOverride = null;

  const StatCard.gradient({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    this.trend,
    this.trendUp = true,
    this.onTap,
    this.gradientOverride,
  })  : compact = false,
        gradient = true,
        iconColor = null,
        backgroundColor = null;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final effectiveIconColor = iconColor ?? theme.colorScheme.primary;
    final effectiveBgColor =
        backgroundColor ?? effectiveIconColor.withOpacity(0.12);
    final radius = BorderRadius.circular(AppConstants.borderRadiusLarge);

    if (gradient) {
      return InkWell(
        onTap: onTap,
        borderRadius: radius,
        child: Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            gradient: gradientOverride ?? AppColors.primaryGradient,
            borderRadius: radius,
            boxShadow: AppColors.primaryGlow(opacity: 0.28),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: Colors.white.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(icon, color: Colors.white, size: 20),
                  ),
                  const Spacer(),
                  if (trend != null)
                    _TrendBadge(trend: trend!, isUp: trendUp, onDark: true),
                ],
              ),
              const SizedBox(height: 14),
              Text(
                value,
                style: theme.textTheme.headlineMedium?.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.w800,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 2),
              Text(
                label,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: Colors.white.withOpacity(0.85),
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      );
    }

    if (compact) {
      return InkWell(
        onTap: onTap,
        borderRadius: radius,
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: theme.cardColor,
            borderRadius: radius,
            border: Border.all(
              color:
                  isDark ? AppColors.dividerDark : AppColors.dividerLight,
              width: 0.6,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Container(
                    padding: const EdgeInsets.all(7),
                    decoration: BoxDecoration(
                      color: effectiveBgColor,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(icon, color: effectiveIconColor, size: 16),
                  ),
                  if (trend != null)
                    _TrendBadge(trend: trend!, isUp: trendUp),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                value,
                style: theme.textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                label,
                style: theme.textTheme.labelSmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      );
    }

    return InkWell(
      onTap: onTap,
      borderRadius: radius,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: theme.cardColor,
          borderRadius: radius,
          border: Border.all(
            color: isDark ? AppColors.dividerDark : AppColors.dividerLight,
            width: 0.6,
          ),
          boxShadow: AppColors.cardShadow(isDark: isDark),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            // Narrow layouts (e.g. three stat cards side-by-side on a phone)
            // don't have room for icon + trend badge on the same row. Flexible
            // + FittedBox lets the badge shrink instead of painting the
            // "RIGHT OVERFLOWED BY 15" debug stripe.
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Container(
                  padding: const EdgeInsets.all(9),
                  decoration: BoxDecoration(
                    color: effectiveBgColor,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(icon, color: effectiveIconColor, size: 20),
                ),
                if (trend != null)
                  Flexible(
                    child: FittedBox(
                      fit: BoxFit.scaleDown,
                      alignment: AlignmentDirectional.centerEnd,
                      child: _TrendBadge(trend: trend!, isUp: trendUp),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 14),
            Text(
              value,
              style: theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 2),
            Text(
              label,
              style: theme.textTheme.bodySmall,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}

class _TrendBadge extends StatelessWidget {
  final String trend;
  final bool isUp;
  final bool onDark;

  const _TrendBadge({
    required this.trend,
    required this.isUp,
    this.onDark = false,
  });

  @override
  Widget build(BuildContext context) {
    final color = isUp ? AppColors.success : AppColors.error;
    final bg = onDark
        ? Colors.white.withOpacity(0.2)
        : color.withOpacity(0.12);
    final fg = onDark ? Colors.white : color;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isUp ? Icons.trending_up : Icons.trending_down,
            size: 14,
            color: fg,
          ),
          const SizedBox(width: 3),
          Text(
            trend,
            style: TextStyle(
              color: fg,
              fontSize: 11,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
