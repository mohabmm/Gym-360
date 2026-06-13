import 'package:flutter/material.dart';
import 'app_button.dart';

/// Empty state display for lists and screens with no data.
class EmptyState extends StatelessWidget {
  final String title;
  final String? subtitle;
  final IconData icon;
  final String? actionLabel;
  final VoidCallback? onAction;
  final Widget? customIcon;

  const EmptyState({
    super.key,
    required this.title,
    this.subtitle,
    this.icon = Icons.inbox_outlined,
    this.actionLabel,
    this.onAction,
    this.customIcon,
  });

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            customIcon ??
                Icon(
                  icon,
                  size: 80,
                  color: Theme.of(context)
                      .colorScheme
                      .onSurface
                      .withOpacity(0.2),
                ),
            const SizedBox(height: 24),
            Text(
              title,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            if (subtitle != null) ...[
              const SizedBox(height: 8),
              Text(
                subtitle!,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: Theme.of(context)
                          .colorScheme
                          .onSurface
                          .withOpacity(0.6),
                    ),
              ),
            ],
            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: 24),
              AppButton(
                text: actionLabel!,
                onPressed: onAction,
                width: 200,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
