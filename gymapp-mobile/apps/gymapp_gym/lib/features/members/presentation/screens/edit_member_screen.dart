import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/members_provider.dart';
import '../../../../core/error_text.dart';

class EditMemberScreen extends ConsumerStatefulWidget {
  final String memberId;

  const EditMemberScreen({super.key, required this.memberId});

  @override
  ConsumerState<EditMemberScreen> createState() => _EditMemberScreenState();
}

class _EditMemberScreenState extends ConsumerState<EditMemberScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();
  bool _loading = false;
  bool _initialized = false;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  void _initializeForm(UserModel member) {
    if (_initialized) return;
    _initialized = true;
    _firstNameController.text = member.firstName;
    _lastNameController.text = member.lastName;
    _phoneController.text = member.phone ?? '';
    _emailController.text = member.email ?? '';
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      // migrated to currentGymIdProvider
      final gymId = ref.read(currentGymIdProvider);
      await apiClient.put<void>(
        ApiEndpoints.gymMemberById(GymId(gymId), MemberId(widget.memberId)),
        data: {
          'firstName': _firstNameController.text.trim(),
          'lastName': _lastNameController.text.trim(),
          'phone': _phoneController.text.trim(),
          'email': _emailController.text.trim(),
        },
        parser: (_) {},
      );
      if (mounted) {
        ref.invalidate(memberDetailProvider(widget.memberId));
        ref.invalidate(membersProvider);
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final member = ref.watch(memberDetailProvider(widget.memberId));
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تعديل العضو' : 'Edit Member'),
      ),
      body: member.when(
        data: (m) {
          _initializeForm(m);
          return Form(
            key: _formKey,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                AppTextField(
                  controller: _firstNameController,
                  label: isArabic ? 'الاسم الاول' : 'First Name',
                  validator: (v) =>
                      v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                ),
                const SizedBox(height: 16),
                AppTextField(
                  controller: _lastNameController,
                  label: isArabic ? 'الاسم الاخير' : 'Last Name',
                  validator: (v) =>
                      v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                ),
                const SizedBox(height: 16),
                AppTextField(
                  controller: _phoneController,
                  label: isArabic ? 'رقم الهاتف' : 'Phone Number',
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 16),
                AppTextField(
                  controller: _emailController,
                  label: isArabic ? 'البريد الالكتروني' : 'Email',
                  keyboardType: TextInputType.emailAddress,
                  // Match add-member: the backend rejects malformed emails with
                  // a 400, so validate inline when one is provided.
                  validator: (v) {
                    final t = v?.trim() ?? '';
                    if (t.isEmpty) return null;
                    return RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(t)
                        ? null
                        : (isArabic
                            ? 'بريد إلكتروني غير صالح'
                            : 'Invalid email address');
                  },
                ),
                const SizedBox(height: 32),
                AppButton(
                  text: isArabic ? 'حفظ التعديلات' : 'Save Changes',
                  isLoading: _loading,
                  onPressed: _submit,
                ),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: isArabic ? 'خطأ في تحميل البيانات' : 'Error loading data',
          onRetry: () => ref.invalidate(memberDetailProvider(widget.memberId)),
        ),
      ),
    );
  }
}
