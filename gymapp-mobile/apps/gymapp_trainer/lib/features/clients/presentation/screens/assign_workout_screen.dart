import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../workouts/presentation/providers/workout_providers.dart';

class AssignWorkoutScreen extends ConsumerStatefulWidget {
  final String clientId;

  const AssignWorkoutScreen({super.key, required this.clientId});

  @override
  ConsumerState<AssignWorkoutScreen> createState() =>
      _AssignWorkoutScreenState();
}

class _AssignWorkoutScreenState extends ConsumerState<AssignWorkoutScreen> {
  String? _assigningId;

  Future<void> _assign(WorkoutModel workout) async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    setState(() => _assigningId = workout.id);
    try {
      final apiClient = ref.read(apiClientProvider);
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }
      final today = DateTime.now();
      final startDate =
          '${today.year}-${today.month.toString().padLeft(2, '0')}-${today.day.toString().padLeft(2, '0')}T00:00:00';
      await apiClient.postVoid(
        ApiEndpoints.assignWorkout,
        data: {
          'clientId': widget.clientId,
          'workoutId': workout.id,
          'trainerId': trainerId,
          'gymId': gymId,
          'startDate': startDate,
        },
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'تم تعيين التمرين بنجاح'
              : 'Workout assigned successfully'),
        ));
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException ? e.message : e.toString();
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => _assigningId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final workoutsAsync = ref.watch(trainerWorkoutsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تعيين تمرين' : 'Assign Workout'),
      ),
      body: workoutsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Text(e is ApiException ? e.message : e.toString()),
        ),
        data: (workouts) {
          if (workouts.isEmpty) {
            return Center(
              child: Text(isArabic
                  ? 'لا توجد تمارين. أنشئ تمريناً أولاً.'
                  : 'No workouts. Create one first.'),
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: workouts.length,
            separatorBuilder: (_, __) => const SizedBox(height: 8),
            itemBuilder: (context, index) {
              final w = workouts[index];
              final isAssigning = _assigningId == w.id;
              return Card(
                child: ListTile(
                  title: Text(isArabic && w.nameAr != null && w.nameAr!.isNotEmpty
                      ? w.nameAr!
                      : w.name),
                  subtitle: Text(
                    isArabic
                        ? '${w.exerciseCount ?? w.exercises.length} تمرين'
                        : '${w.exerciseCount ?? w.exercises.length} exercises',
                  ),
                  trailing: SizedBox(
                    width: 72,
                    child: isAssigning
                        ? const Center(
                            child: SizedBox(
                              width: 24,
                              height: 24,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            ),
                          )
                        : ElevatedButton(
                            onPressed: _assigningId != null
                                ? null
                                : () => _assign(w),
                            style: ElevatedButton.styleFrom(
                              padding: EdgeInsets.zero,
                            ),
                            child: Text(isArabic ? 'تعيين' : 'Assign'),
                          ),
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
