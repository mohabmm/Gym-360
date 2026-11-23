import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/pt_providers.dart';

/// mem.first_month_challenge — the desk's "earned the reward" list. The reward is handed over
/// in person; ticking "delivered" moves the member to the second tab (and can be undone).
class ChallengeAwardsScreen extends StatelessWidget {
  const ChallengeAwardsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(isArabic ? 'مكافآت الشهر الأول' : 'First-month rewards'),
          bottom: TabBar(tabs: [
            Tab(text: isArabic ? 'لسه ما اتسلمتش' : 'To hand over'),
            Tab(text: isArabic ? 'اتسلمت' : 'Delivered'),
          ]),
        ),
        body: const TabBarView(children: [_AwardList(delivered: false), _AwardList(delivered: true)]),
      ),
    );
  }
}

class _AwardList extends ConsumerWidget {
  final bool delivered;
  const _AwardList({required this.delivered});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final awards = ref.watch(challengeAwardsProvider(delivered));
    return RefreshIndicator(
      onRefresh: () => ref.refresh(challengeAwardsProvider(delivered).future),
      child: awards.when(
        data: (list) => list.isEmpty
            ? ListView(children: [
                const SizedBox(height: 60),
                EmptyState(
                  icon: Icons.emoji_events_outlined,
                  title: delivered
                      ? (isArabic ? 'لسه ما فيش مكافآت اتسلمت' : 'No rewards delivered yet')
                      : (isArabic ? 'مفيش مكافآت مستنية' : 'No rewards waiting'),
                  subtitle: delivered
                      ? null
                      : (isArabic
                          ? 'أول ما عضو جديد يخلّص هدف الشهر الأول هيظهر هنا.'
                          : 'When a new member reaches the first-month target, they show up here.'),
                ),
              ])
            : ListView.separated(
                itemCount: list.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
                itemBuilder: (_, i) => _AwardTile(award: list[i], isArabic: isArabic),
              ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => AppErrorWidget(
          message: isArabic ? 'تعذر تحميل القائمة' : 'Could not load the list',
          onRetry: () => ref.invalidate(challengeAwardsProvider(delivered)),
        ),
      ),
    );
  }
}

class _AwardTile extends ConsumerStatefulWidget {
  final ChallengeAwardModel award;
  final bool isArabic;
  const _AwardTile({required this.award, required this.isArabic});

  @override
  ConsumerState<_AwardTile> createState() => _AwardTileState();
}

class _AwardTileState extends ConsumerState<_AwardTile> {
  bool _saving = false;

  bool get _ar => widget.isArabic;

  String _subtitle() {
    final a = widget.award;
    final when = a.reachedAt == null ? '' : DateFormat('d MMM', _ar ? 'ar' : 'en').format(a.reachedAt!.toLocal());
    final coach = a.trainerName == null ? '' : (_ar ? ' · الكابتن ${a.trainerName}' : ' · Coach ${a.trainerName}');
    final done = _ar ? '${a.count}/${a.target} تمرين · $when' : '${a.count}/${a.target} workouts · $when';
    return '$done$coach\n${a.reward(_ar)}';
  }

  Future<void> _toggle(bool delivered) async {
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _saving = true);
    try {
      await ref.read(apiClientProvider).putVoid(
            ApiEndpoints.challengeAwardDelivered(widget.award.awardId),
            data: {'delivered': delivered},
          );
      ref.invalidate(challengeAwardsProvider(false));
      ref.invalidate(challengeAwardsProvider(true));
    } catch (_) {
      messenger.showSnackBar(SnackBar(
        content: Text(_ar ? 'تعذّر الحفظ، حاول مرة أخرى' : 'Could not save, please try again'),
      ));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final a = widget.award;
    return CheckboxListTile(
      key: Key('award-${a.awardId}'),
      value: a.isDelivered,
      onChanged: _saving ? null : (v) => _toggle(v ?? false),
      isThreeLine: true,
      controlAffinity: ListTileControlAffinity.trailing,
      secondary: IconButton(
        tooltip: _ar ? 'ملف العضو' : 'Member profile',
        icon: const Icon(Icons.emoji_events_rounded, color: Colors.amber),
        onPressed: () => context.push('/members/${a.traineeId}'),
      ),
      title: Text(a.traineeName, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(_subtitle()),
    );
  }
}
