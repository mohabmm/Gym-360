import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import 'inbody_scan_request.dart';

/// progress.inbody: saves the values the member confirmed after scanning their InBody sheet.
/// The member comes from the JWT and the gym from the X-Gym-Id header.
class InBodyScanApi {
  final ApiClient _apiClient;

  InBodyScanApi({required ApiClient apiClient}) : _apiClient = apiClient;

  /// `POST /progress/body-metrics/inbody`: 201 for a new entry, 200 when it updated the entry
  /// already logged that day.
  Future<void> save(InBodyScanRequest request) => _apiClient.post<Object?>(
        ApiEndpoints.inBodyScan,
        data: request.toJson(),
        parser: (data) => data,
      );
}

final inBodyScanApiProvider = Provider<InBodyScanApi>(
  (ref) => InBodyScanApi(apiClient: ref.watch(apiClientProvider)),
);
