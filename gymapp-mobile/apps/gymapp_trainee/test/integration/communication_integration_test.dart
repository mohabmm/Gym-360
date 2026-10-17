import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/communication/data/datasources/communication_remote_datasource.dart';
import 'package:gymapp_trainee/features/communication/data/datasources/notifications_remote_datasource.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Chat, notifications, announcements, notification preferences — every
/// communication screen in the trainee app.
void main() {
  late TestApiClient t;
  late CommunicationRemoteDataSource ds;
  late NotificationsRemoteDataSource notifications;

  setUp(() {
    t = buildTestApiClient();
    ds = CommunicationRemoteDataSource(apiClient: t.apiClient);
    notifications = NotificationsRemoteDataSource(apiClient: t.apiClient);
  });

  group('Chat list screen', () {
    test('GET /communication/conversations sends userId, gymId, paging',
        () async {
      t.adapter.onGet(
        ApiEndpoints.conversations,
        respond: (_) => t.adapter.ok([Samples.conversation()]),
      );
      final list =
          await ds.getConversations(userId: 'user-1', gymId: 'gym-1');
      expect(list, hasLength(1));
      expect(list.first.participants, hasLength(2));
      expect(list.first.unreadCount, 2);
      final q = t.onlyRequest.query;
      expect(q['userId']!.first, 'user-1');
      expect(q['gymId']!.first, 'gym-1');
      expect(q['page']!.first, '1');
      expect(q['pageSize']!.first, '20');
    });
  });

  group('Chat thread screen', () {
    test('GET messages for a conversation', () async {
      t.adapter.onGet(
        ApiEndpoints.messages(const ConversationId('conv-1')),
        respond: (_) => t.adapter.ok([Samples.message()]),
      );
      final msgs = await ds.getMessages('conv-1');
      expect(msgs.first.content, contains('Great session'));
    });

    test('POST send message returns message id and omits null media fields',
        () async {
      t.adapter.onPost(
        ApiEndpoints.messages(const ConversationId('conv-1')),
        respond: (_) => t.adapter.created('msg-new'),
      );
      final id = await ds.sendMessage(
        conversationId: 'conv-1',
        draft: const MessageDraft('hey'),
      );
      expect(id, 'msg-new');
      final body = t.onlyRequest.bodyAsMap!;
      expect(body, {'content': 'hey'});
      expect(body.containsKey('mediaUrl'), isFalse);
    });

    test('PUT mark conversation read sends no body (user from JWT)', () async {
      t.adapter.onPut(
        '${ApiEndpoints.conversationById(const ConversationId('conv-1'))}/read',
        respond: (_) => t.adapter.noContent(),
      );
      await ds.markMessagesRead(conversationId: 'conv-1');
      expect(t.onlyRequest.method, 'PUT');
      expect(t.onlyRequest.bodyAsMap, isNull);
    });
  });

  group('Announcements screen', () {
    test('GET /gyms/{gymId}/announcements', () async {
      t.adapter.onGet(
        '${ApiEndpoints.gyms}/gym-1/announcements',
        respond: (_) => t.adapter.ok([Samples.announcement()]),
      );
      final list = await ds.getAnnouncements(gymId: 'gym-1');
      expect(list.first.title, 'Ramadan hours');
    });
  });

  group('Notifications screen', () {
    test('GET /communication/notifications returns list', () async {
      t.adapter.onGet(
        ApiEndpoints.notifications,
        respond: (_) => t.adapter.ok([Samples.notification()]),
      );
      final list = await notifications.getNotifications(gymId: 'gym-1');
      expect(list, hasLength(1));
      expect(list.first.type, 'session_reminder');
      expect(t.onlyRequest.query.containsKey('userId'), isFalse);
      expect(t.onlyRequest.q('gymId'), 'gym-1');
    });

    test('PUT notifications/{id}/read sends no body (user from JWT)', () async {
      t.adapter.onPut(
        ApiEndpoints.notificationMarkRead(const NotificationId('notif-1')),
        respond: (_) => t.adapter.noContent(),
      );
      await notifications.markNotificationRead('notif-1');
      expect(t.onlyRequest.method, 'PUT');
      expect(t.onlyRequest.bodyAsMap, isNull);
    });

    test('PUT notifications/read-all', () async {
      t.adapter.onPut(
        ApiEndpoints.notificationsReadAll,
        respond: (_) => t.adapter.noContent(),
      );
      await notifications.markAllNotificationsRead();
      expect(t.onlyRequest.method, 'PUT');
      expect(t.onlyRequest.bodyAsMap, isNull);
    });

    test('GET unread-count parses both int and {count: N} shapes', () async {
      t.adapter.onGet(
        ApiEndpoints.notificationsUnreadCount,
        respond: (_) => t.adapter.ok(5),
      );
      expect(await notifications.getUnreadNotificationCount(), 5);
    });
  });

  group('Notification preferences screen', () {
    test('GET + PUT cycle', () async {
      t.adapter.onGet(
        ApiEndpoints.notificationPreferences,
        respond: (_) => t.adapter.ok(Samples.notificationPrefs()),
      );
      t.adapter.onPut(
        ApiEndpoints.notificationPreferences,
        respond: (_) => t.adapter.noContent(),
      );

      final prefs = await notifications.getNotificationPreferences(gymId: 'gym-1');
      expect(prefs.promotions, false);
      await notifications.updateNotificationPreferences({
        'userId': 'user-1',
        'gymId': 'gym-1',
        'promotions': true,
      });
      expect(t.adapter.recorded.last.bodyAsMap!['promotions'], true);
    });
  });
}
