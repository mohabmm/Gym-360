/// Identifies one member's nutrition log (food or water) at a gym on a day.
class DailyLogQuery {
  const DailyLogQuery({
    required this.userId,
    required this.gymId,
    required this.date,
  });

  final String userId;
  final String gymId;
  final DateTime date;

  Map<String, dynamic> toQueryParameters() => {
        'userId': userId,
        'gymId': gymId,
        'date': date.toIso8601String().split('T')[0],
      };
}

/// Logs [amountMl] of water for a member at a gym on [date].
class WaterEntryRequest {
  const WaterEntryRequest({
    required this.userId,
    required this.gymId,
    required this.date,
    required this.amountMl,
  });

  final String userId;
  final String gymId;
  final DateTime date;
  final int amountMl;

  /// Body for `POST /nutrition/water-log`; `date` is sent as `YYYY-MM-DD`.
  Map<String, dynamic> toJson() => {
        'userId': userId,
        'gymId': gymId,
        'date': '${date.year.toString().padLeft(4, '0')}-'
            '${date.month.toString().padLeft(2, '0')}-'
            '${date.day.toString().padLeft(2, '0')}',
        'amountMl': amountMl,
      };
}
