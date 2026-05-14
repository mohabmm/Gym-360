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
  bool _obscurePassword = true;
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

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final apiClient = ref.read(apiClientProvider);
      final response = await apiClient.post<AuthResponse>(
        ApiEndpoints.login,
        data: LoginRequest(
          email: _emailController.text.trim(),
          password: _passwordController.text,
        ).toJson(),
        parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
      );

      // Check that the user has a staff role.
      // Identity module emits canonical role strings: 'owner', 'staff' (and
      // legacy 'gym_owner'/'gym_staff'). Granular staff tier (manager,
      // receptionist) is stored per-gym in the Gyms module.
      final role = response.user.role;
      final isStaff = role == AppConstants.roleOwner ||
          role == AppConstants.roleManager ||
          role == AppConstants.roleReceptionist ||
          role == 'owner' ||
          role == 'staff' ||
          role == 'gym_owner' ||
          role == 'gym_staff';

      if (!isStaff) {
        if (!mounted) return;
        setState(() {
          _errorMessage = _isArabic
              ? 'هذا الحساب ليس حساب موظف. يرجى استخدام تطبيق المتدرب.'
              : 'This account is not a staff account. Please use the Trainee app.';
          _isLoading = false;
        });
        return;
      }

      final staffRole = _mapRole(role);
      await ref.read(authProvider.notifier).login(
            response,
            staffRole: staffRole,
          );

      if (mounted) {
        context.go(RoutePaths.dashboard);
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.message;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = _isArabic
            ? 'حدث خطأ. يرجى المحاولة مرة اخرى.'
            : 'An error occurred. Please try again.';
        _isLoading = false;
      });
    }
  }

  String _mapRole(String role) {
    switch (role) {
      case 'owner':
      case 'gym_owner':
        return AppConstants.roleOwner;
      case 'staff':
      case 'gym_staff':
        // Identity backend stores a single GymStaff tier; the granular
        // manager/receptionist split lives in the Gyms module per-gym.
        // Default seeded staff to manager so the app surfaces its full
        // staff feature set; per-gym role can refine later.
        return AppConstants.roleManager;
      default:
        return role;
    }
  }

  bool get _isArabic =>
      Localizations.localeOf(context).languageCode == 'ar';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = _isArabic;

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Form(
                key: _formKey,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Logo & Title
                    Image.asset(
                      'assets/images/logo.png',
                      width: MediaQuery.of(context).size.shortestSide * 0.22,
                      height: MediaQuery.of(context).size.shortestSide * 0.22,
                      fit: BoxFit.contain,
                    ),
                    const SizedBox(height: 16),
                    Text(
                      isArabic ? 'جيم 360 - الإدارة' : 'Gym 360 - Staff',
                      textAlign: TextAlign.center,
                      style: theme.textTheme.headlineMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      isArabic
                          ? 'تسجيل دخول موظفي الصالة'
                          : 'Gym Staff Login',
                      textAlign: TextAlign.center,
                      style: theme.textTheme.bodyLarge?.copyWith(
                        color: theme.colorScheme.onSurface.withOpacity(0.6),
                      ),
                    ),
                    const SizedBox(height: 40),

                    // Email field
                    AppTextField(
                      controller: _emailController,
                      label: isArabic ? 'البريد الالكتروني' : 'Email',
                      hint: isArabic
                          ? 'ادخل بريدك الالكتروني'
                          : 'Enter your email',
                      keyboardType: TextInputType.emailAddress,
                      autocorrect: false,
                      enableSuggestions: false,
                      prefixIcon: const Icon(Icons.email_outlined),
                      validator: (value) => Validators.email(
                        value,
                        locale: isArabic ? 'ar' : 'en',
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Password field
                    AppTextField(
                      controller: _passwordController,
                      label: isArabic ? 'كلمة المرور' : 'Password',
                      hint: isArabic
                          ? 'ادخل كلمة المرور'
                          : 'Enter your password',
                      obscureText: _obscurePassword,
                      prefixIcon: const Icon(Icons.lock_outlined),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword
                              ? Icons.visibility_off_outlined
                              : Icons.visibility_outlined,
                        ),
                        onPressed: () {
                          setState(() => _obscurePassword = !_obscurePassword);
                        },
                      ),
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

                    // Error message
                    if (_errorMessage != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.error.withOpacity(0.1),
                            borderRadius: BorderRadius.circular(
                              AppConstants.borderRadiusSmall,
                            ),
                          ),
                          child: Text(
                            _errorMessage!,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: AppColors.error,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                      ),

                    // Login button
                    AppButton(
                      text: isArabic ? 'تسجيل الدخول' : 'Sign In',
                      onPressed: _isLoading ? null : _login,
                      isLoading: _isLoading,
                    ),
                    const SizedBox(height: 16),

                    // Forgot password
                    TextButton(
                      onPressed: () {
                        // Navigate to forgot password
                      },
                      child: Text(
                        isArabic
                            ? 'نسيت كلمة المرور؟'
                            : 'Forgot Password?',
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
