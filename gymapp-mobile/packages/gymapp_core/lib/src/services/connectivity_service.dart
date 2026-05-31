import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';

/// Monitors network connectivity state.
/// Used to trigger offline queue sync when coming back online.
class ConnectivityService {
  final Connectivity _connectivity;
  final StreamController<bool> _connectionController =
      StreamController<bool>.broadcast();

  bool _isConnected = true;

  ConnectivityService({Connectivity? connectivity})
      : _connectivity = connectivity ?? Connectivity();

  /// Stream of connectivity state changes.
  Stream<bool> get onConnectivityChanged => _connectionController.stream;

  /// Current connectivity state.
  bool get isConnected => _isConnected;

  /// Initialize and start monitoring.
  Future<void> initialize() async {
    final result = await _connectivity.checkConnectivity();
    _isConnected = _hasConnection(result);
    _connectionController.add(_isConnected);

    _connectivity.onConnectivityChanged.listen((result) {
      final connected = _hasConnection(result);
      if (connected != _isConnected) {
        _isConnected = connected;
        _connectionController.add(_isConnected);
      }
    });
  }

  bool _hasConnection(ConnectivityResult result) {
    return result == ConnectivityResult.mobile ||
        result == ConnectivityResult.wifi ||
        result == ConnectivityResult.ethernet;
  }

  void dispose() {
    _connectionController.close();
  }
}
