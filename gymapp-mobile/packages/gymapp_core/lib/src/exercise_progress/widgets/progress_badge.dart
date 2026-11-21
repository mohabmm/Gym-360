import 'package:flutter/material.dart';

/// Small tinted pill: "New best", "+2.5 kg", "Next target: 3 × 10 @ 65 kg".
class ProgressBadge extends StatelessWidget {
  final String label;
  final Color color;
  final IconData? icon;

  const ProgressBadge(this.label, this.color, {super.key, this.icon});

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icon != null) ...[Icon(icon, size: 13, color: color), const SizedBox(width: 3)],
            Flexible(
              child: Text(
                label,
                style: Theme.of(context).textTheme.labelSmall?.copyWith(color: color, fontWeight: FontWeight.w700),
              ),
            ),
          ],
        ),
      );
}
