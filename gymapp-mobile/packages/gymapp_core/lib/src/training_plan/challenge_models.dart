/// mem.first_month_challenge: the trainee's progress and the desk's "earned the reward" list.
/// Mirrors `ChallengeStatusDto` and `ChallengeAwardDto` on the backend.
library;

DateTime? _date(Object? v) => v is String && v.isNotEmpty ? DateTime.tryParse(v) : null;

int _int(Object? v) => v is num ? v.toInt() : 0;

/// The trainee's challenge. [status] is one of [active], [reached], [ended], [notStarted].
class ChallengeStatusModel {
  static const active = 'active';
  static const reached = 'reached';
  static const ended = 'ended';
  static const notStarted = 'not_started';

  final String status;
  final int count;
  final int target;
  final DateTime? windowStart;
  final DateTime? windowEnd;
  final int daysLeft;
  final String rewardTextAr;
  final String rewardTextEn;
  final DateTime? reachedAt;
  final DateTime? deliveredAt;

  const ChallengeStatusModel({
    required this.status,
    required this.count,
    required this.target,
    this.windowStart,
    this.windowEnd,
    this.daysLeft = 0,
    required this.rewardTextAr,
    required this.rewardTextEn,
    this.reachedAt,
    this.deliveredAt,
  });

  factory ChallengeStatusModel.fromJson(Map<String, dynamic> json) => ChallengeStatusModel(
        status: json['status'] as String? ?? notStarted,
        count: _int(json['count']),
        target: _int(json['target']),
        windowStart: _date(json['windowStart']),
        windowEnd: _date(json['windowEnd']),
        daysLeft: _int(json['daysLeft']),
        rewardTextAr: json['rewardTextAr'] as String? ?? '',
        rewardTextEn: json['rewardTextEn'] as String? ?? '',
        reachedAt: _date(json['reachedAt']),
        deliveredAt: _date(json['deliveredAt']),
      );

  bool get isReached => status == reached;

  /// Worth a card: still running, or won and the reward not handed over yet.
  bool get isWorthShowing => status == active || (isReached && deliveredAt == null);

  String reward(bool isArabic) => isArabic || rewardTextEn.isEmpty ? rewardTextAr : rewardTextEn;

  /// The server's count plus workouts finished offline and not uploaded yet, while it runs.
  ChallengeStatusModel withPending(int pending) => pending <= 0 || status != active
      ? this
      : ChallengeStatusModel(
          status: status,
          count: count + pending,
          target: target,
          windowStart: windowStart,
          windowEnd: windowEnd,
          daysLeft: daysLeft,
          rewardTextAr: rewardTextAr,
          rewardTextEn: rewardTextEn,
        );

  double get fraction => target <= 0 ? 0 : (count / target).clamp(0, 1).toDouble();
}

/// A member who reached the target, for the desk.
class ChallengeAwardModel {
  final String awardId;
  final String traineeId;
  final String traineeName;
  final String? trainerName;
  final int count;
  final int target;
  final DateTime? reachedAt;
  final String rewardTextAr;
  final String rewardTextEn;
  final DateTime? deliveredAt;
  final String? deliveredByName;

  const ChallengeAwardModel({
    required this.awardId,
    required this.traineeId,
    required this.traineeName,
    this.trainerName,
    required this.count,
    required this.target,
    this.reachedAt,
    required this.rewardTextAr,
    required this.rewardTextEn,
    this.deliveredAt,
    this.deliveredByName,
  });

  factory ChallengeAwardModel.fromJson(Map<String, dynamic> json) => ChallengeAwardModel(
        awardId: json['awardId'] as String? ?? '',
        traineeId: json['traineeId'] as String? ?? '',
        traineeName: json['traineeName'] as String? ?? '—',
        trainerName: json['trainerName'] as String?,
        count: _int(json['count']),
        target: _int(json['target']),
        reachedAt: _date(json['reachedAt']),
        rewardTextAr: json['rewardTextAr'] as String? ?? '',
        rewardTextEn: json['rewardTextEn'] as String? ?? '',
        deliveredAt: _date(json['deliveredAt']),
        deliveredByName: json['deliveredByName'] as String?,
      );

  bool get isDelivered => deliveredAt != null;

  String reward(bool isArabic) => isArabic || rewardTextEn.isEmpty ? rewardTextAr : rewardTextEn;
}
