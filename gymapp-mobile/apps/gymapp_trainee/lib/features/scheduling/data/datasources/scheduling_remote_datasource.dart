import 'package:gymapp_core/gymapp_core.dart';

List<dynamic> _unwrapItems(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}

class SchedulingRemoteDataSource {
  final ApiClient _apiClient;

  SchedulingRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// BE GET /scheduling/sessions/upcoming requires userId, gymId, role.
  Future<List<SessionModel>> getUpcomingSessions({
    required String userId,
    required String gymId,
  }) async {
    return _apiClient.get<List<SessionModel>>(
      ApiEndpoints.upcomingSessions,
      queryParameters: {
        'userId': userId,
        'gymId': gymId,
        'role': 'member',
      },
      parser: (data) => (data as List<dynamic>)
          .map<SessionModel>((e) => SessionModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  /// BE GET /scheduling/sessions requires gymId. Paginated.
  Future<List<SessionModel>> getAllSessions({
    required String gymId,
    String? memberId,
    String? status,
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.get<List<SessionModel>>(
      ApiEndpoints.sessions,
      queryParameters: {
        'gymId': gymId,
        if (memberId != null) 'memberId': memberId,
        if (status != null) 'status': status,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => _unwrapItems(data)
          .map<SessionModel>((e) => SessionModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<SessionDetailModel> getSessionById(String id) async {
    return _apiClient.get<SessionDetailModel>(
      ApiEndpoints.sessionById(id),
      parser: (data) =>
          SessionDetailModel.fromJson(data as Map<String, dynamic>),
    );
  }

  /// BE POST /scheduling/sessions returns Guid (session ID), 201.
  Future<String> bookSession(BookSessionRequest request) async {
    return _apiClient.post<String>(
      ApiEndpoints.sessions,
      data: request.toJson(),
      parser: (data) => data.toString(),
    );
  }

  Future<void> cancelSession(
    String sessionId, {
    required String cancelledBy,
    String? reason,
  }) async {
    await _apiClient.postVoid(
      '${ApiEndpoints.sessionById(sessionId)}/cancel',
      data: {
        'cancelledBy': cancelledBy,
        if (reason != null) 'reason': reason,
      },
    );
  }

  Future<void> logMemberFeedback(
      String sessionId, Map<String, dynamic> body) async {
    await _apiClient.postVoid(
      ApiEndpoints.sessionMemberLog(sessionId),
      data: body,
    );
  }

  /// BE GET /scheduling/available-slots returns [{startTime, endTime, isAvailable}].
  /// Each startTime is "HH:mm:ss" (TimeOnly). Returns available slots as "HH:mm" strings.
  Future<List<String>> getAvailabilitySlots(
      String trainerId, String gymId, DateTime date) async {
    return _apiClient.get<List<String>>(
      ApiEndpoints.availableSlots,
      queryParameters: {
        'trainerId': trainerId,
        'gymId': gymId,
        'date': date.toIso8601String().split('T')[0],
      },
      parser: (data) {
        final list = data is List ? data : const [];
        return list
            .cast<Map<String, dynamic>>()
            .where((e) => e['isAvailable'] == true)
            .map((e) {
              final t = (e['startTime'] as String? ?? '');
              final parts = t.split(':');
              return parts.length >= 2 ? '${parts[0]}:${parts[1]}' : t;
            })
            .where((s) => s.isNotEmpty)
            .toList();
      },
    );
  }

  Future<WeeklyAvailabilityModel> getWeeklyAvailability({
    required String trainerId,
    required String gymId,
    required DateTime startDate,
    required DateTime endDate,
    String? memberId,
  }) async {
    String fmt(DateTime d) =>
        '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
    return _apiClient.get<WeeklyAvailabilityModel>(
      ApiEndpoints.weeklyAvailability,
      queryParameters: {
        'trainerId': trainerId,
        'gymId': gymId,
        'startDate': fmt(startDate),
        'endDate': fmt(endDate),
        if (memberId != null) 'memberId': memberId,
      },
      parser: (data) =>
          WeeklyAvailabilityModel.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<GymClassModel>> getClasses({
    required String gymId,
    DateTime? date,
  }) async {
    return _apiClient.get<List<GymClassModel>>(
      ApiEndpoints.gymClasses(gymId),
      queryParameters: {
        if (date != null) 'date': date.toIso8601String().split('T')[0],
      },
      parser: (data) => _unwrapItems(data)
          .map<GymClassModel>((e) => _gymClassFromDto(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<void> bookClass({
    required String gymId,
    required String classId,
    required String memberId,
  }) async {
    await _apiClient.postVoid(
      ApiEndpoints.bookGymClass(gymId, classId),
      data: {'memberId': memberId},
    );
  }

  Future<void> cancelClassBooking({
    required String gymId,
    required String classId,
    required String bookingId,
  }) async {
    await _apiClient.delete(
      ApiEndpoints.cancelGymClassBooking(gymId, classId, bookingId),
    );
  }
}

/// The backend `GymClassDto` shape matches [GymClassModel] directly —
/// `trainerName`, `trainerId`, and the nested `schedule` all round-trip through
/// the generated `fromJson`. We still need a hand-rolled parser only when the
/// DTO carries fields the model doesn't recognise; today it does not, so we
/// just defer to the generated constructor.
GymClassModel _gymClassFromDto(Map<String, dynamic> dto) =>
    GymClassModel.fromJson(dto);
