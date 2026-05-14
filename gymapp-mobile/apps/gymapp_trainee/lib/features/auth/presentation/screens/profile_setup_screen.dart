import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/auth_providers.dart';

class ProfileSetupScreen extends ConsumerStatefulWidget {
  const ProfileSetupScreen({super.key});

  @override
  ConsumerState<ProfileSetupScreen> createState() => _ProfileSetupScreenState();
}

class _ProfileSetupScreenState extends ConsumerState<ProfileSetupScreen> {
  String? _selectedGender;
  DateTime? _dateOfBirth;
  final _heightController = TextEditingController();
  final _weightController = TextEditingController();
  String? _fitnessLevel;
  bool _isLoading = false;

  final _fitnessLevels = ['beginner', 'intermediate', 'advanced'];

  @override
  void dispose() {
    _heightController.dispose();
    _weightController.dispose();
    super.dispose();
  }

  Future<void> _handleSave() async {
    setState(() => _isLoading = true);
    try {
      final userId = ref.read(authProvider).valueOrNull?.user?.id;
      if (userId == null) {
        throw Exception('No authenticated user');
      }
      final repo = ref.read(authRepositoryProvider);
      final updated = await repo.updateProfile(
        userId: userId,
        request: UserProfileUpdateRequest(
          gender: _selectedGender,
          dateOfBirth: _dateOfBirth,
          height: double.tryParse(_heightController.text),
          weight: double.tryParse(_weightController.text),
          fitnessLevel: _fitnessLevel,
        ),
      );
      ref.read(authProvider.notifier).updateUser(updated);
      if (mounted) {
        context.go(AppRoutes.linkGym);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'إعداد الملف الشخصي' : 'Profile Setup'),
        actions: [
          TextButton(
            onPressed: () => context.go(AppRoutes.linkGym),
            child: Text(isArabic ? 'تخطي' : 'Skip'),
          ),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                isArabic
                    ? 'أخبرنا عن نفسك'
                    : 'Tell us about yourself',
                style: theme.textTheme.titleLarge,
              ),
              const SizedBox(height: 8),
              Text(
                isArabic
                    ? 'هذا يساعدنا في تخصيص تجربتك'
                    : 'This helps us personalize your experience',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                ),
              ),
              const SizedBox(height: 24),

              // Gender
              Text(
                isArabic ? 'الجنس' : 'Gender',
                style: theme.textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: [
                  ButtonSegment(
                    value: 'male',
                    label: Text(isArabic ? 'ذكر' : 'Male'),
                    icon: const Icon(Icons.male),
                  ),
                  ButtonSegment(
                    value: 'female',
                    label: Text(isArabic ? 'أنثى' : 'Female'),
                    icon: const Icon(Icons.female),
                  ),
                ],
                selected: _selectedGender != null ? {_selectedGender!} : {},
                onSelectionChanged: (selection) {
                  setState(
                      () => _selectedGender = selection.firstOrNull);
                },
                emptySelectionAllowed: true,
              ),
              const SizedBox(height: 24),

              // Date of birth
              Text(
                isArabic ? 'تاريخ الميلاد' : 'Date of Birth',
                style: theme.textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate:
                        DateTime.now().subtract(const Duration(days: 7300)),
                    firstDate: DateTime(1940),
                    lastDate: DateTime.now(),
                  );
                  if (picked != null) {
                    setState(() => _dateOfBirth = picked);
                  }
                },
                icon: const Icon(Icons.calendar_today_outlined),
                label: Text(
                  _dateOfBirth != null
                      ? DateFormatter.formatLong(
                          _dateOfBirth!,
                          locale: isArabic ? 'ar' : 'en',
                        )
                      : (isArabic ? 'اختر التاريخ' : 'Select Date'),
                ),
              ),
              const SizedBox(height: 24),

              // Height
              AppTextField(
                label: isArabic ? 'الطول (سم)' : 'Height (cm)',
                hint: isArabic ? 'الطول (سم)' : 'Height (cm)',
                controller: _heightController,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                ],
                prefixIcon: const Icon(Icons.height),
              ),
              const SizedBox(height: 16),

              // Weight
              AppTextField(
                label: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
                hint: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
                controller: _weightController,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [
                  FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                ],
                prefixIcon: const Icon(Icons.monitor_weight_outlined),
              ),
              const SizedBox(height: 24),

              // Fitness Level
              Text(
                isArabic ? 'مستوى اللياقة' : 'Fitness Level',
                style: theme.textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              SegmentedButton<String>(
                segments: _fitnessLevels.map((level) {
                  String label;
                  if (isArabic) {
                    label = level == 'beginner'
                        ? 'مبتدئ'
                        : level == 'intermediate'
                            ? 'متوسط'
                            : 'متقدم';
                  } else {
                    label = level[0].toUpperCase() + level.substring(1);
                  }
                  return ButtonSegment(value: level, label: Text(label));
                }).toList(),
                selected: _fitnessLevel != null ? {_fitnessLevel!} : {},
                onSelectionChanged: (selection) {
                  setState(() => _fitnessLevel = selection.firstOrNull);
                },
                emptySelectionAllowed: true,
              ),
              const SizedBox(height: 32),

              AppButton(
                text: isArabic ? 'حفظ والمتابعة' : 'Save & Continue',
                isLoading: _isLoading,
                onPressed: _isLoading ? null : _handleSave,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
