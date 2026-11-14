import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/progress_providers.dart';

/// progress.inbody: the member's latest InBody values (if any) and the "Scan InBody" action.
class InBodyCard extends ConsumerWidget {
  final bool isArabic;

  const InBodyCard({super.key, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final latest = ref.watch(progressJourneyProvider).valueOrNull?.entries.reversed
        .where((e) => e.composition != null || e.source == 'InBodyScan')
        .firstOrNull;
    final theme = Theme.of(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(children: [
              Icon(Icons.document_scanner_outlined, color: theme.colorScheme.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Text(isArabic ? 'آخر قياس InBody' : 'Latest InBody', style: theme.textTheme.titleMedium),
              ),
              if (latest != null) Text(latest.date, style: theme.textTheme.bodySmall),
            ]),
            const SizedBox(height: 12),
            if (latest == null)
              Text(isArabic
                  ? 'صوّر ورقة InBody لتسجيل الوزن والطول والدهون والعضلات والماء تلقائياً.'
                  : 'Scan your InBody sheet to log weight, height, fat, muscle and water automatically.')
            else
              InBodyValues(entry: latest, isArabic: isArabic),
            const SizedBox(height: 12),
            FilledButton.icon(
              icon: const Icon(Icons.photo_camera),
              label: Text(isArabic ? 'قراءة InBody' : 'Scan InBody'),
              onPressed: () => context.push(AppRoutes.inBodyScan),
            ),
          ],
        ),
      ),
    );
  }
}

/// Weight, height, body fat, muscle and water (kg and % of weight) and BMI of one entry.
class InBodyValues extends StatelessWidget {
  final BodyMetricEntryModel entry;
  final bool isArabic;

  const InBodyValues({super.key, required this.entry, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final c = entry.composition;
    final tiles = [
      _Tile(label: isArabic ? 'الوزن' : 'Weight', value: _withUnit(entry.weight, 'kg')),
      _Tile(label: isArabic ? 'الطول' : 'Height', value: _withUnit(c?.heightCm, 'cm')),
      _Tile(label: isArabic ? 'الدهون' : 'Body fat', value: _withUnit(entry.bodyFatPercentage, '%')),
      _Tile(
          label: isArabic ? 'العضلات' : 'Muscle',
          value: _massAndShare(c?.skeletalMuscleMassKg, c?.skeletalMusclePercent)),
      _Tile(label: isArabic ? 'الماء' : 'Water', value: _massAndShare(c?.bodyWaterKg, c?.bodyWaterPercent)),
      _Tile(label: 'BMI', value: _withUnit(c?.bmi, '')),
    ];

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: tiles.where((t) => t.value != null).toList(),
    );
  }

  static String? _withUnit(double? v, String unit) => v == null ? null : '${_fmt(v)} $unit'.trim();

  static String? _massAndShare(double? kg, double? percent) {
    if (kg == null) return null;
    return percent == null ? '${_fmt(kg)} kg' : '${_fmt(kg)} kg · ${_fmt(percent)}%';
  }

  static String _fmt(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toStringAsFixed(1);
}

class _Tile extends StatelessWidget {
  final String label;

  /// Null when the entry has no such value; the tile is then left out.
  final String? value;

  const _Tile({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      constraints: const BoxConstraints(minWidth: 96),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: theme.textTheme.bodySmall),
        const SizedBox(height: 2),
        Text(value ?? '', style: theme.textTheme.titleSmall, textDirection: TextDirection.ltr),
      ]),
    );
  }
}
