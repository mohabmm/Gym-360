import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/client_providers.dart';

class ClientNotesScreen extends ConsumerStatefulWidget {
  final String clientId;

  const ClientNotesScreen({super.key, required this.clientId});

  @override
  ConsumerState<ClientNotesScreen> createState() => _ClientNotesScreenState();
}

class _ClientNotesScreenState extends ConsumerState<ClientNotesScreen> {
  final _noteController = TextEditingController();
  bool _isSaving = false;

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _addNote() async {
    final content = _noteController.text.trim();
    if (content.isEmpty) return;

    setState(() => _isSaving = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(activeGymIdProvider);
      if (gymId == null) {
        throw ApiException(
          message: 'No active gym selected.',
          errorCode: 'NO_ACTIVE_GYM',
        );
      }
      await apiClient.postVoid(
        ApiEndpoints.gymMemberNotes(gymId, widget.clientId),
        data: {'content': content},
      );
      _noteController.clear();
      ref.invalidate(clientDetailProvider(widget.clientId));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final clientAsync = ref.watch(clientDetailProvider(widget.clientId));

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ملاحظات العميل' : 'Client Notes'),
      ),
      body: Column(
        children: [
          // Add note area
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Expanded(
                  child: AppTextField(
                    label: isArabic ? 'ملاحظة جديدة' : 'New Note',
                    hint: isArabic
                        ? 'اضف ملاحظة خاصة عن هذا العميل...'
                        : 'Add a private note about this client...',
                    controller: _noteController,
                    maxLines: 3,
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filled(
                  onPressed: _isSaving ? null : _addNote,
                  icon: _isSaving
                      ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Icon(Icons.send),
                ),
              ],
            ),
          ),
          const Divider(height: 1),

          // Notes list
          Expanded(
            child: clientAsync.when(
              data: (data) {
                if (data.notes.isEmpty) {
                  return EmptyState(
                    icon: Icons.note_outlined,
                    title:
                        isArabic ? 'لا توجد ملاحظات' : 'No notes yet',
                    subtitle: isArabic
                        ? 'اضف اول ملاحظة باستخدام الحقل اعلاه'
                        : 'Add your first note using the field above',
                  );
                }
                return ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: data.notes.length,
                  itemBuilder: (context, index) {
                    final note = data.notes[index];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              note.content,
                              style: Theme.of(context).textTheme.bodyMedium,
                            ),
                            const SizedBox(height: 8),
                            Text(
                              DateFormat('dd/MM/yyyy hh:mm a', isArabic ? 'ar' : 'en')
                                  .format(note.createdAt),
                              style:
                                  Theme.of(context).textTheme.bodySmall?.copyWith(
                                        color: AppColors.grey400,
                                      ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
              loading: () =>
                  const Center(child: CircularProgressIndicator()),
              error: (e, _) => AppErrorWidget(
                message: e.toString(),
                onRetry: () =>
                    ref.invalidate(clientDetailProvider(widget.clientId)),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
