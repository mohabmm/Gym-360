import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../../../../core/providers.dart';
import '../providers/checkin_providers.dart';

class CheckInScreen extends ConsumerWidget {
  const CheckInScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final user = authState.valueOrNull?.user;
    final currentGymId = authState.valueOrNull?.currentGymId;
    // Member numbers are per-gym on the BE (GymMembership.memberNumber); the
    // top-level UserModel.memberNumber is a legacy fallback that's rarely set.
    final memberNumber = user?.gymMemberships
            ?.where((m) => currentGymId == null || m.gymId == currentGymId)
            .map((m) => m.memberNumber)
            .firstWhere((n) => n != null && n.isNotEmpty,
                orElse: () => null) ??
        user?.memberNumber;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تسجيل الحضور' : 'Check-in'),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(checkInHistoryProvider),
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child:
                  _MyQrSection(memberNumber: memberNumber, isArabic: isArabic),
            ),

            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
                child: Text(
                  isArabic ? 'سجل الحضور' : 'Check-in History',
                  style: Theme.of(context)
                      .textTheme
                      .titleSmall
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
              ),
            ),

            Consumer(
              builder: (context, ref, _) {
                final historyAsync = ref.watch(checkInHistoryProvider);
                return historyAsync.when(
                  data: (history) {
                    if (history.isEmpty) {
                      return SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Text(
                            isArabic
                                ? 'لا يوجد سجل حضور بعد'
                                : 'No check-in history',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                        ),
                      );
                    }
                    return SliverList(
                      delegate: SliverChildBuilderDelegate(
                        (context, index) => _CheckInTile(
                          checkIn: history[index],
                          isArabic: isArabic,
                        ),
                        childCount: history.length,
                      ),
                    );
                  },
                  loading: () => const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(16),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ),
                  error: (e, _) =>
                      const SliverToBoxAdapter(child: SizedBox.shrink()),
                );
              },
            ),

            const SliverToBoxAdapter(child: SizedBox(height: 24)),
          ],
        ),
      ),
    );
  }
}

class _MyQrSection extends StatelessWidget {
  final String? memberNumber;
  final bool isArabic;

  const _MyQrSection({this.memberNumber, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        children: [
          Text(
            isArabic ? 'رمز QR الخاص بك' : 'Your QR Code',
            style: Theme.of(context)
                .textTheme
                .titleMedium
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 16),
          if (memberNumber != null)
            QrImageView(
              data: memberNumber!,
              version: QrVersions.auto,
              size: 200,
            )
          else
            Container(
              width: 200,
              height: 200,
              color: Colors.grey[200],
              child: Center(
                child: Text(
                  isArabic ? 'لا يوجد رقم عضوية' : 'No member number',
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          const SizedBox(height: 12),
          if (memberNumber != null)
            Text(
              memberNumber!,
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2,
                  ),
            ),
        ],
      ),
    );
  }
}

class _CheckInTile extends StatelessWidget {
  final CheckInModel checkIn;
  final bool isArabic;

  const _CheckInTile({required this.checkIn, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: const Icon(Icons.login),
      title: Text(
        '${checkIn.checkedInAt.day}/${checkIn.checkedInAt.month}/${checkIn.checkedInAt.year}',
      ),
      subtitle: Text(
        '${checkIn.checkedInAt.hour.toString().padLeft(2, '0')}:${checkIn.checkedInAt.minute.toString().padLeft(2, '0')} • ${_methodLabel(checkIn.method, isArabic)}',
      ),
    );
  }

  /// Localizes the BE CheckInMethod enum (QR/MemberNumber/Manual) for the
  /// Arabic-first UI; previously the raw enum string was shown.
  String _methodLabel(String method, bool isArabic) {
    switch (method.toLowerCase().replaceAll(RegExp(r'[\s_\-]'), '')) {
      case 'qr':
        return isArabic ? 'رمز QR' : 'QR';
      case 'membernumber':
        return isArabic ? 'رقم العضو' : 'Member number';
      case 'manual':
        return isArabic ? 'يدوي' : 'Manual';
      default:
        return method;
    }
  }
}
