import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/router.dart';
import '../providers/workouts_providers.dart';

class ExerciseLibraryScreen extends ConsumerWidget {
  const ExerciseLibraryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final logsAsync = ref.watch(workoutLogsProvider);
    final assignmentsAsync = ref.watch(workoutAssignmentsProvider);
    final exercisesAsync = ref.watch(exerciseLibraryProvider);
    final searchQuery = ref.watch(exerciseSearchQueryProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'التمارين' : 'Workouts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.history),
            tooltip: isArabic ? 'السجل' : 'History',
            onPressed: () => context.push(AppRoutes.workoutHistory),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(workoutLogsProvider);
          ref.invalidate(workoutAssignmentsProvider);
          ref.invalidate(exerciseLibraryProvider);
        },
        child: CustomScrollView(
          slivers: [
            // Search bar
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: _SearchBar(isArabic: isArabic),
              ),
            ),

            // When searching, show exercise library results instead of workouts
            if (searchQuery.isNotEmpty) ...[
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  child: Text(
                    isArabic ? 'نتائج البحث' : 'Search Results',
                    style: Theme.of(context)
                        .textTheme
                        .titleSmall
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
              ),
              exercisesAsync.when(
                data: (exercises) {
                  if (exercises.isEmpty) {
                    return SliverToBoxAdapter(
                      child: EmptyState(
                        icon: Icons.search_off,
                        title: isArabic ? 'لا توجد نتائج' : 'No results',
                        subtitle: isArabic
                            ? 'جرب كلمة بحث أخرى'
                            : 'Try a different search term',
                      ),
                    );
                  }
                  return SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) => _ExerciseTile(
                        exercise: exercises[index],
                        isArabic: isArabic,
                      ),
                      childCount: exercises.length,
                    ),
                  );
                },
                loading: () => const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
                error: (error, _) => SliverToBoxAdapter(
                  child: AppErrorWidget(
                    message: error.toString(),
                    onRetry: () => ref.invalidate(exerciseLibraryProvider),
                  ),
                ),
              ),
            ] else ...[
              // Assigned workouts section
              SliverToBoxAdapter(
                child: assignmentsAsync.when(
                  data: (assignments) {
                    if (assignments.isEmpty) return const SizedBox.shrink();
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Padding(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 16, vertical: 4),
                          child: Text(
                            isArabic ? 'تمارين مخصصة لك' : 'Assigned Workouts',
                            style: Theme.of(context)
                                .textTheme
                                .titleSmall
                                ?.copyWith(fontWeight: FontWeight.w700),
                          ),
                        ),
                        ...assignments.map(
                          (a) => _WorkoutAssignmentTile(
                            assignment: a,
                            isArabic: isArabic,
                          ),
                        ),
                        const Divider(height: 24),
                      ],
                    );
                  },
                  loading: () => const SizedBox.shrink(),
                  error: (_, __) => const SizedBox.shrink(),
                ),
              ),

              // Recent Workouts (logged sessions)
              SliverToBoxAdapter(
                child: Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        isArabic ? 'تماريني الأخيرة' : 'Recent Workouts',
                        style: Theme.of(context)
                            .textTheme
                            .titleSmall
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      TextButton.icon(
                        onPressed: () => context.push(AppRoutes.logWorkout),
                        icon: const Icon(Icons.add, size: 18),
                        label: Text(isArabic ? 'سجل تمرين' : 'Log Workout'),
                      ),
                    ],
                  ),
                ),
              ),

              logsAsync.when(
                data: (logs) {
                  if (logs.isEmpty) {
                    return SliverToBoxAdapter(
                      child: EmptyState(
                        icon: Icons.fitness_center_outlined,
                        title: isArabic
                            ? 'لا توجد تمارين بعد'
                            : 'No workouts yet',
                        subtitle: isArabic
                            ? 'سجل تمرينك الأول'
                            : 'Log your first workout',
                      ),
                    );
                  }
                  final recent = logs.take(5).toList();
                  return SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        if (index == recent.length) {
                          if (logs.length <= recent.length) {
                            return const SizedBox.shrink();
                          }
                          return Padding(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 16, vertical: 4),
                            child: TextButton(
                              onPressed: () =>
                                  context.push(AppRoutes.workoutHistory),
                              child: Text(
                                isArabic ? 'عرض الكل' : 'View all',
                              ),
                            ),
                          );
                        }
                        return _WorkoutLogTile(
                          log: recent[index],
                          isArabic: isArabic,
                        );
                      },
                      childCount: recent.length + 1,
                    ),
                  );
                },
                loading: () => const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
                error: (error, _) => SliverToBoxAdapter(
                  child: AppErrorWidget(
                    message: error.toString(),
                    onRetry: () => ref.invalidate(workoutLogsProvider),
                  ),
                ),
              ),

              // Exercise Library section
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                  child: Text(
                    isArabic ? 'مكتبة التمارين' : 'Exercise Library',
                    style: Theme.of(context)
                        .textTheme
                        .titleSmall
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
              ),

              exercisesAsync.when(
                data: (exercises) {
                  if (exercises.isEmpty) {
                    return SliverToBoxAdapter(
                      child: EmptyState(
                        icon: Icons.fitness_center_outlined,
                        title: isArabic ? 'لا توجد تمارين' : 'No exercises',
                        subtitle: isArabic
                            ? 'ابحث عن تمرين بالأعلى'
                            : 'Search for an exercise above',
                      ),
                    );
                  }
                  return SliverList(
                    delegate: SliverChildBuilderDelegate(
                      (context, index) => _ExerciseTile(
                        exercise: exercises[index],
                        isArabic: isArabic,
                      ),
                      childCount: exercises.length,
                    ),
                  );
                },
                loading: () => const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
                error: (error, _) => SliverToBoxAdapter(
                  child: AppErrorWidget(
                    message: error.toString(),
                    onRetry: () => ref.invalidate(exerciseLibraryProvider),
                  ),
                ),
              ),
            ],

            const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        ),
      ),
    );
  }
}

class _SearchBar extends ConsumerWidget {
  final bool isArabic;
  const _SearchBar({required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return TextField(
      decoration: InputDecoration(
        hintText: isArabic ? 'ابحث عن تمرين...' : 'Search exercises...',
        prefixIcon: const Icon(Icons.search),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
        ),
        filled: true,
      ),
      onChanged: (value) {
        ref.read(exerciseSearchQueryProvider.notifier).state = value;
      },
    );
  }
}

class _ExerciseTile extends StatelessWidget {
  final ExerciseModel exercise;
  final bool isArabic;

  const _ExerciseTile({required this.exercise, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final name = isArabic && exercise.nameAr != null ? exercise.nameAr! : exercise.name;
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 2),
      leading: Container(
        width: 40,
        height: 40,
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.secondaryContainer,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(
          Icons.fitness_center,
          size: 20,
          color: Theme.of(context).colorScheme.onSecondaryContainer,
        ),
      ),
      title: Text(name),
      subtitle: exercise.muscleGroups.isEmpty
          ? null
          : Text(exercise.muscleGroups.join(', ')),
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
    final title =
        log.workoutName ?? (isArabic ? 'تمرين حر' : 'Ad-hoc workout');

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
        title:
            Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(dateStr),
            if (log.durationMinutes != null)
              Text(
                '${log.durationMinutes} ${isArabic ? 'دقيقة' : 'min'} • ${log.sets.length} ${isArabic ? 'جولات' : 'sets'}',
                style: TextStyle(
                  color: Theme.of(context).colorScheme.primary,
                  fontWeight: FontWeight.w500,
                ),
              )
            else
              Text(
                '${log.sets.length} ${isArabic ? 'جولات' : 'sets'}',
              ),
          ],
        ),
      ),
    );
  }
}

class _WorkoutAssignmentTile extends StatelessWidget {
  final WorkoutAssignmentModel assignment;
  final bool isArabic;

  const _WorkoutAssignmentTile(
      {required this.assignment, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final workout = assignment.workout;
    final workoutId = workout?.id ?? assignment.workoutId;
    if (workoutId == null) return const SizedBox.shrink();

    return AppCard(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      child: ListTile(
        leading: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Colors.orange.withOpacity(0.1),
            borderRadius: BorderRadius.circular(10),
          ),
          child: const Icon(Icons.assignment, color: Colors.orange),
        ),
        title: Text(
          workout?.name ?? (isArabic ? 'تمرين مخصص' : 'Assigned Workout'),
          style: const TextStyle(fontWeight: FontWeight.w600),
        ),
        subtitle: Text(isArabic ? 'من المدرب' : 'From trainer'),
        trailing: FilledButton.tonal(
          onPressed: () => context
              .push('${AppRoutes.workouts}/guided?workoutId=$workoutId'),
          child: Text(isArabic ? 'ابدأ' : 'Start'),
        ),
      ),
    );
  }
}
