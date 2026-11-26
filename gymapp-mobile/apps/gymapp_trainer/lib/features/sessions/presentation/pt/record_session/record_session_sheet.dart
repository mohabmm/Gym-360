import 'package:flutter/material.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../plans/data/session_plan_days.dart';
import '../pt_coach_widgets.dart' show TrialBadge;
import 'record_session_strings.dart';
import 'session_time_draft.dart';

/// Sends the session (payload = time + note); throws [ApiException] on failure.
typedef RecordSessionSubmit = Future<RecordedSession> Function(Map<String, dynamic> payload);

/// "Record session": who, how many sessions are left, when it is (default: going on now; or
/// yesterday / up to 7 days back, start time, duration), an optional note, then one clear button.
/// Pops with the [RecordedSession] on success.
class RecordSessionSheet extends StatefulWidget {
  final String clientName;
  final SessionPackageModel? package;
  final RecordSessionStrings strings;
  final SessionTimeDraft draft;
  final RecordSessionSubmit onSubmit;
  /// pt.training_plan: the client's plan days (null = no plan, nothing shown).
  final SessionPlanDays? planDays;

  const RecordSessionSheet({
    super.key,
    required this.clientName,
    required this.package,
    required this.strings,
    required this.draft,
    required this.onSubmit,
    this.planDays,
  });

  @override
  State<RecordSessionSheet> createState() => _RecordSessionSheetState();
}

class _RecordSessionSheetState extends State<RecordSessionSheet> {
  final _note = TextEditingController();
  bool _saving = false;
  String? _error;
  late String? _planDayKey = widget.planDays?.suggestedKey;

  SessionTimeDraft get _draft => widget.draft;
  RecordSessionStrings get _s => widget.strings;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  void _update(VoidCallback change) => setState(() {
        change();
        _error = null;
      });

  Future<void> _pickOtherDay() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _draft.day,
      firstDate: _draft.firstDay,
      lastDate: _draft.today,
      helpText: _s.when,
    );
    if (picked != null) _update(() => _draft.setDay(picked));
  }

  Future<void> _pickStart() async {
    final picked = await showTimePicker(context: context, initialTime: _draft.start, helpText: _s.startTime);
    if (picked != null) _update(() => _draft.setStart(picked));
  }

  Future<void> _submit() async {
    final navigator = Navigator.of(context);
    setState(() => _saving = true);
    try {
      final note = _note.text.trim();
      final planDay = _planDayKey;
      final result = await widget.onSubmit({
        ..._draft.toPayload(),
        if (note.isNotEmpty) 'notes': note,
        if (planDay != null && widget.planDays != null) 'planDay': widget.planDays!.payload(planDay),
      });
      navigator.pop(result);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = _friendly(e));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  String _friendly(ApiException e) {
    final code = e.errorCode ?? '';
    if (code.contains('QuickLog.TooOld')) return _s.issue(SessionTimeIssue.tooOld);
    if (code.contains('QuickLog.NotStarted')) return _s.issue(SessionTimeIssue.notStarted);
    if (code.contains('NoSessionsRemaining')) return _s.noSessionsLeft;
    if (code.contains('ClientNotAssignedToTrainer')) return _s.notYourClient;
    return _s.saveFailed;
  }

  @override
  Widget build(BuildContext context) {
    final issue = _draft.issue;
    final message = issue == null ? _error : _s.issue(issue);
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(20, 0, 20, 16 + MediaQuery.viewInsetsOf(context).bottom),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _Header(name: widget.clientName, package: widget.package, strings: _s),
              const SizedBox(height: 20),
              Text(_s.when, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              _dayChips(),
              const SizedBox(height: 12),
              _startAndDuration(),
              const SizedBox(height: 8),
              Text(_s.slot(context, _draft), key: const ValueKey('session-slot'),
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
              if (widget.planDays case final days?) ...[
                const SizedBox(height: 16),
                _PlanDayPicker(
                  days: days,
                  selected: _planDayKey,
                  strings: _s,
                  onSelected: (key) => setState(() => _planDayKey = key),
                ),
              ],
              const SizedBox(height: 16),
              TextField(
                controller: _note,
                maxLength: 500,
                maxLines: 2,
                decoration: InputDecoration(labelText: _s.note, hintText: _s.noteHint),
              ),
              if (message != null) _ErrorLine(text: message),
              const SizedBox(height: 8),
              FilledButton(
                key: const ValueKey('record-session'),
                onPressed: _saving || issue != null ? null : _submit,
                child: _saving
                    ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(_s.record),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _dayChips() {
    final other = !_draft.isToday && !_draft.isYesterday;
    return Wrap(spacing: 8, runSpacing: 8, children: [
      ChoiceChip(
        key: const ValueKey('day-today'),
        label: Text(_s.today),
        selected: _draft.isToday,
        onSelected: (_) => _update(() => _draft.setDay(_draft.today)),
      ),
      ChoiceChip(
        key: const ValueKey('day-yesterday'),
        label: Text(_s.yesterday),
        selected: _draft.isYesterday,
        onSelected: (_) => _update(() => _draft.setDay(_draft.yesterday)),
      ),
      ChoiceChip(
        key: const ValueKey('day-other'),
        avatar: const Icon(Icons.event, size: 18),
        label: Text(other ? _s.day(_draft.day, _draft) : _s.otherDay),
        selected: other,
        onSelected: (_) => _pickOtherDay(),
      ),
    ]);
  }

  Widget _startAndDuration() => Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Expanded(
          child: OutlinedButton.icon(
            key: const ValueKey('start-time'),
            onPressed: _pickStart,
            icon: const Icon(Icons.schedule),
            label: Text('${_s.startTime}: ${_draft.start.format(context)}'),
          ),
        ),
        // One tap back to "going on now" after picking another day or time.
        if (!_draft.isNow)
          TextButton(
            key: const ValueKey('start-now'),
            onPressed: () => _update(_draft.resetToNow),
            child: Text(_s.now),
          ),
        const SizedBox(width: 8),
        DropdownButton<int>(
          key: const ValueKey('duration'),
          value: _draft.minutes,
          items: [
            for (final m in SessionTimeDraft.durations) DropdownMenuItem(value: m, child: Text(_s.minutes(m))),
          ],
          onChanged: (m) => m == null ? null : _update(() => _draft.setMinutes(m)),
        ),
      ]);
}

class _Header extends StatelessWidget {
  final String name;
  final SessionPackageModel? package;
  final RecordSessionStrings strings;

  const _Header({required this.name, required this.package, required this.strings});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final p = package;
    final low = p != null && p.remainingSessions <= 3;
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(strings.title, style: theme.textTheme.labelLarge?.copyWith(color: AppColors.grey500)),
      const SizedBox(height: 4),
      Row(children: [
        Flexible(
          child: Text(name.isEmpty ? strings.client : name,
              style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800), overflow: TextOverflow.ellipsis),
        ),
        if (p?.isTrial ?? false) ...[const SizedBox(width: 8), TrialBadge(isArabic: strings.isArabic)],
      ]),
      if (p != null) ...[
        const SizedBox(height: 4),
        Text(strings.leftAfter(p.remainingSessions, p.totalSessions),
            style: theme.textTheme.bodyMedium?.copyWith(
              color: low ? AppColors.warning : null,
              fontWeight: low ? FontWeight.w700 : null,
            )),
      ],
    ]);
  }
}

class _PlanDayPicker extends StatelessWidget {
  final SessionPlanDays days;
  final String? selected;
  final RecordSessionStrings strings;
  final ValueChanged<String?> onSelected;

  const _PlanDayPicker({required this.days, required this.selected, required this.strings, required this.onSelected});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(strings.planDay, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
      Text(strings.planDayHint, style: theme.textTheme.bodySmall?.copyWith(color: AppColors.grey500)),
      const SizedBox(height: 8),
      Wrap(spacing: 8, runSpacing: 8, children: [
        ChoiceChip(
          key: const ValueKey('plan-day-none'),
          label: Text(strings.noPlanDay),
          selected: selected == null,
          onSelected: (_) => onSelected(null),
        ),
        for (final slot in days.shown)
          ChoiceChip(
            key: ValueKey('plan-day-${slot.day.key}'),
            label: Text(_label(slot)),
            selected: selected == slot.day.key,
            onSelected: (_) => onSelected(slot.day.key),
          ),
      ]),
    ]);
  }

  String _label(PlanSlot slot) {
    final title = slot.day.displayTitle(strings.isArabic);
    return slot.day.key == days.suggestedKey ? '$title · ${strings.nextPlanDay}' : title;
  }
}

class _ErrorLine extends StatelessWidget {
  final String text;

  const _ErrorLine({required this.text});

  @override
  Widget build(BuildContext context) => Padding(
        key: const ValueKey('record-error'),
        padding: const EdgeInsets.only(bottom: 4),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          const Icon(Icons.error_outline, color: AppColors.error, size: 18),
          const SizedBox(width: 6),
          Expanded(child: Text(text, style: const TextStyle(color: AppColors.error))),
        ]),
      );
}
