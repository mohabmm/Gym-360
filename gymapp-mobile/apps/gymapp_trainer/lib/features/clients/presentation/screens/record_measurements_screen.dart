import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/client_providers.dart';

/// pt.progress_measurements — the coach records a client's check-in (from the first session on):
/// last values + live change per field, range checks, date, and one entry per day.
/// The trainee sees it on their Progress tab.
class RecordMeasurementsScreen extends ConsumerStatefulWidget {
  final String clientId;
  const RecordMeasurementsScreen({super.key, required this.clientId});

  @override
  ConsumerState<RecordMeasurementsScreen> createState() => _RecordMeasurementsScreenState();
}

class _RecordMeasurementsScreenState extends ConsumerState<RecordMeasurementsScreen> {
  final _target = TextEditingController();
  ProgressGoalType? _goal;

  @override
  void dispose() {
    _target.dispose();
    super.dispose();
  }

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  Future<String?> _submit(MeasurementSubmission submission) async {
    final strings = MeasurementFormStrings(_isArabic);
    final gymId = ref.read(activeGymIdProvider);
    if (gymId == null) return strings.saveFailed;
    try {
      await ref.read(apiClientProvider).postVoid(ApiEndpoints.bodyMetricsForClient, data: {
        'clientId': widget.clientId,
        'gymId': gymId,
        'date': submission.apiDay,
        ...submission.reading,
      });
    } on FeatureDisabledException {
      return strings.saveFailed;
    } on ApiException catch (e) {
      final duplicate = (e.errorCode ?? '').contains('Duplicate') || e.statusCode == 409;
      return duplicate ? strings.alreadyRecorded : strings.saveFailed;
    }
    await _saveGoalIfChosen();
    ref.invalidate(clientDetailProvider(widget.clientId));
    ref.invalidate(clientJourneyProvider(widget.clientId));
    return null;
  }

  /// First session: the goal picked above the measurements (the measurements are already saved,
  /// so a failure here is not fatal; the goal can be set later from the Progress tab).
  Future<void> _saveGoalIfChosen() async {
    final goal = _goal;
    if (goal == null) return;
    final target = TargetWeightInput.parse(_target.text);
    try {
      await setClientGoal(ref, widget.clientId, GoalSelection(goal, target.isInRange ? target.kg : null));
    } on ApiException catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final journey = ref.watch(clientJourneyProvider(widget.clientId)).valueOrNull;
    // Ask for the goal only while the client has none (normally the first session).
    final needsGoal = journey != null && journey.goal == null;
    return MeasurementEntryPage(
      title: ProgressJourneyStrings(_isArabic).recordMeasurements,
      isArabic: _isArabic,
      journey: journey,
      onSubmit: _submit,
      leading: needsGoal
          ? _FirstSessionGoal(
              strings: ProgressJourneyStrings(_isArabic),
              goal: _goal,
              target: _target,
              onGoal: (g) => setState(() => _goal = g),
            )
          : null,
    );
  }
}

/// Goal chips + optional target weight, shown on the first measurement.
class _FirstSessionGoal extends StatelessWidget {
  final ProgressJourneyStrings strings;
  final ProgressGoalType? goal;
  final TextEditingController target;
  final ValueChanged<ProgressGoalType> onGoal;

  const _FirstSessionGoal({required this.strings, required this.goal, required this.target, required this.onGoal});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(strings.goalTitle, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final g in ProgressGoalType.values)
                  ChoiceChip(label: Text(strings.goal(g)), selected: goal == g, onSelected: (_) => onGoal(g)),
              ],
            ),
            if (goal != null) ...[
              const SizedBox(height: 12),
              TextField(
                controller: target,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
                decoration: InputDecoration(labelText: strings.targetWeight, helperText: strings.targetOptional),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
