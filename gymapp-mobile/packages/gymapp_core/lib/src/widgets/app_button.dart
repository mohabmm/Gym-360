import 'package:flutter/material.dart';
import '../constants/app_constants.dart';
import '../theme/app_colors.dart';

enum AppButtonSize { small, medium, large }

/// Reusable button widget with loading state + gradient variant.
class AppButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final bool isLoading;
  final bool isOutlined;
  final bool isText;
  final bool isGradient;
  final IconData? icon;
  final Color? backgroundColor;
  final Color? textColor;
  final Gradient? gradient;
  final double? width;
  final double height;
  final double borderRadius;
  final AppButtonSize size;

  const AppButton({
    super.key,
    required this.text,
    this.onPressed,
    this.isLoading = false,
    this.isOutlined = false,
    this.isText = false,
    this.isGradient = false,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.gradient,
    this.width,
    this.height = 52,
    this.borderRadius = AppConstants.borderRadius,
    this.size = AppButtonSize.medium,
  });

  const AppButton.outlined({
    super.key,
    required this.text,
    this.onPressed,
    this.isLoading = false,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.width,
    this.height = 52,
    this.borderRadius = AppConstants.borderRadius,
    this.size = AppButtonSize.medium,
  })  : isOutlined = true,
        isText = false,
        isGradient = false,
        gradient = null;

  const AppButton.text({
    super.key,
    required this.text,
    this.onPressed,
    this.isLoading = false,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.width,
    this.height = 44,
    this.borderRadius = AppConstants.borderRadius,
    this.size = AppButtonSize.medium,
  })  : isOutlined = false,
        isText = true,
        isGradient = false,
        gradient = null;

  /// Premium gradient CTA (use sparingly — primary call-to-action).
  const AppButton.gradient({
    super.key,
    required this.text,
    this.onPressed,
    this.isLoading = false,
    this.icon,
    this.gradient,
    this.textColor,
    this.width,
    this.height = 54,
    this.borderRadius = AppConstants.borderRadius,
    this.size = AppButtonSize.large,
  })  : isOutlined = false,
        isText = false,
        isGradient = true,
        backgroundColor = null;

  double get _effectiveHeight {
    switch (size) {
      case AppButtonSize.small:
        return 40;
      case AppButtonSize.medium:
        return height;
      case AppButtonSize.large:
        return height < 54 ? 54 : height;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final effectiveBgColor = backgroundColor ?? theme.colorScheme.primary;
    final effectiveTextColor = textColor ??
        (isOutlined || isText ? theme.colorScheme.primary : AppColors.white);
    final resolvedHeight = _effectiveHeight;

    Widget child;
    if (isLoading) {
      child = SizedBox(
        height: 20,
        width: 20,
        child: CircularProgressIndicator(
          strokeWidth: 2,
          valueColor: AlwaysStoppedAnimation<Color>(effectiveTextColor),
        ),
      );
    } else if (icon != null) {
      child = Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 18),
          const SizedBox(width: 8),
          Flexible(
            child: Text(
              text,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      );
    } else {
      child = Text(text, maxLines: 1, overflow: TextOverflow.ellipsis);
    }

    if (isText) {
      return SizedBox(
        width: width,
        height: resolvedHeight,
        child: TextButton(
          onPressed: isLoading ? null : onPressed,
          style: TextButton.styleFrom(
            foregroundColor: effectiveTextColor,
          ),
          child: child,
        ),
      );
    }

    if (isOutlined) {
      return SizedBox(
        width: width ?? double.infinity,
        height: resolvedHeight,
        child: OutlinedButton(
          onPressed: isLoading ? null : onPressed,
          style: OutlinedButton.styleFrom(
            foregroundColor: effectiveTextColor,
            side: BorderSide(color: effectiveBgColor, width: 1.4),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(borderRadius),
            ),
          ),
          child: child,
        ),
      );
    }

    if (isGradient) {
      final bg = gradient ?? AppColors.primaryGradient;
      final radius = BorderRadius.circular(borderRadius);
      return Opacity(
        opacity: onPressed == null && !isLoading ? 0.55 : 1,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          width: width ?? double.infinity,
          height: resolvedHeight,
          decoration: BoxDecoration(
            gradient: bg,
            borderRadius: radius,
            boxShadow: AppColors.primaryGlow(),
          ),
          child: Material(
            color: Colors.transparent,
            borderRadius: radius,
            child: InkWell(
              onTap: isLoading ? null : onPressed,
              borderRadius: radius,
              child: Center(
                child: DefaultTextStyle.merge(
                  style: theme.textTheme.labelLarge!.copyWith(
                    color: AppColors.white,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.2,
                  ),
                  child: IconTheme.merge(
                    data: const IconThemeData(color: AppColors.white),
                    child: child,
                  ),
                ),
              ),
            ),
          ),
        ),
      );
    }

    return SizedBox(
      width: width ?? double.infinity,
      height: resolvedHeight,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: effectiveBgColor,
          foregroundColor: effectiveTextColor,
          disabledBackgroundColor: effectiveBgColor.withOpacity(0.55),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(borderRadius),
          ),
        ),
        child: child,
      ),
    );
  }
}
