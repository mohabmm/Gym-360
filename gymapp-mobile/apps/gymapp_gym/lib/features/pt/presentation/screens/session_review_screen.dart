import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/pt_providers.dart';

/// pt.dual_session_logging — sessions the trainee said did not happen. Reversing
/// one gives the session back to the trainee's package.
class SessionReviewScreen extends ConsumerWidget {
  const SessionReviewScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final sessions = ref.watch(disputedSessionsProvider);
    final format = DateFormat('EEE d MMM · HH:mm', isArabic ? 'ar' : 'en');

    Future<void> reverse(SessionModel s) async {
      final messenger = ScaffoldMessenger.of(context);
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title:
              Text(isArabic ? 'إلغاء احتساب الحصة؟' : 'Reverse this session?'),
          content: Text(isArabic
              ? 'لن تُحتسب الحصة وستعود إلى باقة المتدرب.'
              : 'The session will no longer count and goes back to the trainee\'s package.'),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: Text(isArabic ? 'إلغاء' : 'Cancel')),
            FilledButton(
                onPressed: () => Navigator.pop(ctx, true),
                child: Text(isArabic ? 'تأكيد' : 'Confirm')),
          ],
        ),
      );
      if (ok != true) return;
      try {
        await ref
            .read(apiClientProvider)
            .postVoid(ApiEndpoints.sessionReverse(s.id));
        ref.invalidate(disputedSessionsProvider);
        messenger.showSnackBar(SnackBar(
            content: Text(
                isArabic ? 'تمت إعادة الحصة للباقة' : 'Session given back')));
      } catch (_) {
        messenger.showSnackBar(SnackBar(
            content: Text(
                isArabic ? 'تعذّر التنفيذ' : 'Could not reverse the session')));
      }
    }

    return Scaffold(
      appBar:
          AppBar(title: Text(isArabic ? 'حصص للمراجعة' : 'Sessions to review')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(disputedSessionsProvider.future),
        child: sessions.when(
          data: (list) => list.isEmpty
              ? ListView(children: [
                  const SizedBox(height: 60),
                  EmptyState(
                    icon: Icons.fact_check_outlined,
                    title:
                        isArabic ? 'لا توجد اعتراضات' : 'No disputed sessions',
                    subtitle: isArabic
                        ? 'يظهر هنا ما يعترض عليه المتدرب.'
                        : 'Sessions a trainee disputes show up here.',
                  ),
                ])
              : ListView.separated(
                  itemCount: list.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final s = list[i];
                    return ListTile(
                      leading: const Icon(Icons.report_outlined,
                          color: AppColors.warning),
                      title: Text(format.format(s.scheduledAt)),
                      subtitle: s.focus == null ? null : Text(s.focus!),
                      trailing: TextButton(
                          onPressed: () => reverse(s),
                          child: Text(isArabic ? 'إلغاء الاحتساب' : 'Reverse')),
                    );
                  },
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(
            message: isArabic ? 'تعذر التحميل' : 'Could not load',
            onRetry: () => ref.invalidate(disputedSessionsProvider),
          ),
        ),
      ),
    );
  }
}
