import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// In-memory stand-in so the interceptor never touches the platform keychain.
class _FakeStorage extends SecureStorageService {
  String? accessToken;
  String? refreshToken;
  int clearTokensCalls = 0;

  @override
  Future<String?> getAccessToken() async => accessToken;

  @override
  Future<String?> getRefreshToken() async => refreshToken;

  @override
  Future<void> clearTokens() async {
    clearTokensCalls++;
    accessToken = null;
    refreshToken = null;
  }
}

/// Answers every request with 401 and records the paths it saw.
class _Always401Adapter implements HttpClientAdapter {
  final List<String> paths = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    paths.add(options.path);
    return ResponseBody.fromString(
      jsonEncode({
        'data': null,
        'errors': [
          {'code': 'User.InvalidCredentials', 'message': 'bad credentials'}
        ],
      }),
      401,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

void main() {
  late _FakeStorage storage;
  late _Always401Adapter adapter;
  late Dio dio;
  late int invalidatedCalls;

  setUp(() {
    storage = _FakeStorage();
    adapter = _Always401Adapter();
    invalidatedCalls = 0;
    dio = Dio(BaseOptions(baseUrl: 'https://api.test'))
      ..httpClientAdapter = adapter;
    dio.interceptors.add(AuthInterceptor(
      storage: storage,
      dio: dio,
      onAuthInvalidated: () async => invalidatedCalls++,
    ));
  });

  test('401 from login is a credential error, not an expired session',
      () async {
    await expectLater(
      dio.post(ApiEndpoints.login, data: {'email': 'a@b.c', 'password': 'x'}),
      throwsA(isA<DioException>()
          .having((e) => e.response?.statusCode, 'status', 401)),
    );

    expect(invalidatedCalls, 0);
    expect(storage.clearTokensCalls, 0);
    expect(adapter.paths, [ApiEndpoints.login]);
  });

  test('401 from a protected endpoint without a refresh token signs out',
      () async {
    await expectLater(
      dio.get(ApiEndpoints.profile),
      throwsA(isA<DioException>()),
    );

    expect(invalidatedCalls, 1);
    expect(storage.clearTokensCalls, 1);
  });
}
