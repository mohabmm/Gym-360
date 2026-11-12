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
  bool _saving = false;

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
    _notes.dispose();
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
      ref.invalidate(clientDetailProvider(widget.clientId));
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

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final pagePad = context.responsivePagePadding;

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
