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
    this.headers = const {},
  });

  final int statusCode;
  final dynamic body;
  final Map<String, List<String>> headers;
}

/// Minimal programmable [HttpClientAdapter] replacement for tests.
///
/// Usage:
/// ```dart
/// final adapter = MockHttpAdapter();
/// adapter.onGet('/api/v1/identity/profile', respond: (_) => adapter.ok({...}));
/// dio.httpClientAdapter = adapter;
/// ```
///
/// Interceptors (auth, gym-id, error) still run on Dio's side — only the
/// network I/O is mocked, so tests exercise the full client stack.
class MockHttpAdapter implements HttpClientAdapter {
  final List<RecordedRequest> recorded = [];
  final List<_Route> _routes = [];

  /// Register a responder for a method + path.
  ///
  /// Path matching is an exact string match against the full request path
  /// (without query string). Trailing-segment placeholders are not supported —
  /// tests should register the fully-interpolated path.
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

  /// 200 OK with the payload placed at the top level (no envelope wrapping).
  /// A handful of BE endpoints (currently `unread-count`) return a bare value.
  _MockResponse okRaw(dynamic payload, {int statusCode = 200}) {
    return _MockResponse(statusCode: statusCode, body: payload);
  }

  /// 201 Created (same envelope, just a different status code).
  _MockResponse created(dynamic payload) => ok(payload, statusCode: 201);

  /// 204 No Content — Dio's JSON decoder trips on empty bodies, so we return
  /// an empty envelope which is always safe to parse.
  _MockResponse noContent() => _MockResponse(statusCode: 200, body: {});

  /// Fully custom response — caller supplies the raw body map. Lets tests
  /// simulate BE-specific envelopes (validation errors with multiple fields,
  /// feature-gated responses with extras, etc.) without hacking `error()`.
  _MockResponse rawResponse(int statusCode, dynamic body) {
    return _MockResponse(statusCode: statusCode, body: body);
  }

  /// BE-style error envelope for a given HTTP status.
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

    final recordedRequest = RecordedRequest(
      method: options.method.toUpperCase(),
      path: options.path,
      query: _asLists(options.queryParameters),
      headers: _asLists(options.headers, lowerCaseKeys: true),
      body: requestBody,
    );
    recorded.add(recordedRequest);

    final route = _routes.firstWhere(
      (r) => r.method == recordedRequest.method && r.path == recordedRequest.path,
      orElse: () => _noRouteFor(recordedRequest),
    );

    return _toResponseBody(route.respond(recordedRequest));
  }

  /// Single-valued `{key: [value]}` view of Dio's headers / query parameters.
  static Map<String, List<String>> _asLists(
    Map<String, dynamic> source, {
    bool lowerCaseKeys = false,
  }) {
    return {
      for (final e in source.entries)
        (lowerCaseKeys ? e.key.toLowerCase() : e.key): [e.value?.toString() ?? ''],
    };
  }

  static _Route _noRouteFor(RecordedRequest request) {
    return _Route(
      method: request.method,
      path: request.path,
      respond: (_) => _MockResponse(
        statusCode: 404,
        body: {
          'data': null,
          'errors': [
            {
              'code': 'NO_ROUTE',
              'message':
                  'MockHttpAdapter has no route for ${request.method} ${request.path}',
            },
          ],
        },
      ),
    );
  }

  static ResponseBody _toResponseBody(_MockResponse response) {
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
    // A real adapter would close sockets; nothing to do for the in-memory mock.
    // Kept so we remain a drop-in replacement for [IOHttpClientAdapter].
    IOHttpClientAdapter().close(force: force);
  }
}

class _Route {
  _Route({required this.method, required this.path, required this.respond});
  final String method;
  final String path;
  final _Responder respond;
}
