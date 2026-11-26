import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'day_label_picker.dart';
import 'exercise_progress_providers.dart';
import 'exercise_row_editor.dart';
import 'session_exercises_draft.dart';

/// pt.exercise_progress: the coach records what the client did this session — day label,
/// sets × reps @ weight, a note and the next target per exercise. Picking a day the client
/// trained before pre-fills last time's exercises (or the targets the coach set); the coach can
/// switch days, clear the day or start over with no template right here, and undo each change.
class SessionExercisesScreen extends ConsumerStatefulWidget {
  final String sessionId;
  final String clientId;

  const SessionExercisesScreen({super.key, required this.sessionId, required this.clientId});

  @override
  ConsumerState<SessionExercisesScreen> createState() => _SessionExercisesScreenState();
}

class _SessionExercisesScreenState extends ConsumerState<SessionExercisesScreen> {
  final _draft = SessionExercisesDraft();
  final _rowKeys = Expando<GlobalKey>();
  bool _saving = false;
  bool _showErrors = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  String _t(String ar, String en) => _isArabic ? ar : en;

  ExerciseProgressModel? get _board => ref.read(clientExerciseProgressProvider(widget.clientId)).valueOrNull;

  String _dayName(String label) => ExerciseDays.display(label, _isArabic);

  Future<void> _pickDay(String? label) async {
    final before = _draft.snapshot();
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
  Future<bool?> _confirmReplace(String label) => showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(_t('استبدال التمارين؟', 'Replace exercises?')),
        content: Text(_t('هنستبدل التمارين اللي كتبتها بتمارين آخر ${_dayName(label)}. تقدر تتراجع بعدها.',
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
    final before = _draft.snapshot();
    setState(() {
      _draft.startEmpty();
      _showErrors = false;
    });
    _undoable(before, _t('بدأت من غير قالب', 'Started without a template'));
  }

  void _undoable(SessionExercisesSnapshot before, String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(
        content: Text(message),
        action: SnackBarAction(
          key: const ValueKey('undo-exercises'),
          label: _t('تراجع', 'Undo'),
          onPressed: () => setState(() => _draft.restore(before)),
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
            Text(_t('أي يوم تمرين؟', 'Which training day?'), style: Theme.of(context).textTheme.titleSmall),
            const SizedBox(height: 8),
            DayLabelPicker(
              selected: _draft.dayLabel,
              isArabic: _isArabic,
              pastDays: SessionExercisesDraft.pastCustomDays(board),
              onChanged: _pickDay,
            ),
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
