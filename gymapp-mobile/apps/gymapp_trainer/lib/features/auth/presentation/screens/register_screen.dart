import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

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
  bool _isLoading = false;
  bool _dataProcessingConsent = false;
  bool _marketingConsent = false;
  String? _errorMessage;

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

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_dataProcessingConsent) {
      setState(() {
        _errorMessage = Localizations.localeOf(context).languageCode == 'ar'
            ? 'يجب الموافقة على سياسة معالجة البيانات'
            : 'You must accept the data processing policy';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    final phone = _phoneController.text.trim();

    try {
      await ref.read(authProvider.notifier).register(
            RegisterRequest(
              email: _emailController.text.trim(),
              password: _passwordController.text,
              firstName: _firstNameController.text.trim(),
              lastName: _lastNameController.text.trim(),
              phone: phone,
              role: 'Trainer',
              dataProcessingConsent: _dataProcessingConsent,
              marketingConsent: _marketingConsent,
            ),
          );
    } on ApiException catch (e) {
      setState(() => _errorMessage = e.message);
    } catch (e) {
      setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تسجيل مدرب جديد' : 'Trainer Registration'),
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
                  isArabic
                      ? 'سجل كمدرب شخصي. حسابك سيكون معلقا حتى يوافق مالك الصالة.'
                      : 'Register as a personal trainer. Your account will be pending until the gym owner approves it.',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                ),
                const SizedBox(height: 24),

                if (_errorMessage != null) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.error.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      _errorMessage!,
                      style: const TextStyle(color: AppColors.error),
                    ),
                  ),
                  const SizedBox(height: 16),
                ],

                // Name fields row
                Row(
                  children: [
                    Expanded(
                      child: AppTextField(
                        label: isArabic ? 'الاسم الاول' : 'First Name',
                        controller: _firstNameController,
                        textInputAction: TextInputAction.next,
                        validator: (v) => v == null || v.isEmpty
                            ? (isArabic ? 'مطلوب' : 'Required')
                            : null,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: AppTextField(
                        label: isArabic ? 'الاسم الاخير' : 'Last Name',
                        controller: _lastNameController,
                        textInputAction: TextInputAction.next,
                        validator: (v) => v == null || v.isEmpty
                            ? (isArabic ? 'مطلوب' : 'Required')
                            : null,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                AppTextField(
                  label: isArabic ? 'البريد الالكتروني' : 'Email',
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.email_outlined),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return isArabic ? 'مطلوب' : 'Required';
                    }
                    if (Validators.email(value) != null) {
                      return isArabic
                          ? 'بريد الكتروني غير صالح'
                          : 'Invalid email';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                AppTextField(
                  label: isArabic ? 'رقم الهاتف (اختياري)' : 'Phone (optional)',
                  controller: _phoneController,
                  keyboardType: TextInputType.phone,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.phone_outlined),
                ),
                const SizedBox(height: 16),

                AppPasswordField(
                  label: isArabic ? 'كلمة المرور' : 'Password',
                  controller: _passwordController,
                  textInputAction: TextInputAction.next,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return isArabic ? 'مطلوب' : 'Required';
                    }
                    if (value.length < 8) {
                      return isArabic
                          ? '8 احرف على الاقل'
                          : 'At least 8 characters';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                AppPasswordField(
                  label: isArabic ? 'تاكيد كلمة المرور' : 'Confirm Password',
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
                const SizedBox(height: 24),

                // PDPL Consent
                CheckboxListTile(
                  value: _dataProcessingConsent,
                  onChanged: (v) =>
                      setState(() => _dataProcessingConsent = v ?? false),
                  title: Text(
                    isArabic
                        ? 'اوافق على سياسة معالجة البيانات الشخصية'
                        : 'I agree to the Personal Data Processing Policy',
                    style: theme.textTheme.bodySmall,
                  ),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                  dense: true,
                ),
                CheckboxListTile(
                  value: _marketingConsent,
                  onChanged: (v) =>
                      setState(() => _marketingConsent = v ?? false),
                  title: Text(
                    isArabic
                        ? 'اوافق على تلقي رسائل تسويقية (اختياري)'
                        : 'I agree to receive marketing communications (optional)',
                    style: theme.textTheme.bodySmall,
                  ),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                  dense: true,
                ),
                const SizedBox(height: 24),

                AppButton(
                  text: isArabic ? 'تسجيل' : 'Register',
                  onPressed: _isLoading ? null : _register,
                  isLoading: _isLoading,
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
                      onPressed: () => context.pop(),
                      child: Text(isArabic ? 'تسجيل الدخول' : 'Sign In'),
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
