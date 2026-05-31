import 'dart:async';

/// Debounces rapid calls, executing only the last one after a delay.
/// Used for search inputs, form validation, etc.
class Debouncer {
  final int milliseconds;
  Timer? _timer;

  Debouncer({this.milliseconds = 500});

  /// Run the action after the debounce delay.
  void run(void Function() action) {
    _timer?.cancel();
    _timer = Timer(Duration(milliseconds: milliseconds), action);
  }

  /// Cancel any pending action.
  void cancel() {
    _timer?.cancel();
  }

  /// Dispose of the debouncer.
  void dispose() {
    _timer?.cancel();
    _timer = null;
  }
}
