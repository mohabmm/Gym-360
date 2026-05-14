import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../membership/presentation/providers/membership_providers.dart';
import '../../data/datasources/scheduling_remote_datasource.dart';
import '../../data/repositories/scheduling_repository.dart';

final schedulingRemoteDataSourceProvider =
    Provider<SchedulingRemoteDataSource>((ref) {
  return SchedulingRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final schedulingRepositoryProvider = Provider<SchedulingRepository>((ref) {
  return SchedulingRepository(
    remoteDataSource: ref.watch(schedulingRemoteDataSourceProvider),
  );
});

final upcomingSessionsListProvider =
    FutureProvider<List<SessionModel>>((ref) async {
  final authState = ref.watch(authProvider).valueOrNull;
  final userId = authState?.user?.id;
  final gymId = authState?.currentGymId;
  if (userId == null || gymId == null) return const [];
  final repo = ref.watch(schedulingRepositoryProvider);
  return repo.getUpcomingSessions(userId: userId, gymId: gymId);
});

final sessionDetailProvider =
    FutureProvider.family<SessionDetailModel, String>((ref, id) async {
  final repo = ref.watch(schedulingRepositoryProvider);
  return repo.getSessionById(id);
});

final mySessionsAllProvider =
    FutureProvider<List<SessionModel>>((ref) async {
  final auth = ref.watch(authProvider).valueOrNull;
  final memberId = auth?.user?.id;
  final gymId = auth?.currentGymId;
  if (memberId == null || gymId == null) return const [];
  final repo = ref.watch(schedulingRepositoryProvider);
  return repo.getAllSessions(gymId: gymId, memberId: memberId);
});

/// Bookable sessions across all Active packages.
///
/// `BE.RemainingSessions` debits Completed + LateCancelled. Pending /
/// Confirmed / InProgress bookings are not yet charged on the server, so we
/// subtract them here to honor the package capacity (Pending = "waiting
/// approval", Confirmed = "reserved"). Returns `null` while either upstream
/// provider is loading so callers can render a loading state instead of
/// incorrectly enabling booking.
final bookableSessionsProvider = Provider<int?>((ref) {
  final packagesAsync = ref.watch(activePackagesProvider);
  final allAsync = ref.watch(mySessionsAllProvider);
  if (packagesAsync.isLoading || allAsync.isLoading) return null;

  final packages = packagesAsync.valueOrNull ?? const <SessionPackageModel>[];
  final totalRemaining = packages
      .where((p) => p.status.toLowerCase() == 'active')
      .fold<int>(0, (sum, p) => sum + p.remainingSessions);

  final all = allAsync.valueOrNull ?? const <SessionModel>[];
  const pendingStatuses = {'pending', 'confirmed', 'inprogress'};
  final pendingBookings =
      all.where((s) => pendingStatuses.contains(s.status.toLowerCase())).length;

  final bookable = totalRemaining - pendingBookings;
  return bookable < 0 ? 0 : bookable;
});

/// Per-gym cancellation/no-show policy. Falls back to a sane default if the
/// endpoint fails so the cancel flow never breaks just because the policy
/// fetch is flaky.
final gymSessionPolicyProvider =
    FutureProvider<GymSessionPolicyModel>((ref) async {
  final gymId = ref.watch(authProvider).valueOrNull?.currentGymId;
  if (gymId == null) {
    return const GymSessionPolicyModel(
      gymId: '',
      cancellationWindowHours: 24,
      noshowThreshold: 3,
      noshowWindowDays: 30,
      noshowAction: 'FlagOnly',
    );
  }
  final api = ref.watch(apiClientProvider);
  try {
    return await api.get<GymSessionPolicyModel>(
      ApiEndpoints.gymSessionPolicy(gymId),
      parser: (data) =>
          GymSessionPolicyModel.fromJson(data as Map<String, dynamic>),
    );
  } catch (_) {
    return GymSessionPolicyModel(
      gymId: gymId,
      cancellationWindowHours: 24,
      noshowThreshold: 3,
      noshowWindowDays: 30,
      noshowAction: 'FlagOnly',
    );
  }
});

final selectedClassDateProvider =
    StateProvider<DateTime>((ref) => DateTime.now());

final gymClassesProvider =
    FutureProvider<List<GymClassModel>>((ref) async {
  final date = ref.watch(selectedClassDateProvider);
  final gymId = ref.watch(authProvider).valueOrNull?.currentGymId;
  if (gymId == null) return const <GymClassModel>[];
  final repo = ref.watch(schedulingRepositoryProvider);
  return repo.getClasses(gymId: gymId, date: date);
});

typedef _SlotParams = ({String trainerId, String gymId, DateTime date});

final availabilitySlotsProvider =
    FutureProvider.family<List<String>, _SlotParams>((ref, params) async {
  final repo = ref.watch(schedulingRepositoryProvider);
  return repo.getAvailabilitySlots(params.trainerId, params.gymId, params.date);
});

typedef WeeklyAvailabilityParams = ({
  String trainerId,
  String gymId,
  DateTime weekStart,
});

final weeklyAvailabilityProvider = FutureProvider.family<
    WeeklyAvailabilityModel, WeeklyAvailabilityParams>((ref, params) async {
  final repo = ref.watch(schedulingRepositoryProvider);
  final memberId = ref.watch(authProvider).valueOrNull?.user?.id;
  return repo.getWeeklyAvailability(
    trainerId: params.trainerId,
    gymId: params.gymId,
    startDate: params.weekStart,
    endDate: params.weekStart.add(const Duration(days: 6)),
    memberId: memberId,
  );
});
