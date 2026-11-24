import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'diet_api.dart';
import 'trainer_diet_strings.dart';

/// What the app already knows about the client (profile and last measurement).
class DietPrefill {
  final DietSex? sex;
  final int? age;
  final double? weightKg;
  final DietGoal goal;

  const DietPrefill({this.sex, this.age, this.weightKg, this.goal = DietGoal.maintain});
}

/// Giving a client a diet: the calculator (prefilled), their daily target, a ready Egyptian
/// diet (the goal's first) and a day of it scaled to the target, then "Give plan".
class DietBuilderScreen extends ConsumerStatefulWidget {
  final String clientId;
  final DietPrefill prefill;

  const DietBuilderScreen({super.key, required this.clientId, required this.prefill});

  @override
  ConsumerState<DietBuilderScreen> createState() => _DietBuilderScreenState();
}

class _DietBuilderScreenState extends ConsumerState<DietBuilderScreen> {
  late DietSex _sex = widget.prefill.sex ?? DietSex.male;
  late DietGoal _goal = widget.prefill.goal;
  DietActivity _activity = DietActivity.light;
  late final _age = TextEditingController(text: widget.prefill.age?.toString() ?? '');
  final _height = TextEditingController();
  late final _weight = TextEditingController(text: widget.prefill.weightKg?.toStringAsFixed(0) ?? '');
  DailyTargetModel? _target;
  String? _dietKey;
  bool _busy = false;

  TrainerDietStrings get _s => TrainerDietStrings(Localizations.localeOf(context).languageCode == 'ar');

  @override
  void dispose() {
    _age.dispose();
    _height.dispose();
    _weight.dispose();
    super.dispose();
  }

  BodyInputModel? _body() {
    final age = int.tryParse(_age.text);
    final height = double.tryParse(_height.text);
    final weight = double.tryParse(_weight.text);
    if (age == null || height == null || weight == null) return null;
    return BodyInputModel(sex: _sex, age: age, heightCm: height, weightKg: weight, activity: _activity, goal: _goal);
  }

  Future<void> _calculate() async {
    final body = _body();
    if (body == null) return _snack(_s.fillAll);
    await _run(() async {
      final target = await ref.read(dietApiProvider).calculate(body);
      setState(() {
        _target = target;
        _dietKey = null;
      });
    });
  }

  Future<void> _give() async {
    final target = _target;
    final key = _dietKey;
    if (target == null || key == null) return;
    final s = _s;
    final navigator = Navigator.of(context);
    await _run(() async {
      await ref.read(dietApiProvider).give(clientId: widget.clientId, dietKey: key, target: target);
      _snack(s.given);
      navigator.pop(true);
    });
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() => _busy = true);
    try {
      await action();
    } on ApiException catch (e) {
      _snack(e.statusCode == 403 ? _s.notYourClient : e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _snack(String text) => ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  void _changed(VoidCallback change) => setState(() {
        change();
        _target = null;
        _dietKey = null;
      });

  @override
  Widget build(BuildContext context) {
    final s = _s;
    final target = _target;
    return RtlScaffold(
      isArabic: s.isArabic,
      appBar: AppBar(title: Text(s.giveDiet)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        children: [
          _BodyForm(
            s: s,
            sex: _sex,
            activity: _activity,
            goal: _goal,
            age: _age,
            height: _height,
            weight: _weight,
            onSex: (v) => _changed(() => _sex = v),
            onActivity: (v) => _changed(() => _activity = v),
            onGoal: (v) => _changed(() => _goal = v),
            onEdited: () => _changed(() {}),
          ),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            key: const Key('diet-calculate'),
            onPressed: _busy ? null : _calculate,
            icon: const Icon(Icons.calculate_outlined),
            label: Text(s.calculate),
          ),
          if (target != null) ...[
            const SizedBox(height: 12),
            _TargetCard(target: target, s: s),
            const SizedBox(height: 12),
            _DietPicker(goal: _goal, selected: _dietKey, s: s, onPick: (key) => setState(() => _dietKey = key)),
            if (_dietKey != null) _DayPreview(dietKey: _dietKey!, calories: target.calories, s: s),
          ],
        ],
      ),
      bottomNavigationBar: target == null || _dietKey == null
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: FilledButton(
                  key: const Key('diet-give'),
                  onPressed: _busy ? null : _give,
                  child: Text(s.give(target.calories)),
                ),
              ),
            ),
    );
  }
}

class _BodyForm extends StatelessWidget {
  final TrainerDietStrings s;
  final DietSex sex;
  final DietActivity activity;
  final DietGoal goal;
  final TextEditingController age;
  final TextEditingController height;
  final TextEditingController weight;
  final ValueChanged<DietSex> onSex;
  final ValueChanged<DietActivity> onActivity;
  final ValueChanged<DietGoal> onGoal;
  final VoidCallback onEdited;

  const _BodyForm({
    required this.s,
    required this.sex,
    required this.activity,
    required this.goal,
    required this.age,
    required this.height,
    required this.weight,
    required this.onSex,
    required this.onActivity,
    required this.onGoal,
    required this.onEdited,
  });

  Widget _number(String label, TextEditingController controller, String key) => Expanded(
        child: TextField(
          key: Key(key),
          controller: controller,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
          decoration: InputDecoration(labelText: label),
          onChanged: (_) => onEdited(),
        ),
      );

  Widget _chips<T>(String label, List<T> values, T selected, String Function(T) text, ValueChanged<T> onPick) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SizedBox(height: 10),
          Text(label),
          Wrap(spacing: 6, runSpacing: 4, children: [
            for (final v in values) ChoiceChip(label: Text(text(v)), selected: v == selected, onSelected: (_) => onPick(v)),
          ]),
        ],
      );

  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.calculator, style: Theme.of(context).textTheme.titleMedium),
        Text(s.prefilled, style: Theme.of(context).textTheme.bodySmall),
        _chips(s.sexLabel, DietSex.values, sex, s.sex, onSex),
        const SizedBox(height: 8),
        Row(children: [
          _number(s.ageLabel, age, 'diet-age'),
          const SizedBox(width: 8),
          _number(s.heightLabel, height, 'diet-height'),
          const SizedBox(width: 8),
          _number(s.weightLabel, weight, 'diet-weight'),
        ]),
        _chips(s.activityLabel, DietActivity.values, activity, s.activity, onActivity),
        _chips(s.goalLabel, DietGoal.values, goal, s.goal, onGoal),
      ]);
}

class _TargetCard extends StatelessWidget {
  final DailyTargetModel target;
  final TrainerDietStrings s;

  const _TargetCard({required this.target, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      key: const Key('diet-target'),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('${s.dailyTarget}: ${s.kcal(target.calories)}', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
          Text('${s.protein} ${s.grams(target.proteinGrams)} · ${s.carbs} ${s.grams(target.carbsGrams)} · ${s.fat} ${s.grams(target.fatGrams)}'),
          const SizedBox(height: 4),
          Text(s.howCalculated(target), style: theme.textTheme.bodySmall),
        ]),
      ),
    );
  }
}

class _DietPicker extends ConsumerWidget {
  final DietGoal goal;
  final String? selected;
  final TrainerDietStrings s;
  final ValueChanged<String> onPick;

  const _DietPicker({required this.goal, required this.selected, required this.s, required this.onPick});

  @override
  Widget build(BuildContext context, WidgetRef ref) => Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.pickDiet, style: Theme.of(context).textTheme.titleMedium),
        ...ref.watch(readyDietsProvider(goal)).when(
              loading: () => const [Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator()))],
              error: (_, __) => [Text(s.failed)],
              data: (diets) => [
                for (final d in diets)
                  Card(
                    color: d.key == selected ? Theme.of(context).colorScheme.primaryContainer : null,
                    child: ListTile(
                      key: Key('diet-${d.key}'),
                      leading: Icon(d.key == selected ? Icons.check_circle : Icons.restaurant_menu),
                      title: Text(d.displayName(s.isArabic)),
                      subtitle: Text('${d.displayDescription(s.isArabic)}\n${d.goal == null ? s.dietGoalAny : s.goal(d.goal!)}'),
                      isThreeLine: true,
                      onTap: () => onPick(d.key),
                    ),
                  ),
              ],
            ),
      ]);
}

class _DayPreview extends ConsumerWidget {
  final String dietKey;
  final int calories;
  final TrainerDietStrings s;

  const _DayPreview({required this.dietKey, required this.calories, required this.s});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final preview = ref.watch(readyDietPreviewProvider((key: dietKey, calories: calories)));
    return preview.maybeWhen(
      data: (p) {
        if (p.days.isEmpty) return const SizedBox.shrink();
        final day = p.days.first;
        return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          const SizedBox(height: 12),
          Text('${s.preview} · ${s.kcal(day.calories)}', style: Theme.of(context).textTheme.titleSmall),
          for (final meal in day.meals)
            ListTile(
              contentPadding: EdgeInsets.zero,
              dense: true,
              title: Text('${meal.displayName(s.isArabic)} · ${s.kcal(meal.calories)}'),
              subtitle: Text([for (final f in meal.foods) '${f.displayName(s.isArabic)} ${s.amount(f)}'].join(' · ')),
            ),
        ]);
      },
      orElse: () => const Padding(padding: EdgeInsets.all(12), child: LinearProgressIndicator()),
    );
  }
}
