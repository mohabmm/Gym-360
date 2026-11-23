import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../data/challenge_providers.dart';
import 'trainee_plan_strings.dart';

/// mem.first_month_challenge home card: "8 / 12", days left and the reward; then "you won"
/// until the desk hands the reward over. Nothing when the challenge is off, over or not started.
class ChallengeCard extends ConsumerWidget {
  const ChallengeCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = TraineePlanStrings(Localizations.localeOf(context).languageCode == 'ar');
    return ref.watch(myChallengeProvider).maybeWhen(
          data: (c) => c != null && c.isWorthShowing ? _Card(challenge: c, s: s) : const SizedBox.shrink(),
          orElse: () => const SizedBox.shrink(),
        );
  }
}

class _Card extends StatelessWidget {
  final ChallengeStatusModel challenge;
  final TraineePlanStrings s;

  const _Card({required this.challenge, required this.s});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final won = challenge.isReached;
    final accent = won ? Colors.amber.shade700 : theme.colorScheme.primary;
    return AppCard(
      key: const Key('challenge-card'),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(won ? Icons.emoji_events_rounded : Icons.flag_rounded, color: accent),
              const SizedBox(width: 8),
              Expanded(
                child: Text(won ? s.challengeWon : s.challengeTitle,
                    style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              ),
              Text(s.challengeCount(challenge.count, challenge.target),
                  key: const Key('challenge-count'),
                  style: theme.textTheme.titleSmall?.copyWith(color: accent, fontWeight: FontWeight.w700)),
            ],
          ),
          const SizedBox(height: 10),
          LinearProgressIndicator(
            value: challenge.fraction,
            minHeight: 8,
            color: accent,
            borderRadius: BorderRadius.circular(4),
          ),
          const SizedBox(height: 8),
          if (!won)
            Text(s.challengeLeft((challenge.target - challenge.count).clamp(0, challenge.target), challenge.daysLeft),
                style: theme.textTheme.bodySmall),
          const SizedBox(height: 4),
          Text(s.challengeReward(challenge.reward(s.isArabic)), style: theme.textTheme.bodyMedium),
          if (won) ...[
            const SizedBox(height: 4),
            Text(s.challengeClaim, style: theme.textTheme.bodySmall),
          ],
        ],
      ),
    );
  }
}
