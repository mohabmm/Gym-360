import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// pt.starter_plan: one coach of the member, whether a one-week program is unlocked, and the
/// member's active 3-day plan or one-week program (limit `Starter` / `OneWeek`).
class ProgramUnlockRow {
  final String trainerId;
  final String trainerName;
  final bool unlocked;

  /// `OneWeek` or `ThirtyDays`: the length the staff chose last.
  final String length;
  final String? planLimit;
  final DateTime? planEndsOn;
  final bool planEnded;

  const ProgramUnlockRow({
    required this.trainerId,
    required this.trainerName,
    required this.unlocked,
    this.length = 'OneWeek',
    this.planLimit,
    this.planEndsOn,
    this.planEnded = false,
  });

  factory ProgramUnlockRow.fromJson(Map<String, dynamic> json) {
    final plan = json['limitedPlan'] is Map ? Map<String, dynamic>.from(json['limitedPlan'] as Map) : null;
    return ProgramUnlockRow(
      trainerId: json['trainerId'] as String? ?? '',
      trainerName: json['trainerName'] as String? ?? '',
      unlocked: json['unlocked'] as bool? ?? false,
      length: json['length'] as String? ?? 'OneWeek',
      planLimit: plan?['limit'] as String?,
      planEndsOn: DateTime.tryParse(plan?['endsOn'] as String? ?? ''),
      planEnded: plan?['hasEnded'] as bool? ?? false,
    );
  }
}

/// The member's coach(es) with a live package and the unlock state (staff, manager, owner).
final memberProgramUnlocksProvider =
    FutureProvider.autoDispose.family<List<ProgramUnlockRow>, String>((ref, memberId) async {
  return ref.watch(apiClientProvider).get<List<ProgramUnlockRow>>(
        ApiEndpoints.planUnlocks,
        queryParameters: {'traineeId': memberId},
        parser: (data) => [
          for (final row in (Map<String, dynamic>.from(data as Map)['coaches'] as List? ?? const []))
            ProgramUnlockRow.fromJson(Map<String, dynamic>.from(row as Map)),
        ],
      );
});

/// «برنامج للمدرب»: the coach starts the member on a 3-day plan. When the member stops coming, the
/// desk unlocks one week or 30 days here: the coach gives a program from the templates and a diet.
/// Each unlock allows one program; the coach giving it locks it again.
class MonthlyProgramsCard extends ConsumerStatefulWidget {
  final String memberId;
  final bool isArabic;

  const MonthlyProgramsCard({super.key, required this.memberId, required this.isArabic});

  @override
  ConsumerState<MonthlyProgramsCard> createState() => _MonthlyProgramsCardState();
}

class _MonthlyProgramsCardState extends ConsumerState<MonthlyProgramsCard> {
  String? _saving;

  String _t(String ar, String en) => widget.isArabic ? ar : en;

  /// [length] `OneWeek` / `ThirtyDays` unlocks that program; null takes the unlock back.
  Future<void> _set(ProgramUnlockRow row, String? length) async {
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _saving = row.trainerId);
    try {
      await ref.read(apiClientProvider).put<Object?>(
            ApiEndpoints.planUnlocks,
            data: {
              'trainerId': row.trainerId,
              'traineeId': widget.memberId,
              'unlocked': length != null,
              'length': length ?? row.length,
            },
            parser: (data) => data,
          );
      ref.invalidate(memberProgramUnlocksProvider(widget.memberId));
      messenger.showSnackBar(SnackBar(
        content: Text(length != null
            ? _t('تم فتح ${_lengthName(length)} ✅ وصل إشعار للمدرب', '${_lengthName(length)} unlocked ✅ the coach is notified')
            : _t('اتقفل البرنامج', 'Program locked')),
      ));
    } on ApiException {
      messenger.showSnackBar(SnackBar(content: Text(_t('تعذّر الحفظ، حاول مرة أخرى', "Couldn't save. Try again."))));
    } finally {
      if (mounted) setState(() => _saving = null);
    }
  }

  String _lengthName(String length) =>
      length == 'ThirtyDays' ? _t('برنامج ٣٠ يوم', '30-day program') : _t('برنامج أسبوع', 'One-week program');

  String _status(BuildContext context, ProgramUnlockRow row) {
    if (row.unlocked) {
      return _t('مفتوح: ${_lengthName(row.length)} – المدرب يقدر يدّيه', 'Unlocked: ${_lengthName(row.length)} – the coach can give it');
    }
    final endsOn = row.planEndsOn;
    if (row.planLimit == null || endsOn == null) return _t('مفيش خطة دلوقتي', 'No plan right now');
    final name = row.planLimit == 'Starter' ? _t('خطة البداية ٣ أيام', 'The 3-day starter plan') : _lengthName(row.planLimit!);
    if (row.planEnded) {
      return row.planLimit == 'Starter'
          ? _t('خلصت $name', '$name is over')
          : _t('خلص $name – لو لسه غايب افتح برنامج تاني', '$name is over – unlock another if they are still away');
    }
    final ends = MaterialLocalizations.of(context).formatMediumDate(endsOn);
    return _t('$name شغال · آخر يوم $ends', '$name running · last day $ends');
  }

  @override
  Widget build(BuildContext context) {
    final rows = ref.watch(memberProgramUnlocksProvider(widget.memberId));
    final list = rows.valueOrNull ?? const <ProgramUnlockRow>[];
    if (list.isEmpty) return const SizedBox.shrink();
    return AppCard(
      key: const ValueKey('monthly-programs-card'),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(_t('برنامج للمدرب: أسبوع أو ٣٠ يوم', "Coach's program: one week or 30 days"),
            style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
        const SizedBox(height: 4),
        Text(_t('العضو غاب؟ افتح للمدرب أسبوع أو ٣٠ يوم من القوالب عشان يدّيله خطة وأكل ويرجع الجيم.',
            'Member stopped coming? Unlock one week or 30 days from the templates so the coach gives a plan and a diet to bring them back.')),
        for (final row in list) _CoachRow(row: row, status: _status(context, row), saving: _saving == row.trainerId, onSet: _set, t: _t),
      ]),
    );
  }
}

/// One coach: name, status, and "one week" / "30 days" to unlock, or "lock" while unlocked.
class _CoachRow extends StatelessWidget {
  final ProgramUnlockRow row;
  final String status;
  final bool saving;
  final Future<void> Function(ProgramUnlockRow row, String? length) onSet;
  final String Function(String ar, String en) t;

  const _CoachRow({required this.row, required this.status, required this.saving, required this.onSet, required this.t});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: 12),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(row.trainerName, style: Theme.of(context).textTheme.titleSmall),
          Text(status),
          const SizedBox(height: 8),
          Wrap(spacing: 8, children: row.unlocked
              ? [
                  OutlinedButton(
                    key: ValueKey('lock-${row.trainerId}'),
                    onPressed: saving ? null : () => onSet(row, null),
                    child: Text(t('اقفله', 'Lock')),
                  ),
                ]
              : [
                  FilledButton(
                    key: ValueKey('unlock-week-${row.trainerId}'),
                    onPressed: saving ? null : () => onSet(row, 'OneWeek'),
                    child: Text(t('أسبوع', 'One week')),
                  ),
                  FilledButton.tonal(
                    key: ValueKey('unlock-month-${row.trainerId}'),
                    onPressed: saving ? null : () => onSet(row, 'ThirtyDays'),
                    child: Text(t('٣٠ يوم', '30 days')),
                  ),
                ]),
        ]),
      );
}
