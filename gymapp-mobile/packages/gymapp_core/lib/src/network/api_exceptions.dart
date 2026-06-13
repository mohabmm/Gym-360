import '../errors/failure.dart';

/// Custom exceptions for API error handling.
class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final String? errorCode;
  final List<String> errors;

  const ApiException({
    required this.message,
    this.statusCode,
    this.errorCode,
    this.errors = const [],
  });

  @override
  String toString() => 'ApiException($statusCode): $message';

  Failure toFailure() {
    if (statusCode == null) {
      return const NetworkFailure();
    }
    return ServerFailure.fromStatusCode(
      statusCode!,
      message: message,
    );
  }
}

class UnauthorizedException extends ApiException {
  const UnauthorizedException({
    String message = 'Authentication required',
  }) : super(message: message, statusCode: 401, errorCode: 'UNAUTHORIZED');
}

class ForbiddenException extends ApiException {
  const ForbiddenException({
    String message = 'Access denied',
  }) : super(message: message, statusCode: 403, errorCode: 'FORBIDDEN');
}

class NotFoundException extends ApiException {
  const NotFoundException({
    String message = 'Resource not found',
  }) : super(message: message, statusCode: 404, errorCode: 'NOT_FOUND');
}

class ValidationException extends ApiException {
  final Map<String, String> fieldErrors;

  const ValidationException({
    String message = 'Validation error',
    this.fieldErrors = const {},
    List<String> errors = const [],
  }) : super(
          message: message,
          statusCode: 400,
          errorCode: 'VALIDATION_ERROR',
          errors: errors,
        );
}

class FeatureGatedException extends ApiException {
  final String featureCode;
  final String requiredPlan;

  const FeatureGatedException({
    required this.featureCode,
    required this.requiredPlan,
  }) : super(
          message: 'Feature requires $requiredPlan plan',
          statusCode: 403,
          errorCode: 'FEATURE_GATED',
        );

  @override
  Failure toFailure() => FeatureGatedFailure(
        featureCode: featureCode,
        requiredPlan: requiredPlan,
      );
}

class StorageLimitException extends ApiException {
  final int currentCount;
  final int maxCount;
  final String resourceType;

  const StorageLimitException({
    required this.currentCount,
    required this.maxCount,
    required this.resourceType,
  }) : super(
          message: 'Storage limit reached',
          statusCode: 403,
          errorCode: 'STORAGE_LIMIT',
        );

  @override
  Failure toFailure() => StorageLimitFailure(
        currentCount: currentCount,
        maxCount: maxCount,
        resourceType: resourceType,
      );
}

class GymSuspendedException extends ApiException {
  const GymSuspendedException()
      : super(
          message: 'Gym subscription is suspended',
          statusCode: 403,
          errorCode: 'GYM_SUSPENDED',
        );

  @override
  Failure toFailure() => const GymSuspendedFailure();
}
