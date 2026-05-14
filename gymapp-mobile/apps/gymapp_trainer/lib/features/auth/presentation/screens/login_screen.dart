import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) return;

    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authProvider.notifier).login(
            _emailController.text.trim(),
            _passwordController.text,
          );
    } on ApiException catch (e) {
      setState(() => _errorMessage = _localizeApiError(e, isArabic));
    } catch (e) {
      setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  String _localizeApiError(ApiException e, bool isArabic) {
    switch (e.errorCode) {
      case 'ROLE_TRAINEE_NOT_ALLOWED':
        return isArabic
            ? 'هذا الحساب حساب متدرب. الرجاء تسجيل الدخول من تطبيق المتدرب.'
            : 'This is a trainee account. Please sign in from the Trainee app.';
      case 'ROLE_NOT_ALLOWED':
        return isArabic
            ? 'هذا التطبيق مخصص للمدربين فقط. الحساب المستخدم ليس حساب مدرب.'
            : 'This app is for trainers only. The account you used is not a trainer account.';
      default:
        return e.message;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 60),
                // Logo and title
                Image.asset(
                  'assets/images/logo.png',
                  width: MediaQuery.of(context).size.shortestSide * 0.22,
                  height: MediaQuery.of(context).size.shortestSide * 0.22,
                  fit: BoxFit.contain,
                ),
                const SizedBox(height: 16),
                Text(
                  isArabic ? 'تطبيق المدرب' : 'Trainer App',
                  style: theme.textTheme.headlineMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  isArabic
                      ? 'سجل دخولك لادارة عملائك وجلساتك'
                      : 'Sign in to manage your clients and sessions',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 48),

                // Error message
                if (_errorMessage != null) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.error.withOpacity(0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.error_outline,
                            color: AppColors.error, size: 20),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            _errorMessage!,
                            style: const TextStyle(color: AppColors.error),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                ],

                // Email field
                AppTextField(
                  label: isArabic ? 'البريد الالكتروني' : 'Email',
                  hint: isArabic ? 'ادخل بريدك الالكتروني' : 'Enter your email',
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  prefixIcon: const Icon(Icons.email_outlined),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return isArabic
                          ? 'البريد الالكتروني مطلوب'
                          : 'Email is required';
                    }
                    if (Validators.email(value) != null) {
                      return isArabic
                          ? 'بريد الكتروني غير صالح'
                          : 'Invalid email address';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 16),

                // Password field
                AppPasswordField(
                  label: isArabic ? 'كلمة المرور' : 'Password',
                  hint: isArabic ? 'ادخل كلمة المرور' : 'Enter your password',
                  controller: _passwordController,
                  textInputAction: TextInputAction.done,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return isArabic
                          ? 'كلمة المرور مطلوبة'
                          : 'Password is required';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 24),

                // Login button
                AppButton(
                  text: isArabic ? 'تسجيل الدخول' : 'Sign In',
                  onPressed: _isLoading ? null : _login,
                  isLoading: _isLoading,
                ),
                const SizedBox(height: 16),

                // Register link
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      isArabic ? 'ليس لديك حساب؟' : "Don't have an account?",
                      style: theme.textTheme.bodyMedium,
                    ),
                    TextButton(
                      onPressed: () => context.push(AppRoutes.register),
                      child: Text(
                        isArabic ? 'سجل الان' : 'Register',
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
