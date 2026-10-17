import 'package:gymapp_core/gymapp_core.dart';

import '../datasources/scheduling_remote_datasource.dart';

class SchedulingRepository {
  final SchedulingRemoteDataSource _remoteDataSource;

  SchedulingRepository({required SchedulingRemoteDataSource remoteDataSource})
      : _remoteDataSource = remoteDataSource;

  Future<List<SessionModel>> getUpcomingSessions({
    required String userId,
    required String gymId,
  }) =>
      _remoteDataSource.getUpcomingSessions(userId: userId, gymId: gymId);

  Future<List<SessionModel>> getAllSessions(SessionListQuery query) =>
      _remoteDataSource.getAllSessions(query);

  Future<SessionDetailModel> getSessionById(String id) =>
      _remoteDataSource.getSessionById(id);

  /// Returns the session ID (Guid string).
  Future<String> bookSession(BookSessionRequest request) =>
      _remoteDataSource.bookSession(request);

  Future<void> cancelSession(SessionCancellation cancellation) =>
      _remoteDataSource.cancelSession(cancellation);

  Future<void> logMemberFeedback(
          String sessionId, Map<String, dynamic> body) =>
      _remoteDataSource.logMemberFeedback(sessionId, body);

  Future<List<String>> getAvailabilitySlots(AvailabilitySlotsQuery query) =>
      _remoteDataSource.getAvailabilitySlots(query);

  Future<WeeklyAvailabilityModel> getWeeklyAvailability(
          WeeklyAvailabilityQuery query) =>
      _remoteDataSource.getWeeklyAvailability(query);

  Future<List<GymClassModel>> getClasses({
    required String gymId,
    DateTime? date,
  }) =>
      _remoteDataSource.getClasses(gymId: gymId, date: date);

  Future<void> bookClass(ClassBookingRequest booking) =>
      _remoteDataSource.bookClass(booking);

  Future<void> cancelClassBooking(ClassBookingCancellation cancellation) =>
      _remoteDataSource.cancelClassBooking(cancellation);
}
