import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/workout_providers.dart';

class WorkoutBuilderScreen extends ConsumerStatefulWidget {
  final String? workoutId;

  const WorkoutBuilderScreen({super.key, this.workoutId});

  @override
  ConsumerState<WorkoutBuilderScreen> createState() =>
      _WorkoutBuilderScreenState();
}

class _WorkoutBuilderScreenState extends ConsumerState<WorkoutBuilderScreen> {
  final _nameController = TextEditingController();
  final _nameArController = TextEditingController();
  final _descriptionController = TextEditingController();
  final List<_ExerciseEntry> _exercises = [];
  bool _isTemplate = false;
  bool _isSaving = false;
  bool _isHydrating = false;
  Object? _hydrationError;

  bool get _isEditing => widget.workoutId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) {
      _isHydrating = true;
      WidgetsBinding.instance.addPostFrameCallback((_) => _hydrate());
    }
  }

  Future<void> _hydrate() async {
    try {
      final workout = await ref
          .read(workoutDetailProvider(widget.workoutId!).future);
      // Resolve exercise display names from the library; falls back to a
      // generic label if a custom/deleted exercise isn't in the catalog.
      final library = await ref.read(exerciseLibraryProvider.future);
      final byId = {for (final e in library) e.id: e};
      if (!mounted) return;
      setState(() {
        _nameController.text = workout.name;
        _nameArController.text = workout.nameAr ?? '';
        _descriptionController.text = workout.description ?? '';
        _isTemplate = workout.isTemplate;
        _exercises
          ..clear()
          ..addAll(workout.exercises.map((e) {
            final ex = byId[e.exerciseId];
            final isArabic =
                Localizations.localeOf(context).languageCode == 'ar';
            final fallback = isArabic ? 'تمرين' : 'Exercise';
            final name = ex == null
                ? fallback
                : ExerciseLabels.name(ex, isArabic: isArabic);
            return _ExerciseEntry(
              exerciseId: e.exerciseId,
              exerciseName: name,
              sets: e.sets,
              reps: int.tryParse(e.reps ?? '') ?? 12,
              restSeconds: e.restSeconds ?? 60,
              notes: e.notes,
            );
          }));
        _isHydrating = false;
      });
    } catch (err) {
      if (!mounted) return;
      setState(() {
        _isHydrating = false;
        _hydrationError = err;
      });
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _nameArController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_nameController.text.trim().isEmpty) {
      final isArabic = Localizations.localeOf(context).languageCode == 'ar';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic ? 'أدخل اسم التمرين' : 'Enter a workout name'),
      ));
      return;
    }
    if (_exercises.isEmpty) {
      final isArabic = Localizations.localeOf(context).languageCode == 'ar';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isArabic
                ? 'أضف تمرينًا واحدًا على الأقل قبل الحفظ'
                : 'Add at least one exercise before saving',
          ),
        ),
      );
      return;
    }
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }

      final apiClient = ref.read(apiClientProvider);
      final exercises = _exercises
          .map((e) => {
                'exerciseId': e.exerciseId,
                'sets': e.sets,
                'reps': e.reps.toString(),
                'restSeconds': e.restSeconds,
                if (e.notes != null && e.notes!.isNotEmpty) 'notes': e.notes,
              })
          .toList();

      if (_isEditing) {
        await apiClient.putVoid(
          ApiEndpoints.workoutById(widget.workoutId!),
          data: {
            'name': _nameController.text.trim(),
            if (_nameArController.text.trim().isNotEmpty)
              'nameAr': _nameArController.text.trim(),
            'description': _descriptionController.text.trim().isEmpty
                ? null
                : _descriptionController.text.trim(),
            'exercises': exercises,
          },
        );
      } else {
        await apiClient.postVoid(
          ApiEndpoints.workouts,
          data: {
            'trainerId': trainerId,
            'gymId': gymId,
            'name': _nameController.text.trim(),
            if (_nameArController.text.trim().isNotEmpty)
              'nameAr': _nameArController.text.trim(),
            'description': _descriptionController.text.trim().isEmpty
                ? null
                : _descriptionController.text.trim(),
            'exercises': exercises,
            'isTemplate': _isTemplate,
          },
        );
      }

      ref.invalidate(trainerWorkoutsProvider);
      ref.invalidate(workoutTemplatesProvider);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException ? e.message : e.toString();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg)),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  void _addExercise(ExerciseModel exercise) {
    FocusManager.instance.primaryFocus?.unfocus();
    setState(() {
      _exercises.add(_ExerciseEntry(
        exerciseId: exercise.id,
        exerciseName: ExerciseLabels.name(
          exercise,
          isArabic: Localizations.localeOf(context).languageCode == 'ar',
        ),
        sets: 3,
        reps: 12,
        restSeconds: 60,
      ));
    });
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(
          _isEditing
              ? (isArabic ? 'تعديل التمرين' : 'Edit Workout')
              : (isArabic ? 'انشاء تمرين' : 'Create Workout'),
        ),
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            TextButton(
              onPressed: _save,
              child: Text(isArabic ? 'حفظ' : 'Save'),
            ),
        ],
      ),
      body: _isHydrating
          ? const Center(child: CircularProgressIndicator())
          : _hydrationError != null
              ? AppErrorWidget(
                  message: _hydrationError.toString(),
                  onRetry: () {
                    setState(() {
                      _isHydrating = true;
                      _hydrationError = null;
                    });
                    _hydrate();
                  },
                )
              : _buildForm(context, isArabic, theme),
    );
  }

  Widget _buildForm(BuildContext context, bool isArabic, ThemeData theme) {
    return ListView(
        padding: const EdgeInsets.all(16),
        children: [
          AppTextField(
            label: isArabic ? 'اسم التمرين (عربي)' : 'Workout Name (Arabic)',
            hint: isArabic ? 'مثال: تمرين الصدر' : 'e.g., تمرين الصدر',
            controller: _nameArController,
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'اسم التمرين (انجليزي)' : 'Workout Name (English)',
            hint: isArabic ? 'مثال: Chest Workout' : 'e.g., Chest Workout',
            controller: _nameController,
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'الوصف' : 'Description',
            hint: isArabic ? 'وصف مختصر (اختياري)' : 'Brief description (optional)',
            controller: _descriptionController,
            maxLines: 2,
          ),
          const SizedBox(height: 12),
          SwitchListTile(
            title: Text(isArabic ? 'حفظ كقالب' : 'Save as template'),
            subtitle: Text(
              isArabic
                  ? 'يمكن استخدامه مع عملاء آخرين'
                  : 'Can be reused with other clients',
            ),
            value: _isTemplate,
            onChanged: (v) => setState(() => _isTemplate = v),
            contentPadding: EdgeInsets.zero,
          ),
          const Divider(),
          const SizedBox(height: 8),

          // Exercises section
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                isArabic ? 'التمارين' : 'Exercises',
                style: theme.textTheme.titleMedium
                    ?.copyWith(fontWeight: FontWeight.w600),
              ),
              TextButton.icon(
                onPressed: () => _showExercisePicker(context, isArabic),
                icon: const Icon(Icons.add, size: 18),
                label: Text(isArabic ? 'اضف تمرين' : 'Add Exercise'),
              ),
            ],
          ),
          const SizedBox(height: 8),

          if (_exercises.isEmpty)
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: theme.colorScheme.surface,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: AppColors.grey200),
              ),
              child: Column(
                children: [
                  Icon(Icons.fitness_center,
                      size: 40, color: AppColors.grey300),
                  const SizedBox(height: 8),
                  Text(
                    isArabic
                        ? 'اضف تمارين لبناء روتين التمرين'
                        : 'Add exercises to build the workout routine',
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(color: AppColors.grey400),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            )
          else
            ReorderableListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: _exercises.length,
              onReorder: (oldIndex, newIndex) {
                setState(() {
                  if (newIndex > oldIndex) newIndex--;
                  final item = _exercises.removeAt(oldIndex);
                  _exercises.insert(newIndex, item);
                });
              },
              itemBuilder: (context, index) {
                final ex = _exercises[index];
                return _ExerciseRow(
                  key: ValueKey('$index-${ex.exerciseId}'),
                  entry: ex,
                  isArabic: isArabic,
                  onChanged: (updated) {
                    setState(() => _exercises[index] = updated);
                  },
                  onDelete: () {
                    setState(() => _exercises.removeAt(index));
                  },
                );
              },
            ),

          const SizedBox(height: 24),
          AppButton(
            text: isArabic ? 'حفظ التمرين' : 'Save Workout',
            onPressed: _isSaving ? null : _save,
            isLoading: _isSaving,
          ),
        ],
      );
  }

  void _showExercisePicker(BuildContext context, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (ctx) => _ExercisePickerSheet(
        isArabic: isArabic,
        onSelected: (exercise) {
          Navigator.of(ctx).pop();
          _addExercise(exercise);
        },
      ),
    );
  }
}

class _ExerciseEntry {
  final String exerciseId;
  final String exerciseName;
  int sets;
  int reps;
  int restSeconds;
  String? notes;

  _ExerciseEntry({
    required this.exerciseId,
    required this.exerciseName,
    required this.sets,
    required this.reps,
    required this.restSeconds,
    this.notes,
  });

  _ExerciseEntry copyWith({
    int? sets,
    int? reps,
    int? restSeconds,
    String? notes,
  }) =>
      _ExerciseEntry(
        exerciseId: exerciseId,
        exerciseName: exerciseName,
        sets: sets ?? this.sets,
        reps: reps ?? this.reps,
        restSeconds: restSeconds ?? this.restSeconds,
        notes: notes ?? this.notes,
      );
}

class _ExerciseRow extends StatelessWidget {
  final _ExerciseEntry entry;
  final bool isArabic;
  final ValueChanged<_ExerciseEntry> onChanged;
  final VoidCallback onDelete;

  const _ExerciseRow({
    super.key,
    required this.entry,
    required this.isArabic,
    required this.onChanged,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    entry.exerciseName,
                    style: theme.textTheme.titleSmall
                        ?.copyWith(fontWeight: FontWeight.w600),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.delete_outline,
                      color: AppColors.error, size: 20),
                  onPressed: onDelete,
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(),
                ),
                const SizedBox(width: 8),
                const Icon(Icons.drag_handle, color: AppColors.grey400),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                _CounterField(
                  label: isArabic ? 'مجموعات' : 'Sets',
                  value: entry.sets,
                  onChanged: (v) => onChanged(entry.copyWith(sets: v)),
                ),
                const SizedBox(width: 4),
                _CounterField(
                  label: isArabic ? 'تكرار' : 'Reps',
                  value: entry.reps,
                  onChanged: (v) => onChanged(entry.copyWith(reps: v)),
                ),
                const SizedBox(width: 4),
                _CounterField(
                  label: isArabic ? 'راحة (ث)' : 'Rest (s)',
                  value: entry.restSeconds,
                  step: 15,
                  onChanged: (v) =>
                      onChanged(entry.copyWith(restSeconds: v)),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CounterField extends StatelessWidget {
  final String label;
  final int value;
  final int step;
  final ValueChanged<int> onChanged;

  const _CounterField({
    required this.label,
    required this.value,
    this.step = 1,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Expanded(
      child: Column(
        children: [
          Text(label, style: theme.textTheme.labelSmall),
          const SizedBox(height: 4),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SizedBox(
                width: 28,
                height: 28,
                child: IconButton(
                  icon: const Icon(Icons.remove, size: 14),
                  onPressed: value > step
                      ? () => onChanged(value - step)
                      : null,
                  padding: EdgeInsets.zero,
                  style: IconButton.styleFrom(
                    minimumSize: const Size(28, 28),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                ),
              ),
              Text('$value',
                  style: theme.textTheme.titleSmall
                      ?.copyWith(fontWeight: FontWeight.w700)),
              SizedBox(
                width: 28,
                height: 28,
                child: IconButton(
                  icon: const Icon(Icons.add, size: 14),
                  onPressed: () => onChanged(value + step),
                  padding: EdgeInsets.zero,
                  style: IconButton.styleFrom(
                    minimumSize: const Size(28, 28),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ExercisePickerSheet extends ConsumerStatefulWidget {
  final bool isArabic;
  final ValueChanged<ExerciseModel> onSelected;

  const _ExercisePickerSheet({
    required this.isArabic,
    required this.onSelected,
  });

  @override
  ConsumerState<_ExercisePickerSheet> createState() =>
      _ExercisePickerSheetState();
}

class _ExercisePickerSheetState
    extends ConsumerState<_ExercisePickerSheet> {
  String _searchQuery = '';
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final exercisesAsync = ref.watch(exerciseLibraryProvider);

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.75,
      maxChildSize: 0.95,
      builder: (context, scrollController) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
            child: Column(
              children: [
                Container(
                  width: 40,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: 12),
                  decoration: BoxDecoration(
                    color: AppColors.grey300,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
                Text(
                  widget.isArabic
                      ? 'اختر تمرينا'
                      : 'Select Exercise',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 12),
                AppSearchField(
                  controller: _searchController,
                  hint: widget.isArabic
                      ? 'بحث في التمارين...'
                      : 'Search exercises...',
                  onChanged: (v) => setState(() => _searchQuery = v),
                  onClear: () => setState(() {
                    _searchQuery = '';
                    _searchController.clear();
                  }),
                ),
              ],
            ),
          ),
          Expanded(
            child: exercisesAsync.when(
              data: (exercises) {
                final filtered = _searchQuery.isEmpty
                    ? exercises
                    : exercises
                        .where((e) =>
                            ExerciseLabels.matches(e, _searchQuery))
                        .toList();

                return ListView.builder(
                  controller: scrollController,
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: filtered.length,
                  itemBuilder: (context, index) {
                    final exercise = filtered[index];
                    return ListTile(
                      title: Text(ExerciseLabels.name(
                        exercise,
                        isArabic: widget.isArabic,
                      )),
                      subtitle: Text(ExerciseLabels.muscleGroups(
                        exercise.muscleGroups,
                        isArabic: widget.isArabic,
                      )),
                      onTap: () => widget.onSelected(exercise),
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
    );
  }
}
