import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../providers/workouts_providers.dart';

class WorkoutHistoryScreen extends ConsumerWidget {
  const WorkoutHistoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final logsAsync = ref.watch(workoutLogsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'سجل التمارين' : 'Workout History'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(workoutLogsProvider),
        child: logsAsync.when(
          data: (logs) {
            if (logs.isEmpty) {
              return EmptyState(
                icon: Icons.history,
                title:
                    isArabic ? 'لا توجد تمارين مسجلة' : 'No workouts logged',
                subtitle: isArabic
                    ? 'ابدأ تسجيل تماريناتك اليومية'
                    : 'Start logging your workouts',
              );
            }
            return ListView.builder(
              padding: const EdgeInsets.symmetric(vertical: 8),
              itemCount: logs.length,
              itemBuilder: (context, index) {
                final log = logs[index];
                return _WorkoutLogTile(log: log, isArabic: isArabic);
              },
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => AppErrorWidget(
            message: error.toString(),
            onRetry: () => ref.invalidate(workoutLogsProvider),
          ),
        ),
      ),
    );
  }
}

class _WorkoutLogTile extends StatelessWidget {
  final WorkoutLogModel log;
  final bool isArabic;

  const _WorkoutLogTile({required this.log, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final dateStr = DateFormat(isArabic ? 'dd/MM/yyyy' : 'MMM dd, yyyy')
        .format(log.date ?? log.startedAt);

    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        leading: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.primary.withOpacity(0.1),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(
            Icons.fitness_center,
            color: Theme.of(context).colorScheme.primary,
          ),
        ),
        title: Text(
          log.workoutName ?? (isArabic ? 'تمرين حر' : 'Ad-hoc workout'),
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(dateStr),
            if (log.durationMinutes != null)
              Text(
                '${log.durationMinutes} ${isArabic ? 'دقيقة' : 'min'}',
                style: TextStyle(
                  color: Theme.of(context).colorScheme.primary,
                  fontWeight: FontWeight.w500,
                ),
              ),
          ],
        ),
        trailing: log.sets.isNotEmpty
            ? Text(
                '${log.sets.length} ${isArabic ? 'جولات' : 'sets'}',
                style: Theme.of(context).textTheme.bodySmall,
              )
            : null,
      ),
    );
  }
}
