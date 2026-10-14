import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
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
      List<dynamic> ownedGyms = const [];
      var response = await apiClient.post<AuthResponse>(
        ApiEndpoints.login,
        data: LoginRequest(
          email: _emailController.text.trim(),
          password: _passwordController.text,
        ).toJson(),
        parser: (data) {
          final json = data as Map<String, dynamic>;
          final user = json['user'];
          if (user is Map && user['ownedGyms'] is List) {
            ownedGyms = user['ownedGyms'] as List<dynamic>;
          }
          return AuthResponse.fromJson(json);
        },
      );

      // Identity emits canonical role strings: 'owner', 'staff' (and legacy
      // 'gym_owner'/'gym_staff'). The granular staff tier (manager,
      // receptionist) travels in the access token and is resolved by
      // AuthNotifier.login.
      final role = response.user.role;
      final isOwner = role == 'owner' || role == 'gym_owner';
      final isStaff = isOwner || role == 'staff' || role == 'gym_staff';

      if (!isStaff) {
        if (!mounted) return;
        setState(() {
          _errorMessage = _isArabic
              ? 'هذا الحساب ليس حساب موظف. يرجى استخدام التطبيق الخاص بدورك.'
              : 'This account is not a staff account. Please use the app for your role.';
          _isLoading = false;
        });
        return;
      }

      // Owners log in without a gym-scoped token. Scope it to one of their
      // gyms before entering the app, otherwise every gym screen is empty.
      if (isOwner &&
          (response.user.gymId ?? '').isEmpty &&
          ownedGyms.isNotEmpty) {
        final gymId = await _chooseOwnedGym(ownedGyms);
        if (gymId == null) {
          if (mounted) setState(() => _isLoading = false);
          return;
        }
        final storage = ref.read(secureStorageProvider);
        await storage.saveTokens(
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
        );
        try {
          response = await apiClient.post<AuthResponse>(
            ApiEndpoints.selectGym,
            data: {'gymId': gymId},
            parser: (data) =>
                AuthResponse.fromJson(data as Map<String, dynamic>),
          );
        } catch (_) {
          await storage.clearTokens();
          rethrow;
        }
      }

      await ref.read(authProvider.notifier).login(response);

      if (mounted) {
        context.go(RoutePaths.dashboard);
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = staffErrorText(e, isArabic: _isArabic);
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

  /// Returns the gym the owner wants to manage: the only one they own, or
  /// their pick when they own several. Null when the picker is dismissed.
  Future<String?> _chooseOwnedGym(List<dynamic> ownedGyms) async {
    final gyms = ownedGyms
        .whereType<Map>()
        .map((g) => (
              id: (g['gymId'] ?? g['id'] ?? '').toString(),
              name: (g['name'] ?? '').toString(),
            ))
        .where((g) => g.id.isNotEmpty)
        .toList();
    if (gyms.isEmpty) return null;
    if (gyms.length == 1) return gyms.first.id;
    if (!mounted) return null;

    final isArabic = _isArabic;
    return showDialog<String>(
      context: context,
      builder: (ctx) => SimpleDialog(
        title: Text(isArabic ? 'اختر الصالة' : 'Choose a gym'),
        children: [
          for (final gym in gyms)
            SimpleDialogOption(
              onPressed: () => Navigator.of(ctx).pop(gym.id),
              child: Text(gym.name.isEmpty ? gym.id : gym.name),
            ),
        ],
      ),
    );
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
