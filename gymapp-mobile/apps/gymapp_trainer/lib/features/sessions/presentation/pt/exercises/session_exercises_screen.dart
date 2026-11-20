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
/// trained before pre-fills last time's exercises (or the targets the coach set).
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

  void _pickDay(String? label) {
    final board = ref.read(clientExerciseProgressProvider(widget.clientId)).valueOrNull;
    setState(() {
      if (label == null) {
        _draft.dayLabel = null;
      } else if (_draft.applyRepeat(board, label)) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(
              _t('تمت تعبئة تمارين آخر ${ExerciseDays.display(label, true)}', "Filled in last $label's exercises")),
        ));
      }
    });
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
    ref.watch(clientExerciseProgressProvider(widget.clientId));
    return RtlScaffold(
      isArabic: _isArabic,
      appBar: AppBar(title: Text(_t('تمارين الحصة', 'Session exercises'))),
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
            DayLabelPicker(selected: _draft.dayLabel, isArabic: _isArabic, onChanged: _pickDay),
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
