import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/client_providers.dart';

/// pt.progress_measurements — the coach records a client's measurements (from
/// the first session on). The trainee sees them on their Progress tab.
class RecordMeasurementsScreen extends ConsumerStatefulWidget {
  final String clientId;
  const RecordMeasurementsScreen({super.key, required this.clientId});

  @override
  ConsumerState<RecordMeasurementsScreen> createState() => _RecordMeasurementsScreenState();
}

class _Field {
  final String key;
  final String ar;
  final String en;
  final String unit;
  const _Field(this.key, this.ar, this.en, this.unit);
}

const _bodyFields = [
  _Field('weight', 'الوزن', 'Weight', 'kg'),
  _Field('bodyFatPercentage', 'نسبة الدهون', 'Body fat', '%'),
];

const _measurementFields = [
  _Field('chest', 'الصدر', 'Chest', 'cm'),
  _Field('waist', 'الوسط', 'Waist', 'cm'),
  _Field('hips', 'الأرداف', 'Hips', 'cm'),
  _Field('leftArm', 'الذراع الأيسر', 'Left arm', 'cm'),
  _Field('rightArm', 'الذراع الأيمن', 'Right arm', 'cm'),
  _Field('leftThigh', 'الفخذ الأيسر', 'Left thigh', 'cm'),
  _Field('rightThigh', 'الفخذ الأيمن', 'Right thigh', 'cm'),
];

class _RecordMeasurementsScreenState extends ConsumerState<RecordMeasurementsScreen> {
  final _controllers = {
    for (final f in [..._bodyFields, ..._measurementFields]) f.key: TextEditingController(),
  };
  final _notes = TextEditingController();
  final _target = TextEditingController();
  ProgressGoalType? _goal;
  bool _saving = false;

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
    _notes.dispose();
    _target.dispose();
    super.dispose();
  }

  double? _value(String key) => double.tryParse(_controllers[key]!.text.trim().replaceAll(',', '.'));

  bool get _hasAnyValue => _controllers.keys.any((k) => _value(k) != null);

  Future<void> _save() async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final gymId = ref.read(activeGymIdProvider);
    if (!_hasAnyValue || gymId == null) return;

    final measurements = {for (final f in _measurementFields) f.key: _value(f.key)};
    final today = DateTime.now();
    setState(() => _saving = true);
    try {
      await ref.read(apiClientProvider).postVoid(ApiEndpoints.bodyMetricsForClient, data: {
        'clientId': widget.clientId,
        'gymId': gymId,
        'date': '${today.year.toString().padLeft(4, '0')}-${today.month.toString().padLeft(2, '0')}-${today.day.toString().padLeft(2, '0')}',
        'weight': _value('weight'),
        'weightUnit': 'kg',
        'bodyFatPercentage': _value('bodyFatPercentage'),
        'measurements': measurements.values.any((v) => v != null) ? measurements : null,
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      await _saveGoalIfChosen();
      ref.invalidate(clientDetailProvider(widget.clientId));
      ref.invalidate(clientJourneyProvider(widget.clientId));
      messenger.showSnackBar(SnackBar(content: Text(isArabic ? 'تم حفظ القياسات' : 'Measurements saved')));
      router.pop();
    } on FeatureDisabledException {
      router.pop();
    } on ApiException catch (e) {
      final duplicate = (e.errorCode ?? '').contains('Duplicate') || e.statusCode == 409;
      messenger.showSnackBar(SnackBar(
        content: Text(duplicate
            ? (isArabic ? 'تم تسجيل قياسات لهذا العميل اليوم بالفعل' : 'Measurements were already recorded today')
            : (isArabic ? 'تعذّر الحفظ، حاول مرة أخرى' : 'Could not save, please try again')),
      ));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  /// First session: the goal picked above the measurements (the measurements are already saved,
  /// so a failure here is not fatal; the goal can be set later from the Progress tab).
  Future<void> _saveGoalIfChosen() async {
    final goal = _goal;
    if (goal == null) return;
    final target = TargetWeightInput.parse(_target.text);
    try {
      await setClientGoal(ref, widget.clientId, GoalSelection(goal, target.isInRange ? target.kg : null));
    } on ApiException catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final pagePad = context.responsivePagePadding;
    // Ask for the goal only while the client has none (normally the first session).
    final journey = ref.watch(clientJourneyProvider(widget.clientId));
    final needsGoal = journey.hasValue && journey.value!.goal == null;

    Widget field(_Field f) => Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: TextField(
            controller: _controllers[f.key],
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(labelText: isArabic ? f.ar : f.en, suffixText: f.unit),
          ),
        );

    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'تسجيل القياسات' : 'Record measurements')),
      body: ListView(
        padding: EdgeInsets.fromLTRB(pagePad, 16, pagePad, 32),
        children: [
          if (needsGoal) ...[
            _FirstSessionGoal(
              strings: ProgressJourneyStrings(isArabic),
              goal: _goal,
              target: _target,
              onGoal: (g) => setState(() => _goal = g),
            ),
            const SizedBox(height: 16),
          ],
          ..._bodyFields.map(field),
          const SizedBox(height: 4),
          Text(isArabic ? 'المقاسات (سم)' : 'Measurements (cm)', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          ..._measurementFields.map(field),
          TextField(
            controller: _notes,
            maxLength: 500,
            maxLines: 2,
            decoration: InputDecoration(labelText: isArabic ? 'ملاحظات (اختياري)' : 'Notes (optional)'),
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: _saving || !_hasAnyValue ? null : _save,
            child: _saving
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(isArabic ? 'حفظ' : 'Save'),
          ),
        ],
      ),
    );
  }
}

/// Goal chips + optional target weight, shown on the first measurement.
class _FirstSessionGoal extends StatelessWidget {
  final ProgressJourneyStrings strings;
  final ProgressGoalType? goal;
  final TextEditingController target;
  final ValueChanged<ProgressGoalType> onGoal;

  const _FirstSessionGoal({required this.strings, required this.goal, required this.target, required this.onGoal});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(strings.goalTitle, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final g in ProgressGoalType.values)
                  ChoiceChip(label: Text(strings.goal(g)), selected: goal == g, onSelected: (_) => onGoal(g)),
              ],
            ),
            if (goal != null) ...[
              const SizedBox(height: 12),
              TextField(
                controller: target,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
                decoration: InputDecoration(labelText: strings.targetWeight, helperText: strings.targetOptional),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
