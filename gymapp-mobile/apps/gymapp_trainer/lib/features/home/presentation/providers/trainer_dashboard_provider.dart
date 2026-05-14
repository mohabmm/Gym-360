import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../scheduling/presentation/providers/scheduling_providers.dart';

/// Dashboard data model for the trainer home screen.
class TrainerDashboardData {
  final List<SessionModel> todaySessions;
  final int pendingBookings;
  final int unreadMessages;
  final int totalClients;
  final int sessionsCompletedThisMonth;
  final int newClientsThisMonth;
  final double averageRating;
  final int ratingCount;

  const TrainerDashboardData({
    this.todaySessions = const [],
    this.pendingBookings = 0,
    this.unreadMessages = 0,
    this.totalClients = 0,
    this.sessionsCompletedThisMonth = 0,
    this.newClientsThisMonth = 0,
    this.averageRating = 0.0,
    this.ratingCount = 0,
  });
}

final trainerDashboardProvider =
    FutureProvider.autoDispose<TrainerDashboardData>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final authState = ref.watch(authProvider);
  final trainerId = authState.user?.id;
  if (gymId == null || trainerId == null) {
    return const TrainerDashboardData();
  }

  List<dynamic> _asList(dynamic data) {
    if (data is List) return data;
    if (data is Map<String, dynamic> && data['items'] is List) {
      return data['items'] as List<dynamic>;
    }
    return const [];
  }

  // Upcoming sessions and pending requests are sourced from the same
  // providers the Schedule and Booking Requests screens watch — sharing
  // them prevents Home and Schedule from drifting after a status change
  // or invalidation on either side.
  //
  // Today filter:
  //  - scheduledAt on today's local date
  //  - status is confirmed/scheduled/in-progress (NOT pending — those belong
  //    on the Booking Requests screen until the trainer accepts them).
  List<SessionModel> todaySessions = const [];
  try {
    final now = DateTime.now();
    final todayDate =
        '${now.year}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}';
    final raw = await apiClient.get<List<dynamic>>(
      ApiEndpoints.sessions,
      queryParameters: {
        'gymId': gymId,
        'trainerId': trainerId,
        'from': todayDate,
        'to': todayDate,
        'page': 1,
        'pageSize': 50,
      },
      parser: (data) => _asList(data),
    );
    // BE SessionStatus enum values returned as PascalCase strings:
    //   Pending, Confirmed, InProgress, Completed,
    //   CancelledByTrainer, CancelledByMember, LateCancelled, NoShow
    // Today list shows only sessions the trainer can act on. Filter out
    // pending (lives in Booking Requests) and ALL cancel/no-show variants.
    const hiddenStatuses = {
      'pending',
      'cancelled',
      'cancelledbytrainer',
      'cancelledbymember',
      'latecancelled',
      'noshow',
    };
    todaySessions = raw
        .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
        .where((s) => !hiddenStatuses.contains(s.status.toLowerCase()))
        .toList();
  } catch (_) {}

  // Pending booking requests — shared with the Booking Requests screen.
  int pendingCount = 0;
  try {
    final pending = await ref.watch(pendingBookingRequestsProvider.future);
    pendingCount = pending.length;
  } catch (_) {}

  // Unread conversations. BE requires userId, gymId, page, pageSize.
  int unreadCount = 0;
  try {
    final conversations = await apiClient.get<List<dynamic>>(
      ApiEndpoints.conversations,
      queryParameters: {
        'userId': trainerId,
        'gymId': gymId,
        'page': 1,
        'pageSize': 50,
      },
      parser: (data) => _asList(data),
    );
    unreadCount = conversations.fold<int>(
      0,
      (sum, c) =>
          sum + ((c as Map<String, dynamic>)['unreadCount'] as int? ?? 0),
    );
  } catch (_) {}

  // Client count — trainer-scoped, NOT gym-scoped. /gyms/{id}/members without
  // trainerId returns the whole gym roster, which made Home report "1 client"
  // immediately after a trainee was added (regardless of whether that trainee
  // had any package booked against this trainer).
  int totalClients = 0;
  try {
    final members = await apiClient.get<List<dynamic>>(
      ApiEndpoints.gymMembers(gymId),
      queryParameters: {'page': 1, 'pageSize': 1, 'trainerId': trainerId},
      parser: (data) {
        if (data is Map<String, dynamic>) {
          final total = data['totalCount'] ?? data['total'];
          if (total is num) return [for (var i = 0; i < total.toInt(); i++) 0];
          if (data['items'] is List) return data['items'] as List<dynamic>;
        }
        if (data is List) return data;
        return const [];
      },
    );
    totalClients = members.length;
  } catch (_) {}

  // Trainer rating summary — use public profile endpoint (requires gymId).
  // The BE returns `displayRating` (0 when no ratings exist), plus a
  // testimonials list. We use the count of testimonials as a fallback
  // signal since the DTO doesn't include a raw rating count.
  double avgRating = 0.0;
  int ratingCount = 0;
  try {
    final profile = await apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.trainerPublicProfile(trainerId),
      queryParameters: {'gymId': gymId},
      parser: (data) => data is Map<String, dynamic> ? data : {},
    );
    avgRating = (profile['displayRating'] as num?)?.toDouble() ??
        (profile['averageRating'] as num?)?.toDouble() ??
        0.0;
    final testimonials = profile['testimonials'];
    if (testimonials is List) {
      ratingCount = testimonials.length;
    }
  } catch (_) {}

  int completedThisMonth = 0;
  try {
    final stats = await apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.sessionStats,
      queryParameters: {'trainerId': trainerId, 'gymId': gymId},
      parser: (data) => data is Map<String, dynamic> ? data : {},
    );
    completedThisMonth =
        (stats['completedCount'] as num?)?.toInt() ?? 0;
  } catch (_) {}

  return TrainerDashboardData(
    todaySessions: todaySessions,
    pendingBookings: pendingCount,
    unreadMessages: unreadCount,
    totalClients: totalClients,
    sessionsCompletedThisMonth: completedThisMonth,
    newClientsThisMonth: 0,
    averageRating: avgRating,
    ratingCount: ratingCount,
  );
});
