import 'package:freezed_annotation/freezed_annotation.dart';

part 'message_model.freezed.dart';
part 'message_model.g.dart';

@freezed
class ConversationModel with _$ConversationModel {
  const factory ConversationModel({
    required String id,
    required String gymId,
    required String type,
    required List<ConversationParticipantModel> participants,
    @Default(0) int unreadCount,
    required DateTime createdAt,
    required DateTime updatedAt,
    DateTime? lastMessageAt,
    MessageModel? lastMessage,
  }) = _ConversationModel;

  factory ConversationModel.fromJson(Map<String, dynamic> json) =>
      _$ConversationModelFromJson(json);
}

@freezed
class ConversationParticipantModel with _$ConversationParticipantModel {
  const factory ConversationParticipantModel({
    required String userId,
    required String name,
    String? photoUrl,
    required String role,
  }) = _ConversationParticipantModel;

  factory ConversationParticipantModel.fromJson(Map<String, dynamic> json) =>
      _$ConversationParticipantModelFromJson(json);
}

@freezed
class MessageModel with _$MessageModel {
  const factory MessageModel({
    required String id,
    required String conversationId,
    required String senderId,
    String? senderName,
    String? senderPhotoUrl,
    required String content,
    String? mediaUrl,
    @Default('None') String mediaType,
    @Default(false) bool isRead,
    DateTime? readAt,
    required DateTime sentAt,
  }) = _MessageModel;

  factory MessageModel.fromJson(Map<String, dynamic> json) =>
      _$MessageModelFromJson(json);
}

@freezed
class SendMessageRequest with _$SendMessageRequest {
  const factory SendMessageRequest({
    required String senderId,
    required String content,
    String? mediaUrl,
    String? mediaType,
  }) = _SendMessageRequest;

  factory SendMessageRequest.fromJson(Map<String, dynamic> json) =>
      _$SendMessageRequestFromJson(json);
}
