import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  bool _isSaving = false;
  bool _initialized = false;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  void _initFromUser(UserModel user) {
    if (_initialized) return;
    _initialized = true;
    _firstNameController.text = user.firstName;
    _lastNameController.text = user.lastName;
    _phoneController.text = user.phone ?? '';
  }

  Future<void> _save() async {
    setState(() => _isSaving = true);
    try {
      final userId = ref.read(authProvider).user?.id;
      if (userId == null) {
        throw Exception('No authenticated user');
      }
      final apiClient = ref.read(apiClientProvider);
      await apiClient.putVoid(
        ApiEndpoints.profile,
        data: {
          'userId': userId,
          'firstName': _firstNameController.text.trim(),
          'lastName': _lastNameController.text.trim(),
          'phone': _phoneController.text.trim().isEmpty
              ? null
              : _phoneController.text.trim(),
        },
      );
      await ref.read(authProvider.notifier).refreshProfile();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(Localizations.localeOf(context).languageCode == 'ar'
                ? 'تم حفظ التغييرات'
                : 'Changes saved'),
            backgroundColor: AppColors.success,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final auth = ref.watch(authProvider);
    final user = auth.user;

    if (user != null) _initFromUser(user);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الملف الشخصي' : 'Profile'),
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2)),
            )
          else
            TextButton(
              onPressed: _save,
              child: Text(isArabic ? 'حفظ' : 'Save'),
            ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Avatar
          Center(
            child: Stack(
              children: [
                AvatarWidget(
                  imageUrl: user?.photoUrl,
                  name: user != null
                      ? '${user.firstName} ${user.lastName}'
                      : '',
                  size: 80,
                ),
                Positioned(
                  right: 0,
                  bottom: 0,
                  child: CircleAvatar(
                    radius: 14,
                    backgroundColor: theme.colorScheme.primary,
                    child: const Icon(Icons.camera_alt,
                        size: 14, color: Colors.white),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          // Fields
          Row(
            children: [
              Expanded(
                child: AppTextField(
                  label: isArabic ? 'الاسم الاول' : 'First Name',
                  controller: _firstNameController,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: AppTextField(
                  label: isArabic ? 'اسم العائلة' : 'Last Name',
                  controller: _lastNameController,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          AppTextField(
            label: isArabic ? 'رقم الهاتف' : 'Phone Number',
            controller: _phoneController,
            keyboardType: TextInputType.phone,
          ),
          if (user != null) ...[
            const SizedBox(height: 24),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      isArabic ? 'معلومات الحساب' : 'Account Info',
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 12),
                    _InfoRow(
                      label: isArabic ? 'البريد الالكتروني' : 'Email',
                      value: user.email,
                    ),
                    _InfoRow(
                      label: isArabic ? 'الدور' : 'Role',
                      value: _localizedRole(user.role, isArabic),
                    ),
                  ],
                ),
              ),
            ),
          ],

          const SizedBox(height: 24),
          AppButton(
            text: isArabic ? 'حفظ التغييرات' : 'Save Changes',
            onPressed: _isSaving ? null : _save,
            isLoading: _isSaving,
          ),
        ],
      ),
    );
  }
}

String _localizedRole(String role, bool isArabic) {
  switch (role.toLowerCase()) {
    case 'trainer':
      return isArabic ? 'مدرب' : 'Trainer';
    case 'trainee':
    case 'member':
      return isArabic ? 'متدرب' : 'Trainee';
    case 'staff':
    case 'receptionist':
      return isArabic ? 'موظف استقبال' : 'Staff';
    case 'owner':
    case 'gym_owner':
      return isArabic ? 'مالك صالة' : 'Gym Owner';
    case 'admin':
    case 'super_admin':
      return isArabic ? 'مدير النظام' : 'Admin';
    default:
      return role;
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;

  const _InfoRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Text(
            '$label: ',
            style: theme.textTheme.bodySmall
                ?.copyWith(color: AppColors.grey500),
          ),
          Text(
            value,
            style: theme.textTheme.bodySmall
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}
