import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../../core/staff_created_account.dart';
import '../providers/trainers_provider.dart';


/// Shown when the phone/email already belongs to a trainer on this gym's roster.
const String trainerAlreadyAddedMessage = 'The trainer is already added to this gym.';

/// The details staff enter for a new trainer.
class NewTrainer {
  final String firstName;
  final String lastName;
  final String phone;
  final String? email;

  const NewTrainer({required this.firstName, required this.lastName, required this.phone, this.email});

  /// Body for staff-create-trainer; a blank email is sent as null.
  Map<String, dynamic> toJson() => {
        'firstName': firstName,
        'lastName': lastName,
        'phone': phone,
        'email': (email == null || email!.isEmpty) ? null : email,
      };
}

/// The trainer's user id, and their first password — null when they already had an
/// account (the backend leaves an existing trainer's password unchanged).
typedef CreatedTrainer = ({String userId, String? initialPassword});

/// Creates the trainer's account (or reuses the one behind the phone/email),
/// then adds them to this gym's roster. Same two calls as the staff web app.
Future<CreatedTrainer> createTrainerForGym(ApiClient apiClient,
    {required String gymId, required NewTrainer trainer}) async {
  final created = await apiClient.post<Map<String, dynamic>>(
    ApiEndpoints.identityStaffCreateTrainer,
    data: trainer.toJson(),
    parser: (data) => data as Map<String, dynamic>,
  );
  final userId = created['userId'] as String;
  await apiClient.post<void>(
    ApiEndpoints.gymTrainers(GymId(gymId)),
    data: {'trainerId': userId},
    parser: (_) {},
  );
  return (userId: userId, initialPassword: created['initialPassword'] as String?);
}

/// Opens Add Trainer and returns the new coach's user id, or null if staff backed out.
Future<String?> pushAddTrainer(BuildContext context) => context.push<String>(RoutePaths.addTrainer);

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
      final created = await createTrainerForGym(
        ref.read(apiClientProvider),
        gymId: ref.read(currentGymIdProvider),
        trainer: NewTrainer(
          firstName: _firstNameController.text.trim(),
          lastName: _lastNameController.text.trim(),
          phone: phone,
          email: _emailController.text.trim(),
        ),
      );
      ref.invalidate(trainersProvider);
      if (!mounted) return;
      await _showLoginDetails(phone, created.initialPassword);
      // Hand the id back so a caller (e.g. Add Member) can select the new coach.
      if (mounted) context.pop(created.userId);
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

  Future<void> _showLoginDetails(String phone, String? initialPassword) {
    final isArabic = _isArabic;
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تمت اضافة المدرب' : 'Trainer added'),
        content: _LoginDetails(phone: phone, initialPassword: initialPassword, isArabic: isArabic),
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
                        ? 'كلمة المرور الاولى للمدرب الجديد: $kStaffCreatedAccountPassword'
                        : "A new trainer's first password: $kStaffCreatedAccountPassword",
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

/// Login details after adding: phone + first password for a new account, or a note
/// that an existing trainer keeps signing in with their own password.
class _LoginDetails extends StatelessWidget {
  final String phone;
  final String? initialPassword;
  final bool isArabic;
  const _LoginDetails({required this.phone, required this.initialPassword, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final password = initialPassword;
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(password == null
            ? (isArabic
                ? 'هذا المدرب لديه حساب بالفعل. يسجل الدخول في تطبيق المدرب بكلمة المرور الحالية الخاصة به:'
                : 'This trainer already has an account. They sign in to the Trainer app with their current password:')
            : (isArabic
                ? 'يسجل المدرب الدخول في تطبيق المدرب بهذه البيانات، ثم يغير كلمة المرور:'
                : 'The trainer signs in to the Trainer app with these details, then changes the password:')),
        const SizedBox(height: 12),
        _LoginLine(label: isArabic ? 'الهاتف' : 'Phone', value: phone),
        if (password != null) _LoginLine(label: isArabic ? 'كلمة المرور' : 'Password', value: password),
      ],
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
