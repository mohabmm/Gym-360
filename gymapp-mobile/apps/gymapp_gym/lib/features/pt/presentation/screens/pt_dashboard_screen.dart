import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/pt_providers.dart';
import '../widgets/pt_dashboard_card.dart';

/// pt.owner_dashboard — trial-to-paid conversion per coach, renewals and sessions logged.
class PtDashboardScreen extends ConsumerStatefulWidget {
  const PtDashboardScreen({super.key});

  @override
  ConsumerState<PtDashboardScreen> createState() => _PtDashboardScreenState();
}

class _PtDashboardScreenState extends ConsumerState<PtDashboardScreen> {
  int _days = 30;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final dashboard = ref.watch(ptDashboardProvider(_days));
    final pagePad = context.responsivePagePadding;

    return Scaffold(
      appBar: AppBar(
          title: Text(isArabic ? 'لوحة التدريب الشخصي' : 'PT dashboard')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(ptDashboardProvider(_days).future),
        child: ListView(
          padding: EdgeInsets.all(pagePad),
          children: [
            SegmentedButton<int>(
              segments: [
                for (final d in const [7, 30, 90])
                  ButtonSegment(
                      value: d, label: Text(isArabic ? '$d يوم' : '$d days')),
              ],
              selected: {_days},
              onSelectionChanged: (v) => setState(() => _days = v.first),
            ),
            const SizedBox(height: 16),
            dashboard.when(
              data: (d) => Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  PtTotalsGrid(dashboard: d),
                  const SizedBox(height: 20),
                  Text(isArabic ? 'حسب المدرب' : 'By coach',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 8),
                  if (d.coaches.isEmpty)
                    Text(isArabic
                        ? 'لا توجد بيانات في هذه الفترة'
                        : 'No activity in this period')
                  else
                    for (final c in d.coaches) PtCoachTile(row: c),
                ],
              ),
              loading: () => const Center(
                  child: Padding(
                      padding: EdgeInsets.all(32),
                      child: CircularProgressIndicator())),
              error: (_, __) => AppErrorWidget(
                message: isArabic
                    ? 'تعذر تحميل اللوحة'
                    : 'Could not load the dashboard',
                onRetry: () => ref.invalidate(ptDashboardProvider(_days)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
