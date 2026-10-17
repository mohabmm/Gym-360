import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/workout_providers.dart';

class ExerciseManagerScreen extends ConsumerStatefulWidget {
  const ExerciseManagerScreen({super.key});

  @override
  ConsumerState<ExerciseManagerScreen> createState() =>
      _ExerciseManagerScreenState();
}

class _ExerciseManagerScreenState
    extends ConsumerState<ExerciseManagerScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  String _searchQuery = '';
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'مكتبة التمارين' : 'Exercise Library'),
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(text: isArabic ? 'جميع التمارين' : 'All Exercises'),
            Tab(text: isArabic ? 'تماريني المخصصة' : 'My Custom'),
          ],
        ),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppSearchField(
              controller: _searchController,
              hint: isArabic ? 'بحث...' : 'Search...',
              onChanged: (v) => setState(() => _searchQuery = v),
              onClear: () => setState(() {
                _searchQuery = '';
                _searchController.clear();
              }),
            ),
          ),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [
                _AllExercisesTab(
                    searchQuery: _searchQuery, isArabic: isArabic),
                _CustomExercisesTab(
                    searchQuery: _searchQuery, isArabic: isArabic),
              ],
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _onCreateExercisePressed(context, isArabic),
        child: const Icon(Icons.add),
        tooltip: isArabic ? 'تمرين مخصص' : 'Custom Exercise',
      ),
    );
  }

  // Custom exercises are plan-gated on the BE (`custom_exercise_library`).
  // Check up front so the trainer is told why instead of filling in the form
  // and having the save rejected with a 403.
  Future<void> _onCreateExercisePressed(
      BuildContext context, bool isArabic) async {
    var enabled = true;
    try {
      final features = await ref.read(trainerPlanFeaturesProvider.future);
      enabled = features.isEnabled(_customExerciseFeature);
    } catch (_) {
      // Unknown plan state: let the BE decide; the form surfaces its answer.
    }
    if (!context.mounted) return;
    if (!enabled) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(_featureUnavailableMessage(isArabic))),
      );
      return;
    }
    _showCreateExerciseDialog(context, isArabic);
  }

  void _showCreateExerciseDialog(BuildContext context, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _CreateExerciseDialog(isArabic: isArabic),
    );
  }
}

const _customExerciseFeature = 'custom_exercise_library';

String _featureUnavailableMessage(bool isArabic) => isArabic
    ? 'التمارين المخصصة غير متاحة في باقة الصالة الحالية. تواصل مع ادارة الصالة للترقية.'
    : "Custom exercises aren't included in your gym's current plan. Ask the gym to upgrade.";

// Matches backend enum names in src/Modules/Training/.../Enums — these must
// be exact: the API Parse<T> is case-insensitive but the string has to map to
// an enum member. Empty muscleGroups is rejected by CreateExerciseCommandValidator.
const _muscleGroups = <String>[
  'Chest', 'Back', 'Shoulders', 'Biceps', 'Triceps',
  'Legs', 'Glutes', 'Core', 'Cardio', 'FullBody',
];
const _equipmentOptions = <String>[
  'Barbell', 'Dumbbell', 'Machine', 'Cable', 'Bodyweight',
  'Kettlebell', 'Band', 'Other', 'None',
];
const _categoryOptions = <String>[
  'Strength', 'Cardio', 'Flexibility', 'Plyometric',
];

class _CreateExerciseDialog extends ConsumerStatefulWidget {
  final bool isArabic;

  const _CreateExerciseDialog({required this.isArabic});

  @override
  ConsumerState<_CreateExerciseDialog> createState() =>
      _CreateExerciseDialogState();
}

class _CreateExerciseDialogState extends ConsumerState<_CreateExerciseDialog> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _nameArController = TextEditingController();
  final Set<String> _selectedMuscles = {};
  String _equipment = 'None';
  String _category = 'Strength';
  bool _muscleGroupError = false;
  bool _submitting = false;
  String? _submitError;

  @override
  void dispose() {
    _nameController.dispose();
    _nameArController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    final authState = ref.watch(authProvider);
    final trainerId = authState.user?.id;
    final gymId = ref.watch(activeGymIdProvider);
    final missingContext = trainerId == null || gymId == null;

    return SingleChildScrollView(
      padding: EdgeInsets.only(
        left: 16,
        right: 16,
        top: 16,
        bottom: MediaQuery.of(context).viewInsets.bottom + 16,
      ),
      child: Form(
        key: _formKey,
        autovalidateMode: AutovalidateMode.onUserInteraction,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isArabic ? 'تمرين مخصص جديد' : 'New Custom Exercise',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            if (missingContext)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 12),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  isArabic
                      ? 'لا يوجد مدرب مسجل الدخول أو نادي نشط. يرجى تسجيل الدخول واختيار نادي.'
                      : 'No authenticated trainer or active gym. Please sign in and select a gym.',
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.onErrorContainer,
                  ),
                ),
              ),
            AppTextField(
              label: isArabic ? 'الاسم (عربي)' : 'Name (Arabic)',
              controller: _nameArController,
              maxLength: 200,
            ),
            const SizedBox(height: 8),
            AppTextField(
              label: isArabic ? 'الاسم (انجليزي)' : 'Name (English)',
              controller: _nameController,
              maxLength: 200,
              validator: (v) {
                if (v == null || v.trim().isEmpty) {
                  return isArabic
                      ? 'الاسم الانجليزي مطلوب'
                      : 'English name is required';
                }
                return null;
              },
            ),
            const SizedBox(height: 12),
            Text(
              isArabic ? 'المجموعات العضلية *' : 'Muscle Groups *',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 6),
            Wrap(
              spacing: 6,
              runSpacing: 4,
              children: _muscleGroups.map((m) {
                final selected = _selectedMuscles.contains(m);
                return FilterChip(
                  label: Text(ExerciseLabels.muscleGroup(m, language: AppLanguage.of(context))),
                  selected: selected,
                  onSelected: (on) => setState(() {
                    if (on) {
                      _selectedMuscles.add(m);
                    } else {
                      _selectedMuscles.remove(m);
                    }
                    _muscleGroupError = _selectedMuscles.isEmpty;
                  }),
                );
              }).toList(),
            ),
            if (_muscleGroupError)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  isArabic
                      ? 'اختر مجموعة عضلية واحدة على الاقل'
                      : 'Select at least one muscle group',
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.error,
                    fontSize: 12,
                  ),
                ),
              ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              value: _equipment,
              decoration: InputDecoration(
                labelText: isArabic ? 'المعدات' : 'Equipment',
              ),
              items: _equipmentOptions
                  .map((e) => DropdownMenuItem(
                        value: e,
                        child: Text(
                            ExerciseLabels.equipment(e, language: AppLanguage.of(context))),
                      ))
                  .toList(),
              onChanged: (v) => setState(() => _equipment = v ?? 'None'),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              value: _category,
              decoration: InputDecoration(
                labelText: isArabic ? 'التصنيف' : 'Category',
              ),
              items: _categoryOptions
                  .map((c) => DropdownMenuItem(
                        value: c,
                        child: Text(
                            ExerciseLabels.category(c, language: AppLanguage.of(context))),
                      ))
                  .toList(),
              onChanged: (v) => setState(() => _category = v ?? 'Strength'),
            ),
            const SizedBox(height: 16),
            // Shown inline: a SnackBar would render on the Scaffold underneath
            // this sheet, i.e. invisible, so a failed save looked like a no-op.
            if (_submitError != null) ...[
              Text(
                _submitError!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
              const SizedBox(height: 12),
            ],
            AppButton(
              text: isArabic ? 'اضافة' : 'Add',
              onPressed: _submitting || missingContext
                  ? null
                  : () => _submit(trainerId, gymId),
              isLoading: _submitting,
            ),
            const SizedBox(height: 8),
            Center(
              child: TextButton(
                onPressed: _submitting ? null : () => Navigator.pop(context),
                child: Text(isArabic ? 'الغاء' : 'Cancel'),
              ),
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }

  Future<void> _submit(String trainerId, String gymId) async {
    final isArabic = widget.isArabic;
    final isFormValid = _formKey.currentState?.validate() ?? false;
    final hasMuscles = _selectedMuscles.isNotEmpty;
    setState(() => _muscleGroupError = !hasMuscles);
    if (!isFormValid || !hasMuscles) return;

    setState(() {
      _submitting = true;
      _submitError = null;
    });
    try {
      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.exercises,
        data: {
          'name': _nameController.text.trim(),
          if (_nameArController.text.trim().isNotEmpty)
            'nameAr': _nameArController.text.trim(),
          'muscleGroups': _selectedMuscles.toList(),
          'equipment': _equipment,
          'category': _category,
          'scope': 'GymSpecific',
          'gymId': gymId,
          'createdBy': trainerId,
        },
      );
      ref.invalidate(customExercisesProvider);
      ref.invalidate(exerciseLibraryProvider);
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(isArabic ? 'تم اضافة التمرين' : 'Exercise added'),
        ),
      );
    } catch (e) {
      if (!mounted) return;
      final String reason;
      if (e is FeatureGatedException) {
        reason = _featureUnavailableMessage(isArabic);
      } else {
        final detail = e is ApiException ? e.message : e.toString();
        reason = isArabic ? 'فشل الحفظ: $detail' : 'Failed to save: $detail';
      }
      setState(() {
        _submitting = false;
        _submitError = reason;
      });
    }
  }
}

class _AllExercisesTab extends ConsumerWidget {
  final String searchQuery;
  final bool isArabic;

  const _AllExercisesTab(
      {required this.searchQuery, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final exercisesAsync = ref.watch(exerciseLibraryProvider);

    return exercisesAsync.when(
      data: (exercises) {
        final filtered = searchQuery.isEmpty
            ? exercises
            : exercises
                .where((e) => ExerciseLabels.matches(e, searchQuery))
                .toList();

        if (filtered.isEmpty) {
          return EmptyState(
            icon: Icons.fitness_center,
            title: isArabic ? 'لا توجد نتائج' : 'No results',
          );
        }

        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(exerciseLibraryProvider),
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: filtered.length,
            itemBuilder: (context, index) {
              final exercise = filtered[index];
              return _ExerciseTile(
                  exercise: exercise, isArabic: isArabic);
            },
          ),
        );
      },
      loading: () => ListView.builder(
        itemCount: 8,
        itemBuilder: (_, __) =>
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              child: ShimmerCard(height: 60),
            ),
      ),
      error: (e, _) => AppErrorWidget(
        message: e.toString(),
        onRetry: () => ref.invalidate(exerciseLibraryProvider),
      ),
    );
  }
}

class _CustomExercisesTab extends ConsumerWidget {
  final String searchQuery;
  final bool isArabic;

  const _CustomExercisesTab(
      {required this.searchQuery, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final exercisesAsync = ref.watch(customExercisesProvider);

    return exercisesAsync.when(
      data: (exercises) {
        final filtered = searchQuery.isEmpty
            ? exercises
            : exercises
                .where((e) => ExerciseLabels.matches(e, searchQuery))
                .toList();

        if (filtered.isEmpty) {
          return EmptyState(
            icon: Icons.add_circle_outline,
            title: isArabic ? 'لا توجد تمارين مخصصة' : 'No custom exercises',
            subtitle: isArabic
                ? 'اضغط + لانشاء تمرين مخصص'
                : 'Tap + to create a custom exercise',
          );
        }

        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(customExercisesProvider),
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: filtered.length,
            itemBuilder: (context, index) {
              final exercise = filtered[index];
              return _ExerciseTile(
                  exercise: exercise, isArabic: isArabic);
            },
          ),
        );
      },
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => AppErrorWidget(
        message: e.toString(),
        onRetry: () => ref.invalidate(customExercisesProvider),
      ),
    );
  }
}

class _ExerciseTile extends StatelessWidget {
  final ExerciseModel exercise;
  final bool isArabic;

  const _ExerciseTile({required this.exercise, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor:
              Theme.of(context).colorScheme.primary.withOpacity(0.1),
          child: Icon(
            Icons.fitness_center,
            color: Theme.of(context).colorScheme.primary,
            size: 18,
          ),
        ),
        title: Text(ExerciseLabels.name(exercise, language: AppLanguage.of(context))),
        subtitle: Text(ExerciseLabels.muscleGroups(
          exercise.muscleGroups,
          language: AppLanguage.of(context),
        )),
      ),
    );
  }
}
