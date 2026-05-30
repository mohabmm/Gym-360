import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/members_provider.dart';

class AddMemberScreen extends ConsumerStatefulWidget {
  const AddMemberScreen({super.key});

  @override
  ConsumerState<AddMemberScreen> createState() => _AddMemberScreenState();
}

class _AddMemberScreenState extends ConsumerState<AddMemberScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();
  bool _loading = false;

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
    final apiClient = ref.read(apiClientProvider);
    final gymId = ref.read(currentGymIdProvider);
    final phone = _phoneController.text.trim();
    final email = _emailController.text.trim();
    try {
      final String userId;
      try {
        userId = await apiClient.post<String>(
          ApiEndpoints.identityStaffCreateUser,
          data: {
            'firstName': _firstNameController.text.trim(),
            'lastName': _lastNameController.text.trim(),
            'phone': phone,
            if (email.isNotEmpty) 'email': email,
          },
          parser: (data) => (data as Map<String, dynamic>)['userId'] as String,
        );
      } on ApiException catch (e) {
        // 409 = phone/email already belongs to an existing member. Offer to re-link
        // them to this gym instead of creating a duplicate (matches staff web).
        if (e.statusCode == 409) {
          await _offerLinkExisting(phone);
          return;
        }
        rethrow;
      }

      await apiClient.post<void>(
        ApiEndpoints.gymMembers(gymId),
        data: {'userId': userId},
        parser: (_) {},
      );

      if (mounted) {
        ref.invalidate(membersProvider);
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e is ApiException ? e.message : e.toString())));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// Resolves the existing trainee behind a duplicate phone, asks staff to confirm,
  /// then re-links them to this gym via staff-link-existing (no duplicate created).
  Future<void> _offerLinkExisting(String phone) async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final apiClient = ref.read(apiClientProvider);

    Map<String, dynamic> match;
    try {
      match = await apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.identityStaffLookupUser,
        queryParameters: {'phone': phone},
        parser: (data) => data as Map<String, dynamic>,
      );
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
      return;
    }

    final name =
        '${match['firstName'] ?? ''} ${match['lastName'] ?? ''}'.trim();
    final alreadyMember = match['isAlreadyMemberOfThisGym'] == true;
    if (!mounted) return;

    if (alreadyMember) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? '$name عضو بالفعل في هذه الصالة'
            : '$name is already a member of this gym'),
      ));
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'عضو موجود' : 'Existing member'),
        content: Text(isArabic
            ? 'يوجد عضو مسجل بهذا الرقم: $name (${match['email'] ?? ''}). إضافته إلى صالتك؟'
            : 'A member with this phone already exists: $name (${match['email'] ?? ''}). Add them to your gym?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'إلغاء' : 'Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'إضافة' : 'Add'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await apiClient.post<void>(
        ApiEndpoints.identityStaffLinkExisting,
        data: {'phone': phone},
        parser: (_) {},
      );
      if (mounted) {
        ref.invalidate(membersProvider);
        context.pop();
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اضافة عضو' : 'Add Member'),
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
                    controller: _phoneController,
                    label: isArabic ? 'رقم الهاتف' : 'Phone Number',
                    keyboardType: TextInputType.phone,
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _emailController,
                    label: isArabic ? 'البريد الالكتروني' : 'Email (optional)',
                    keyboardType: TextInputType.emailAddress,
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'اضافة العضو' : 'Add Member',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}
