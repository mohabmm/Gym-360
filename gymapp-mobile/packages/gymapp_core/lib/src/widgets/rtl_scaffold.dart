import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import '../theme/responsive.dart';

/// RTL + responsive aware scaffold.
///
/// - Forces `TextDirection.rtl` when [isArabic] is true (or reads locale).
/// - Wraps the body in [SafeArea] to prevent notch/gesture-area overlap.
/// - Caps content width on tablets / foldables via [ResponsiveCenter] when
///   [constrainWidth] is true (default). This keeps content readable and
///   stops cards from stretching to 1024dp on iPad.
class RtlScaffold extends StatelessWidget {
  final Widget body;
  final PreferredSizeWidget? appBar;
  final Widget? bottomNavigationBar;
  final Widget? floatingActionButton;
  final FloatingActionButtonLocation? floatingActionButtonLocation;
  final Widget? drawer;
  final Widget? endDrawer;
  final Color? backgroundColor;
  final bool? resizeToAvoidBottomInset;
  final bool extendBodyBehindAppBar;
  final bool? isArabic;
  final bool safeArea;
  final bool constrainWidth;
  final double? maxContentWidth;
  final EdgeInsetsGeometry? padding;

  const RtlScaffold({
    super.key,
    required this.body,
    this.appBar,
    this.bottomNavigationBar,
    this.floatingActionButton,
    this.floatingActionButtonLocation,
    this.drawer,
    this.endDrawer,
    this.backgroundColor,
    this.resizeToAvoidBottomInset,
    this.extendBodyBehindAppBar = false,
    this.isArabic,
    this.safeArea = true,
    this.constrainWidth = true,
    this.maxContentWidth,
    this.padding,
  });

  TextDirection _resolveDirection(BuildContext context) {
    if (isArabic == true) return TextDirection.rtl;
    if (isArabic == false) return TextDirection.ltr;
    final localeCode = Localizations.maybeLocaleOf(context)?.languageCode;
    return localeCode == 'ar' ? TextDirection.rtl : TextDirection.ltr;
  }

  @override
  Widget build(BuildContext context) {
    Widget content = body;

    if (constrainWidth && context.isTablet) {
      content = ResponsiveCenter(
        maxWidth: maxContentWidth,
        padding: padding ?? EdgeInsets.zero,
        child: content,
      );
    } else if (padding != null) {
      content = Padding(padding: padding!, child: content);
    }

    if (safeArea) {
      content = SafeArea(
        top: !extendBodyBehindAppBar,
        bottom: bottomNavigationBar == null,
        child: content,
      );
    }

    final scaffold = Scaffold(
      appBar: appBar,
      body: content,
      bottomNavigationBar: bottomNavigationBar,
      floatingActionButton: floatingActionButton,
      floatingActionButtonLocation: floatingActionButtonLocation,
      drawer: drawer,
      endDrawer: endDrawer,
      backgroundColor: backgroundColor,
      resizeToAvoidBottomInset: resizeToAvoidBottomInset,
      extendBodyBehindAppBar: extendBodyBehindAppBar,
    );

    return Directionality(
      textDirection: _resolveDirection(context),
      child: scaffold,
    );
  }
}

/// Section header used in list views.
///
/// Uses the display font via `headlineSmall` and adds a subtle accent bar
/// on the leading edge so sections scan quickly — matches the web sidebar
/// section styling.
class SectionHeader extends StatelessWidget {
  final String title;
  final String? subtitle;
  final String? actionText;
  final VoidCallback? onAction;
  final IconData? icon;
  final bool showAccentBar;

  const SectionHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.actionText,
    this.onAction,
    this.icon,
    this.showAccentBar = true,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding:
          const EdgeInsetsDirectional.fromSTEB(16, 16, 16, 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          if (showAccentBar)
            Container(
              width: 3,
              height: 18,
              margin: const EdgeInsetsDirectional.only(end: 10),
              decoration: BoxDecoration(
                gradient: AppColors.primaryGradient,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          if (icon != null) ...[
            Icon(icon, size: 18, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
          ],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  style: theme.textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (subtitle != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      subtitle!,
                      style: theme.textTheme.bodySmall,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
              ],
            ),
          ),
          if (actionText != null)
            TextButton(
              onPressed: onAction,
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(actionText!),
                  const SizedBox(width: 2),
                  Icon(
                    Directionality.of(context) == TextDirection.rtl
                        ? Icons.chevron_left
                        : Icons.chevron_right,
                    size: 16,
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
