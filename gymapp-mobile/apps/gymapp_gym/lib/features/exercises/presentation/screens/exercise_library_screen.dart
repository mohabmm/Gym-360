import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/exercises_provider.dart';

class ExerciseLibraryScreen extends ConsumerStatefulWidget {
  const ExerciseLibraryScreen({super.key});

  @override
  ConsumerState<ExerciseLibraryScreen> createState() =>
      _ExerciseLibraryScreenState();
}

class _ExerciseLibraryScreenState extends ConsumerState<ExerciseLibraryScreen> {
  final _searchController = TextEditingController();
  String _search = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final exercises = ref.watch(exercisesProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'مكتبة التمارين' : 'Exercise Library'),
        actions: [
          TextButton(
            onPressed: () => context.push(RoutePaths.customExercises),
            child: Text(isArabic ? 'تمارين مخصصة' : 'Custom'),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppTextField(
              controller: _searchController,
              hint: isArabic ? 'بحث عن تمرين' : 'Search exercises',
              prefixIcon: const Icon(Icons.search),
              onChanged: (v) => setState(() => _search = v.toLowerCase()),
            ),
          ),
          Expanded(
            child: exercises.when(
              data: (items) {
                final filtered = _search.isEmpty
                    ? items
                    : items.where((e) {
                        return e.name.toLowerCase().contains(_search) ||
                            (e.nameAr?.contains(_search) ?? false) ||
                            e.muscleGroup.toLowerCase().contains(_search);
                      }).toList();

                if (filtered.isEmpty) {
                  return EmptyState(
                    icon: Icons.fitness_center_outlined,
                    title: isArabic ? 'لا توجد تمارين' : 'No exercises found',
                  );
                }

                return RefreshIndicator(
                  onRefresh: () => ref.refresh(exercisesProvider.future),
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    itemCount: filtered.length,
                    itemBuilder: (context, index) {
                      final ex = filtered[index];
                      return AppCard(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: Theme.of(context)
                                  .colorScheme
                                  .primary
                                  .withOpacity(0.1),
                              shape: BoxShape.circle,
                            ),
                            child: Icon(
                              Icons.fitness_center,
                              color: Theme.of(context).colorScheme.primary,
                              size: 20,
                            ),
                          ),
                          title: Text(
                            isArabic && ex.nameAr != null ? ex.nameAr! : ex.name,
                            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                                  fontWeight: FontWeight.w600,
                                ),
                          ),
                          subtitle: Text(ex.muscleGroup),
                          trailing: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: Colors.grey.shade200,
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              ex.category,
                              style: Theme.of(context).textTheme.labelSmall,
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => AppErrorWidget(
                message: isArabic ? 'خطأ في التحميل' : 'Error loading exercises',
                onRetry: () => ref.invalidate(exercisesProvider),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
