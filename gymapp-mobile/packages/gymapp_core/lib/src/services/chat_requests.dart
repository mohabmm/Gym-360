/// The two parties of a direct conversation within a gym.
class ConversationParticipants {
  const ConversationParticipants({
    required this.gymId,
    required this.participant2,
  });

  final String gymId;
  final String participant2;

  Map<String, dynamic> toJson() => {
        'gymId': gymId,
        'participant2': participant2,
      };
}

/// A chat message to send, with optional media.
class MessageDraft {
  const MessageDraft(this.content, {this.mediaUrl, this.mediaType});

  final String content;
  final String? mediaUrl;
  final String? mediaType;

  Map<String, dynamic> toJson() => {
        'content': content,
        if (mediaUrl != null) 'mediaUrl': mediaUrl,
        if (mediaType != null) 'mediaType': mediaType,
      };
}
