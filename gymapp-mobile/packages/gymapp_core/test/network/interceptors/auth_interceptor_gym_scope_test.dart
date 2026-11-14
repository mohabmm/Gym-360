import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

class _FakeStorage extends SecureStorageService {
  String? accessToken = 'old-access';
  String? refreshToken = 'old-refresh';
  int clearTokensCalls = 0;

  @override
  Future<String?> getAccessToken() async => accessToken;

  @override
  Future<String?> getRefreshToken() async => refreshToken;

  @override
  Future<void> saveTokens({required AccessToken accessToken, required RefreshToken refreshToken}) async {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
  }

  @override
  Future<void> clearTokens() async {
    clearTokensCalls++;
    accessToken = null;
    refreshToken = null;
  }
}

typedef _Reply = ({int status, Object body});

/// Answers each request from a per-path queue and records what it saw.
class _ScriptedAdapter implements HttpClientAdapter {
  final Map<String, List<_Reply>> replies;
  final List<({String path, String? auth})> seen = [];

  _ScriptedAdapter(this.replies);

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    seen.add((path: options.path, auth: options.headers['Authorization'] as String?));
    final reply = replies[options.path]!.removeAt(0);
    return ResponseBody.fromString(jsonEncode(reply.body), reply.status, headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    });
  }

  @override
  void close({bool force = false}) {}
}

const _trainers = '/api/v1/gyms/g1/trainers';

_Reply _error(int status, String code) => (status: status, body: {
      'data': null,
      'errors': [
        {'code': code, 'message': code}
      ],
    });

final _refreshed = (status: 200, body: {
  'data': {'accessToken': 'new-access', 'refreshToken': 'new-refresh'},
});

void main() {
  late _FakeStorage storage;
  late int signedOut;

  Dio dioWith(_ScriptedAdapter adapter) {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.test'))..httpClientAdapter = adapter;
    dio.interceptors.add(AuthInterceptor(storage: storage, dio: dio, onAuthInvalidated: () async => signedOut++));
    return dio;
  }

  setUp(() {
    storage = _FakeStorage();
    signedOut = 0;
  });

  test('GYM_NOT_FOUND: refreshes the token once and retries, so an empty list loads', () async {
    final adapter = _ScriptedAdapter({
      _trainers: [_error(404, 'GYM_NOT_FOUND'), (status: 200, body: {'data': <Object>[]})],
      ApiEndpoints.refreshToken: [_refreshed],
    });

    final response = await dioWith(adapter).get(_trainers);

    expect(response.data['data'], isEmpty);
    expect(adapter.seen.map((r) => r.path), [_trainers, ApiEndpoints.refreshToken, _trainers]);
    expect(adapter.seen.last.auth, 'Bearer new-access');
    expect(storage.refreshToken, 'new-refresh');
    expect(signedOut, 0);
  });

  test('a plain 404 is a real not-found: no refresh', () async {
    final adapter = _ScriptedAdapter({
      _trainers: [_error(404, 'Gym.NotFound')],
    });

    await expectLater(dioWith(adapter).get(_trainers), throwsA(isA<DioException>()));

    expect(adapter.seen.map((r) => r.path), [_trainers]);
    expect(signedOut, 0);
  });

  test('retry still failing shows the error but keeps the user signed in', () async {
    final adapter = _ScriptedAdapter({
      _trainers: [_error(404, 'GYM_NOT_FOUND'), _error(404, 'GYM_NOT_FOUND')],
      ApiEndpoints.refreshToken: [_refreshed],
    });

    await expectLater(
      dioWith(adapter).get(_trainers),
      throwsA(isA<DioException>().having((e) => e.response?.statusCode, 'status', 404)),
    );

    expect(adapter.seen, hasLength(3), reason: 'one refresh, one retry, no loop');
    expect(signedOut, 0);
    expect(storage.clearTokensCalls, 0);
  });

  test('refresh rejected: the session is over and the user is signed out', () async {
    final adapter = _ScriptedAdapter({
      _trainers: [_error(404, 'GYM_NOT_FOUND')],
      ApiEndpoints.refreshToken: [_error(400, 'User.InvalidCredentials')],
    });

    await expectLater(dioWith(adapter).get(_trainers), throwsA(isA<DioException>()));

    expect(signedOut, 1);
    expect(storage.clearTokensCalls, 1);
  });

  test('401 still refreshes and retries as before', () async {
    final adapter = _ScriptedAdapter({
      _trainers: [_error(401, 'AUTH.TOKEN_EXPIRED'), (status: 200, body: {'data': <Object>[]})],
      ApiEndpoints.refreshToken: [_refreshed],
    });

    final response = await dioWith(adapter).get(_trainers);

    expect(response.statusCode, 200);
    expect(signedOut, 0);
  });
}
