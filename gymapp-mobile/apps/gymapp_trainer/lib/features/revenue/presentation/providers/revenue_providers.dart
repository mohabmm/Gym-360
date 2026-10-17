import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// Trainer earnings summary (read-only).
/// Calls GET /api/v1/billing/trainers/{trainerId}/earnings with the current
/// gym context and the current calendar month as the date range.
final trainerEarningsProvider =
    FutureProvider.autoDispose<TrainerEarningsSummary>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final authState = ref.watch(authProvider);
  final gymId = ref.watch(activeGymIdProvider);

  final trainerId = authState.user?.id;
  if (trainerId == null || gymId == null) {
    return TrainerEarningsSummary.empty();
  }

  final now = DateTime.now();
  final firstOfMonth = DateTime(now.year, now.month, 1);
  final fromStr =
      '${firstOfMonth.year}-${firstOfMonth.month.toString().padLeft(2, '0')}-${firstOfMonth.day.toString().padLeft(2, '0')}';
  final toStr =
      '${now.year}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}';

  try {
    return await apiClient.get<TrainerEarningsSummary>(
      ApiEndpoints.trainerEarnings(TrainerId(trainerId)),
      queryParameters: {
        'gymId': gymId,
        'from': fromStr,
        'to': toStr,
      },
      parser: (data) {
        if (data is Map<String, dynamic>) {
          return TrainerEarningsSummary.fromJson(data);
        }
        return TrainerEarningsSummary.empty();
      },
    );
  } catch (_) {
    return TrainerEarningsSummary.empty();
  }
});

/// Client session packages (visible to trainer) — BE endpoint:
/// GET /api/v1/billing/packages/my-clients?gymId=...
/// TrainerId is derived from the JWT on the backend.
final allClientPackagesProvider =
    FutureProvider.autoDispose<List<SessionPackageModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (gymId == null) return <SessionPackageModel>[];

  return apiClient.get<List<SessionPackageModel>>(
    ApiEndpoints.myClientPackages,
    queryParameters: {'gymId': gymId},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <SessionPackageModel>[];
      }
      return list
          .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});

/// Model for the trainer earnings summary returned by the API.
class TrainerEarningsSummary {
  final int totalSessions;
  final double totalEarningsEgp;
  final String currency;
  final List<EarningsRecord> records;

  const TrainerEarningsSummary({
    required this.totalSessions,
    required this.totalEarningsEgp,
    required this.currency,
    required this.records,
  });

  factory TrainerEarningsSummary.empty() => const TrainerEarningsSummary(
        totalSessions: 0,
        totalEarningsEgp: 0,
        currency: 'EGP',
        records: [],
      );

  factory TrainerEarningsSummary.fromJson(Map<String, dynamic> json) {
    return TrainerEarningsSummary(
      totalSessions: (json['totalSessions'] as num?)?.toInt() ?? 0,
      totalEarningsEgp: (json['totalEarningsEgp'] as num?)?.toDouble() ?? 0.0,
      currency: json['currency'] as String? ?? 'EGP',
      records: (json['records'] as List<dynamic>?)
              ?.map((e) => EarningsRecord.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

class EarningsRecord {
  final String sessionId;
  final String memberId;
  final double rateApplied;
  final String currency;
  final DateTime earnedAt;

  const EarningsRecord({
    required this.sessionId,
    required this.memberId,
    required this.rateApplied,
    required this.currency,
    required this.earnedAt,
  });

  factory EarningsRecord.fromJson(Map<String, dynamic> json) {
    return EarningsRecord(
      sessionId: json['sessionId'] as String,
      memberId: json['memberId'] as String,
      rateApplied: (json['rateApplied'] as num?)?.toDouble() ?? 0,
      currency: json['currency'] as String? ?? 'EGP',
      // Defensive: a null/malformed timestamp on one earning must not crash the
      // entire earnings list. Fall back to now rather than throwing.
      earnedAt:
          DateTime.tryParse(json['earnedAt']?.toString() ?? '') ??
              DateTime.now(),
    );
  }
}
