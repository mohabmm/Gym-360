import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// Provider for the login action.
final loginProvider =
    FutureProvider.autoDispose.family<AuthResponse, LoginRequest>(
  (ref, request) async {
    final apiClient = ref.read(apiClientProvider);
    return apiClient.post<AuthResponse>(
      ApiEndpoints.login,
      data: request.toJson(),
      parser: (data) => AuthResponse.fromJson(data as Map<String, dynamic>),
    );
  },
);

/// Provider for fetching staff info after login to get role.
final staffInfoProvider =
    FutureProvider.autoDispose.family<StaffModel?, String>(
  (ref, gymId) async {
    final apiClient = ref.read(apiClientProvider);
    try {
      final staffList = await apiClient.get<List<StaffModel>>(
        ApiEndpoints.gymStaff(GymId(gymId)),
        parser: (data) => (data as List)
            .map((e) => StaffModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
      final userId = ref.read(authProvider).valueOrNull?.user?.id;
      if (userId == null) return null;
      return staffList.where((s) => s.userId == userId).firstOrNull;
    } catch (_) {
      return null;
    }
  },
);
