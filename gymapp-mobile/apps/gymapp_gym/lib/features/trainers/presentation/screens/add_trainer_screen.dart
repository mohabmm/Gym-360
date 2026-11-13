import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../providers/trainers_provider.dart';

/// Starting password the backend gives a staff-created trainer.
const String trainerInitialPassword = '123456';

/// Shown when the phone/email already belongs to a trainer on this gym's roster.
const String trainerAlreadyAddedMessage = 'The trainer is already added to this gym.';

/// Creates the trainer's account (or reuses the one behind the phone/email),
/// then adds them to this gym's roster. Same two calls as the staff web app.
Future<void> createTrainerForGym(
  ApiClient apiClient, {
  required String gymId,
  required String firstName,
  required String lastName,
  required String phone,
  String? email,
}) async {
  final created = await apiClient.post<Map<String, dynamic>>(
    ApiEndpoints.identityStaffCreateTrainer,
    data: {
      'firstName': firstName,
      'lastName': lastName,
      'phone': phone,
      'email': (email == null || email.isEmpty) ? null : email,
    },
    parser: (data) => data as Map<String, dynamic>,
  );
  await apiClient.post<void>(
    ApiEndpoints.gymTrainers(GymId(gymId)),
    data: {'trainerId': created['userId']},
    parser: (_) {},
  );
}

class AddTrainerScreen extends ConsumerStatefulWidget {
  const AddTrainerScreen({super.key});

  @override
  ConsumerState<AddTrainerScreen> createState() => _AddTrainerScreenState();
}

class _AddTrainerScreenState extends ConsumerState<AddTrainerScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();
  bool _loading = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    final phone = _phoneController.text.trim();
    try {
      await createTrainerForGym(
        ref.read(apiClientProvider),
        gymId: ref.read(currentGymIdProvider),
        firstName: _firstNameController.text.trim(),
        lastName: _lastNameController.text.trim(),
        phone: phone,
        email: _emailController.text.trim(),
      );
      ref.invalidate(trainersProvider);
      if (!mounted) return;
      await _showLoginDetails(phone);
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) _showError(e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _showError(Object error) {
    final alreadyAdded =
        error is ApiException && error.message.trim() == trainerAlreadyAddedMessage;
    final text = alreadyAdded && _isArabic
        ? 'هذا المدرب مضاف بالفعل الى الصالة.'
        : staffErrorText(error, isArabic: _isArabic);
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _showLoginDetails(String phone) {
    final isArabic = _isArabic;
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تمت اضافة المدرب' : 'Trainer added'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(isArabic
                ? 'يسجل المدرب الدخول في تطبيق المدرب بهذه البيانات، ثم يغير كلمة المرور:'
                : 'The trainer signs in to the Trainer app with these details, then changes the password:'),
            const SizedBox(height: 12),
            _LoginLine(label: isArabic ? 'الهاتف' : 'Phone', value: phone),
            _LoginLine(
              label: isArabic ? 'كلمة المرور' : 'Password',
              value: trainerInitialPassword,
            ),
          ],
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(isArabic ? 'تم' : 'Done'),
          ),
        ],
      ),
    );
  }

  String? _required(String? v) =>
      (v == null || v.trim().isEmpty) ? (_isArabic ? 'مطلوب' : 'Required') : null;

  String? _phone(String? v) {
    final missing = _required(v);
    if (missing != null) return missing;
    return v!.trim().length < 7
        ? (_isArabic ? 'رقم الهاتف قصير جدا' : 'Phone number is too short')
        : null;
  }

  String? _email(String? v) {
    final t = v?.trim() ?? '';
    if (t.isEmpty) return null;
    return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(t)
        ? null
        : (_isArabic ? 'بريد إلكتروني غير صالح' : 'Invalid email address');
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic;
    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'اضافة مدرب' : 'Add Trainer')),
      body: Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AppTextField(
                    controller: _firstNameController,
                    label: isArabic ? 'الاسم الاول' : 'First Name',
                    validator: _required,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _lastNameController,
                    label: isArabic ? 'الاسم الاخير' : 'Last Name',
                    validator: _required,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _phoneController,
                    label: isArabic ? 'رقم الهاتف' : 'Phone Number',
                    keyboardType: TextInputType.phone,
                    validator: _phone,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _emailController,
                    label: isArabic ? 'البريد الالكتروني (اختياري)' : 'Email (optional)',
                    keyboardType: TextInputType.emailAddress,
                    validator: _email,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    isArabic
                        ? 'كلمة المرور الاولى للمدرب: $trainerInitialPassword'
                        : "Trainer's first password: $trainerInitialPassword",
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'اضافة المدرب' : 'Add Trainer',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}

class _LoginLine extends StatelessWidget {
  final String label;
  final String value;
  const _LoginLine({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Text('$label: '),
          // Phone and password read left-to-right even in Arabic.
          SelectableText(
            value,
            textDirection: TextDirection.ltr,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ],
      ),
    );
  }
}
