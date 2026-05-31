import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

void main() {
  group('EitherExtensions', () {
    test('getOrNull returns right value', () {
      final Either<Failure, int> r = const Right(42);
      expect(r.getOrNull(), 42);
    });

    test('getOrNull returns null for Left', () {
      final Either<Failure, int> l = const Left(NetworkFailure());
      expect(l.getOrNull(), isNull);
    });

    test('getLeftOrNull returns Left value', () {
      final Either<Failure, int> l = const Left(NetworkFailure());
      expect(l.getLeftOrNull(), isA<NetworkFailure>());
    });

    test('tap runs side effect only for Right', () {
      var hits = 0;
      const Right<Failure, int>(7).tap((r) => hits++);
      const Left<Failure, int>(NetworkFailure()).tap((r) => hits++);
      expect(hits, 1);
    });

    test('tapLeft runs side effect only for Left', () {
      var hits = 0;
      const Right<Failure, int>(7).tapLeft((l) => hits++);
      const Left<Failure, int>(NetworkFailure()).tapLeft((l) => hits++);
      expect(hits, 1);
    });
  });

  group('FutureEitherExtensions', () {
    test('flatMap chains Right values', () async {
      Future<Either<Failure, int>> fetch() async => const Right(2);
      final result = await fetch().flatMap((r) async => Right(r * 10));
      expect(result.getOrNull(), 20);
    });

    test('flatMap short-circuits on Left', () async {
      Future<Either<Failure, int>> fetch() async =>
          const Left(NetworkFailure());
      final result = await fetch().flatMap((r) async => Right(r * 10));
      expect(result.getOrNull(), isNull);
      expect(result.getLeftOrNull(), isA<NetworkFailure>());
    });

    test('mapRight transforms the Right branch', () async {
      Future<Either<Failure, int>> fetch() async => const Right(5);
      final result = await fetch().mapRight((r) => r + 1);
      expect(result.getOrNull(), 6);
    });

    test('getOrElse returns default for Left', () async {
      Future<Either<Failure, int>> fetch() async =>
          const Left(NetworkFailure());
      expect(await fetch().getOrElse(99), 99);
    });
  });
}
