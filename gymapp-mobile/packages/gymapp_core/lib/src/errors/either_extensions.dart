import 'package:dartz/dartz.dart';
import 'failure.dart';

/// Type alias for the common Either pattern used in usecases.
typedef ResultFuture<T> = Future<Either<Failure, T>>;
typedef ResultVoid = Future<Either<Failure, void>>;

/// Extension methods on Either for convenience.
extension EitherExtensions<L, R> on Either<L, R> {
  /// Get the right value or null.
  R? getOrNull() => fold((_) => null, (r) => r);

  /// Get the left value or null.
  L? getLeftOrNull() => fold((l) => l, (_) => null);

  /// Map the right value asynchronously.
  Future<Either<L, T>> mapAsync<T>(Future<T> Function(R r) f) async {
    return fold(
      (l) => Left(l),
      (r) async => Right(await f(r)),
    );
  }

  /// Execute a side effect on the right value.
  Either<L, R> tap(void Function(R r) f) {
    fold((_) {}, (r) => f(r));
    return this;
  }

  /// Execute a side effect on the left value.
  Either<L, R> tapLeft(void Function(L l) f) {
    fold((l) => f(l), (_) {});
    return this;
  }
}

/// Extension on Future<Either> for chaining operations.
extension FutureEitherExtensions<L, R> on Future<Either<L, R>> {
  /// FlatMap on the right value.
  Future<Either<L, T>> flatMap<T>(
    Future<Either<L, T>> Function(R r) f,
  ) async {
    final result = await this;
    return result.fold(
      (l) => Left(l),
      (r) => f(r),
    );
  }

  /// Map on the right value.
  Future<Either<L, T>> mapRight<T>(T Function(R r) f) async {
    final result = await this;
    return result.map(f);
  }

  /// Get the right value or a default.
  Future<R> getOrElse(R defaultValue) async {
    final result = await this;
    return result.getOrElse(() => defaultValue);
  }
}
