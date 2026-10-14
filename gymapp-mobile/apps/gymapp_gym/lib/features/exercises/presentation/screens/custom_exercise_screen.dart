import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/exercises_provider.dart';
import '../../../../core/error_text.dart';

class CustomExerciseScreen extends ConsumerStatefulWidget {
  const CustomExerciseScreen({super.key});

  @override
  ConsumerState<CustomExerciseScreen> createState() =>
      _CustomExerciseScreenState();
}

class _CustomExerciseScreenState extends ConsumerState<CustomExerciseScreen> {
  final _nameController = TextEditingController();
  final _descController = TextEditingController();
  bool _adding = false;

  @override
  void dispose() {
    _nameController.dispose();
    _descController.dispose();
    super.dispose();
  }

  Future<void> _addExercise() async {
    if (_nameController.text.isEmpty) return;
    setState(() => _adding = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.post<void>(
        ApiEndpoints.exercises,
        data: {
          'name': _nameController.text.trim(),
          'description': _descController.text.trim(),
          'muscleGroups': ['FullBody'],
          'equipment': 'None',
          'category': 'Strength',
          'scope': 'GymSpecific',
          'gymId': gymId,
        },
        parser: (_) {},
      );
      ref.invalidate(customExercisesProvider);
      _nameController.clear();
      _descController.clear();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    } finally {
      if (mounted) setState(() => _adding = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final exercises = ref.watch(customExercisesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'التمارين المخصصة' : 'Custom Exercises'),
      ),
      // Business-only (PRODUCT_SPEC §4.1). Lower plans get the upgrade prompt
      // instead of a form the backend will reject.
      body: FeatureGate(
        featureCode: FeatureCodes.customExerciseLibrary,
        child: Column(
          children: [
            // Add form
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  AppTextField(
                    controller: _nameController,
                    label: isArabic ? 'اسم التمرين' : 'Exercise Name',
                  ),
                  const SizedBox(height: 8),
                  AppTextField(
                    controller: _descController,
                    label:
                        isArabic ? 'الوصف (اختياري)' : 'Description (optional)',
                  ),
                  const SizedBox(height: 8),
                  AppButton(
                    text: isArabic ? 'اضافة تمرين' : 'Add Exercise',
                    isLoading: _adding,
                    onPressed: _addExercise,
                  ),
                ],
              ),
            ),
            const Divider(),
            Expanded(
              child: exercises.when(
                data: (items) {
                  if (items.isEmpty) {
                    return EmptyState(
                      icon: Icons.fitness_center_outlined,
                      title: isArabic
                          ? 'لا توجد تمارين مخصصة'
                          : 'No custom exercises',
                    );
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: items.length,
                    itemBuilder: (context, index) {
                      final ex = items[index];
                      return AppCard(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(ex.name),
                          subtitle: ex.description != null
                              ? Text(ex.description!)
                              : null,
                        ),
                      );
                    },
                  );
                },
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => AppErrorWidget(
                  message: isArabic ? 'خطأ في التحميل' : 'Error loading',
                  onRetry: () => ref.invalidate(customExercisesProvider),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
