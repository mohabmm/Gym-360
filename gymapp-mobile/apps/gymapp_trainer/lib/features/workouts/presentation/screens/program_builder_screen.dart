import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/workout_providers.dart';

class ProgramBuilderScreen extends ConsumerStatefulWidget {
  const ProgramBuilderScreen({super.key});

  @override
  ConsumerState<ProgramBuilderScreen> createState() =>
      _ProgramBuilderScreenState();
}

class _ProgramBuilderScreenState extends ConsumerState<ProgramBuilderScreen> {
  final _nameController = TextEditingController();
  final _nameArController = TextEditingController();
  final _descriptionController = TextEditingController();
  int _durationWeeks = 4;
  bool _isSaving = false;
  final Map<String, String?> _weekDayWorkouts = {};

  @override
  void dispose() {
    _nameController.dispose();
    _nameArController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_nameController.text.trim().isEmpty) return;
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }

      const dayNames = [
        'Sunday',
        'Monday',
        'Tuesday',
        'Wednesday',
        'Thursday',
        'Friday',
        'Saturday',
      ];

      // Group day-workouts per week.
      final weeksMap = <int, List<Map<String, dynamic>>>{};
      for (final entry
          in _weekDayWorkouts.entries.where((e) => e.value != null)) {
        final parts = entry.key.split('-');
        final weekNumber = int.parse(parts[0]);
        final dayOfWeek = int.parse(parts[1]);
        weeksMap.putIfAbsent(weekNumber, () => []).add({
          'dayOfWeek': dayNames[dayOfWeek.clamp(0, 6)],
          'workoutId': entry.value,
        });
      }

      final weeks = List.generate(_durationWeeks, (i) => i + 1)
          .map((w) => {
                'weekNumber': w,
                'dayWorkouts': weeksMap[w] ?? <Map<String, dynamic>>[],
              })
          .toList();

      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.programs,
        data: {
          'trainerId': trainerId,
          'gymId': gymId,
          'name': _nameController.text.trim(),
          if (_nameArController.text.trim().isNotEmpty)
            'nameAr': _nameArController.text.trim(),
          'description': _descriptionController.text.trim().isEmpty
              ? null
              : _descriptionController.text.trim(),
          'durationWeeks': _durationWeeks,
          'weeks': weeks,
          'isTemplate': false,
        },
      );
      ref.invalidate(trainerProgramsProvider);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final workoutsAsync = ref.watch(trainerWorkoutsProvider);

    final dayNames = isArabic
        ? ['احد', 'اثنين', 'ثلاثاء', 'اربعاء', 'خميس', 'جمعة', 'سبت']
        : ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'بناء برنامج' : 'Build Program'),
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2)),
            )
          else
            TextButton(
              onPressed: _save,
              child: Text(isArabic ? 'حفظ' : 'Save'),
            ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          AppTextField(
            label: isArabic ? 'اسم البرنامج (عربي)' : 'Program Name (Arabic)',
            controller: _nameArController,
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'اسم البرنامج (انجليزي)' : 'Program Name (English)',
            controller: _nameController,
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'الوصف' : 'Description',
            controller: _descriptionController,
            maxLines: 2,
          ),
          const SizedBox(height: 16),

          // Duration weeks
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                isArabic ? 'المدة (اسابيع)' : 'Duration (weeks)',
                style: theme.textTheme.titleSmall,
              ),
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.remove_circle_outline),
                    onPressed: _durationWeeks > 1
                        ? () => setState(() => _durationWeeks--)
                        : null,
                  ),
                  Text(
                    '$_durationWeeks',
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  IconButton(
                    icon: const Icon(Icons.add_circle_outline),
                    onPressed: _durationWeeks < 52
                        ? () => setState(() => _durationWeeks++)
                        : null,
                  ),
                ],
              ),
            ],
          ),
          const Divider(),
          const SizedBox(height: 8),

          // Weekly schedule grid
          Text(
            isArabic ? 'جدول الاسبوع' : 'Weekly Schedule',
            style: theme.textTheme.titleMedium
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          workoutsAsync.when(
            data: (workouts) => Column(
              children: List.generate(_durationWeeks, (weekIndex) {
                final week = weekIndex + 1;
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic ? 'الاسبوع $week' : 'Week $week',
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    ...List.generate(7, (dayIndex) {
                      final key = '$week-$dayIndex';
                      final selectedWorkoutId = _weekDayWorkouts[key];
                      final selectedWorkout = selectedWorkoutId != null
                          ? workouts
                              .where((w) => w.id == selectedWorkoutId)
                              .firstOrNull
                          : null;

                      return ListTile(
                        dense: true,
                        leading: Text(
                          dayNames[dayIndex],
                          style: theme.textTheme.labelMedium,
                        ),
                        title: Text(
                          selectedWorkout != null
                              ? (isArabic && selectedWorkout.nameAr != null
                                  ? selectedWorkout.nameAr!
                                  : selectedWorkout.name)
                              : (isArabic ? 'يوم راحة' : 'Rest day'),
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: selectedWorkout != null
                                ? theme.colorScheme.primary
                                : AppColors.grey400,
                          ),
                        ),
                        trailing: const Icon(Icons.edit, size: 16),
                        onTap: () => _pickWorkout(
                            context, key, workouts, isArabic),
                      );
                    }),
                    const Divider(),
                  ],
                );
              }),
            ),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => AppErrorWidget(message: e.toString()),
          ),

          const SizedBox(height: 24),
          AppButton(
            text: isArabic ? 'حفظ البرنامج' : 'Save Program',
            onPressed: _isSaving ? null : _save,
            isLoading: _isSaving,
          ),
        ],
      ),
    );
  }

  void _pickWorkout(
    BuildContext context,
    String key,
    List<WorkoutModel> workouts,
    bool isArabic,
  ) {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            isArabic ? 'اختر تمرينا' : 'Select Workout',
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          ListTile(
            title: Text(isArabic ? 'يوم راحة' : 'Rest day'),
            leading: const Icon(Icons.hotel),
            onTap: () {
              setState(() => _weekDayWorkouts[key] = null);
              Navigator.pop(ctx);
            },
          ),
          ...workouts.map(
            (w) => ListTile(
              title: Text(isArabic && w.nameAr != null ? w.nameAr! : w.name),
              leading: const Icon(Icons.fitness_center),
              onTap: () {
                setState(() => _weekDayWorkouts[key] = w.id);
                Navigator.pop(ctx);
              },
            ),
          ),
        ],
      ),
    );
  }
}
