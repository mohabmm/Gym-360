/// Resolves the API base URL for the current build/runtime.
///
/// Priority:
///   1. `--dart-define=API_BASE_URL=...` (always wins if non-empty).
///   2. Default: staging.
class ApiBaseUrl {
  ApiBaseUrl._();

  static const String _override =
      String.fromEnvironment('API_BASE_URL', defaultValue: '');

  static const String _default = 'https://staging-api.gym-365.com';

  static String resolve() {
    if (_override.isNotEmpty) return _override;
    return _default;
  }
}
