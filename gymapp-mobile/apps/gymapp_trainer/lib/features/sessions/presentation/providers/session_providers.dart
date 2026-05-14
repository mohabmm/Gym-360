import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

/// A single session detail by ID.
final sessionDetailProvider =
    FutureProvider.autoDispose.family<SessionModel, String>((ref, id) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<SessionModel>(
    ApiEndpoints.sessionById(id),
    parser: (data) => SessionModel.fromJson(data as Map<String, dynamic>),
  );
});
