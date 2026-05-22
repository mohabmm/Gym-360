import 'package:gymapp_core/gymapp_core.dart';

class MembershipRemoteDataSource {
  final ApiClient _apiClient;

  MembershipRemoteDataSource({required ApiClient apiClient})
      : _apiClient = apiClient;

  /// Returns the trainee's session packages at the current gym.
  ///
  /// Backed by `GET /api/v1/billing/me/packages`. `X-Gym-Id` is supplied
  /// automatically by [GymIdInterceptor]. Defaults to the server's
  /// `status=Active` filter; pass [status] = `"All"`, `"Expired"`, or
  /// `"Exhausted"` to widen the query per the PRD.
  Future<List<SessionPackageModel>> getActivePackages({String? status}) async {
    return _apiClient.get<List<SessionPackageModel>>(
      ApiEndpoints.myPackages,
      queryParameters: {
        if (status != null) 'status': status,
      },
      parser: (data) {
        // BE returns ApiResponse<PagedResponse<MySessionPackageDto>>;
        // ApiClient already unwraps `data`, leaving a PagedResponse object
        // ({ items, page, pageSize, totalCount }) here. Older fixtures shipped
        // a raw list, so accept both shapes.
        final List<dynamic> items;
        if (data is List) {
          items = data;
        } else if (data is Map && data['items'] is List) {
          items = data['items'] as List<dynamic>;
        } else {
          return const [];
        }
        return items
            .map<SessionPackageModel>((e) => SessionPackageModel.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }

  /// Returns the trainee's payment history at the current gym, paginated.
  ///
  /// Backed by `GET /api/v1/billing/me/payments?page=&pageSize=`.
  /// Voided payments are included by server contract (Founder Q9) and surface
  /// with `status == 'Voided'` and positive `amount`.
  Future<PaginatedResult<PaymentModel>> getPayments({
    int page = 1,
    int pageSize = 20,
  }) async {
    return _apiClient.getPaginated<PaymentModel>(
      ApiEndpoints.myPayments,
      params: PaginationParams(page: page, pageSize: pageSize),
      itemParser: (json) => PaymentModel.fromJson(json),
    );
  }
}
