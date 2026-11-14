import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';

import 'recorded_request.dart';

export 'recorded_request.dart';

typedef _Responder = _MockResponse Function(RecordedRequest request);

class _MockResponse {
  _MockResponse({
    required this.statusCode,
    required this.body,
  }) : headers = const {};

  final int statusCode;
  final dynamic body;
  final Map<String, List<String>> headers;
}

/// Minimal programmable [HttpClientAdapter] replacement for tests.
///
/// Interceptors (auth, gym-id, error) still run on Dio's side — only the
/// network I/O is mocked, so tests exercise the full client stack.
class MockHttpAdapter implements HttpClientAdapter {
  final List<RecordedRequest> recorded = [];
  final List<_Route> _routes = [];

  void _register(_Route route) => _routes.add(route);

  void onGet(String path, {required _Responder respond}) =>
      _register(_Route(method: 'GET', path: path, respond: respond));
  void onPost(String path, {required _Responder respond}) =>
      _register(_Route(method: 'POST', path: path, respond: respond));
  void onPut(String path, {required _Responder respond}) =>
      _register(_Route(method: 'PUT', path: path, respond: respond));
  void onPatch(String path, {required _Responder respond}) =>
      _register(_Route(method: 'PATCH', path: path, respond: respond));
  void onDelete(String path, {required _Responder respond}) =>
      _register(_Route(method: 'DELETE', path: path, respond: respond));

  /// 200 OK wrapping [payload] in the BE's `{ data, errors, meta }` envelope.
  _MockResponse ok(dynamic payload, {Map<String, dynamic>? meta, int statusCode = 200}) {
    return _MockResponse(
      statusCode: statusCode,
      body: {
        'data': payload,
        'errors': <dynamic>[],
        if (meta != null) 'meta': meta,
      },
    );
  }

  _MockResponse okRaw(dynamic payload, {int statusCode = 200}) {
    return _MockResponse(statusCode: statusCode, body: payload);
  }

  _MockResponse created(dynamic payload) => ok(payload, statusCode: 201);

  _MockResponse noContent() => _MockResponse(statusCode: 200, body: {});

  _MockResponse rawResponse(int statusCode, dynamic body) {
    return _MockResponse(statusCode: statusCode, body: body);
  }

  _MockResponse error(
    int status, {
    String code = 'ERROR',
    String message = 'Something went wrong.',
    Map<String, dynamic>? extras,
  }) {
    return _MockResponse(
      statusCode: status,
      body: {
        'data': null,
        'errors': [
          {
            'code': code,
            'message': message,
            if (extras != null) 'meta': extras,
          },
        ],
        'errorCode': code,
        if (extras != null) ...extras,
      },
    );
  }

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final requestBody = await _drainRequestBody(options, requestStream);

    final headers = <String, List<String>>{};
    options.headers.forEach((k, v) {
      headers[k.toLowerCase()] = [v?.toString() ?? ''];
    });

    final query = <String, List<String>>{};
    options.queryParameters.forEach((k, v) {
      query[k] = [v?.toString() ?? ''];
    });

    final recordedRequest = RecordedRequest(
      method: options.method.toUpperCase(),
      path: options.path,
      query: query,
      headers: headers,
      body: requestBody,
    );
    recorded.add(recordedRequest);

    final route = _routes.firstWhere(
      (r) => r.method == recordedRequest.method && r.path == recordedRequest.path,
      orElse: () => _Route(
        method: recordedRequest.method,
        path: recordedRequest.path,
        respond: (_) => _MockResponse(
          statusCode: 404,
          body: {
            'data': null,
            'errors': [
              {
                'code': 'NO_ROUTE',
                'message':
                    'MockHttpAdapter has no route for ${recordedRequest.method} ${recordedRequest.path}',
              },
            ],
          },
        ),
      ),
    );

    final response = route.respond(recordedRequest);
    final encoded = response.body is String
        ? response.body as String
        : jsonEncode(response.body);

    return ResponseBody.fromString(
      encoded,
      response.statusCode,
      headers: {
        'content-type': ['application/json; charset=utf-8'],
        ...response.headers,
      },
    );
  }

  Future<dynamic> _drainRequestBody(
    RequestOptions options,
    Stream<Uint8List>? stream,
  ) async {
    if (stream == null) return options.data;
    final chunks = <int>[];
    await for (final c in stream) {
      chunks.addAll(c);
    }
    if (chunks.isEmpty) return options.data;
    final text = utf8.decode(chunks);
    if (text.isEmpty) return options.data;
    try {
      return jsonDecode(text);
    } catch (_) {
      return text;
    }
  }

  @override
  void close({bool force = false}) {
    IOHttpClientAdapter().close(force: force);
  }
}

class _Route {
  _Route({required this.method, required this.path, required this.respond});
  final String method;
  final String path;
  final _Responder respond;
}
