import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/nutrition_providers.dart';

class MealPlanBuilderScreen extends ConsumerStatefulWidget {
  const MealPlanBuilderScreen({super.key});

  @override
  ConsumerState<MealPlanBuilderScreen> createState() =>
      _MealPlanBuilderScreenState();
}

class _MealPlanBuilderScreenState
    extends ConsumerState<MealPlanBuilderScreen> {
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  int _durationDays = 7;
  int _targetCalories = 2000;
  bool _isSaving = false;

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<bool> _save() async {
    if (_nameController.text.trim().isEmpty) return false;
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }

      // Default macro split: 30% protein, 45% carbs, 25% fat.
      final proteinCalories = _targetCalories * 0.30;
      final carbsCalories = _targetCalories * 0.45;
      final fatCalories = _targetCalories * 0.25;
      final macros = {
        'proteinGrams': (proteinCalories / 4).round(),
        'carbsGrams': (carbsCalories / 4).round(),
        'fatGrams': (fatCalories / 9).round(),
      };

      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.mealPlans,
        data: {
          'trainerId': trainerId,
          'gymId': gymId,
          'name': _nameController.text.trim(),
          'description': _descriptionController.text.trim().isEmpty
              ? null
              : _descriptionController.text.trim(),
          'dailyCalorieTarget': _targetCalories,
          'macroTargets': macros,
          'days': <Map<String, dynamic>>[],
          'isTemplate': false,
        },
      );
      ref.invalidate(trainerMealPlansProvider);
      return true;
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
      return false;
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final mealPlansAsync = ref.watch(trainerMealPlansProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'خطط التغذية' : 'Meal Plans'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showCreateDialog(context, isArabic),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(trainerMealPlansProvider),
        child: mealPlansAsync.when(
          data: (plans) {
            if (plans.isEmpty) {
              return EmptyState(
                icon: Icons.restaurant_menu,
                title: isArabic ? 'لا توجد خطط تغذية' : 'No meal plans yet',
                subtitle: isArabic
                    ? 'انشئ خطة تغذية للعملاء'
                    : 'Create a nutrition plan for your clients',
              );
            }

            return ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: plans.length,
              itemBuilder: (context, index) {
                final plan = plans[index];
                return Card(
                  margin: const EdgeInsets.only(bottom: 8),
                  child: ListTile(
                    leading: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primary.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Icon(Icons.restaurant,
                          color: theme.colorScheme.primary, size: 20),
                    ),
                    title: Text(plan.name ?? ''),
                    subtitle: Text(
                      [
                        '${plan.totalCalories ?? 0} kcal',
                        if (plan.status.isNotEmpty) plan.status,
                      ].join(' • '),
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => _showPlanDetails(context, plan, isArabic),
                  ),
                );
              },
            );
          },
          loading: () => ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: 4,
            itemBuilder: (_, __) => const Padding(
              padding: EdgeInsets.only(bottom: 8),
              child: ShimmerCard(height: 72),
            ),
          ),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل خطط التغذية. يرجى المحاولة مرة أخرى.'
                : 'Could not load meal plans. Please try again.',
            onRetry: () => ref.invalidate(trainerMealPlansProvider),
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showCreateDialog(context, isArabic),
        icon: const Icon(Icons.add),
        label: Text(isArabic ? 'خطة جديدة' : 'New Plan'),
      ),
    );
  }

  void _showPlanDetails(
      BuildContext context, MealPlanModel plan, bool isArabic) {
    final theme = Theme.of(context);
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(plan.name ?? '', style: theme.textTheme.titleLarge),
              const SizedBox(height: 8),
              if (plan.description != null && plan.description!.isNotEmpty)
                Text(plan.description!, style: theme.textTheme.bodyMedium),
              const SizedBox(height: 16),
              _DetailLine(
                label: isArabic ? 'السعرات' : 'Calories',
                value: '${plan.totalCalories ?? 0} kcal',
              ),
              _DetailLine(
                label: isArabic ? 'الحالة' : 'Status',
                value: plan.status,
              ),
              if (plan.startDate != null)
                _DetailLine(
                  label: isArabic ? 'البداية' : 'Start',
                  value: DateFormatter.formatShort(plan.startDate!),
                ),
              if (plan.endDate != null)
                _DetailLine(
                  label: isArabic ? 'النهاية' : 'End',
                  value: DateFormatter.formatShort(plan.endDate!),
                ),
              const SizedBox(height: 16),
              AppButton(
                text: isArabic ? 'إغلاق' : 'Close',
                onPressed: () => Navigator.pop(ctx),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showCreateDialog(BuildContext context, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (ctx) => SingleChildScrollView(
        padding: EdgeInsets.only(
          left: 16,
          right: 16,
          top: 16,
          bottom: MediaQuery.of(ctx).viewInsets.bottom + 16,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                isArabic ? 'خطة تغذية جديدة' : 'New Meal Plan',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 16),
              AppTextField(
                label: isArabic ? 'اسم الخطة' : 'Plan Name',
                controller: _nameController,
              ),
              const SizedBox(height: 12),
              AppTextField(
                label: isArabic ? 'الوصف' : 'Description',
                controller: _descriptionController,
                maxLines: 2,
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isArabic ? 'المدة (ايام)' : 'Duration (days)',
                          style: Theme.of(context).textTheme.labelMedium,
                        ),
                        Row(
                          children: [
                            IconButton(
                              icon: const Icon(Icons.remove),
                              onPressed: _durationDays > 1
                                  ? () =>
                                      setState(() => _durationDays--)
                                  : null,
                            ),
                            Text('$_durationDays'),
                            IconButton(
                              icon: const Icon(Icons.add),
                              onPressed: () =>
                                  setState(() => _durationDays++),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isArabic ? 'السعرات' : 'Calories',
                          style: Theme.of(context).textTheme.labelMedium,
                        ),
                        Row(
                          children: [
                            IconButton(
                              icon: const Icon(Icons.remove),
                              onPressed: _targetCalories > 1200
                                  ? () => setState(
                                      () => _targetCalories -= 100)
                                  : null,
                            ),
                            Text('$_targetCalories'),
                            IconButton(
                              icon: const Icon(Icons.add),
                              onPressed: () => setState(
                                  () => _targetCalories += 100),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              AppButton(
                text: isArabic ? 'انشاء الخطة' : 'Create Plan',
                onPressed: _isSaving ? null : () async {
                  // Validate before dismissing — popping first meant an empty
                  // name silently did nothing and the save error/loading state
                  // could never reach the user.
                  if (_nameController.text.trim().isEmpty) {
                    ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                      content: Text(
                          isArabic ? 'ادخل اسم الخطة' : 'Enter a plan name'),
                    ));
                    return;
                  }
                  final ok = await _save();
                  if (ok && ctx.mounted) Navigator.pop(ctx);
                },
                isLoading: _isSaving,
              ),
              const SizedBox(height: 8),
            ],
          ),
        ),
    );
  }
}

class _DetailLine extends StatelessWidget {
  final String label;
  final String value;

  const _DetailLine({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Text(label, style: Theme.of(context).textTheme.bodySmall),
          const Spacer(),
          Text(
            value,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w500,
                ),
          ),
        ],
      ),
    );
  }
}
