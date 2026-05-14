import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

final checkInLogProvider =
    FutureProvider.autoDispose<List<CheckInModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  // migrated to currentGymIdProvider
  final gymId = ref.watch(currentGymIdProvider);

  if (gymId.isEmpty) return <CheckInModel>[];
  return apiClient.get<List<CheckInModel>>(
    ApiEndpoints.checkInHistory(gymId),
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <CheckInModel>[];
      }
      return list
          .map((e) => CheckInModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});
