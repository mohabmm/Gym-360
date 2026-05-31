import 'dart:convert';
import 'package:hive/hive.dart';
import '../constants/app_constants.dart';
import '../constants/storage_keys.dart';

/// Offline write queue with 7-day retention and last-writer-wins strategy.
/// Queues write operations when offline and syncs when connectivity returns.
class OfflineQueueService {
  late Box _queueBox;

  Future<void> initialize() async {
    _queueBox = await Hive.openBox(StorageKeys.offlineQueueBox);
    await _cleanExpiredItems();
  }

  /// Add an operation to the offline queue.
  Future<void> enqueue(OfflineOperation operation) async {
    if (_queueBox.length >= AppConstants.maxOfflineQueueItems) {
      // Remove oldest item to make room
      await _queueBox.deleteAt(0);
    }
    await _queueBox.add(operation.toJson());
  }

  /// Get all pending operations ordered by timestamp.
  List<OfflineOperation> getPendingOperations() {
    final operations = <OfflineOperation>[];
    for (var i = 0; i < _queueBox.length; i++) {
      final json = _queueBox.getAt(i) as Map?;
      if (json != null) {
        final op = OfflineOperation.fromJson(Map<String, dynamic>.from(json));
        if (!op.isExpired) {
          operations.add(op);
        }
      }
    }
    // Sort by timestamp for ordered replay
    operations.sort((a, b) => a.timestamp.compareTo(b.timestamp));
    return operations;
  }

  /// Remove a completed operation.
  Future<void> removeOperation(String operationId) async {
    for (var i = 0; i < _queueBox.length; i++) {
      final json = _queueBox.getAt(i) as Map?;
      if (json != null && json['id'] == operationId) {
        await _queueBox.deleteAt(i);
        return;
      }
    }
  }

  /// Clear all pending operations.
  Future<void> clearAll() async {
    await _queueBox.clear();
  }

  /// Get count of pending operations.
  int get pendingCount {
    int count = 0;
    for (var i = 0; i < _queueBox.length; i++) {
      final json = _queueBox.getAt(i) as Map?;
      if (json != null) {
        final op = OfflineOperation.fromJson(Map<String, dynamic>.from(json));
        if (!op.isExpired) count++;
      }
    }
    return count;
  }

  /// Remove items older than 7 days.
  Future<void> _cleanExpiredItems() async {
    final keysToDelete = <int>[];
    for (var i = 0; i < _queueBox.length; i++) {
      final json = _queueBox.getAt(i) as Map?;
      if (json != null) {
        final op = OfflineOperation.fromJson(Map<String, dynamic>.from(json));
        if (op.isExpired) {
          keysToDelete.add(i);
        }
      }
    }
    // Delete in reverse order to maintain indices
    for (final key in keysToDelete.reversed) {
      await _queueBox.deleteAt(key);
    }
  }
}

/// Represents a single queued offline operation.
class OfflineOperation {
  final String id;
  final String method;
  final String path;
  final Map<String, dynamic>? body;
  final DateTime timestamp;
  final String? resourceId;
  final String resourceType;

  OfflineOperation({
    required this.id,
    required this.method,
    required this.path,
    this.body,
    required this.timestamp,
    this.resourceId,
    required this.resourceType,
  });

  bool get isExpired {
    final expiryDate = timestamp.add(
      const Duration(days: AppConstants.offlineQueueRetentionDays),
    );
    return DateTime.now().isAfter(expiryDate);
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'method': method,
        'path': path,
        'body': body != null ? jsonEncode(body) : null,
        'timestamp': timestamp.toIso8601String(),
        'resourceId': resourceId,
        'resourceType': resourceType,
      };

  factory OfflineOperation.fromJson(Map<String, dynamic> json) {
    return OfflineOperation(
      id: json['id'] as String,
      method: json['method'] as String,
      path: json['path'] as String,
      body: json['body'] != null
          ? jsonDecode(json['body'] as String) as Map<String, dynamic>
          : null,
      timestamp: DateTime.parse(json['timestamp'] as String),
      resourceId: json['resourceId'] as String?,
      resourceType: json['resourceType'] as String,
    );
  }
}
