class PendingTestimonialModel {
  const PendingTestimonialModel({
    required this.id,
    required this.trainerId,
    required this.gymId,
    required this.content,
    this.createdAt,
  });

  final String id;
  final String trainerId;
  final String gymId;
  final String content;
  final DateTime? createdAt;

  factory PendingTestimonialModel.fromJson(Map<String, dynamic> json) {
    final createdAtRaw = json['createdAt'] as String?;
    return PendingTestimonialModel(
      id: json['id'] as String,
      trainerId: json['trainerId'] as String,
      gymId: json['gymId'] as String,
      content: (json['commentText'] ?? json['content'] ?? '') as String,
      createdAt: createdAtRaw == null ? null : DateTime.tryParse(createdAtRaw),
    );
  }
}
