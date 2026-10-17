import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/staff_provider.dart';
import '../../../../core/error_text.dart';

class AddStaffScreen extends ConsumerStatefulWidget {
  const AddStaffScreen({super.key});

  @override
  ConsumerState<AddStaffScreen> createState() => _AddStaffScreenState();
}

class _AddStaffScreenState extends ConsumerState<AddStaffScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  String _role = AppConstants.roleReceptionist;
  bool _loading = false;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);

      final userId = await apiClient.post<String>(
        ApiEndpoints.identityStaffCreateUser,
        data: {
          'firstName': _firstNameController.text.trim(),
          'lastName': _lastNameController.text.trim(),
          'email': _emailController.text.trim(),
          if (_phoneController.text.trim().isNotEmpty)
            'phone': _phoneController.text.trim(),
        },
        parser: (data) => (data as Map<String, dynamic>)['userId'] as String,
      );

      await apiClient.post<void>(
        ApiEndpoints.gymStaff(GymId(gymId)),
        data: {'userId': userId, 'role': _role},
        parser: (_) {},
      );

      if (mounted) {
        ref.invalidate(staffListProvider);
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اضافة موظف' : 'Add Staff'),
      ),
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
                    controller: _emailController,
                    label: isArabic ? 'البريد الالكتروني' : 'Email',
                    keyboardType: TextInputType.emailAddress,
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _phoneController,
                    label: isArabic ? 'رقم الهاتف' : 'Phone',
                    keyboardType: TextInputType.phone,
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    isArabic ? 'الدور' : 'Role',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          fontWeight: FontWeight.w500,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      AppConstants.roleReceptionist,
                      AppConstants.roleManager,
                    ].map((r) {
                      final labels = {
                        AppConstants.roleReceptionist: isArabic ? 'موظف استقبال' : 'Receptionist',
                        AppConstants.roleManager: isArabic ? 'مدير' : 'Manager',
                      };
                      return ChoiceChip(
                        label: Text(labels[r] ?? r),
                        selected: _role == r,
                        onSelected: (_) => setState(() => _role = r),
                      );
                    }).toList(),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'اضافة الموظف' : 'Add Staff Member',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}
