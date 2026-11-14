/// Body of `POST /progress/body-metrics/inbody`: the values the member confirmed. Masses in kg.
class InBodyScanRequest {
  final DateTime date;
  final double? weightKg;
  final double? heightCm;
  final double? bodyFatPercentage;
  final double? skeletalMuscleMassKg;
  final double? bodyWaterKg;

  const InBodyScanRequest({
    required this.date,
    this.weightKg,
    this.heightCm,
    this.bodyFatPercentage,
    this.skeletalMuscleMassKg,
    this.bodyWaterKg,
  });

  /// Builds the request from the review form, keyed by the JSON field names.
  factory InBodyScanRequest.fromValues(DateTime date, Map<String, double> values) => InBodyScanRequest(
        date: date,
        weightKg: values['weightKg'],
        heightCm: values['heightCm'],
        bodyFatPercentage: values['bodyFatPercentage'],
        skeletalMuscleMassKg: values['skeletalMuscleMassKg'],
        bodyWaterKg: values['bodyWaterKg'],
      );

  Map<String, dynamic> toJson() => {
        'date': '${date.year.toString().padLeft(4, '0')}-'
            '${date.month.toString().padLeft(2, '0')}-'
            '${date.day.toString().padLeft(2, '0')}',
        if (weightKg != null) 'weightKg': weightKg,
        if (heightCm != null) 'heightCm': heightCm,
        if (bodyFatPercentage != null) 'bodyFatPercentage': bodyFatPercentage,
        if (skeletalMuscleMassKg != null) 'skeletalMuscleMassKg': skeletalMuscleMassKg,
        if (bodyWaterKg != null) 'bodyWaterKg': bodyWaterKg,
      };
}
