import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/pt_providers.dart';

bool _ar(BuildContext context) =>
    Localizations.localeOf(context).languageCode == 'ar';

/// Headline PT numbers: conversion, renewals, sessions logged, trials.
class PtTotalsGrid extends StatelessWidget {
  final PtDashboard dashboard;
  const PtTotalsGrid({super.key, required this.dashboard});

  @override
  Widget build(BuildContext context) {
    final ar = _ar(context);
    final t = dashboard.totals;
    final cards = [
      StatCard(
          label: ar ? 'نسبة التحويل' : 'Trial → paid',
          value: '${t.conversionRate.toStringAsFixed(0)}%',
          icon: Icons.trending_up,
          iconColor: AppColors.success,
          compact: true),
      StatCard(
          label: ar ? 'التجديدات' : 'Renewals',
          value: '${t.renewals}',
          icon: Icons.autorenew,
          iconColor: AppColors.info,
          compact: true),
      StatCard(
          label: ar ? 'الحصص المسجلة' : 'Sessions logged',
          value: '${t.sessionsLogged}',
          icon: Icons.fitness_center,
          iconColor: AppColors.primary,
          compact: true),
      StatCard(
          label: ar ? 'تجارب نشطة' : 'Active trials',
          value: '${dashboard.activeTrials}',
          icon: Icons.hourglass_bottom,
          iconColor: AppColors.warning,
          compact: true),
    ];
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10,
      crossAxisSpacing: 10,
      childAspectRatio: 1.9,
      children: cards,
    );
  }
}

class PtCoachTile extends StatelessWidget {
  final PtCoachRow row;
  const PtCoachTile({super.key, required this.row});

  @override
  Widget build(BuildContext context) {
    final ar = _ar(context);
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
                row.trainerName.isEmpty
                    ? (ar ? 'مدرب' : 'Coach')
                    : row.trainerName,
                style: theme.textTheme.titleSmall
                    ?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 6),
            Wrap(spacing: 16, runSpacing: 4, children: [
              Text(ar
                  ? 'التحويل: ${row.trialsConverted}/${row.trialsFinished} (${row.conversionRate.toStringAsFixed(0)}%)'
                  : 'Converted: ${row.trialsConverted}/${row.trialsFinished} (${row.conversionRate.toStringAsFixed(0)}%)'),
              Text(ar
                  ? 'تجديدات: ${row.renewals}'
                  : 'Renewals: ${row.renewals}'),
              Text(ar
                  ? 'باقات جديدة: ${row.newPaidPackages}'
                  : 'New packages: ${row.newPaidPackages}'),
              Text(ar
                  ? 'حصص: ${row.sessionsLogged}'
                  : 'Sessions: ${row.sessionsLogged}'),
            ]),
          ],
        ),
      ),
    );
  }
}

/// Owner/manager summary card on the desk home (last 30 days).
class PtDashboardCard extends ConsumerWidget {
  const PtDashboardCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ar = _ar(context);
    return ref.watch(ptDashboardProvider(30)).when(
          data: (d) => Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SectionHeader(
                title: ar
                    ? 'أداء التدريب الشخصي (٣٠ يومًا)'
                    : 'Personal training (30 days)',
                actionText: ar ? 'التفاصيل' : 'Details',
                onAction: () => context.push('/pt/dashboard'),
              ),
              PtTotalsGrid(dashboard: d),
            ],
          ),
          loading: () => const ShimmerCard(height: 160),
          error: (_, __) => const SizedBox.shrink(),
        );
  }
}
