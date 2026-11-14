import 'package:flutter/material.dart';

import '../../progress_journey.dart';
import '../measurement_form_controller.dart';
import '../measurement_form_strings.dart';
import '../measurement_rules.dart';
import 'check_in_header.dart';
import 'check_in_saved_sheet.dart';
import 'measurement_save_bar.dart';
import 'measurement_sections.dart';

/// What the page hands to the app to send: the day and the reading part of the request.
class MeasurementSubmission {
  final DateTime date;
  final Map<String, dynamic> reading;

  const MeasurementSubmission({required this.date, required this.reading});

  String get apiDay => apiDate(date);
}

/// Sends a submission; returns null on success or the message to show on failure.
typedef MeasurementSubmit = Future<String?> Function(MeasurementSubmission submission);

/// pt.progress_measurements: one check-in for coach or member — date, body, tape, notes, live
/// validation against the last reading, an unsaved-changes guard and a saved summary.
class MeasurementEntryPage extends StatefulWidget {
  final String title;
  final bool isArabic;

  /// The client's history; null while loading (the form works without it).
  final ProgressJourney? journey;

  /// Shown above the form (e.g. the coach's first-session goal step).
  final Widget? leading;
  final MeasurementSubmit onSubmit;

  /// Member app: show the "what changed" sheet after saving instead of a snackbar.
  final bool celebrate;
  final DateTime? now;

  const MeasurementEntryPage({
    super.key,
    required this.title,
    required this.isArabic,
    required this.journey,
    required this.onSubmit,
    this.leading,
    this.celebrate = false,
    this.now,
  });

  @override
  State<MeasurementEntryPage> createState() => _MeasurementEntryPageState();
}

class _MeasurementEntryPageState extends State<MeasurementEntryPage> {
  final _form = MeasurementFormController();
  late DateTime _date = _today;
  bool _saving = false;

  DateTime get _now => widget.now ?? DateTime.now();
  DateTime get _today => DateTime(_now.year, _now.month, _now.day);
  MeasurementFormStrings get _strings => MeasurementFormStrings(widget.isArabic);
  bool get _alreadyRecorded => hasEntryOn(widget.journey, _date);

  @override
  void initState() {
    super.initState();
    _form.useJourney(widget.journey);
  }

  @override
  void didUpdateWidget(MeasurementEntryPage old) {
    super.didUpdateWidget(old);
    if (!identical(old.journey, widget.journey)) _form.useJourney(widget.journey);
  }

  @override
  void dispose() {
    _form.dispose();
    super.dispose();
  }

  String? get _blockedReason {
    if (_alreadyRecorded) return _strings.alreadyRecorded;
    if (_form.hasErrors) return _strings.fixErrors;
    if (_form.filledCount == 0) return _strings.enterOne;
    return null;
  }

  Future<void> _save() async {
    final navigator = Navigator.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final changes = _form.changes;
    setState(() => _saving = true);
    final error = await widget.onSubmit(MeasurementSubmission(date: _date, reading: _form.reading()));
    if (!mounted) return;
    setState(() => _saving = false);
    if (error != null) {
      messenger.showSnackBar(SnackBar(content: Text(error)));
      return;
    }
    if (widget.celebrate) {
      await showCheckInSavedSheet(context, strings: _strings, changes: changes);
    } else {
      messenger.showSnackBar(SnackBar(content: Text(_strings.savedSummary(changes))));
    }
    navigator.pop();
  }

  Future<void> _confirmLeave() async {
    final s = _strings;
    final discard = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(s.discardTitle),
        content: Text(s.discardBody),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: Text(s.keepEditing)),
          TextButton(onPressed: () => Navigator.pop(dialogContext, true), child: Text(s.discard)),
        ],
      ),
    );
    if (discard == true && mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    final s = _strings;
    return ListenableBuilder(
      listenable: _form,
      builder: (context, _) => PopScope(
        canPop: !_form.isDirty || _saving,
        onPopInvokedWithResult: (didPop, _) {
          if (!didPop) _confirmLeave();
        },
        child: Scaffold(
          appBar: AppBar(title: Text(widget.title)),
          body: GestureDetector(
            onTap: () => FocusScope.of(context).unfocus(),
            child: ListView(
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
              children: _sections(s),
            ),
          ),
          bottomNavigationBar: MeasurementSaveBar(
            strings: s,
            filled: _form.filledCount,
            blockedReason: _blockedReason,
            saving: _saving,
            onSave: _save,
          ),
        ),
      ),
    );
  }

  List<Widget> _sections(MeasurementFormStrings s) {
    const gap = SizedBox(height: 12);
    return [
      CheckInHeader(
        strings: s,
        lastCheckIn: widget.journey?.latest == null ? null : ProgressJourney.dateOf(widget.journey!.latest!),
        date: _date,
        now: _now,
        alreadyRecorded: _alreadyRecorded,
        onDate: (d) => setState(() => _date = d),
      ),
      gap,
      if (widget.leading != null) ...[widget.leading!, gap],
      BodySection(form: _form, strings: s),
      gap,
      TapeSection(form: _form, strings: s),
      gap,
      TextField(
        controller: _form.notes,
        maxLength: 500,
        maxLines: 2,
        decoration: InputDecoration(labelText: s.notes, hintText: s.notesHint),
      ),
    ];
  }
}
