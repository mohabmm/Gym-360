import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/workout_providers.dart';

class TemplateLibraryScreen extends ConsumerStatefulWidget {
  const TemplateLibraryScreen({super.key});

  @override
  ConsumerState<TemplateLibraryScreen> createState() =>
      _TemplateLibraryScreenState();
}

class _TemplateLibraryScreenState
    extends ConsumerState<TemplateLibraryScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  String _searchQuery = '';
  int _currentTab = 0;
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _tabController.addListener(() {
      if (_tabController.index != _currentTab) {
        setState(() => _currentTab = _tabController.index);
      }
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  ({IconData icon, String label, VoidCallback onPressed}) _fabConfigFor(
      int tabIndex, bool isArabic, BuildContext context) {
    switch (tabIndex) {
      case 1: // Templates
        return (
          icon: Icons.add,
          label: isArabic ? 'قالب جديد' : 'New Template',
          onPressed: () async {
            await context.push(AppRoutes.workoutBuilder);
            if (context.mounted) {
              ref.invalidate(trainerWorkoutsProvider);
              ref.invalidate(workoutTemplatesProvider);
            }
          },
        );
      case 2: // Programs
        return (
          icon: Icons.add,
          label: isArabic ? 'برنامج جديد' : 'New Program',
          onPressed: () => context.push(AppRoutes.programBuilder),
        );
      case 0:
      default:
        return (
          icon: Icons.add,
          label: isArabic ? 'تمرين جديد' : 'New Workout',
          onPressed: () async {
            await context.push(AppRoutes.workoutBuilder);
            if (context.mounted) {
              ref.invalidate(trainerWorkoutsProvider);
              ref.invalidate(workoutTemplatesProvider);
            }
          },
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final fab = _fabConfigFor(_currentTab, isArabic, context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'التمارين' : 'Workouts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: isArabic ? 'انشاء تمرين' : 'Create Workout',
            onPressed: () async {
              await context.push(AppRoutes.workoutBuilder);
              if (context.mounted) {
                ref.invalidate(trainerWorkoutsProvider);
                ref.invalidate(workoutTemplatesProvider);
              }
            },
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              if (value == 'exercises') {
                context.push(AppRoutes.exerciseManager);
              } else if (value == 'programs') {
                context.push(AppRoutes.programBuilder);
              }
            },
            itemBuilder: (_) => [
              PopupMenuItem(
                value: 'exercises',
                child: Text(isArabic ? 'ادارة التمارين' : 'Exercise Manager'),
              ),
              PopupMenuItem(
                value: 'programs',
                child: Text(isArabic ? 'بناء برنامج' : 'Program Builder'),
              ),
            ],
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(text: isArabic ? 'تماريني' : 'My Workouts'),
            Tab(text: isArabic ? 'القوالب' : 'Templates'),
            Tab(text: isArabic ? 'البرامج' : 'Programs'),
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
                _WorkoutsTab(
                    searchQuery: _searchQuery,
                    isArabic: isArabic),
                _TemplatesTab(
                    searchQuery: _searchQuery,
                    isArabic: isArabic),
                _ProgramsTab(
                    searchQuery: _searchQuery,
                    isArabic: isArabic),
              ],
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: fab.onPressed,
        icon: Icon(fab.icon),
        label: Text(fab.label),
      ),
    );
  }
}

class _WorkoutsTab extends ConsumerWidget {
  final String searchQuery;
  final bool isArabic;

  const _WorkoutsTab(
      {required this.searchQuery, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final workoutsAsync = ref.watch(trainerWorkoutsProvider);

    return workoutsAsync.when(
      data: (workouts) {
        // BE's /training/workouts returns both templates and non-templates.
        // The Templates tab shows templates; My Workouts should only list
        // actual workouts.
        final nonTemplates =
            workouts.where((w) => !w.isTemplate).toList();
        final filtered = searchQuery.isEmpty
            ? nonTemplates
            : nonTemplates.where((w) {
                final name = (w.nameAr ?? w.name).toLowerCase();
                return name.contains(searchQuery.toLowerCase());
              }).toList();

        if (filtered.isEmpty) {
          return EmptyState(
            icon: Icons.fitness_center,
            title: isArabic ? 'لا توجد تمارين' : 'No workouts yet',
            subtitle: isArabic
                ? 'انشئ اول تمرين لك'
                : 'Create your first workout',
          );
        }

        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(trainerWorkoutsProvider),
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: filtered.length,
            itemBuilder: (context, index) {
              final workout = filtered[index];
              return _WorkoutCard(
                workout: workout,
                isArabic: isArabic,
                onTap: () =>
                    context.push('/workouts/builder/${workout.id}'),
              );
            },
          ),
        );
      },
      loading: () => ListView.builder(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: 5,
        itemBuilder: (_, __) => const Padding(
          padding: EdgeInsets.only(bottom: 8),
          child: ShimmerCard(height: 80),
        ),
      ),
      error: (e, _) => AppErrorWidget(
        message: e.toString(),
        onRetry: () => ref.invalidate(trainerWorkoutsProvider),
      ),
    );
  }
}

class _TemplatesTab extends ConsumerWidget {
  final String searchQuery;
  final bool isArabic;

  const _TemplatesTab(
      {required this.searchQuery, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final templatesAsync = ref.watch(workoutTemplatesProvider);

    return templatesAsync.when(
      data: (templates) {
        final filtered = searchQuery.isEmpty
            ? templates
            : templates.where((w) {
                final name = (w.nameAr ?? w.name).toLowerCase();
                return name.contains(searchQuery.toLowerCase());
              }).toList();

        if (filtered.isEmpty) {
          return EmptyState(
            icon: Icons.library_books_outlined,
            title: isArabic ? 'لا توجد قوالب' : 'No templates',
          );
        }

        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(workoutTemplatesProvider),
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: filtered.length,
            itemBuilder: (context, index) {
              final workout = filtered[index];
              return _WorkoutCard(
                workout: workout,
                isArabic: isArabic,
                onTap: () =>
                    context.push('/workouts/builder/${workout.id}'),
              );
            },
          ),
        );
      },
      loading: () => ListView.builder(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: 5,
        itemBuilder: (_, __) => const Padding(
          padding: EdgeInsets.only(bottom: 8),
          child: ShimmerCard(height: 80),
        ),
      ),
      error: (e, _) => AppErrorWidget(
        message: e.toString(),
        onRetry: () => ref.invalidate(workoutTemplatesProvider),
      ),
    );
  }
}

class _ProgramsTab extends ConsumerWidget {
  final String searchQuery;
  final bool isArabic;

  const _ProgramsTab(
      {required this.searchQuery, required this.isArabic});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final programsAsync = ref.watch(trainerProgramsProvider);

    return programsAsync.when(
      data: (programs) {
        final filtered = searchQuery.isEmpty
            ? programs
            : programs.where((p) {
                final name = (p.nameAr ?? p.name).toLowerCase();
                return name.contains(searchQuery.toLowerCase());
              }).toList();

        if (filtered.isEmpty) {
          return EmptyState(
            icon: Icons.event_note_outlined,
            title: isArabic ? 'لا توجد برامج' : 'No programs yet',
            subtitle: isArabic
                ? 'انشئ برنامجا متعدد الاسابيع'
                : 'Create a multi-week program',
          );
        }

        return RefreshIndicator(
          onRefresh: () async => ref.invalidate(trainerProgramsProvider),
          child: ListView.builder(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: filtered.length,
            itemBuilder: (context, index) {
              final program = filtered[index];
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  leading: const CircleAvatar(
                    child: Icon(Icons.calendar_month),
                  ),
                  title: Text(program.nameAr ?? program.name),
                  subtitle: Text(
                    isArabic
                        ? '${program.durationWeeks} اسابيع'
                        : '${program.durationWeeks} weeks',
                  ),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () =>
                      context.push(AppRoutes.programBuilder),
                ),
              );
            },
          ),
        );
      },
      loading: () => ListView.builder(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        itemCount: 4,
        itemBuilder: (_, __) => const Padding(
          padding: EdgeInsets.only(bottom: 8),
          child: ShimmerCard(height: 72),
        ),
      ),
      error: (e, _) => AppErrorWidget(
        message: e.toString(),
        onRetry: () => ref.invalidate(trainerProgramsProvider),
      ),
    );
  }
}

String _exerciseCountLabel(int count, bool isArabic) {
  if (!isArabic) return '$count ${count == 1 ? 'exercise' : 'exercises'}';
  // Arabic plural rules: 1 = singular, 2 = dual, 3-10 = plural, 11+ = singular form.
  if (count == 0) return 'لا تمارين';
  if (count == 1) return 'تمرين واحد';
  if (count == 2) return 'تمرينان';
  if (count >= 3 && count <= 10) return '$count تمارين';
  return '$count تمرين';
}

class _WorkoutCard extends StatelessWidget {
  final WorkoutModel workout;
  final bool isArabic;
  final VoidCallback onTap;

  const _WorkoutCard({
    required this.workout,
    required this.isArabic,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius:
            BorderRadius.circular(AppConstants.borderRadius),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primary.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(
                  Icons.fitness_center,
                  color: theme.colorScheme.primary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic && workout.nameAr != null
                          ? workout.nameAr!
                          : workout.name,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    Text(
                      _exerciseCountLabel(
                        workout.exerciseCount ?? workout.exercises.length,
                        isArabic,
                      ),
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right, color: AppColors.grey400),
            ],
          ),
        ),
      ),
    );
  }
}
