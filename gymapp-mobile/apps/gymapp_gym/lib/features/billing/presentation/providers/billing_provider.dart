import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

final paymentsProvider =
    FutureProvider.autoDispose<List<PaymentModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<PaymentModel>>(
    ApiEndpoints.payments,
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <PaymentModel>[];
      }
      return list
          .map((e) => PaymentModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});

final packageTemplatesProvider =
    FutureProvider.autoDispose<List<SessionPackageTemplateModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<SessionPackageTemplateModel>>(
    ApiEndpoints.packageTemplates,
    parser: (data) => (data as List)
        .map((e) => SessionPackageTemplateModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});

final activePackagesProvider =
    FutureProvider.autoDispose<List<SessionPackageModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<SessionPackageModel>>(
    ApiEndpoints.myClientPackages,
    parser: (data) => (data as List)
        .map((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
});
