import 'package:dio/dio.dart';
import '../../services/secure_storage_service.dart';

/// Injects the X-Gym-Id header into every request.
/// Multi-gym support: trainers can work at multiple gyms (Q3).
/// The current gym is stored in secure storage and switched via gym switcher.
class GymIdInterceptor extends Interceptor {
  final SecureStorageService _storage;

  GymIdInterceptor({required SecureStorageService storage})
      : _storage = storage;

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    // Skip if already set (e.g., for specific gym queries)
    if (options.headers.containsKey('X-Gym-Id')) {
      return handler.next(options);
    }

    final gymId = await _storage.getCurrentGymId();
    if (gymId != null && gymId.isNotEmpty) {
      options.headers['X-Gym-Id'] = gymId;
    }

    return handler.next(options);
  }
}
