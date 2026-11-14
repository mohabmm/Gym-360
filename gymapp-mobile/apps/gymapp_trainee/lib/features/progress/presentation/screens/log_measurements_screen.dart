import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../gamification/presentation/providers/gamification_providers.dart';
import '../../../home/presentation/providers/home_providers.dart';
import '../providers/progress_providers.dart';

/// The member's own check-in: same form as the coach's (last values, live change, range checks,
/// one entry per day), then a "what changed" sheet that celebrates wins.
class LogMeasurementsScreen extends ConsumerWidget {
  const LogMeasurementsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return MeasurementEntryPage(
      title: isArabic ? 'قياسات الجسم' : 'Body measurements',
      isArabic: isArabic,
      journey: ref.watch(progressJourneyProvider).valueOrNull,
      celebrate: true,
      onSubmit: (submission) => _submit(ref, submission, MeasurementFormStrings(isArabic)),
    );
  }

  Future<String?> _submit(WidgetRef ref, MeasurementSubmission submission, MeasurementFormStrings strings) async {
    final auth = await ref.read(authProvider.future);
    final userId = auth.user?.id;
    final gymId = auth.currentGymId;
    if (userId == null || gymId == null) return strings.saveFailed;
    try {
      await ref.read(progressRepositoryProvider).logBodyMetric({
        'userId': userId,
        'gymId': gymId,
        'date': submission.apiDay,
        ...submission.reading,
        'source': 'SelfReported',
        'loggedBy': userId,
      });
    } on ApiException catch (e) {
      return e.statusCode == 409 ? strings.alreadyRecorded : strings.saveFailed;
    } catch (_) {
      return strings.saveFailed;
    }
    refreshProgress(ref);
    // Keep the Home weight tile and leaderboard in sync with the new value.
    ref.invalidate(progressDashboardProvider);
    ref.invalidate(leaderboardProvider);
    return null;
  }
}
