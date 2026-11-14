import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:image_picker/image_picker.dart';

import '../../../gamification/presentation/providers/gamification_providers.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../../data/inbody/inbody_scan_api.dart';
import '../../data/inbody/inbody_scan_request.dart';
import '../../data/inbody/inbody_sheet_parser.dart';
import '../../data/inbody/inbody_text_reader.dart';
import '../providers/progress_providers.dart';

/// Picks a photo of an InBody sheet (camera or gallery).
typedef InBodyPhotoPicker = Future<String?> Function(ImageSource source);

Future<String?> _pickWithImagePicker(ImageSource source) async {
  // Full resolution reads small print better; cap the size to keep memory in check.
  final file = await ImagePicker().pickImage(source: source, maxWidth: 2400);
  return file?.path;
}

final inBodyPhotoPickerProvider = Provider<InBodyPhotoPicker>((_) => _pickWithImagePicker);

/// One value on the review form.
class _InBodyField {
  final String key;
  final String labelAr;
  final String labelEn;
  final ReadValue? Function(InBodyReading reading) read;

  const _InBodyField(this.key, this.labelAr, this.labelEn, this.read);
}

final _fields = <_InBodyField>[
  _InBodyField('weightKg', 'الوزن (كجم)', 'Weight (kg)', (r) => r.weightKg),
  _InBodyField('heightCm', 'الطول (سم)', 'Height (cm)', (r) => r.heightCm),
  _InBodyField('bodyFatPercentage', 'نسبة الدهون (%)', 'Body fat (%)', (r) => r.bodyFatPercent),
  _InBodyField('skeletalMuscleMassKg', 'كتلة العضلات الهيكلية (كجم)', 'Skeletal muscle mass (kg)',
      (r) => r.skeletalMuscleMassKg),
  _InBodyField('bodyWaterKg', 'إجمالي ماء الجسم (كجم / لتر)', 'Total body water (kg / L)', (r) => r.bodyWaterKg),
];

/// progress.inbody: the member photographs their InBody result sheet, the values are read on
/// the phone, the member checks / edits them, and they are saved as one InBody measurement.
class InBodyScanScreen extends ConsumerStatefulWidget {
  const InBodyScanScreen({super.key});

  @override
  ConsumerState<InBodyScanScreen> createState() => _InBodyScanScreenState();
}

class _InBodyScanScreenState extends ConsumerState<InBodyScanScreen> {
  final _controllers = {for (final f in _fields) f.key: TextEditingController()};
  InBodyReading? _reading;
  DateTime _date = DateUtils.dateOnly(DateTime.now());
  bool _isReading = false;
  bool _isSaving = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  String _t(String ar, String en) => _isArabic ? ar : en;

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final reading = _reading;
    return Scaffold(
      appBar: AppBar(title: Text(_t('قراءة InBody', 'Scan InBody'))),
      body: reading == null
          ? _PickPhoto(isArabic: _isArabic, isReading: _isReading, onPick: _scan)
          : _ReviewForm(
              isArabic: _isArabic,
              reading: reading,
              controllers: _controllers,
              date: _date,
              isSaving: _isSaving,
              onPickDate: _pickDate,
              onRetake: () => setState(() => _reading = null),
              onSave: _save,
            ),
    );
  }

  Future<void> _scan(ImageSource source) async {
    final path = await ref.read(inBodyPhotoPickerProvider)(source);
    if (path == null || !mounted) return;

    setState(() => _isReading = true);
    try {
      final reading = await ref.read(inBodyTextReaderProvider).read(path);
      if (!mounted) return;
      _fill(reading);
      if (reading.isEmpty) {
        _snack(_t('لم نتمكن من قراءة الورقة. صوّرها في إضاءة جيدة أو أدخل القيم بنفسك.',
            "Couldn't read the sheet. Retake it in good light, or type the values in."));
      }
    } catch (_) {
      if (mounted) _snack(_t('تعذرت قراءة الصورة', 'Could not read the photo'));
    } finally {
      if (mounted) setState(() => _isReading = false);
    }
  }

  void _fill(InBodyReading reading) {
    for (final field in _fields) {
      _controllers[field.key]!.text = field.read(reading)?.value.toString() ?? '';
    }
    final today = DateUtils.dateOnly(DateTime.now());
    final sheetDate = reading.testDate;
    setState(() {
      _reading = reading;
      _date = sheetDate != null && !sheetDate.isAfter(today) ? sheetDate : today;
    });
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2000),
      lastDate: DateTime.now(),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    final values = {
      for (final f in _fields)
        if (double.tryParse(_controllers[f.key]!.text) case final v?) f.key: v,
    };
    if (values.isEmpty) {
      _snack(_t('أدخل قيمة واحدة على الأقل', 'Enter at least one value'));
      return;
    }

    setState(() => _isSaving = true);
    try {
      await ref.read(inBodyScanApiProvider).save(InBodyScanRequest.fromValues(_date, values));
      refreshProgress(ref);
      ref.invalidate(progressDashboardProvider);
      ref.invalidate(leaderboardProvider);
      if (!mounted) return;
      _snack(_t('تم حفظ قياسات InBody', 'InBody results saved'));
      context.pop();
    } catch (e) {
      final message = e is ApiException && e.statusCode == 400 && e.message.isNotEmpty
          ? e.message
          : _t('تعذر حفظ القياسات', 'Could not save the results');
      if (mounted) _snack(message, error: true);
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  void _snack(String message, {bool error = false}) {
    // Replace, don't queue: the latest message is the one that matters.
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message), backgroundColor: error ? Colors.red : null));
  }
}

class _PickPhoto extends StatelessWidget {
  final bool isArabic;
  final bool isReading;
  final ValueChanged<ImageSource> onPick;

  const _PickPhoto({required this.isArabic, required this.isReading, required this.onPick});

  @override
  Widget build(BuildContext context) {
    if (isReading) {
      return Center(
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const CircularProgressIndicator(),
          const SizedBox(height: 16),
          Text(isArabic ? 'جارٍ قراءة الورقة…' : 'Reading the sheet…'),
        ]),
      );
    }

    final theme = Theme.of(context);
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        Icon(Icons.document_scanner_outlined, size: 72, color: theme.colorScheme.primary),
        const SizedBox(height: 16),
        Text(
          isArabic
              ? 'صوّر ورقة نتيجة InBody كاملة ومستوية وفي إضاءة جيدة.'
              : 'Take a photo of the whole InBody result sheet, flat and in good light.',
          style: theme.textTheme.titleMedium,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 8),
        Text(
          isArabic
              ? 'تتم القراءة على هاتفك ولا تُرفع الصورة. ستراجع القيم قبل الحفظ.'
              : 'The photo is read on your phone and never uploaded. You will check the values before saving.',
          style: theme.textTheme.bodyMedium,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 32),
        FilledButton.icon(
          icon: const Icon(Icons.photo_camera),
          label: Text(isArabic ? 'التقط صورة' : 'Take photo'),
          onPressed: () => onPick(ImageSource.camera),
        ),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          icon: const Icon(Icons.photo_library_outlined),
          label: Text(isArabic ? 'اختر من المعرض' : 'Choose from gallery'),
          onPressed: () => onPick(ImageSource.gallery),
        ),
      ],
    );
  }
}

class _ReviewForm extends StatelessWidget {
  final bool isArabic;
  final InBodyReading reading;
  final Map<String, TextEditingController> controllers;
  final DateTime date;
  final bool isSaving;
  final VoidCallback onPickDate;
  final VoidCallback onRetake;
  final VoidCallback onSave;

  const _ReviewForm({
    required this.isArabic,
    required this.reading,
    required this.controllers,
    required this.date,
    required this.isSaving,
    required this.onPickDate,
    required this.onRetake,
    required this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          isArabic
              ? 'راجع القيم وعدّل أي قيمة غير صحيحة. الحقول المميزة تحتاج إلى مراجعة.'
              : 'Check the values and fix anything that is wrong. Highlighted fields need a look.',
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        const SizedBox(height: 8),
        ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.event),
          title: Text(isArabic ? 'تاريخ القياس' : 'Test date'),
          subtitle: Text(MaterialLocalizations.of(context).formatMediumDate(date)),
          trailing: const Icon(Icons.edit_calendar),
          onTap: onPickDate,
        ),
        for (final field in _fields)
          _ValueField(
            fieldKey: field.key,
            label: isArabic ? field.labelAr : field.labelEn,
            controller: controllers[field.key]!,
            needsCheck: field.read(reading)?.isLow ?? true,
            isArabic: isArabic,
          ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: isSaving ? null : onSave,
          child: isSaving
              ? const SizedBox(
                  height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : Text(isArabic ? 'حفظ القياسات' : 'Save results'),
        ),
        TextButton.icon(
          onPressed: isSaving ? null : onRetake,
          icon: const Icon(Icons.refresh),
          label: Text(isArabic ? 'إعادة التصوير' : 'Retake photo'),
        ),
      ],
    );
  }
}

class _ValueField extends StatelessWidget {
  final String fieldKey;
  final String label;
  final TextEditingController controller;
  final bool needsCheck;
  final bool isArabic;

  const _ValueField({
    required this.fieldKey,
    required this.label,
    required this.controller,
    required this.needsCheck,
    required this.isArabic,
  });

  @override
  Widget build(BuildContext context) {
    final warn = Theme.of(context).colorScheme.tertiary;
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextField(
        key: ValueKey('inbody-$fieldKey'),
        controller: controller,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
        decoration: InputDecoration(
          labelText: label,
          helperText: needsCheck ? (isArabic ? 'تأكد من هذه القيمة' : 'Please check this value') : null,
          helperStyle: needsCheck ? TextStyle(color: warn) : null,
          suffixIcon: needsCheck ? Icon(Icons.warning_amber_rounded, color: warn) : null,
          border: const OutlineInputBorder(),
        ),
      ),
    );
  }
}
