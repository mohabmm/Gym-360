import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';

class BroadcastScreen extends ConsumerStatefulWidget {
  const BroadcastScreen({super.key});

  @override
  ConsumerState<BroadcastScreen> createState() =>
      _BroadcastScreenState();
}

class _BroadcastScreenState extends ConsumerState<BroadcastScreen> {
  final _messageController = TextEditingController();
  bool _isSending = false;
  bool _sent = false;

  @override
  void dispose() {
    _messageController.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final content = _messageController.text.trim();
    if (content.isEmpty) return;

    setState(() => _isSending = true);
    try {
      final authState = ref.read(authProvider);
      final senderId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (senderId == null || gymId == null) {
        throw Exception('No authenticated sender or active gym');
      }

      final roster = await ref.read(clientRosterProvider.future);
      final recipientIds = roster.map((c) => c.user.id).toList();
      if (recipientIds.isEmpty) {
        throw Exception('No recipients available');
      }

      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.broadcast,
        data: {
          'gymId': gymId,
          'senderId': senderId,
          'content': content,
          'recipientIds': recipientIds,
        },
      );
      setState(() => _sent = true);
    } catch (_) {
      if (mounted) {
        final isArabic = Localizations.localeOf(context).languageCode == 'ar';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(isArabic
                ? 'تعذر إرسال الرسالة. يرجى المحاولة مرة أخرى.'
                : 'Could not send the message. Please try again.'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'رسالة جماعية' : 'Broadcast Message'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: _sent
              ? Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.check_circle,
                        color: AppColors.success, size: 72),
                    const SizedBox(height: 16),
                    Text(
                      isArabic ? 'تم ارسال الرسالة!' : 'Message sent!',
                      style: theme.textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      isArabic
                          ? 'تم ارسال رسالتك لجميع عملائك'
                          : 'Your message was sent to all your clients',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: AppColors.grey500,
                      ),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 24),
                    AppButton.outlined(
                      text: isArabic ? 'ارسال رسالة اخرى' : 'Send another',
                      onPressed: () => setState(() {
                        _sent = false;
                        _messageController.clear();
                      }),
                    ),
                  ],
                )
              : Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppColors.info.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.info_outline,
                              color: AppColors.info, size: 18),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              isArabic
                                  ? 'ستُرسل هذه الرسالة لجميع عملائك المسجلين'
                                  : 'This message will be sent to all your registered clients',
                              style: theme.textTheme.bodySmall,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      height: 220,
                      child: AppTextField(
                        label: isArabic ? 'الرسالة' : 'Message',
                        hint: isArabic
                            ? 'اكتب رسالتك هنا...'
                            : 'Type your message here...',
                        controller: _messageController,
                        maxLines: 10,
                      ),
                    ),
                    const SizedBox(height: 16),
                    AppButton(
                      text: isArabic
                          ? 'ارسال للجميع'
                          : 'Send to All Clients',
                      onPressed: _isSending ? null : _send,
                      isLoading: _isSending,
                    ),
                  ],
                ),
        ),
      ),
    );
  }
}
