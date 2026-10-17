/// Filters for `GET /progress/body-metrics` (userId + gymId required).
class BodyMetricsQuery {
  const BodyMetricsQuery({
    required this.userId,
    required this.gymId,
    this.metricType,
    this.from,
    this.to,
  });

  final String userId;
  final String gymId;
  final String? metricType;
  final DateTime? from;
  final DateTime? to;

  Map<String, dynamic> toQueryParameters() => {
        'userId': userId,
        'gymId': gymId,
        if (metricType != null) 'metricType': metricType,
        if (from != null) 'from': from!.toIso8601String().split('T')[0],
        if (to != null) 'to': to!.toIso8601String().split('T')[0],
      };
}
