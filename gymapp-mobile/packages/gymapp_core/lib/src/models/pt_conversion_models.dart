/// Trial-to-paid conversion models (pt.trial_recap, pt.package_interest).
library;

/// The coach's post-trial plan for the trainee. No price: the desk prices the package.
class TrialRecapModel {
  final String packageId;
  final String trainerId;
  final String trainerName;
  final String? trainerPhotoUrl;
  final String note;
  final int recommendedSessions;
  final DateTime sentAt;
  final bool trialFinished;

  const TrialRecapModel({
    required this.packageId,
    required this.trainerId,
    required this.trainerName,
    this.trainerPhotoUrl,
    required this.note,
    required this.recommendedSessions,
    required this.sentAt,
    required this.trialFinished,
  });

  factory TrialRecapModel.fromJson(Map<String, dynamic> j) => TrialRecapModel(
        packageId: j['packageId'] as String? ?? '',
        trainerId: j['trainerId'] as String? ?? '',
        trainerName: j['trainerName'] as String? ?? '',
        trainerPhotoUrl: j['trainerPhotoUrl'] as String?,
        note: j['note'] as String? ?? '',
        recommendedSessions: (j['recommendedSessions'] as num?)?.toInt() ?? 0,
        sentAt: DateTime.tryParse(j['sentAt'] as String? ?? '') ?? DateTime.now(),
        trialFinished: j['trialFinished'] == true,
      );
}

/// Result of the trainee tapping "I'm interested".
class PackageInterestReceipt {
  final String interestId;
  final bool alreadyPending;

  const PackageInterestReceipt({required this.interestId, required this.alreadyPending});

  factory PackageInterestReceipt.fromJson(Map<String, dynamic> j) => PackageInterestReceipt(
        interestId: j['interestId'] as String? ?? '',
        alreadyPending: j['alreadyPending'] == true,
      );
}

/// A desk lead: a trainee who tapped "I'm interested".
class PackageInterestLead {
  final String id;
  final String memberId;
  final String memberName;
  final String? memberPhone;
  final String trainerId;
  final String trainerName;

  /// TrialRecap | TrialFollowUp | Renewal.
  final String source;
  final int? recommendedSessions;

  /// pending | converted | dismissed.
  final String state;
  final DateTime createdAt;

  const PackageInterestLead({
    required this.id,
    required this.memberId,
    required this.memberName,
    this.memberPhone,
    required this.trainerId,
    required this.trainerName,
    required this.source,
    this.recommendedSessions,
    required this.state,
    required this.createdAt,
  });

  bool get isPending => state == 'pending';

  factory PackageInterestLead.fromJson(Map<String, dynamic> j) => PackageInterestLead(
        id: j['id'] as String? ?? '',
        memberId: j['memberId'] as String? ?? '',
        memberName: j['memberName'] as String? ?? '',
        memberPhone: j['memberPhone'] as String?,
        trainerId: j['trainerId'] as String? ?? '',
        trainerName: j['trainerName'] as String? ?? '',
        source: j['source'] as String? ?? '',
        recommendedSessions: (j['recommendedSessions'] as num?)?.toInt(),
        state: j['state'] as String? ?? 'pending',
        createdAt: DateTime.tryParse(j['createdAt'] as String? ?? '') ?? DateTime.now(),
      );
}

/// Package sizes a coach can recommend: same rule as the desk's quick-assign (multiples of 4, 4–40).
const List<int> recommendableSessionCounts = [4, 8, 12, 16, 20, 24, 32, 40];
