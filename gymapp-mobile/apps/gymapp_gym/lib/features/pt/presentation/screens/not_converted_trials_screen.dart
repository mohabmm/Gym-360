import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../providers/pt_providers.dart';

/// pt.trial_end_alerts — trainees who finished their free trial and have not bought
/// a package. Tap a trainee to open their profile and assign a package.
class NotConvertedTrialsScreen extends ConsumerWidget {
  const NotConvertedTrialsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final trials = ref.watch(notConvertedTrialsProvider);
    final date = DateFormat('d MMM', isArabic ? 'ar' : 'en');

    return Scaffold(
      appBar: AppBar(
          title: Text(isArabic
              ? 'أنهوا التجربة ولم يشتركوا'
              : 'Finished trial, not converted')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(notConvertedTrialsProvider.future),
        child: trials.when(
          data: (list) => list.isEmpty
              ? ListView(children: [
                  const SizedBox(height: 60),
                  EmptyState(
                    icon: Icons.verified_outlined,
                    title:
                        isArabic ? 'لا أحد في القائمة' : 'Nobody to follow up',
                    subtitle: isArabic
                        ? 'كل من أنهى التجربة اشترك في باقة.'
                        : 'Everyone who finished a trial bought a package.',
                  ),
                ])
              : ListView.separated(
                  itemCount: list.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final t = list[i];
                    return ListTile(
                      leading: CircleAvatar(
                          child: Text(t.memberName.isEmpty
                              ? '?'
                              : t.memberName.characters.first)),
                      title: Text(t.memberName.isEmpty
                          ? (isArabic ? 'متدرب' : 'Trainee')
                          : t.memberName),
                      subtitle: Text(isArabic
                          ? 'المدرب: ${t.trainerName} · انتهت ${date.format(t.finishedAt)} (منذ ${t.daysSinceFinished} يوم)'
                          : 'Coach: ${t.trainerName} · ended ${date.format(t.finishedAt)} (${t.daysSinceFinished}d ago)'),
                      trailing: t.windowClosed
                          ? Text(isArabic ? 'أكثر من ١٤ يومًا' : '>14 days',
                              style: const TextStyle(
                                  color: AppColors.error, fontSize: 12))
                          : const Icon(Icons.chevron_right),
                      onTap: () => context.push('/members/${t.memberId}'),
                    );
                  },
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(
            message:
                isArabic ? 'تعذر تحميل القائمة' : 'Could not load the list',
            onRetry: () => ref.invalidate(notConvertedTrialsProvider),
          ),
        ),
      ),
    );
  }
}
