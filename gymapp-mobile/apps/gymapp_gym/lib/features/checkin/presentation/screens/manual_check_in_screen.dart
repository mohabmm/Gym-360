import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/checkin_provider.dart';
import '../../../../core/error_text.dart';

class ManualCheckInScreen extends ConsumerStatefulWidget {
  const ManualCheckInScreen({super.key});

  @override
  ConsumerState<ManualCheckInScreen> createState() =>
      _ManualCheckInScreenState();
}

class _ManualCheckInScreenState extends ConsumerState<ManualCheckInScreen> {
  final _memberNumberController = TextEditingController();
  bool _loading = false;
  CheckInResultModel? _result;

  @override
  void dispose() {
    _memberNumberController.dispose();
    super.dispose();
  }

  Future<void> _checkIn() async {
    final number = _memberNumberController.text.trim();
    if (number.isEmpty) return;
    setState(() {
      _loading = true;
      _result = null;
    });
    try {
      final authState = ref.read(authProvider).valueOrNull;
      final gymId = ref.read(currentGymIdProvider);
      if (gymId.isEmpty) {
        throw Exception('No active gym selected');
      }
      final staffId = authState?.user?.id;

      final apiClient = ref.read(apiClientProvider);
      final result = await apiClient.post<CheckInResultModel>(
        ApiEndpoints.checkIn(gymId),
        data: {
          'method': 'MemberNumber',
          'memberNumber': number,
          if (staffId != null) 'staffId': staffId,
        },
        parser: (data) =>
            CheckInResultModel.fromJson(data as Map<String, dynamic>),
      );
      setState(() => _result = result);
      ref.invalidate(checkInLogProvider);
    } catch (e) {
      setState(() => _result = CheckInResultModel(
            success: false,
            errorCode: 'error',
            errorMessage: staffErrorText(e),
          ));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تسجيل حضور يدوي' : 'Manual Check-In'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            AppTextField(
              controller: _memberNumberController,
              label: isArabic ? 'رقم العضو' : 'Member Number',
              keyboardType: TextInputType.number,
              hint: isArabic ? 'ادخل رقم العضو' : 'Enter member number',
              onChanged: (_) => setState(() => _result = null),
            ),
            const SizedBox(height: 16),
            AppButton(
              text: isArabic ? 'تسجيل الحضور' : 'Check In',
              isLoading: _loading,
              onPressed: _checkIn,
            ),
            const SizedBox(height: 24),
            if (_result != null)
              AnimatedSwitcher(
                duration: const Duration(milliseconds: 300),
                child: _result!.success
                    ? _SuccessCard(result: _result!, isArabic: isArabic)
                    : _ErrorCard(result: _result!, isArabic: isArabic),
              ),
          ],
        ),
      ),
    );
  }
}

class _SuccessCard extends StatelessWidget {
  final CheckInResultModel result;
  final bool isArabic;

  const _SuccessCard({required this.result, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      border: Border.all(color: AppColors.success, width: 2),
      child: Column(
        children: [
          const Icon(Icons.check_circle, color: AppColors.success, size: 48),
          const SizedBox(height: 8),
          Text(
            isArabic ? 'تم تسجيل الحضور' : 'Check-in Successful',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: AppColors.success,
                  fontWeight: FontWeight.w700,
                ),
          ),
          if (result.memberName != null) ...[
            const SizedBox(height: 8),
            Text(result.memberName!, style: Theme.of(context).textTheme.bodyLarge),
          ],
        ],
      ),
    );
  }
}

class _ErrorCard extends StatelessWidget {
  final CheckInResultModel result;
  final bool isArabic;

  const _ErrorCard({required this.result, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      border: Border.all(color: AppColors.error, width: 2),
      child: Column(
        children: [
          const Icon(Icons.error, color: AppColors.error, size: 48),
          const SizedBox(height: 8),
          Text(
            isArabic ? 'فشل تسجيل الحضور' : 'Check-in Failed',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: AppColors.error,
                  fontWeight: FontWeight.w700,
                ),
          ),
          if (result.errorMessage != null) ...[
            const SizedBox(height: 4),
            Text(
              result.errorMessage!,
              style: Theme.of(context).textTheme.bodySmall,
              textAlign: TextAlign.center,
            ),
          ],
        ],
      ),
    );
  }
}
