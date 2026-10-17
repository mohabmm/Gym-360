import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/workouts_providers.dart';

class AdHocWorkoutScreen extends ConsumerStatefulWidget {
  const AdHocWorkoutScreen({super.key});

  @override
  ConsumerState<AdHocWorkoutScreen> createState() =>
      _AdHocWorkoutScreenState();
}

class _AdHocExerciseGroup {
  final ExerciseModel exercise;
  final List<_AdHocSet> sets;

  _AdHocExerciseGroup({required this.exercise, List<_AdHocSet>? sets})
      : sets = sets ?? [_AdHocSet()];
}

class _AdHocSet {
  final TextEditingController reps = TextEditingController();
  final TextEditingController weight = TextEditingController();
  final TextEditingController rpe = TextEditingController();

  void dispose() {
    reps.dispose();
    weight.dispose();
    rpe.dispose();
  }
}

class _AdHocWorkoutScreenState extends ConsumerState<AdHocWorkoutScreen> {
  final List<_AdHocExerciseGroup> _groups = [];
  final DateTime _startTime = DateTime.now();
  bool _submitting = false;

  @override
  void dispose() {
    for (final g in _groups) {
      for (final s in g.sets) {
        s.dispose();
      }
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تمرين حر' : 'Ad-hoc Workout'),
      ),
      body: _groups.isEmpty
          ? _EmptyState(isArabic: isArabic, onAdd: _openExercisePicker)
          : ListView.builder(
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: const EdgeInsets.all(16),
              itemCount: _groups.length + 1,
              itemBuilder: (context, index) {
                if (index == _groups.length) {
                  return Padding(
                    padding: const EdgeInsets.only(top: 8, bottom: 80),
                    child: OutlinedButton.icon(
                      onPressed: _openExercisePicker,
                      icon: const Icon(Icons.add),
                      label: Text(
                        isArabic ? 'إضافة تمرين' : 'Add Exercise',
                      ),
                    ),
                  );
                }
                return _ExerciseGroupCard(
                  group: _groups[index],
                  isArabic: isArabic,
                  onAddSet: () => setState(
                    () => _groups[index].sets.add(_AdHocSet()),
                  ),
                  onRemoveSet: (setIndex) => setState(() {
                    _groups[index].sets[setIndex].dispose();
                    _groups[index].sets.removeAt(setIndex);
                  }),
                  onRemoveExercise: () => setState(() {
                    for (final s in _groups[index].sets) {
                      s.dispose();
                    }
                    _groups.removeAt(index);
                  }),
                );
              },
            ),
      bottomNavigationBar: _groups.isEmpty
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: _submitting ? null : _finish,
                    child: _submitting
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : Text(
                            isArabic ? 'حفظ وإنهاء' : 'Save & Finish',
                          ),
                  ),
                ),
              ),
            ),
    );
  }

  void _openExercisePicker() {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _ExercisePickerSheet(
        onPick: (exercise) {
          setState(() {
            _groups.add(_AdHocExerciseGroup(exercise: exercise));
          });
          Navigator.of(context).pop();
        },
      ),
    );
  }

  Future<void> _finish() async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final sets = <Map<String, dynamic>>[];

    for (final group in _groups) {
      var setNumber = 0;
      for (final s in group.sets) {
        final reps = int.tryParse(s.reps.text.trim());
        if (reps == null || reps < 1) continue;
        setNumber++;
        final weight = double.tryParse(s.weight.text.trim());
        sets.add({
          'exerciseId': group.exercise.id,
          'setNumber': setNumber,
          'reps': reps,
          if (weight != null) 'weight': weight,
          'weightUnit': 'kg',
          'isCompleted': true,
        });
      }
    }

    if (sets.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isArabic
                ? 'أضف جولة واحدة على الأقل بعدد تكرارات'
                : 'Add at least one set with reps',
          ),
        ),
      );
      return;
    }

    final avgRpe = _collectAverageRpe();
    final now = DateTime.now();
    final elapsedSeconds = now.difference(_startTime).inSeconds;
    final durationMinutes = elapsedSeconds <= 0 ? 1 : ((elapsedSeconds + 59) ~/ 60);

    setState(() => _submitting = true);
    try {
      final repo = ref.read(workoutsRepositoryProvider);
      await repo.logWorkout({
        'date': _formatDate(now),
        'startedAt': _startTime.toUtc().toIso8601String(),
        'completedAt': now.toUtc().toIso8601String(),
        'durationMinutes': durationMinutes,
        'sets': sets,
        if (avgRpe != null) 'perceivedExertion': avgRpe,
      });
      ref.invalidate(workoutLogsProvider);
      ref.invalidate(workoutsProvider);
      if (mounted) {
        context.go(AppRoutes.workouts);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              isArabic ? 'تم حفظ التمرين بنجاح!' : 'Workout saved!',
            ),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException && e.statusCode == 403
            ? (isArabic
                ? 'هذه الميزة غير متاحة في خطتك الحالية'
                : 'This feature is not available on your current plan')
            : (isArabic ? 'تعذر حفظ التمرين' : 'Could not save workout');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  int? _collectAverageRpe() {
    final values = <int>[];
    for (final g in _groups) {
      for (final s in g.sets) {
        final v = int.tryParse(s.rpe.text.trim());
        if (v != null && v >= 1 && v <= 10) values.add(v);
      }
    }
    if (values.isEmpty) return null;
    final avg = values.reduce((a, b) => a + b) / values.length;
    return avg.round().clamp(1, 10);
  }
}

String _formatDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-'
    '${d.month.toString().padLeft(2, '0')}-'
    '${d.day.toString().padLeft(2, '0')}';

class _EmptyState extends StatelessWidget {
  final bool isArabic;
  final VoidCallback onAdd;

  const _EmptyState({required this.isArabic, required this.onAdd});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.fitness_center,
              size: 64,
              color: Theme.of(context).colorScheme.primary,
            ),
            const SizedBox(height: 16),
            Text(
              isArabic
                  ? 'ابدأ بإضافة تمرين من المكتبة'
                  : 'Start by adding an exercise from the library',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge,
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: onAdd,
              icon: const Icon(Icons.add),
              label: Text(isArabic ? 'إضافة تمرين' : 'Add Exercise'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ExerciseGroupCard extends StatelessWidget {
  final _AdHocExerciseGroup group;
  final bool isArabic;
  final VoidCallback onAddSet;
  final void Function(int) onRemoveSet;
  final VoidCallback onRemoveExercise;

  const _ExerciseGroupCard({
    required this.group,
    required this.isArabic,
    required this.onAddSet,
    required this.onRemoveSet,
    required this.onRemoveExercise,
  });

  @override
  Widget build(BuildContext context) {
    final name = isArabic && group.exercise.nameAr != null
        ? group.exercise.nameAr!
        : group.exercise.name;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    name,
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.delete_outline),
                  onPressed: onRemoveExercise,
                  tooltip: isArabic ? 'حذف التمرين' : 'Remove exercise',
                ),
              ],
            ),
            const SizedBox(height: 8),
            for (int i = 0; i < group.sets.length; i++)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  children: [
                    SizedBox(
                      width: 32,
                      child: Text(
                        '${i + 1}',
                        style: Theme.of(context).textTheme.titleSmall,
                      ),
                    ),
                    Expanded(
                      child: AppTextField(
                        label: isArabic ? 'تكرارات' : 'Reps',
                        hint: isArabic ? 'تكرارات' : 'Reps',
                        controller: group.sets[i].reps,
                        keyboardType: TextInputType.number,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: AppTextField(
                        label: isArabic ? 'كجم' : 'kg',
                        hint: isArabic ? 'كجم' : 'kg',
                        controller: group.sets[i].weight,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        inputFormatters: [
                          FilteringTextInputFormatter.allow(
                              RegExp(r'[0-9.]')),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: AppTextField(
                        label: 'RPE',
                        hint: 'RPE',
                        controller: group.sets[i].rpe,
                        keyboardType: TextInputType.number,
                        inputFormatters: [
                          FilteringTextInputFormatter.digitsOnly,
                        ],
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close, size: 20),
                      tooltip: isArabic ? 'احذف الجولة' : 'Remove set',
                      onPressed: group.sets.length > 1
                          ? () => onRemoveSet(i)
                          : null,
                    ),
                  ],
                ),
              ),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: TextButton.icon(
                onPressed: onAddSet,
                icon: const Icon(Icons.add, size: 18),
                label: Text(isArabic ? 'إضافة جولة' : 'Add Set'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ExercisePickerSheet extends ConsumerStatefulWidget {
  final void Function(ExerciseModel) onPick;

  const _ExercisePickerSheet({required this.onPick});

  @override
  ConsumerState<_ExercisePickerSheet> createState() =>
      _ExercisePickerSheetState();
}

class _ExercisePickerSheetState extends ConsumerState<_ExercisePickerSheet> {
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final exercisesAsync = ref.watch(exerciseLibraryProvider);

    return DraggableScrollableSheet(
      initialChildSize: 0.75,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom,
        ),
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: AppTextField(
                controller: _searchController,
                hint: isArabic ? 'ابحث عن تمرين' : 'Search exercises',
                prefixIcon: const Icon(Icons.search),
                onChanged: (v) {
                  ref.read(exerciseSearchQueryProvider.notifier).state = v;
                },
              ),
            ),
            Expanded(
              child: exercisesAsync.when(
                data: (exercises) {
                  if (exercises.isEmpty) {
                    return Center(
                      child: Text(
                        isArabic ? 'لا توجد نتائج' : 'No results',
                      ),
                    );
                  }
                  return ListView.builder(
                    controller: scrollController,
                    itemCount: exercises.length,
                    itemBuilder: (_, i) {
                      final ex = exercises[i];
                      return ListTile(
                        title: Text(
                          isArabic && ex.nameAr != null ? ex.nameAr! : ex.name,
                        ),
                        subtitle: ex.muscleGroups.isEmpty
                            ? null
                            : Text(ExerciseLabels.muscleGroups(
                                ex.muscleGroups,
                                language: AppLanguage.of(context),
                              )),
                        onTap: () => widget.onPick(ex),
                      );
                    },
                  );
                },
                loading: () =>
                    const Center(child: CircularProgressIndicator()),
                error: (e, _) => AppErrorWidget(message: e.toString()),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
