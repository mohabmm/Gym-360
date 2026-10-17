/// Formats a date as `yyyy-MM-dd` (the BE `DateOnly` wire format).
String _isoDate(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

/// Filters for `GET /scheduling/sessions` (paginated).
class SessionListQuery {
  const SessionListQuery({
    required this.gymId,
    this.memberId,
    this.status,
    this.page = 1,
    this.pageSize = 20,
  });

  final String gymId;
  final String? memberId;
  final String? status;
  final int page;
  final int pageSize;

  Map<String, dynamic> toQueryParameters() => {
        'gymId': gymId,
        if (memberId != null) 'memberId': memberId,
        if (status != null) 'status': status,
        'page': page,
        'pageSize': pageSize,
      };
}

/// Cancels a 1:1 session on behalf of [cancelledBy].
class SessionCancellation {
  const SessionCancellation({
    required this.sessionId,
    required this.cancelledBy,
    this.reason,
  });

  final String sessionId;
  final String cancelledBy;
  final String? reason;

  Map<String, dynamic> toJson() => {
        'cancelledBy': cancelledBy,
        if (reason != null) 'reason': reason,
      };
}

/// A trainer's open slots at a gym on one day.
class AvailabilitySlotsQuery {
  const AvailabilitySlotsQuery({
    required this.trainerId,
    required this.gymId,
    required this.date,
  });

  final String trainerId;
  final String gymId;
  final DateTime date;

  Map<String, dynamic> toQueryParameters() => {
        'trainerId': trainerId,
        'gymId': gymId,
        'date': date.toIso8601String().split('T')[0],
      };
}

/// A trainer's weekly availability window, optionally scoped to a member.
class WeeklyAvailabilityQuery {
  const WeeklyAvailabilityQuery({
    required this.trainerId,
    required this.gymId,
    required this.startDate,
    required this.endDate,
    this.memberId,
  });

  final String trainerId;
  final String gymId;
  final DateTime startDate;
  final DateTime endDate;
  final String? memberId;

  Map<String, dynamic> toQueryParameters() => {
        'trainerId': trainerId,
        'gymId': gymId,
        'startDate': _isoDate(startDate),
        'endDate': _isoDate(endDate),
        if (memberId != null) 'memberId': memberId,
      };
}

/// Books [memberId] into a gym class.
class ClassBookingRequest {
  const ClassBookingRequest({
    required this.gymId,
    required this.classId,
    required this.memberId,
  });

  final String gymId;
  final String classId;
  final String memberId;
}

/// Cancels an existing gym-class booking.
class ClassBookingCancellation {
  const ClassBookingCancellation({
    required this.gymId,
    required this.classId,
    required this.bookingId,
  });

  final String gymId;
  final String classId;
  final String bookingId;
}
