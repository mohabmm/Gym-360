import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// Client summary for roster listing.
class ClientSummary {
  final UserModel user;
  final SessionModel? lastSession;
  final SessionModel? nextSession;
  final List<String> goalTags;
  final RemoteWorkoutStatusModel? remoteWorkoutStatus;

  const ClientSummary({
    required this.user,
    this.lastSession,
    this.nextSession,
    this.goalTags = const [],
    this.remoteWorkoutStatus,
  });
}

/// Full client detail with 5 tabs of data.
class ClientDetailData {
  final UserModel user;
  final List<SessionModel> sessions;
  final List<WorkoutAssignmentModel> workoutAssignments;
  final List<BodyMetricEntryModel> bodyMetrics;
  final List<ProgressPhotoModel> progressPhotos;
  final StreakModel? streak;
  final SessionPackageModel? activePackage;
  final RemoteWorkoutStatusModel? remoteWorkoutStatus;
  final List<ClientNote> notes;

  const ClientDetailData({
    required this.user,
    this.sessions = const [],
    this.workoutAssignments = const [],
    this.bodyMetrics = const [],
    this.progressPhotos = const [],
    this.streak,
    this.activePackage,
    this.remoteWorkoutStatus,
    this.notes = const [],
  });
}

class ClientNote {
  final String id;
  final String content;
  final DateTime createdAt;

  const ClientNote({
    required this.id,
    required this.content,
    required this.createdAt,
  });

  factory ClientNote.fromJson(Map<String, dynamic> json) => ClientNote(
        id: json['id'] as String,
        content: json['content'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Map of memberId -> display name, derived from the trainer's roster.
/// Used by Home/Schedule session cards to label sessions with the actual
/// client name instead of a generic "Client" placeholder.
final clientNameByIdProvider = Provider.autoDispose<Map<String, String>>((ref) {
  final rosterAsync = ref.watch(clientRosterProvider);
  return rosterAsync.maybeWhen(
    data: (clients) => {
      for (final c in clients)
        c.user.id: '${c.user.firstName} ${c.user.lastName}'.trim(),
    },
    orElse: () => const <String, String>{},
  );
});

/// Roster of clients booked against the signed-in trainer.
/// BE filters /gyms/{id}/members?trainerId=<me> to members on Active session
/// packages owned by this trainer. Without the trainerId param the endpoint
/// returns the entire gym roster (used by owner/staff), which is wrong for
/// the trainer client list — passing trainerId is required here.
final clientRosterProvider =
    FutureProvider.autoDispose<List<ClientSummary>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  final trainerId = ref.watch(authProvider).user?.id;
  if (gymId == null || trainerId == null) return <ClientSummary>[];

  final members = await apiClient.get<List<UserModel>>(
    ApiEndpoints.gymMembers(gymId),
    queryParameters: {'page': 1, 'pageSize': 100, 'trainerId': trainerId},
    parser: (data) {
      // BE wraps list responses as PagedResponse { items: [...] }.
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <UserModel>[];
      }
      // BE MemberDto carries `memberId` (the user id) plus enriched
      // identity fields. UserModel expects `id`, so alias before parsing.
      return list.map((e) {
        final json = Map<String, dynamic>.from(e as Map);
        json['id'] ??= json['memberId'];
        return UserModel.fromJson(json);
      }).toList();
    },
  );

  return members.map((user) {
    return ClientSummary(
      user: user,
      goalTags: user.goals ?? [],
    );
  }).toList();
});

/// Detailed client data by ID.
final clientDetailProvider =
    FutureProvider.autoDispose.family<ClientDetailData, String>(
  (ref, clientId) async {
    final apiClient = ref.watch(apiClientProvider);
    final gymId = ref.watch(activeGymIdProvider);
    if (gymId == null) {
      throw ApiException(
        message: 'No active gym selected.',
        errorCode: 'NO_ACTIVE_GYM',
      );
    }

    // Fetch user (gym-scoped). BE MemberDto carries `memberId` (user id) plus
    // enriched identity fields — alias to `id` before parsing UserModel.
    final user = await apiClient.get<UserModel>(
      ApiEndpoints.gymMemberById(gymId, clientId),
      parser: (data) {
        final json = Map<String, dynamic>.from(data as Map);
        json['id'] ??= json['memberId'];
        return UserModel.fromJson(json);
      },
    );

    // Fetch sessions (BE: gymId required, filter by memberId)
    List<SessionModel> sessions = <SessionModel>[];
    try {
      sessions = await apiClient.get<List<SessionModel>>(
        ApiEndpoints.sessions,
        queryParameters: {
          'gymId': gymId,
          'memberId': clientId,
          'page': 1,
          'pageSize': 50,
        },
        parser: (data) {
          final List<dynamic> list;
          if (data is List) {
            list = data;
          } else if (data is Map<String, dynamic> && data['items'] is List) {
            list = data['items'] as List<dynamic>;
          } else {
            return <SessionModel>[];
          }
          return list
              .map((e) => SessionModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
      );
    } catch (_) {}

    // Fetch workout assignments for this client.
    List<WorkoutAssignmentModel> workouts = <WorkoutAssignmentModel>[];
    try {
      workouts = await apiClient.get<List<WorkoutAssignmentModel>>(
        ApiEndpoints.workoutAssignments,
        queryParameters: {'clientId': clientId, 'page': 1, 'pageSize': 50},
        parser: (data) {
          final List<dynamic> list;
          if (data is List) {
            list = data;
          } else if (data is Map<String, dynamic> && data['items'] is List) {
            list = data['items'] as List<dynamic>;
          } else {
            return <WorkoutAssignmentModel>[];
          }
          return list
              .map((e) =>
                  WorkoutAssignmentModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
      );
    } catch (_) {}

    // Fetch progress data. BE `/progress/clients/{clientId}` requires
    // trainerId + gymId query params and returns a ClientProgressResponse
    // object — recent body metrics live under `recentMetrics`, with the
    // single most-recent metric under `latestMetrics`.
    List<BodyMetricEntryModel> bodyMetrics = <BodyMetricEntryModel>[];
    try {
      final trainerId = ref.read(authProvider).user?.id;
      if (trainerId != null) {
        bodyMetrics = await apiClient.get<List<BodyMetricEntryModel>>(
          ApiEndpoints.clientProgress(clientId),
          queryParameters: {'trainerId': trainerId, 'gymId': gymId},
          parser: (data) {
            if (data is Map<String, dynamic>) {
              final recent = data['recentMetrics'];
              if (recent is List) {
                return recent
                    .map((e) => BodyMetricEntryModel.fromJson(
                        e as Map<String, dynamic>))
                    .toList();
              }
              final latest = data['latestMetrics'];
              if (latest is Map<String, dynamic>) {
                return [BodyMetricEntryModel.fromJson(latest)];
              }
            }
            return <BodyMetricEntryModel>[];
          },
        );
      }
    } catch (_) {}

    // Fetch remote workout status
    RemoteWorkoutStatusModel? remoteStatus;
    try {
      remoteStatus = await apiClient.get<RemoteWorkoutStatusModel>(
        ApiEndpoints.remoteWorkoutStatus,
        queryParameters: {'clientId': clientId, 'gymId': gymId},
        parser: (data) =>
            RemoteWorkoutStatusModel.fromJson(data as Map<String, dynamic>),
      );
    } catch (_) {}

    // Fetch active package via generic packages endpoint filtered by memberId.
    SessionPackageModel? activePackage;
    try {
      final packages = await apiClient.get<List<SessionPackageModel>>(
        ApiEndpoints.sessionPackages,
        queryParameters: {
          'gymId': gymId,
          'memberId': clientId,
          'status': 'active',
        },
        parser: (data) {
          if (data is List) {
            return data
                .map((e) =>
                    SessionPackageModel.fromJson(e as Map<String, dynamic>))
                .toList();
          }
          return <SessionPackageModel>[];
        },
      );
      if (packages.isNotEmpty) {
        activePackage = packages.firstWhere(
          // BE PackageStatus serializes PascalCase ("Active"); compare
          // case-insensitively so the active package is actually selected
          // instead of silently falling back to packages.first.
          (p) => p.status.toLowerCase() == 'active',
          orElse: () => packages.first,
        );
      }
    } catch (_) {}

    // Fetch member notes
    List<ClientNote> notes = <ClientNote>[];
    try {
      notes = await apiClient.get<List<ClientNote>>(
        ApiEndpoints.gymMemberNotes(gymId, clientId),
        parser: (data) {
          if (data is List) {
            return data
                .map((e) => ClientNote.fromJson(e as Map<String, dynamic>))
                .toList();
          }
          return <ClientNote>[];
        },
      );
    } catch (_) {}

    return ClientDetailData(
      user: user,
      sessions: sessions,
      workoutAssignments: workouts,
      bodyMetrics: bodyMetrics,
      remoteWorkoutStatus: remoteStatus,
      activePackage: activePackage,
      notes: notes,
    );
  },
);

/// Remote workout toggle provider per client.
final remoteWorkoutProvider = FutureProvider.autoDispose
    .family<RemoteWorkoutStatusModel?, String>((ref, clientId) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(activeGymIdProvider);
  if (gymId == null) return null;
  try {
    return await apiClient.get<RemoteWorkoutStatusModel>(
      ApiEndpoints.remoteWorkoutStatus,
      queryParameters: {'clientId': clientId, 'gymId': gymId},
      parser: (data) =>
          RemoteWorkoutStatusModel.fromJson(data as Map<String, dynamic>),
    );
  } catch (_) {
    return null;
  }
});
