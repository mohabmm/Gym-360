import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

class OnboardingFormScreen extends ConsumerStatefulWidget {
  final String clientId;

  const OnboardingFormScreen({super.key, required this.clientId});

  @override
  ConsumerState<OnboardingFormScreen> createState() =>
      _OnboardingFormScreenState();
}

class _OnboardingFormScreenState extends ConsumerState<OnboardingFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _medicalHistoryController = TextEditingController();
  final _fitnessGoalsController = TextEditingController();
  final _injuriesController = TextEditingController();
  final _medicationsController = TextEditingController();
  String? _fitnessLevel;
  bool _isSaving = false;

  @override
  void dispose() {
    _medicalHistoryController.dispose();
    _fitnessGoalsController.dispose();
    _injuriesController.dispose();
    _medicationsController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _isSaving = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(activeGymIdProvider);
      if (gymId == null) {
        throw ApiException(
          message: 'No active gym selected.',
          errorCode: 'NO_ACTIVE_GYM',
        );
      }
      await apiClient.postVoid(
        ApiEndpoints.gymMemberOnboarding(gymId, widget.clientId),
        data: {
          'medicalHistory': _medicalHistoryController.text,
          'fitnessGoals': _fitnessGoalsController.text,
          'injuries': _injuriesController.text,
          'medications': _medicationsController.text,
          'fitnessLevel': _fitnessLevel,
        },
      );
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'نموذج الاستقبال' : 'Intake Form'),
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            TextButton(
              onPressed: _save,
              child: Text(isArabic ? 'حفظ' : 'Save'),
            ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              isArabic
                  ? 'مستوى اللياقة البدنية'
                  : 'Fitness Level',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 8),
            DropdownButtonFormField<String>(
              value: _fitnessLevel,
              hint: Text(isArabic ? 'اختر المستوى' : 'Select level'),
              items: [
                DropdownMenuItem(
                  value: 'beginner',
                  child: Text(isArabic ? 'مبتدئ' : 'Beginner'),
                ),
                DropdownMenuItem(
                  value: 'intermediate',
                  child: Text(isArabic ? 'متوسط' : 'Intermediate'),
                ),
                DropdownMenuItem(
                  value: 'advanced',
                  child: Text(isArabic ? 'متقدم' : 'Advanced'),
                ),
              ],
              onChanged: (value) => setState(() => _fitnessLevel = value),
              decoration: const InputDecoration(border: OutlineInputBorder()),
            ),
            const SizedBox(height: 16),
            AppTextField(
              label: isArabic ? 'اهداف اللياقة' : 'Fitness Goals',
              hint: isArabic
                  ? 'مثال: خسارة وزن، بناء عضلات'
                  : 'e.g., weight loss, muscle gain',
              controller: _fitnessGoalsController,
              maxLines: 3,
            ),
            const SizedBox(height: 16),
            AppTextField(
              label: isArabic ? 'التاريخ الطبي' : 'Medical History',
              hint: isArabic
                  ? 'اي حالات طبية يجب معرفتها'
                  : 'Any medical conditions we should know about',
              controller: _medicalHistoryController,
              maxLines: 3,
            ),
            const SizedBox(height: 16),
            AppTextField(
              label: isArabic ? 'الاصابات' : 'Injuries',
              hint: isArabic
                  ? 'الاصابات الحالية او السابقة'
                  : 'Current or past injuries',
              controller: _injuriesController,
              maxLines: 2,
            ),
            const SizedBox(height: 16),
            AppTextField(
              label: isArabic ? 'الادوية' : 'Medications',
              hint: isArabic
                  ? 'الادوية التي يتناولها العميل'
                  : 'Medications currently taken',
              controller: _medicationsController,
              maxLines: 2,
            ),
            const SizedBox(height: 32),
            AppButton(
              text: isArabic ? 'حفظ النموذج' : 'Save Form',
              onPressed: _isSaving ? null : _save,
              isLoading: _isSaving,
            ),
          ],
        ),
      ),
    );
  }
}
