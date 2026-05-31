import 'package:dartz/dartz.dart';
import '../errors/failure.dart';

/// Base use case following Clean Architecture.
/// [Type] is the return type, [Params] is the input parameters type.
abstract class UseCase<Type, Params> {
  Future<Either<Failure, Type>> call(Params params);
}

/// Use case with no parameters.
abstract class UseCaseNoParams<Type> {
  Future<Either<Failure, Type>> call();
}

/// Use case that returns a stream.
abstract class StreamUseCase<Type, Params> {
  Stream<Either<Failure, Type>> call(Params params);
}

/// No parameters marker class.
class NoParams {
  const NoParams();
}
