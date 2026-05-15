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

  Future<List<SessionModel>> getAllSessions({
    required String gymId,
    String? memberId,
    String? status,
  }) =>
      _remoteDataSource.getAllSessions(
        gymId: gymId,
        memberId: memberId,
        status: status,
      );

  Future<SessionDetailModel> getSessionById(String id) =>
      _remoteDataSource.getSessionById(id);

  /// Returns the session ID (Guid string).
  Future<String> bookSession(BookSessionRequest request) =>
      _remoteDataSource.bookSession(request);

  Future<void> cancelSession(
    String sessionId, {
    required String cancelledBy,
    String? reason,
  }) =>
      _remoteDataSource.cancelSession(
        sessionId,
        cancelledBy: cancelledBy,
        reason: reason,
      );

  Future<void> logMemberFeedback(
          String sessionId, Map<String, dynamic> body) =>
      _remoteDataSource.logMemberFeedback(sessionId, body);

  Future<List<String>> getAvailabilitySlots(
          String trainerId, String gymId, DateTime date) =>
      _remoteDataSource.getAvailabilitySlots(trainerId, gymId, date);

  Future<WeeklyAvailabilityModel> getWeeklyAvailability({
    required String trainerId,
    required String gymId,
    required DateTime startDate,
    required DateTime endDate,
    String? memberId,
  }) =>
      _remoteDataSource.getWeeklyAvailability(
        trainerId: trainerId,
        gymId: gymId,
        startDate: startDate,
        endDate: endDate,
        memberId: memberId,
      );

  Future<List<GymClassModel>> getClasses({
    required String gymId,
    DateTime? date,
  }) =>
      _remoteDataSource.getClasses(gymId: gymId, date: date);

  Future<void> bookClass({
    required String gymId,
    required String classId,
    required String memberId,
  }) =>
      _remoteDataSource.bookClass(
        gymId: gymId,
        classId: classId,
        memberId: memberId,
      );

  Future<void> cancelClassBooking({
    required String gymId,
    required String classId,
    required String bookingId,
  }) =>
      _remoteDataSource.cancelClassBooking(
        gymId: gymId,
        classId: classId,
        bookingId: bookingId,
      );
}
