import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/gamification_providers.dart';

class LeaderboardScreen extends ConsumerWidget {
  const LeaderboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final leaderboardAsync = ref.watch(leaderboardProvider);
    final selectedType = ref.watch(selectedLeaderboardTypeProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'المتصدرون' : 'Leaderboard'),
      ),
      body: Column(
        children: [
          // Type selector
          Padding(
            padding: const EdgeInsets.all(16),
            child: SegmentedButton<String>(
              segments: [
                ButtonSegment(
                    value: 'attendance',
                    label: Text(isArabic ? 'الحضور' : 'Attendance')),
                ButtonSegment(
                    value: 'weightloss',
                    label: Text(isArabic ? 'فقدان الوزن' : 'Weight Loss')),
                ButtonSegment(
                    value: 'consistency',
                    label: Text(isArabic ? 'الالتزام' : 'Consistency')),
              ],
              selected: {selectedType},
              onSelectionChanged: (sel) {
                ref.read(selectedLeaderboardTypeProvider.notifier).state =
                    sel.first;
              },
            ),
          ),

          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(leaderboardProvider),
              child: leaderboardAsync.when(
                data: (board) {
                  if (board.entries.isEmpty) {
                    return EmptyState(
                      icon: Icons.leaderboard_outlined,
                      title: isArabic
                          ? 'لا توجد بيانات بعد'
                          : 'No data yet',
                    );
                  }
                  return Column(
                    children: [
                      // My position
                      if (board.myPosition != null)
                        Padding(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 16, vertical: 4),
                          child: AppCard(
                            child: ListTile(
                              leading: Container(
                                width: 36,
                                height: 36,
                                decoration: BoxDecoration(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .primary,
                                  shape: BoxShape.circle,
                                ),
                                child: Center(
                                  child: Text(
                                    '#${board.myPosition!.position}',
                                    style: const TextStyle(
                                        color: Colors.white,
                                        fontWeight: FontWeight.w700),
                                  ),
                                ),
                              ),
                              title: Text(
                                isArabic
                                    ? 'مركزي الحالي'
                                    : 'My Current Rank',
                                style: const TextStyle(
                                    fontWeight: FontWeight.w600),
                              ),
                              trailing: Text(
                                '${board.myPosition!.score.toStringAsFixed(0)} pts',
                                style: TextStyle(
                                    color: Theme.of(context)
                                        .colorScheme
                                        .primary,
                                    fontWeight: FontWeight.w700),
                              ),
                            ),
                          ),
                        ),

                      // List
                      Expanded(
                        child: ListView.builder(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          itemCount: board.entries.length,
                          itemBuilder: (context, index) {
                            final entry = board.entries[index];
                            return _LeaderboardEntryTile(
                              entry: entry,
                              isArabic: isArabic,
                            );
                          },
                        ),
                      ),
                    ],
                  );
                },
                loading: () =>
                    const Center(child: CircularProgressIndicator()),
                error: (e, _) => AppErrorWidget(
                  message: e.toString(),
                  onRetry: () => ref.invalidate(leaderboardProvider),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _LeaderboardEntryTile extends StatelessWidget {
  final LeaderboardEntryModel entry;
  final bool isArabic;

  const _LeaderboardEntryTile(
      {required this.entry, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    Color? rankColor;
    IconData? rankIcon;
    if (entry.position == 1) {
      rankColor = Colors.amber;
      rankIcon = Icons.emoji_events;
    } else if (entry.position == 2) {
      rankColor = Colors.grey[400];
      rankIcon = Icons.emoji_events;
    } else if (entry.position == 3) {
      rankColor = Colors.brown[400];
      rankIcon = Icons.emoji_events;
    }

    return ListTile(
      leading: rankIcon != null
          ? Icon(rankIcon, color: rankColor, size: 28)
          : SizedBox(
              width: 28,
              child: Text(
                '#${entry.position}',
                textAlign: TextAlign.center,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
            ),
      title: Text(entry.memberName),
      trailing: Text(
        entry.scoreLabel ??
            '${entry.score.toStringAsFixed(0)} pts',
        style: TextStyle(
            color: Theme.of(context).colorScheme.primary,
            fontWeight: FontWeight.w600),
      ),
    );
  }
}
