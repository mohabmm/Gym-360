import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../providers/auth_providers.dart';

class LinkGymScreen extends ConsumerStatefulWidget {
  const LinkGymScreen({super.key});

  @override
  ConsumerState<LinkGymScreen> createState() => _LinkGymScreenState();
}

class _LinkGymScreenState extends ConsumerState<LinkGymScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final _codeController = TextEditingController();
  bool _isLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _linkWithCode() async {
    final code = _codeController.text.trim();
    if (code.length != AppConstants.gymCodeLength) {
      setState(() => _error = 'Code must be ${AppConstants.gymCodeLength} characters');
      return;
    }

    await _linkGym(LinkGymRequest(gymCode: code));
  }

  Future<void> _linkWithQr(String qrData) async {
    await _linkGym(LinkGymRequest(qrData: qrData));
  }

  Future<void> _linkGym(LinkGymRequest request) async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final repo = ref.read(authRepositoryProvider);
      await repo.linkGym(request);

      // Re-fetch profile to get gym memberships
      final profile = await repo.getProfile();
      await ref.read(authProvider.notifier).updateUser(profile);

      if (profile.gymMemberships?.isNotEmpty == true) {
        final gymId = profile.gymMemberships!.first.gymId;
        await ref.read(authProvider.notifier).setActiveGym(gymId);
      }

      if (mounted) context.go(AppRoutes.home);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ربط الصالة الرياضية' : 'Link Your Gym'),
        actions: [
          TextButton(
            onPressed: () => context.go(AppRoutes.home),
            child: Text(isArabic ? 'تخطي' : 'Skip'),
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(
              icon: const Icon(Icons.keyboard),
              text: isArabic ? 'كود الصالة' : 'Gym Code',
            ),
            Tab(
              icon: const Icon(Icons.qr_code_scanner),
              text: isArabic ? 'مسح QR' : 'Scan QR',
            ),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          // Tab 1: Code entry
          Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 32),
                Icon(
                  Icons.vpn_key_outlined,
                  size: 64,
                  color: theme.colorScheme.primary.withOpacity(0.6),
                ),
                const SizedBox(height: 24),
                Text(
                  isArabic
                      ? 'أدخل كود الصالة المكون من 6 أحرف'
                      : 'Enter the 6-character gym code',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                Text(
                  isArabic
                      ? 'احصل على الكود من موظفي الصالة'
                      : 'Get this code from your gym staff',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                ),
                const SizedBox(height: 32),
                AppTextField(
                  hint: isArabic ? 'مثال: ABC123' : 'e.g., ABC123',
                  controller: _codeController,
                  textInputAction: TextInputAction.done,
                  maxLength: AppConstants.gymCodeLength,
                  onSubmitted: (_) => _linkWithCode(),
                ),
                const SizedBox(height: 8),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Text(
                      _error!,
                      style: TextStyle(color: theme.colorScheme.error),
                      textAlign: TextAlign.center,
                    ),
                  ),
                const SizedBox(height: 16),
                AppButton(
                  text: isArabic ? 'ربط الصالة' : 'Link Gym',
                  isLoading: _isLoading,
                  onPressed: _isLoading ? null : _linkWithCode,
                ),
              ],
            ),
          ),

          // Tab 2: QR scanner
          Column(
            children: [
              Expanded(
                child: MobileScanner(
                  onDetect: (capture) {
                    final barcode = capture.barcodes.firstOrNull;
                    if (barcode?.rawValue != null && !_isLoading) {
                      _linkWithQr(barcode!.rawValue!);
                    }
                  },
                ),
              ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(
                    _error!,
                    style: TextStyle(color: theme.colorScheme.error),
                    textAlign: TextAlign.center,
                  ),
                ),
              if (_isLoading)
                const Padding(
                  padding: EdgeInsets.all(16),
                  child: CircularProgressIndicator(),
                ),
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  isArabic
                      ? 'وجه الكاميرا نحو رمز QR الخاص بالصالة'
                      : 'Point your camera at the gym QR code',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
