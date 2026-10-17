import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

final announcementsProvider =
    FutureProvider.autoDispose<List<AnnouncementModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  // migrated to currentGymIdProvider
  final gymId = ref.watch(currentGymIdProvider);
  if (gymId.isEmpty) return <AnnouncementModel>[];
  return apiClient.get<List<AnnouncementModel>>(
    ApiEndpoints.gymAnnouncements(GymId(gymId)),
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <AnnouncementModel>[];
      }
      return list
          .map((e) => AnnouncementModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});
