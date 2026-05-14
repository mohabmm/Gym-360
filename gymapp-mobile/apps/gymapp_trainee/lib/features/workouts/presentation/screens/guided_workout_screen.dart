import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/workouts_providers.dart';

class GuidedWorkoutScreen extends ConsumerStatefulWidget {
  final String? workoutId;

  const GuidedWorkoutScreen({super.key, this.workoutId});

  @override
  ConsumerState<GuidedWorkoutScreen> createState() =>
      _GuidedWorkoutScreenState();
}

class _GuidedWorkoutScreenState extends ConsumerState<GuidedWorkoutScreen> {
  int? _reps;
  double? _weight;
  int? _rpe;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final guidedState = ref.watch(guidedWorkoutProvider);

    if (guidedState == null) {
      return _WorkoutLoader(workoutId: widget.workoutId, isArabic: isArabic);
    }

    if (guidedState.isComplete) {
      return _WorkoutCompleteScreen(state: guidedState, isArabic: isArabic);
    }

    final exercises = guidedState.workout.exercises;
    final exercise = exercises[guidedState.currentExerciseIndex];
    final exerciseName = exercise.exercise?.name ?? 'Exercise';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تمرين موجّه' : 'Guided Workout'),
        actions: [
          TextButton(
            onPressed: () {
              ref.read(guidedWorkoutProvider.notifier).endWorkout();
              context.pop();
            },
            child: Text(
              isArabic ? 'إنهاء' : 'End',
              style: const TextStyle(color: Colors.red),
            ),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Progress
            LinearProgressIndicator(
              value: (guidedState.currentExerciseIndex) / exercises.length,
              borderRadius: BorderRadius.circular(4),
            ),
            const SizedBox(height: 4),
            Text(
              '${isArabic ? 'تمرين' : 'Exercise'} ${guidedState.currentExerciseIndex + 1} / ${exercises.length}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 24),

            // Exercise name
            Text(
              exerciseName,
              style: Theme.of(context)
                  .textTheme
                  .headlineSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              '${isArabic ? 'الجولة' : 'Set'} ${guidedState.currentSetIndex + 1} / ${exercise.sets}',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                  ),
            ),

            if (exercise.reps != null && exercise.reps!.isNotEmpty)
              Text(
                '${isArabic ? 'التكرارات المستهدفة:' : 'Target reps:'} ${exercise.reps}',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            if (exercise.weight != null)
              Text(
                '${isArabic ? 'الوزن المقترح:' : 'Suggested weight:'} ${exercise.weight} kg',
                style: Theme.of(context).textTheme.bodyMedium,
              ),

            const SizedBox(height: 24),

            // Log inputs
            Row(
              children: [
                Expanded(
                  child: AppTextField(
                    label: isArabic ? 'التكرارات' : 'Reps',
                    hint: isArabic ? 'التكرارات' : 'Reps',
                    keyboardType: TextInputType.number,
                    inputFormatters: [
                      FilteringTextInputFormatter.digitsOnly,
                    ],
                    onChanged: (v) => _reps = int.tryParse(v),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: AppTextField(
                    label: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
                    hint: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
                    keyboardType: TextInputType.number,
                    inputFormatters: [
                      FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                    ],
                    onChanged: (v) => _weight = double.tryParse(v),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            AppTextField(
              label: isArabic ? 'مستوى الجهد (1-10)' : 'RPE (1-10)',
              hint: isArabic ? 'مستوى الجهد (1-10)' : 'RPE (1-10)',
              keyboardType: TextInputType.number,
              inputFormatters: [
                FilteringTextInputFormatter.digitsOnly,
              ],
              onChanged: (v) => _rpe = int.tryParse(v),
            ),

            const Spacer(),

            // Buttons
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () {
                      ref.read(guidedWorkoutProvider.notifier).skipExercise();
                    },
                    child: Text(isArabic ? 'تخطي' : 'Skip'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  flex: 2,
                  child: FilledButton(
                    onPressed: _completeSet,
                    child: Text(isArabic ? 'إتمام الجولة' : 'Complete Set'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  void _completeSet() {
    final guidedState = ref.read(guidedWorkoutProvider);
    if (guidedState == null) return;

    final exercise =
        guidedState.workout.exercises[guidedState.currentExerciseIndex];

    final setLog = LogSetModel(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      exerciseId: exercise.exerciseId,
      exerciseName: exercise.exercise?.name,
      setNumber: guidedState.currentSetIndex + 1,
      reps: _reps ?? int.tryParse(exercise.reps ?? ''),
      weight: _weight ?? exercise.weight,
      rpe: _rpe,
    );

    ref.read(guidedWorkoutProvider.notifier).completeSet(setLog);
  }
}

class _WorkoutLoader extends ConsumerWidget {
  final String? workoutId;
  final bool isArabic;

  const _WorkoutLoader({required this.workoutId, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (workoutId == null) {
      return Scaffold(
        appBar: AppBar(),
        body: Center(
          child: Text(isArabic ? 'لم يتم تحديد تمرين' : 'No workout specified'),
        ),
      );
    }

    final workoutAsync = ref.watch(workoutDetailProvider(workoutId!));

    return workoutAsync.when(
      data: (workout) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          ref.read(guidedWorkoutProvider.notifier).startWorkout(workout);
        });
        return Scaffold(
          appBar: AppBar(),
          body: const Center(child: CircularProgressIndicator()),
        );
      },
      loading: () => Scaffold(
        appBar: AppBar(),
        body: const Center(child: CircularProgressIndicator()),
      ),
      error: (e, _) => Scaffold(
        appBar: AppBar(),
        body: AppErrorWidget(message: e.toString()),
      ),
    );
  }
}

class _WorkoutCompleteScreen extends ConsumerWidget {
  final GuidedWorkoutState state;
  final bool isArabic;

  const _WorkoutCompleteScreen(
      {required this.state, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final duration =
        DateTime.now().difference(state.startTime).inMinutes;

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.check_circle, size: 80, color: Colors.green),
              const SizedBox(height: 16),
              Text(
                isArabic ? 'أحسنت!' : 'Great Job!',
                style: Theme.of(context)
                    .textTheme
                    .headlineMedium
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 8),
              Text(
                '${isArabic ? 'أنهيت' : 'Completed'} ${state.workout.name}',
                style: Theme.of(context).textTheme.bodyLarge,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                '$duration ${isArabic ? 'دقيقة' : 'minutes'}',
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      color: Theme.of(context).colorScheme.primary,
                      fontWeight: FontWeight.w700,
                    ),
              ),
              const SizedBox(height: 32),
              FilledButton(
                onPressed: () async {
                  Object? caughtError;
                  try {
                    await ref
                        .read(guidedWorkoutProvider.notifier)
                        .finishAndLog(ref);
                  } catch (e) {
                    caughtError = e;
                  }
                  if (context.mounted) {
                    context.go(AppRoutes.workouts);
                    if (caughtError != null) {
                      final isPlanError = caughtError is ApiException &&
                          caughtError.statusCode == 403;
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(isPlanError
                              ? (isArabic
                                  ? 'هذه الميزة غير متاحة في خطتك الحالية'
                                  : 'This feature is not available on your current plan')
                              : (isArabic
                                  ? 'تعذر حفظ التمرين'
                                  : 'Could not save workout')),
                          backgroundColor: Colors.red,
                        ),
                      );
                    } else {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(isArabic
                              ? 'تم حفظ التمرين بنجاح!'
                              : 'Workout saved!'),
                          backgroundColor: Colors.green,
                        ),
                      );
                    }
                  }
                },
                child: Text(isArabic ? 'حفظ وإنهاء' : 'Save & Finish'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
