import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/nutrition_providers.dart';

class NutritionLogScreen extends ConsumerWidget {
  const NutritionLogScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final foodLogAsync = ref.watch(foodLogProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'سجل التغذية' : 'Nutrition Log'),
        actions: [
          IconButton(
            icon: const Icon(Icons.water_drop_outlined),
            tooltip: isArabic ? 'متابعة الماء' : 'Water Tracker',
            onPressed: () => context.push(AppRoutes.waterTracker),
          ),
          IconButton(
            icon: const Icon(Icons.restaurant_menu),
            tooltip: isArabic ? 'خطة الوجبات' : 'Meal Plan',
            onPressed: () => context.push(AppRoutes.mealPlan),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(foodLogProvider),
        child: foodLogAsync.when(
          data: (log) => CustomScrollView(
            slivers: [
              // Daily macros summary
              SliverToBoxAdapter(
                child: _MacrosSummary(log: log, isArabic: isArabic),
              ),

              // Add food button
              SliverToBoxAdapter(
                child: Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: OutlinedButton.icon(
                    onPressed: () => _showAddFoodSheet(context, ref, isArabic),
                    icon: const Icon(Icons.add),
                    label:
                        Text(isArabic ? 'إضافة وجبة' : 'Add Food'),
                  ),
                ),
              ),

              // Food entries grouped by meal type
              ..._buildMealGroups(context, ref, log, isArabic),

              const SliverToBoxAdapter(child: SizedBox(height: 80)),
            ],
          ),
          loading: () =>
              const Center(child: CircularProgressIndicator()),
          error: (e, _) => AppErrorWidget(
            message: isArabic
                ? 'تعذر تحميل سجل التغذية. يرجى المحاولة مرة أخرى.'
                : 'Could not load the nutrition log. Please try again.',
            onRetry: () => ref.invalidate(foodLogProvider),
          ),
        ),
      ),
    );
  }

  List<Widget> _buildMealGroups(BuildContext context, WidgetRef ref,
      FoodLogModel log, bool isArabic) {
    final mealTypes = ['breakfast', 'lunch', 'dinner', 'snack'];
    return mealTypes.map((type) {
      final entries =
          log.entries.where((e) => e.mealType == type).toList();
      if (entries.isEmpty) return const SliverToBoxAdapter(child: SizedBox.shrink());

      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                _mealTypeLabel(type, isArabic),
                style: Theme.of(context)
                    .textTheme
                    .titleSmall
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              ...entries.map((entry) => _FoodEntryTile(
                  entry: entry, isArabic: isArabic, ref: ref)),
              const Divider(height: 16),
            ],
          ),
        ),
      );
    }).toList();
  }

  String _mealTypeLabel(String type, bool isArabic) {
    final labels = {
      'breakfast': isArabic ? 'الإفطار' : 'Breakfast',
      'lunch': isArabic ? 'الغداء' : 'Lunch',
      'dinner': isArabic ? 'العشاء' : 'Dinner',
      'snack': isArabic ? 'وجبة خفيفة' : 'Snack',
    };
    return labels[type] ?? type;
  }

  void _showAddFoodSheet(
      BuildContext context, WidgetRef ref, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _AddFoodSheet(isArabic: isArabic),
    );
  }
}

class _MacrosSummary extends StatelessWidget {
  final FoodLogModel log;
  final bool isArabic;

  const _MacrosSummary({required this.log, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      margin: const EdgeInsets.all(16),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Text(
              '${log.totalCalories ?? 0} ${isArabic ? 'سعرة حرارية' : 'kcal'}',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                    color: Theme.of(context).colorScheme.primary,
                  ),
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _MacroItem(
                  label: isArabic ? 'بروتين' : 'Protein',
                  value: '${(log.totalProtein ?? 0).toStringAsFixed(0)}g',
                  color: Colors.blue,
                ),
                _MacroItem(
                  label: isArabic ? 'كربوهيدرات' : 'Carbs',
                  value: '${(log.totalCarbs ?? 0).toStringAsFixed(0)}g',
                  color: Colors.orange,
                ),
                _MacroItem(
                  label: isArabic ? 'دهون' : 'Fats',
                  value: '${(log.totalFat ?? 0).toStringAsFixed(0)}g',
                  color: Colors.red,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _MacroItem extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _MacroItem(
      {required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          value,
          style: Theme.of(context)
              .textTheme
              .titleMedium
              ?.copyWith(fontWeight: FontWeight.w700, color: color),
        ),
        Text(label, style: Theme.of(context).textTheme.bodySmall),
      ],
    );
  }
}

class _FoodEntryTile extends StatelessWidget {
  final FoodLogEntryModel entry;
  final bool isArabic;
  final WidgetRef ref;

  const _FoodEntryTile(
      {required this.entry, required this.isArabic, required this.ref});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(entry.name),
      subtitle: Text(
          '${entry.quantity} ${entry.unit} • ${entry.calories} ${isArabic ? 'سعرة' : 'kcal'}'),
      trailing: IconButton(
        icon: const Icon(Icons.delete_outline, size: 20),
        onPressed: () async {
          try {
            final repo = ref.read(nutritionRepositoryProvider);
            await repo.deleteFoodEntry(entry.id);
            ref.invalidate(foodLogProvider);
          } catch (e) {
            if (context.mounted) {
              ScaffoldMessenger.of(context)
                  .showSnackBar(SnackBar(content: Text(e.toString())));
            }
          }
        },
      ),
    );
  }
}

class _AddFoodSheet extends ConsumerStatefulWidget {
  final bool isArabic;

  const _AddFoodSheet({required this.isArabic});

  @override
  ConsumerState<_AddFoodSheet> createState() => _AddFoodSheetState();
}

class _AddFoodSheetState extends ConsumerState<_AddFoodSheet> {
  FoodItemModel? _selectedFood;
  String _mealType = 'breakfast';
  final _quantityController = TextEditingController(text: '100');
  bool _isAdding = false;

  @override
  void dispose() {
    _quantityController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = widget.isArabic;
    final searchQuery = ref.watch(foodSearchQueryProvider);
    final searchResults = ref.watch(foodSearchResultsProvider);

    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
        left: 16,
        right: 16,
        top: 16,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            isArabic ? 'إضافة طعام' : 'Add Food',
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 12),

          // Meal type
          DropdownButtonFormField<String>(
            value: _mealType,
            decoration: InputDecoration(
              labelText: isArabic ? 'نوع الوجبة' : 'Meal Type',
              border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12)),
            ),
            items: [
              DropdownMenuItem(
                  value: 'breakfast',
                  child: Text(isArabic ? 'الإفطار' : 'Breakfast')),
              DropdownMenuItem(
                  value: 'lunch',
                  child: Text(isArabic ? 'الغداء' : 'Lunch')),
              DropdownMenuItem(
                  value: 'dinner',
                  child: Text(isArabic ? 'العشاء' : 'Dinner')),
              DropdownMenuItem(
                  value: 'snack',
                  child:
                      Text(isArabic ? 'وجبة خفيفة' : 'Snack')),
            ],
            onChanged: (v) => setState(() => _mealType = v!),
          ),
          const SizedBox(height: 12),

          // Food search
          TextField(
            decoration: InputDecoration(
              hintText: isArabic ? 'ابحث عن الطعام...' : 'Search food...',
              prefixIcon: const Icon(Icons.search),
              border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12)),
            ),
            onChanged: (v) =>
                ref.read(foodSearchQueryProvider.notifier).state = v,
          ),

          if (searchQuery.isNotEmpty)
            SizedBox(
              height: 160,
              child: searchResults.when(
                data: (items) => ListView.builder(
                  itemCount: items.length,
                  itemBuilder: (ctx, i) => ListTile(
                    title: Text(isArabic && items[i].nameAr != null
                        ? items[i].nameAr!
                        : items[i].name),
                    subtitle: Text(
                        '${items[i].caloriesPer100g} kcal/100g'),
                    onTap: () => setState(() => _selectedFood = items[i]),
                    selected: _selectedFood?.id == items[i].id,
                  ),
                ),
                loading: () =>
                    const Center(child: CircularProgressIndicator()),
                error: (_, __) => const SizedBox.shrink(),
              ),
            ),

          if (_selectedFood != null) ...[
            const SizedBox(height: 8),
            AppTextField(
              label: isArabic ? 'الكمية (جرام)' : 'Quantity (g)',
              hint: isArabic ? 'الكمية (جرام)' : 'Quantity (g)',
              controller: _quantityController,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              inputFormatters: [
                FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
              ],
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _isAdding ? null : _addFood,
                child: _isAdding
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(isArabic ? 'إضافة' : 'Add'),
              ),
            ),
          ],

          const SizedBox(height: 16),
        ],
      ),
    );
  }

  Future<void> _addFood() async {
    final food = _selectedFood;
    if (food == null) return;

    final quantity = double.tryParse(_quantityController.text) ?? 100;
    final ratio = quantity / 100;

    setState(() => _isAdding = true);
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final userId = authState?.user?.id;
      final gymId = authState?.currentGymId;
      if (userId == null || gymId == null) {
        throw Exception('No authenticated user or active gym');
      }
      final today = DateTime.now();
      final dateStr = '${today.year.toString().padLeft(4, '0')}-'
          '${today.month.toString().padLeft(2, '0')}-'
          '${today.day.toString().padLeft(2, '0')}';

      final repo = ref.read(nutritionRepositoryProvider);
      await repo.addFoodEntry({
        'userId': userId,
        'gymId': gymId,
        'date': dateStr,
        'mealType': _mealType,
        'foodItems': [
          {
            'name': food.name,
            'quantity': quantity,
            'unit': 'g',
            'calories': food.caloriesPer100g * ratio,
            'protein': food.proteinPer100g * ratio,
            'carbs': food.carbsPer100g * ratio,
            'fat': food.fatPer100g * ratio,
          },
        ],
      });
      ref.invalidate(foodLogProvider);
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _isAdding = false);
    }
  }
}
