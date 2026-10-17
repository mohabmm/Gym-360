import 'package:gymapp_core/gymapp_core.dart';

import '../models/scheduling_requests.dart';

export '../models/scheduling_requests.dart';

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
  Future<List<SessionModel>> getAllSessions(SessionListQuery query) async {
    return _apiClient.get<List<SessionModel>>(
      ApiEndpoints.sessions,
      queryParameters: query.toQueryParameters(),
      parser: (data) => _unwrapItems(data)
          .map<SessionModel>((e) => SessionModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<SessionDetailModel> getSessionById(String id) async {
    return _apiClient.get<SessionDetailModel>(
      ApiEndpoints.sessionById(SessionId(id)),
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

  Future<void> cancelSession(SessionCancellation cancellation) async {
    await _apiClient.postVoid(
      '${ApiEndpoints.sessionById(SessionId(cancellation.sessionId))}/cancel',
      data: cancellation.toJson(),
    );
  }

  Future<void> logMemberFeedback(
      String sessionId, Map<String, dynamic> body) async {
    await _apiClient.postVoid(
      ApiEndpoints.sessionMemberLog(SessionId(sessionId)),
      data: body,
    );
  }

  /// BE GET /scheduling/available-slots returns [{startTime, endTime, isAvailable}].
  /// Each startTime is "HH:mm:ss" (TimeOnly). Returns available slots as "HH:mm" strings.
  Future<List<String>> getAvailabilitySlots(
      AvailabilitySlotsQuery query) async {
    return _apiClient.get<List<String>>(
      ApiEndpoints.availableSlots,
      queryParameters: query.toQueryParameters(),
      parser: _availableStartTimes,
    );
  }

  Future<WeeklyAvailabilityModel> getWeeklyAvailability(
      WeeklyAvailabilityQuery query) async {
    return _apiClient.get<WeeklyAvailabilityModel>(
      ApiEndpoints.weeklyAvailability,
      queryParameters: query.toQueryParameters(),
      parser: (data) =>
          WeeklyAvailabilityModel.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<GymClassModel>> getClasses({
    required String gymId,
    DateTime? date,
  }) async {
    return _apiClient.get<List<GymClassModel>>(
      ApiEndpoints.gymClasses(GymId(gymId)),
      queryParameters: {
        if (date != null) 'date': date.toIso8601String().split('T')[0],
      },
      parser: (data) => _unwrapItems(data)
          .map<GymClassModel>((e) => _gymClassFromDto(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<void> bookClass(ClassBookingRequest booking) async {
    await _apiClient.postVoid(
      ApiEndpoints.bookGymClass(GymId(booking.gymId), ClassId(booking.classId)),
      data: {'memberId': booking.memberId},
    );
  }

  Future<void> cancelClassBooking(ClassBookingCancellation cancellation) async {
    await _apiClient.delete(
      ApiEndpoints.cancelGymClassBooking(
        GymId(cancellation.gymId),
        ClassId(cancellation.classId),
        BookingId(cancellation.bookingId),),
    );
  }
}

/// Parses `[{startTime, endTime, isAvailable}]` into the start times of the
/// available slots, as "HH:mm" strings.
List<String> _availableStartTimes(dynamic data) {
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
}

/// The backend `GymClassDto` shape matches [GymClassModel] directly —
/// `trainerName`, `trainerId`, and the nested `schedule` all round-trip through
/// the generated `fromJson`. We still need a hand-rolled parser only when the
/// DTO carries fields the model doesn't recognise; today it does not, so we
/// just defer to the generated constructor.
GymClassModel _gymClassFromDto(Map<String, dynamic> dto) =>
    GymClassModel.fromJson(dto);
