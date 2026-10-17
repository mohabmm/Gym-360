import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:percent_indicator/percent_indicator.dart';

import '../../../../core/providers.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../../data/models/nutrition_requests.dart';
import '../providers/nutrition_providers.dart';

String _fmtLiters(double liters) {
  final s = liters.toStringAsFixed(2);
  return s.endsWith('0') ? liters.toStringAsFixed(1) : s;
}

class WaterTrackerScreen extends ConsumerWidget {
  const WaterTrackerScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final waterAsync = ref.watch(waterLogProvider);

    return PopScope(
      onPopInvokedWithResult: (didPop, _) {
        if (didPop) ref.invalidate(todaysWaterProvider);
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(isArabic ? 'متابعة الماء' : 'Water Tracker'),
        ),
        body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(waterLogProvider),
        child: waterAsync.when(
          data: (log) => _WaterTrackerContent(log: log, isArabic: isArabic),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل متابعة الماء. يرجى المحاولة مرة أخرى.'
                : 'Could not load the water tracker. Please try again.',
            onRetry: () => ref.invalidate(waterLogProvider),
          ),
        ),
        ),
      ),
    );
  }
}

class _WaterTrackerContent extends ConsumerWidget {
  final WaterLogModel log;
  final bool isArabic;

  const _WaterTrackerContent(
      {required this.log, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Guard targetMl == 0 (a user with no target set): 0/0 = NaN, and
    // CircularPercentIndicator asserts 0 <= percent <= 1, so NaN crashes the
    // screen.
    final percentage = (log.targetMl > 0
            ? log.totalMl / log.targetMl
            : 0.0)
        .clamp(0.0, 1.0);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          // Circular progress
          CircularPercentIndicator(
            radius: 120.0,
            lineWidth: 16.0,
            percent: percentage,
            center: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.water_drop, color: Colors.cyan, size: 32),
                const SizedBox(height: 4),
                Text(
                  '${_fmtLiters(log.totalMl / 1000)}L',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: Colors.cyan,
                      ),
                ),
                Text(
                  '/ ${_fmtLiters(log.targetMl / 1000)}L',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
            progressColor: Colors.cyan,
            backgroundColor: Colors.cyan.withOpacity(0.1),
            animation: true,
            animationDuration: 800,
            circularStrokeCap: CircularStrokeCap.round,
          ),

          const SizedBox(height: 32),

          Text(
            _glassesLabel(log.entries.length, isArabic: isArabic),
            style: Theme.of(context).textTheme.titleMedium,
          ),

          const SizedBox(height: 24),

          // Quick add buttons
          Text(
            isArabic ? 'أضف ماء' : 'Add Water',
            style: Theme.of(context)
                .textTheme
                .titleSmall
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 12),
          // 2×2 grid so we never get the 3-then-1 wrap that fell out of
          // WrapAlignment.center when 4 chips couldn't fit one row (BUG-18).
          GridView.count(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            crossAxisCount: 2,
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            childAspectRatio: 3.2,
            children: [
              _WaterButton(
                  amount: 150,
                  label: '150ml',
                  isArabic: isArabic),
              _WaterButton(
                  amount: 250,
                  label: '250ml',
                  isArabic: isArabic),
              _WaterButton(
                  amount: 350,
                  label: '350ml',
                  isArabic: isArabic),
              _WaterButton(
                  amount: 500,
                  label: '500ml',
                  isArabic: isArabic),
            ],
          ),

          const SizedBox(height: 24),

          // Recent entries
          if (log.entries.isNotEmpty) ...[
            Align(
              alignment: isArabic
                  ? Alignment.centerRight
                  : Alignment.centerLeft,
              child: Text(
                isArabic ? 'السجل اليومي' : 'Today\'s Log',
                style: Theme.of(context)
                    .textTheme
                    .titleSmall
                    ?.copyWith(fontWeight: FontWeight.w600),
              ),
            ),
            const SizedBox(height: 8),
            ...log.entries.reversed.take(8).map(
                  (e) {
                    // BE ships loggedAt as UTC; render in the device's local
                    // zone so a 19:51 entry doesn't read as 17:51 (BUG-17).
                    final local = e.loggedAt.toLocal();
                    final hh = local.hour.toString().padLeft(2, '0');
                    final mm = local.minute.toString().padLeft(2, '0');
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.water_drop,
                          color: Colors.cyan, size: 20),
                      title: Text('${e.amountMl} ml'),
                      subtitle: Text('$hh:$mm'),
                    );
                  },
                ),
          ],
        ],
      ),
    );
  }
}

/// Pluralised "N glasses today" label. Arabic follows its own plural rules
/// (1→singular, 2→dual, 3–10→plural, 11+→singular accusative), not the
/// English one/other distinction — fixes BUG-16 where "1 glasses today"
/// appeared after the first entry.
String _glassesLabel(int count, {required bool isArabic}) {
  if (!isArabic) {
    final noun = count == 1 ? 'glass' : 'glasses';
    return '$count $noun today';
  }
  if (count == 0) return 'لا أكواب اليوم';
  if (count == 1) return 'كوب واحد اليوم';
  if (count == 2) return 'كوبان اليوم';
  if (count >= 3 && count <= 10) return '$count أكواب اليوم';
  return '$count كوباً اليوم';
}

class _WaterButton extends ConsumerStatefulWidget {
  final int amount;
  final String label;
  final bool isArabic;

  const _WaterButton(
      {required this.amount,
      required this.label,
      required this.isArabic});

  @override
  ConsumerState<_WaterButton> createState() => _WaterButtonState();
}

class _WaterButtonState extends ConsumerState<_WaterButton> {
  bool _isLoading = false;

  @override
  Widget build(BuildContext context) {
    return FilledButton.tonal(
      onPressed: _isLoading ? null : _add,
      child: _isLoading
          ? const SizedBox(
              height: 16,
              width: 16,
              child: CircularProgressIndicator(strokeWidth: 2))
          : Text(widget.label),
    );
  }

  Future<void> _add() async {
    setState(() => _isLoading = true);
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final userId = authState?.user?.id;
      final gymId = authState?.currentGymId;
      if (userId == null || gymId == null) {
        throw Exception('No authenticated user or active gym');
      }
      final repo = ref.read(nutritionRepositoryProvider);
      await repo.addWaterEntry(WaterEntryRequest(
        userId: userId,
        gymId: gymId,
        date: DateTime.now(),
        amountMl: widget.amount,
      ));
      ref.invalidate(waterLogProvider);
      ref.invalidate(todaysWaterProvider);
    } catch (_) {
      if (mounted) {
        final isArabic =
            Localizations.localeOf(context).languageCode == 'ar';
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'تعذر حفظ الإضافة. يرجى المحاولة مرة أخرى.'
              : 'Could not save the entry. Please try again.'),
        ));
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }
}
