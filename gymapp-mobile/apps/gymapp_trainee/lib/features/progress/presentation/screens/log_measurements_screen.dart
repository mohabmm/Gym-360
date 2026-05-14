import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../gamification/presentation/providers/gamification_providers.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../providers/progress_providers.dart';

class LogMeasurementsScreen extends ConsumerStatefulWidget {
  const LogMeasurementsScreen({super.key});

  @override
  ConsumerState<LogMeasurementsScreen> createState() =>
      _LogMeasurementsScreenState();
}

class _LogMeasurementsScreenState
    extends ConsumerState<LogMeasurementsScreen> {
  final _controllers = <String, TextEditingController>{};
  bool _isSaving = false;

  final _fields = [
    ('weight', 'الوزن (كجم)', 'Weight (kg)'),
    ('chest', 'الصدر (سم)', 'Chest (cm)'),
    ('waist', 'الخصر (سم)', 'Waist (cm)'),
    ('hips', 'الورك (سم)', 'Hips (cm)'),
    ('leftBicep', 'العضلة اليسرى (سم)', 'Left Bicep (cm)'),
    ('rightBicep', 'العضلة اليمنى (سم)', 'Right Bicep (cm)'),
    ('leftThigh', 'الفخذ الأيسر (سم)', 'Left Thigh (cm)'),
    ('rightThigh', 'الفخذ الأيمن (سم)', 'Right Thigh (cm)'),
  ];

  @override
  void initState() {
    super.initState();
    for (final field in _fields) {
      _controllers[field.$1] = TextEditingController();
    }
  }

  @override
  void dispose() {
    for (final ctrl in _controllers.values) {
      ctrl.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'قياسات الجسم' : 'Body Measurements'),
      ),
      body: ListView(
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            isArabic
                ? 'أدخل القياسات التي تريد تسجيلها (اتركها فارغة إذا لم ترد تسجيلها)'
                : 'Enter measurements you want to log (leave empty to skip)',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
          const SizedBox(height: 16),
          ..._fields.map(
            (field) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: AppTextField(
                label: isArabic ? field.$2 : field.$3,
                hint: isArabic ? field.$2 : field.$3,
                controller: _controllers[field.$1],
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              onPressed: _isSaving ? null : _save,
              child: _isSaving
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white))
                  : Text(isArabic ? 'حفظ القياسات' : 'Save Measurements'),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _save() async {
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final userId = authState?.user?.id;
      final gymId = authState?.currentGymId;
      if (userId == null || gymId == null) {
        throw Exception('No authenticated user or active gym');
      }

      final now = DateTime.now();
      final dateStr = '${now.year.toString().padLeft(4, '0')}-'
          '${now.month.toString().padLeft(2, '0')}-'
          '${now.day.toString().padLeft(2, '0')}';

      double? parse(String key) =>
          double.tryParse(_controllers[key]?.text ?? '');

      final weight = parse('weight');
      final measurementValues = <String, dynamic>{
        if (parse('chest') != null) 'chest': parse('chest'),
        if (parse('waist') != null) 'waist': parse('waist'),
        if (parse('hips') != null) 'hips': parse('hips'),
        if (parse('leftBicep') != null) 'leftArm': parse('leftBicep'),
        if (parse('rightBicep') != null) 'rightArm': parse('rightBicep'),
        if (parse('leftThigh') != null) 'leftThigh': parse('leftThigh'),
        if (parse('rightThigh') != null) 'rightThigh': parse('rightThigh'),
      };

      if (weight == null && measurementValues.isEmpty) {
        if (mounted) {
          setState(() => _isSaving = false);
          final isArabic =
              Localizations.localeOf(context).languageCode == 'ar';
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(isArabic
                  ? 'أدخل على الأقل قياساً واحداً قبل الحفظ'
                  : 'Enter at least one measurement before saving'),
            ),
          );
        }
        return;
      }

      final repo = ref.read(progressRepositoryProvider);
      await repo.logBodyMetric({
        'userId': userId,
        'gymId': gymId,
        'date': dateStr,
        if (weight != null) 'weight': weight,
        if (weight != null) 'weightUnit': 'kg',
        if (measurementValues.isNotEmpty)
          'measurements': {
            ...measurementValues,
            'measurementUnit': 'cm',
          },
        'source': 'SelfReported',
        'loggedBy': userId,
      });

      ref.invalidate(progressDashboardDetailProvider);
      ref.invalidate(bodyMetricsProvider);
      // Keep the Home weight tile and leaderboard in sync with the new value.
      ref.invalidate(progressDashboardProvider);
      ref.invalidate(leaderboardProvider);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) {
        final isArabicErr = Localizations.localeOf(context).languageCode == 'ar';
        final msg = e is ApiException && e.statusCode == 409
            ? (isArabicErr
                ? 'تم تسجيل القياسات اليوم بالفعل'
                : 'Measurements already logged for today')
            : (isArabicErr ? 'تعذر حفظ القياسات' : 'Could not save measurements');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }
}
