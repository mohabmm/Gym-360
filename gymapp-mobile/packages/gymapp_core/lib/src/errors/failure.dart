import 'package:equatable/equatable.dart';

/// Base failure class for domain error handling.
/// Uses module-prefixed error codes matching backend conventions.
abstract class Failure extends Equatable {
  final String code;
  final String message;
  final String? messageAr;

  const Failure({
    required this.code,
    required this.message,
    this.messageAr,
  });

  @override
  List<Object?> get props => [code, message];
}

/// Server-side error returned from the API.
class ServerFailure extends Failure {
  final int? statusCode;
  final List<String> errors;

  const ServerFailure({
    required super.code,
    required super.message,
    super.messageAr,
    this.statusCode,
    this.errors = const [],
  });

  factory ServerFailure.fromStatusCode(int statusCode, {String? message}) {
    switch (statusCode) {
      case 400:
        return ServerFailure(
          code: 'BAD_REQUEST',
          message: message ?? 'Invalid request',
          messageAr: 'طلب غير صالح',
          statusCode: statusCode,
        );
      case 401:
        return ServerFailure(
          code: 'UNAUTHORIZED',
          message: message ?? 'Authentication required',
          messageAr: 'يجب تسجيل الدخول',
          statusCode: statusCode,
        );
      case 403:
        return ServerFailure(
          code: 'FORBIDDEN',
          message: message ?? 'Access denied',
          messageAr: 'الوصول مرفوض',
          statusCode: statusCode,
        );
      case 404:
        return ServerFailure(
          code: 'NOT_FOUND',
          message: message ?? 'Resource not found',
          messageAr: 'العنصر غير موجود',
          statusCode: statusCode,
        );
      case 409:
        return ServerFailure(
          code: 'CONFLICT',
          message: message ?? 'Conflict',
          messageAr: 'تعارض في البيانات',
          statusCode: statusCode,
        );
      case 422:
        return ServerFailure(
          code: 'VALIDATION',
          message: message ?? 'Validation error',
          messageAr: 'خطأ في التحقق',
          statusCode: statusCode,
        );
      case 429:
        return ServerFailure(
          code: 'RATE_LIMITED',
          message: message ?? 'Too many requests',
          messageAr: 'طلبات كثيرة جدا',
          statusCode: statusCode,
        );
      case 500:
        return ServerFailure(
          code: 'SERVER_ERROR',
          message: message ?? 'Internal server error',
          messageAr: 'خطأ في الخادم',
          statusCode: statusCode,
        );
      default:
        return ServerFailure(
          code: 'UNKNOWN',
          message: message ?? 'An unexpected error occurred',
          messageAr: 'حدث خطأ غير متوقع',
          statusCode: statusCode,
        );
    }
  }

  @override
  List<Object?> get props => [code, message, statusCode, errors];
}

/// Network connection failure.
class NetworkFailure extends Failure {
  const NetworkFailure()
      : super(
          code: 'NETWORK_ERROR',
          message: 'No internet connection',
          messageAr: 'لا يوجد اتصال بالإنترنت',
        );
}

/// Request timeout failure.
class TimeoutFailure extends Failure {
  const TimeoutFailure()
      : super(
          code: 'TIMEOUT',
          message: 'Request timed out',
          messageAr: 'انتهت مهلة الطلب',
        );
}

/// Local cache failure.
class CacheFailure extends Failure {
  const CacheFailure({
    String message = 'Cache error',
    String? messageAr,
  }) : super(
          code: 'CACHE_ERROR',
          message: message,
          messageAr: messageAr ?? 'خطأ في التخزين المؤقت',
        );
}

/// Feature not available on current plan.
class FeatureGatedFailure extends Failure {
  final String featureCode;
  final String requiredPlan;

  const FeatureGatedFailure({
    required this.featureCode,
    required this.requiredPlan,
  }) : super(
          code: 'FEATURE_GATED',
          message: 'This feature requires the $requiredPlan plan',
          messageAr: 'هذه الميزة تتطلب خطة $requiredPlan',
        );

  @override
  List<Object?> get props => [code, featureCode, requiredPlan];
}

/// Storage limit exceeded.
class StorageLimitFailure extends Failure {
  final int currentCount;
  final int maxCount;
  final String resourceType;

  const StorageLimitFailure({
    required this.currentCount,
    required this.maxCount,
    required this.resourceType,
  }) : super(
          code: 'STORAGE_LIMIT',
          message:
              'You\'ve reached your $resourceType limit ($currentCount/$maxCount)',
          messageAr: 'لقد وصلت إلى الحد الأقصى لـ $resourceType ($currentCount/$maxCount)',
        );

  @override
  List<Object?> get props => [code, currentCount, maxCount, resourceType];
}

/// Gym is suspended.
class GymSuspendedFailure extends Failure {
  const GymSuspendedFailure()
      : super(
          code: 'GYM_SUSPENDED',
          message:
              'Your gym\'s subscription has been suspended. Please contact your gym management.',
          messageAr:
              'تم تعليق اشتراك الصالة الرياضية. يرجى التواصل مع إدارة الصالة.',
        );
}

/// Validation failure for form inputs.
class ValidationFailure extends Failure {
  final Map<String, String> fieldErrors;

  const ValidationFailure({
    required this.fieldErrors,
  }) : super(
          code: 'VALIDATION',
          message: 'Please check the form for errors',
          messageAr: 'يرجى التحقق من النموذج للأخطاء',
        );

  @override
  List<Object?> get props => [code, fieldErrors];
}
