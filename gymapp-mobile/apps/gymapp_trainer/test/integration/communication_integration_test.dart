import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

/// Covers ChatListScreen, ChatScreen, BroadcastScreen, FormCheckScreen
/// (form-check is part of chat flow).
void main() {
  late TestApiClient t;

  setUp(() {
    t = buildTestApiClient();
  });

  group('ChatListScreen', () {
    test('GET /communication/conversations with userId+gymId', () async {
      t.adapter.onGet(
        ApiEndpoints.conversations,
        respond: (_) => t.adapter.ok([Samples.conversation()]),
      );

      final convs = await t.apiClient.get<List<ConversationModel>>(
        ApiEndpoints.conversations,
        queryParameters: {
          'userId': kTrainerId,
          'gymId': kGymId,
          'page': 1,
          'pageSize': 50,
        },
        parser: (data) => (data as List<dynamic>)
            .map((e) => ConversationModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.method, 'GET');
      expect(req.path, '/api/v1/communication/conversations');
      expect(req.q('userId'), kTrainerId);
      expect(req.q('gymId'), kGymId);
      expect(convs.single.participants, hasLength(2));
      expect(convs.single.unreadCount, 2);
      expect(convs.single.gymId, kGymId);
    });
  });

  group('ChatScreen', () {
    test('GET /conversations/{id}/messages with userId+pagination', () async {
      t.adapter.onGet(
        ApiEndpoints.messages('conv-1'),
        respond: (_) => t.adapter.ok([Samples.message()]),
      );

      final msgs = await t.apiClient.get<List<MessageModel>>(
        ApiEndpoints.messages('conv-1'),
        queryParameters: {
          'userId': kTrainerId,
          'page': 1,
          'pageSize': 100,
        },
        parser: (data) => (data as List<dynamic>)
            .map((e) => MessageModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

      final req = t.onlyRequest;
      expect(req.path,
          '/api/v1/communication/conversations/conv-1/messages');
      expect(req.q('userId'), kTrainerId);
      expect(req.q('pageSize'), '100');
      expect(msgs.single.content, 'Thanks coach!');
    });

    test('POST /conversations/{id}/messages sends { senderId, content }',
        () async {
      t.adapter.onPost(
        ApiEndpoints.messages('conv-1'),
        respond: (_) => t.adapter.created(Samples.message()),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.messages('conv-1'),
        data: {
          'senderId': kTrainerId,
          'content': 'Great session today',
        },
      );

      final req = t.onlyRequest;
      expect(req.method, 'POST');
      expect(req.path,
          '/api/v1/communication/conversations/conv-1/messages');
      expect(req.bodyAsMap, {
        'senderId': kTrainerId,
        'content': 'Great session today',
      });
    });
  });

  group('BroadcastScreen', () {
    test('POST /communication/broadcast sends recipientIds array', () async {
      t.adapter.onPost(
        ApiEndpoints.broadcast,
        respond: (_) => t.adapter.created({'id': 'bc-1'}),
      );

      await t.apiClient.postVoid(
        ApiEndpoints.broadcast,
        data: {
          'gymId': kGymId,
          'senderId': kTrainerId,
          'content': 'Gym closed Friday',
          'recipientIds': ['client-1', 'client-2', 'client-3'],
        },
      );

      final body = t.onlyRequest.bodyAsMap!;
      expect(body['senderId'], kTrainerId);
      expect(body['gymId'], kGymId);
      expect(body['content'], 'Gym closed Friday');
      expect(body['recipientIds'], ['client-1', 'client-2', 'client-3']);
    });

    test('403 feature-gated broadcast surfaces FeatureGatedException',
        () async {
      // ErrorInterceptor maps to FeatureGatedException only when the error
      // code is 'PLAN.FEATURE_NOT_AVAILABLE' and reads the meta keys
      // 'feature' + 'requiredPlan'.
      t.adapter.onPost(
        ApiEndpoints.broadcast,
        respond: (_) => t.adapter.error(
          403,
          code: 'PLAN.FEATURE_NOT_AVAILABLE',
          message: 'Broadcasts require Pro plan',
          extras: {
            'feature': 'broadcasts',
            'requiredPlan': 'Pro',
          },
        ),
      );

      await expectLater(
        t.apiClient.postVoid(
          ApiEndpoints.broadcast,
          data: {
            'gymId': kGymId,
            'senderId': kTrainerId,
            'content': 'x',
            'recipientIds': <String>[],
          },
        ),
        throwsA(
          predicate(
            (e) =>
                e is FeatureGatedException &&
                e.featureCode == 'broadcasts' &&
                e.requiredPlan == 'Pro',
          ),
        ),
      );
    });
  });
}
