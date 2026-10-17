/// Captures every HTTP call Dio makes so tests can assert the exact method,
/// path, query parameters, headers, and request body sent to the backend.
class RecordedRequest {
  RecordedRequest({
    required this.method,
    required this.path,
    required this.query,
    required this.headers,
    required this.body,
  });

  final String method;
  final String path;
  final Map<String, List<String>> query;
  final Map<String, List<String>> headers;
  final dynamic body;

  /// First value for a query parameter (or `null`).
  String? q(String key) => query[key]?.first;

  /// First value for a header (or `null`).
  String? header(String key) {
    // Dio lowercases header keys on the way out.
    return headers[key.toLowerCase()]?.first;
  }

  Map<String, dynamic>? get bodyAsMap =>
      body is Map<String, dynamic> ? body as Map<String, dynamic> : null;

  @override
  String toString() => '$method $path query=$query body=$body';
}
