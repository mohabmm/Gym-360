import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../plans/data/session_plan_days.dart';
import 'day_label_picker.dart';
import 'exercise_progress_providers.dart';
import 'exercise_row_editor.dart';
import 'session_exercises_draft.dart';

/// pt.exercise_progress: the coach records what the client did this session — day label,
/// sets × reps @ weight, a note and the next target per exercise. Picking a day the client
/// trained before pre-fills last time's exercises (or the targets the coach set); the coach can
/// switch days, clear the day or start over with no template right here, and undo each change.
///
/// When the member has a training plan (pt.training_plan) the coach chooses where the exercises
/// come from: the member's plan (default — the day counted when recording, [planDayKey], or the
/// member's next day) or from scratch, and can switch between them at any time, with Undo.
class SessionExercisesScreen extends ConsumerStatefulWidget {
  final String sessionId;
  final String clientId;
  final String? planDayKey;

  const SessionExercisesScreen({super.key, required this.sessionId, required this.clientId, this.planDayKey});

  @override
  ConsumerState<SessionExercisesScreen> createState() => _SessionExercisesScreenState();
}

/// Where the session's exercises come from.
enum ExerciseSource { plan, scratch }

/// The screen as it was, so a change can be undone (the form, and where it came from).
typedef _Snapshot = ({SessionExercisesSnapshot draft, ExerciseSource source, String? planKey});

class _SessionExercisesScreenState extends ConsumerState<SessionExercisesScreen> {
  final _draft = SessionExercisesDraft();
  final _rowKeys = Expando<GlobalKey>();
  bool _saving = false;
  bool _showErrors = false;
  ExerciseSource _source = ExerciseSource.scratch;
  String? _planKey;
  bool _planOffered = false;

  @override
  void initState() {
    super.initState();
    ref.listenManual<AsyncValue<SessionPlanDays?>>(
        sessionPlanDaysProvider(widget.clientId), (_, next) => _offerPlan(next.valueOrNull));
    // Already loaded (e.g. the record sheet just read it): offer it once the screen is built.
    WidgetsBinding.instance.addPostFrameCallback((_) => _offerPlan(_planDays));
  }

  /// Once the plan is known: open on it (the day counted when recording, else the member's next
  /// day), unless the coach already started typing their own exercises.
  void _offerPlan(SessionPlanDays? days) {
    if (!mounted || _planOffered || days == null || days.slots.isEmpty) return;
    _planOffered = true;
    if (!_draft.rows.every((r) => r.isUntouched)) return;
    final keys = days.slots.map((s) => s.day.key).toList();
    final key = [widget.planDayKey, days.suggestedKey].whereType<String>().where(keys.contains).firstOrNull ?? keys.first;
    setState(() {
      _draft.pickPlanDay(_slot(days, key).day, isArabic: _isArabic);
      _source = ExerciseSource.plan;
      _planKey = key;
    });
  }

  static PlanSlot _slot(SessionPlanDays days, String key) => days.slots.firstWhere((s) => s.day.key == key);

  SessionPlanDays? get _planDays => ref.read(sessionPlanDaysProvider(widget.clientId)).valueOrNull;

  _Snapshot _snap() => (draft: _draft.snapshot(), source: _source, planKey: _planKey);

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  String _t(String ar, String en) => _isArabic ? ar : en;

  ExerciseProgressModel? get _board => ref.read(clientExerciseProgressProvider(widget.clientId)).valueOrNull;

  String _dayName(String label) => ExerciseDays.display(label, _isArabic);

  Future<void> _pickDay(String? label) async {
    final before = _snap();
    if (label == null) {
      final cleared = _draft.clearDay();
      setState(() {});
      if (cleared) _undoable(before, _t('تم إلغاء اليوم وتمارينه', 'Day and its exercises cleared'));
      return;
    }
    var pick = _draft.pickDay(_board, label);
    if (pick == DayPick.needsConfirm) {
      final replace = await _confirmReplace(label);
      if (replace == null || !mounted) return;
      // "Keep mine" still names the day as picked.
      if (!replace) {
        setState(() => _draft.dayLabel = label);
        return;
      }
      pick = _draft.pickDay(_board, label, replace: true);
    }
    setState(() {});
    switch (pick) {
      case DayPick.filled:
        _undoable(before, _t('تمت تعبئة تمارين آخر ${_dayName(label)}', "Filled in last ${_dayName(label)}'s exercises"));
      case DayPick.cleared:
        _undoable(before, _t('بدأت ${_dayName(label)} من جديد', 'Started ${_dayName(label)} fresh'));
      case DayPick.labelled || DayPick.needsConfirm:
        break;
    }
  }

  /// The coach already wrote exercises: replace them with last time's (true), keep them (false)?
  Future<bool?> _confirmReplace(String label, {bool fromPlan = false}) => showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(_t('استبدال التمارين؟', 'Replace exercises?')),
        content: Text(fromPlan
            ? _t('هنستبدل التمارين اللي كتبتها بتمارين «$label» من خطة العضو. تقدر تتراجع بعدها.',
                'Your exercises will be replaced with "$label" from the member\'s plan. You can undo it.')
            : _t('هنستبدل التمارين اللي كتبتها بتمارين آخر ${_dayName(label)}. تقدر تتراجع بعدها.',
                "Your exercises will be replaced with last ${_dayName(label)}'s. You can undo it.")),
        actions: [
          TextButton(
            key: const ValueKey('keep-mine'),
            onPressed: () => Navigator.pop(context, false),
            child: Text(_t('احتفظ بتماريني', 'Keep mine')),
          ),
          FilledButton(
            key: const ValueKey('replace-exercises'),
            onPressed: () => Navigator.pop(context, true),
            child: Text(_t('استبدال', 'Replace')),
          ),
        ],
      ),
    );

  /// Empty form, no template: the coach writes their own exercises.
  void _startEmpty() {
    final before = _snap();
    setState(() {
      _draft.startEmpty();
      _showErrors = false;
    });
    _undoable(before, _t('بدأت من غير قالب', 'Started without a template'));
  }

  /// From the member's plan: fills that plan day's exercises (asks first if the coach wrote their own).
  Future<void> _choosePlanDay(String key) async {
    final days = _planDays;
    if (days == null) return;
    final day = _slot(days, key).day;
    final before = _snap();
    var pick = _draft.pickPlanDay(day, isArabic: _isArabic);
    if (pick == DayPick.needsConfirm) {
      final replace = await _confirmReplace(day.displayTitle(_isArabic), fromPlan: true);
      if (replace == null || !mounted) return;
      pick = replace ? _draft.pickPlanDay(day, isArabic: _isArabic, replace: true) : DayPick.labelled;
      if (!replace) _draft.dayLabel = SessionExercisesDraft.planDayLabel(day, _isArabic);
    }
    setState(() {
      _source = ExerciseSource.plan;
      _planKey = key;
      _showErrors = false;
    });
    if (pick == DayPick.filled) {
      _undoable(before, _t('تمت تعبئة تمارين «${day.displayTitle(true)}» من خطة العضو',
          'Filled in "${day.displayTitle(false)}" from the member\'s plan'));
    }
  }

  /// From scratch: no template, one empty exercise — the coach writes their own.
  void _chooseScratch() {
    final before = _snap();
    setState(() {
      _draft
        ..startEmpty()
        ..dayLabel = null;
      _source = ExerciseSource.scratch;
      _showErrors = false;
    });
    _undoable(before, _t('بدأت من البداية بدون خطة', 'Started from scratch'));
  }

  void _chooseSource(ExerciseSource source) {
    if (source == _source) return;
    if (source == ExerciseSource.scratch) return _chooseScratch();
    final days = _planDays;
    if (days == null || days.slots.isEmpty) return;
    _choosePlanDay(_planKey ?? days.suggestedKey ?? days.slots.first.day.key);
  }

  void _undoable(_Snapshot before, String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(
        content: Text(message),
        action: SnackBarAction(
          key: const ValueKey('undo-exercises'),
          label: _t('تراجع', 'Undo'),
          onPressed: () => setState(() {
            _draft.restore(before.draft);
            _source = before.source;
            _planKey = before.planKey;
          }),
        ),
      ));
  }

  /// Marks what still blocks saving and brings the first incomplete exercise into view.
  bool _blockedByIncompleteRow() {
    final invalid = _draft.firstInvalid;
    if (invalid == null) return false;
    setState(() => _showErrors = true);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(_issueMessage(invalid))));
    final rowContext = _rowKeys[invalid]?.currentContext;
    if (rowContext != null) {
      Scrollable.ensureVisible(rowContext, duration: const Duration(milliseconds: 300), alignment: 0.1);
    }
    return true;
  }

  String _issueMessage(ExerciseRowDraft row) {
    if (row.issue == ExerciseRowIssue.missingName) {
      return _t('اكتب اسم التمرين أو احذفه', 'Name this exercise or remove it');
    }
    final ar = row.setsMissing.join('، ');
    final en = row.setsMissing.join(', ');
    final name = row.name.trim();
    return row.isTimed
        ? _t('أكمل وقت المجموعة $ar في «$name»', 'Fill in the time for set $en of "$name"')
        : _t('أكمل عدات المجموعة $ar في «$name»', 'Fill in the reps for set $en of "$name"');
  }

  Future<void> _save() async {
    if (_blockedByIncompleteRow()) return;
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    setState(() => _saving = true);
    try {
      await saveSessionExercises(ref, sessionId: widget.sessionId, clientId: widget.clientId, draft: _draft);
      messenger.showSnackBar(SnackBar(content: Text(_t('تم حفظ التمارين', 'Exercises saved'))));
      router.pop();
    } on ApiException catch (_) {
      messenger.showSnackBar(SnackBar(content: Text(_t('تعذر الحفظ، حاول مرة أخرى', "Couldn't save. Try again."))));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    // Warm the board so picking a day can pre-fill last time's numbers.
    final board = ref.watch(clientExerciseProgressProvider(widget.clientId)).valueOrNull;
    final planDays = ref.watch(sessionPlanDaysProvider(widget.clientId)).valueOrNull;
    return RtlScaffold(
      isArabic: _isArabic,
      appBar: AppBar(
        title: Text(_t('تمارين الحصة', 'Session exercises')),
        actions: [
          if (_draft.canClear)
            TextButton.icon(
              key: const ValueKey('start-empty'),
              onPressed: _startEmpty,
              icon: const Icon(Icons.restart_alt),
              label: Text(_t('ابدأ من جديد', 'Start over')),
            ),
        ],
      ),
      // A Column (not a lazy ListView) keeps every exercise built, so off-screen rows keep their
      // state and the first incomplete one can be scrolled into view.
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (planDays != null && planDays.slots.isNotEmpty) ...[
              _SourceSwitch(source: _source, isArabic: _isArabic, onChanged: _chooseSource),
              const SizedBox(height: 16),
            ],
            if (_source == ExerciseSource.plan && planDays != null)
              _PlanDayPicker(days: planDays, selected: _planKey, isArabic: _isArabic, onPick: _choosePlanDay)
            else ...[
              Text(_t('أي يوم تمرين؟', 'Which training day?'), style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: 8),
              DayLabelPicker(
                selected: _draft.dayLabel,
                isArabic: _isArabic,
                pastDays: SessionExercisesDraft.pastCustomDays(board),
                onChanged: _pickDay,
              ),
            ],
            const SizedBox(height: 16),
            for (final row in _draft.rows)
              ExerciseRowEditor(
                key: _rowKeys[row] ??= GlobalKey(),
                draft: row,
                isArabic: _isArabic,
                showErrors: _showErrors,
                onChanged: () => setState(() {}),
                onRemove: () => setState(() => _draft.rows.remove(row)),
              ),
            OutlinedButton.icon(
              onPressed: () => setState(() => _draft.rows.add(ExerciseRowDraft())),
              icon: const Icon(Icons.add),
              label: Text(_t('أضف تمرين', 'Add exercise')),
            ),
          ],
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: FilledButton(
            onPressed: _draft.hasEntries && !_saving ? _save : null,
            child: _saving
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : Text(_t('حفظ التمارين', 'Save exercises')),
          ),
        ),
      ),
    );
  }
}

/// "From the member's plan" or "From scratch" — the coach can switch at any time.
class _SourceSwitch extends StatelessWidget {
  final ExerciseSource source;
  final bool isArabic;
  final ValueChanged<ExerciseSource> onChanged;

  const _SourceSwitch({required this.source, required this.isArabic, required this.onChanged});

  @override
  Widget build(BuildContext context) => SegmentedButton<ExerciseSource>(
        key: const ValueKey('exercise-source'),
        showSelectedIcon: false,
        segments: [
          ButtonSegment(
            value: ExerciseSource.plan,
            icon: const Icon(Icons.event_note_outlined),
            label: Text(isArabic ? 'من خطة العضو' : "Member's plan", key: const ValueKey('source-plan')),
          ),
          ButtonSegment(
            value: ExerciseSource.scratch,
            icon: const Icon(Icons.edit_outlined),
            label: Text(isArabic ? 'من البداية' : 'From scratch', key: const ValueKey('source-scratch')),
          ),
        ],
        selected: {source},
        onSelectionChanged: (picked) => onChanged(picked.first),
      );
}

/// The plan days of one week (week picker when the plan has more than one); tapping a day,
/// even the selected one, fills its exercises.
class _PlanDayPicker extends StatefulWidget {
  final SessionPlanDays days;
  final String? selected;
  final bool isArabic;
  final ValueChanged<String> onPick;

  const _PlanDayPicker({required this.days, required this.selected, required this.isArabic, required this.onPick});

  @override
  State<_PlanDayPicker> createState() => _PlanDayPickerState();
}

class _PlanDayPickerState extends State<_PlanDayPicker> {
  int? _week;

  List<int> get _weeks => widget.days.slots.map((s) => s.week).toSet().toList()..sort();

  int get _shownWeek =>
      _week ??
      widget.days.slots.where((s) => s.day.key == widget.selected).firstOrNull?.week ??
      widget.days.shown.firstOrNull?.week ??
      _weeks.first;

  @override
  Widget build(BuildContext context) {
    final ar = widget.isArabic;
    final week = _shownWeek;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(children: [
          Expanded(
            child: Text(ar ? 'يوم الخطة' : 'Plan day', style: Theme.of(context).textTheme.titleSmall),
          ),
          if (_weeks.length > 1)
            DropdownButton<int>(
              key: const ValueKey('plan-week'),
              value: week,
              items: [
                for (final w in _weeks) DropdownMenuItem(value: w, child: Text(ar ? 'الأسبوع $w' : 'Week $w')),
              ],
              onChanged: (w) => setState(() => _week = w),
            ),
        ]),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final slot in widget.days.slots.where((s) => s.week == week))
              ChoiceChip(
                key: ValueKey('plan-exercises-day-${slot.day.key}'),
                label: Text(slot.day.displayTitle(ar)),
                selected: slot.day.key == widget.selected,
                onSelected: (_) => widget.onPick(slot.day.key),
              ),
          ],
        ),
      ],
    );
  }
}
