import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/auth_providers.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleForgotPassword(bool isArabic) async {
    final controller =
        TextEditingController(text: _emailController.text.trim());
    final email = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'استعادة كلمة المرور' : 'Reset Password'),
        content: TextField(
          controller: controller,
          keyboardType: TextInputType.emailAddress,
          decoration: InputDecoration(
            labelText: isArabic ? 'البريد الإلكتروني' : 'Email',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(isArabic ? 'إلغاء' : 'Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, controller.text.trim()),
            child: Text(isArabic ? 'إرسال' : 'Send'),
          ),
        ],
      ),
    );
    if (email == null || email.isEmpty || !mounted) return;

    final messenger = ScaffoldMessenger.of(context);
    try {
      final apiClient = ref.read(apiClientProvider);
      await apiClient.post<void>(
        ApiEndpoints.forgotPassword,
        data: {'email': email},
        parser: (_) {},
      );
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            isArabic
                ? 'تم إرسال رابط الاستعادة إلى بريدك'
                : 'Reset link sent to your email',
          ),
        ),
      );
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) return;

    ref.read(loginFormProvider.notifier).state =
        const LoginFormState(isLoading: true);

    try {
      final repo = ref.read(authRepositoryProvider);
      final response = await repo.login(LoginRequest(
        email: _emailController.text.trim(),
        password: _passwordController.text,
      ));
      await ref.read(authProvider.notifier).login(response);
      if (mounted) {
        ref.read(loginFormProvider.notifier).state = const LoginFormState();
      }
    } on ApiException catch (e) {
      if (mounted) {
        ref.read(loginFormProvider.notifier).state =
            LoginFormState(error: e.message);
      }
    } catch (e) {
      if (mounted) {
        ref.read(loginFormProvider.notifier).state =
            LoginFormState(error: e.toString());
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final loginState = ref.watch(loginFormProvider);

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 48),
                // Logo
                Image.asset(
                  'assets/images/logo.png',
                  width: MediaQuery.of(context).size.shortestSide * 0.22,
                  height: MediaQuery.of(context).size.shortestSide * 0.22,
                  fit: BoxFit.contain,
                ),
                const SizedBox(height: 16),
                Text(
                  isArabic ? AppConstants.appNameAr : AppConstants.appName,
                  textAlign: TextAlign.center,
                  style: theme.textTheme.headlineLarge?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  isArabic ? 'مرحبا بك مجددا' : 'Welcome Back',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyLarge?.copyWith(
                    color: theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                ),
                const SizedBox(height: 48),

                // Email
                AppTextField(
                  label: isArabic ? 'البريد الإلكتروني' : 'Email',
                  hint: isArabic ? 'أدخل بريدك الإلكتروني' : 'Enter your email',
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

                // Password
                AppPasswordField(
                  label: isArabic ? 'كلمة المرور' : 'Password',
                  hint: isArabic ? 'أدخل كلمة المرور' : 'Enter your password',
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
                const SizedBox(height: 8),

                // Forgot password
                Align(
                  alignment:
                      isArabic ? Alignment.centerLeft : Alignment.centerRight,
                  child: TextButton(
                    onPressed: () => _handleForgotPassword(isArabic),
                    child: Text(
                      isArabic ? 'نسيت كلمة المرور؟' : 'Forgot Password?',
                    ),
                  ),
                ),
                const SizedBox(height: 8),

                // Error message
                if (loginState.error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Text(
                      loginState.error!,
                      style: TextStyle(color: theme.colorScheme.error),
                      textAlign: TextAlign.center,
                    ),
                  ),

                // Login button
                AppButton(
                  text: isArabic ? 'تسجيل الدخول' : 'Login',
                  isLoading: loginState.isLoading,
                  onPressed: loginState.isLoading ? null : _handleLogin,
                ),
                const SizedBox(height: 24),

                // Register link
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      isArabic ? 'ليس لديك حساب؟' : 'Don\'t have an account?',
                      style: theme.textTheme.bodyMedium,
                    ),
                    TextButton(
                      onPressed: () => context.go(AppRoutes.register),
                      child: Text(
                        isArabic ? 'سجل الآن' : 'Register',
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
