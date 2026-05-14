import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/router.dart';
import '../providers/auth_providers.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  void _handleNext() {
    if (!_formKey.currentState!.validate()) return;

    final notifier = ref.read(registrationProvider.notifier);
    notifier.updateFirstName(_firstNameController.text.trim());
    notifier.updateLastName(_lastNameController.text.trim());
    notifier.updateEmail(_emailController.text.trim());
    notifier.updatePhone(
      _phoneController.text.trim().isNotEmpty
          ? _phoneController.text.trim()
          : null,
    );
    notifier.updatePassword(_passwordController.text);

    context.go(AppRoutes.consent);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final regState = ref.watch(registrationProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'إنشاء حساب' : 'Create Account'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go(AppRoutes.login),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  isArabic ? 'بياناتك الشخصية' : 'Personal Information',
                  style: theme.textTheme.titleLarge,
                ),
                const SizedBox(height: 24),

                // First name
                AppTextField(
                  label: isArabic ? 'الاسم الأول' : 'First Name',
                  controller: _firstNameController,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.person_outlined),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return isArabic
                          ? 'الاسم الأول مطلوب'
                          : 'First name is required';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Last name
                AppTextField(
                  label: isArabic ? 'اسم العائلة' : 'Last Name',
                  controller: _lastNameController,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.person_outlined),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return isArabic
                          ? 'اسم العائلة مطلوب'
                          : 'Last name is required';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Email
                AppTextField(
                  label: isArabic ? 'البريد الإلكتروني' : 'Email',
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.email_outlined),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return isArabic
                          ? 'البريد الإلكتروني مطلوب'
                          : 'Email is required';
                    }
                    if (!value.contains('@')) {
                      return isArabic
                          ? 'بريد إلكتروني غير صالح'
                          : 'Invalid email';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Phone (optional — Egyptian local format like 01128506941
                // is auto-normalized to +201128506941 on the backend).
                AppTextField(
                  label: isArabic ? 'رقم الهاتف (اختياري)' : 'Phone (Optional)',
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.phone_outlined),
                ),
                const SizedBox(height: 16),

                // Password
                AppPasswordField(
                  label: isArabic ? 'كلمة المرور' : 'Password',
                  controller: _passwordController,
                  textInputAction: TextInputAction.next,
                  validator: (value) {
                    if (value == null || value.length < 8) {
                      return isArabic
                          ? 'كلمة المرور يجب أن تكون 8 أحرف على الأقل'
                          : 'Password must be at least 8 characters';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Confirm Password
                AppPasswordField(
                  label: isArabic ? 'تأكيد كلمة المرور' : 'Confirm Password',
                  controller: _confirmPasswordController,
                  textInputAction: TextInputAction.done,
                  validator: (value) {
                    if (value != _passwordController.text) {
                      return isArabic
                          ? 'كلمات المرور غير متطابقة'
                          : 'Passwords do not match';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 8),

                if (regState.error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Text(
                      regState.error!,
                      style: TextStyle(color: theme.colorScheme.error),
                      textAlign: TextAlign.center,
                    ),
                  ),

                const SizedBox(height: 24),
                AppButton(
                  text: isArabic ? 'التالي' : 'Next',
                  onPressed: _handleNext,
                ),
                const SizedBox(height: 16),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      isArabic ? 'لديك حساب بالفعل؟' : 'Already have an account?',
                      style: theme.textTheme.bodyMedium,
                    ),
                    TextButton(
                      onPressed: () => context.go(AppRoutes.login),
                      child: Text(
                        isArabic ? 'تسجيل الدخول' : 'Login',
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
