import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../../core/router.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../providers/session_providers.dart';

class PostSessionLogScreen extends ConsumerStatefulWidget {
  final String sessionId;
  final String? initialNotes;

  const PostSessionLogScreen({
    super.key,
    required this.sessionId,
    this.initialNotes,
  });

  @override
  ConsumerState<PostSessionLogScreen> createState() =>
      _PostSessionLogScreenState();
}

class _PostSessionLogScreenState
    extends ConsumerState<PostSessionLogScreen> {
  int _trainerRating = 5;
  late final TextEditingController _trainerNotesController =
      TextEditingController(text: widget.initialNotes ?? '');
  final List<_ExerciseRow> _exercises = <_ExerciseRow>[];
  bool _isSubmitting = false;

  @override
  void dispose() {
    _trainerNotesController.dispose();
    for (final row in _exercises) {
      row.dispose();
    }
    super.dispose();
  }

  void _addExercise() {
    setState(() => _exercises.add(_ExerciseRow()));
  }

  void _removeExercise(int index) {
    setState(() {
      _exercises[index].dispose();
      _exercises.removeAt(index);
    });
  }

  Future<void> _submit() async {
    setState(() => _isSubmitting = true);
    try {
      final trainerId = ref.read(authProvider).user?.id;
      if (trainerId == null) {
        throw Exception('No authenticated trainer');
      }
      final notes = _trainerNotesController.text.trim();
      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.sessionTrainerLog(widget.sessionId),
        data: {
          'trainerId': trainerId,
          'exercisesPerformed': notes.isEmpty ? '-' : notes,
          'clientEffortRating': _trainerRating,
          'nextSessionFocus': notes.isEmpty ? null : notes,
          'attendanceConfirmed': true,
        },
      );

      final items = _exercises
          .where((e) => e.nameController.text.trim().isNotEmpty)
          .toList();
      if (items.isNotEmpty) {
        final payload = <Map<String, dynamic>>[];
        for (var i = 0; i < items.length; i++) {
          final row = items[i];
          payload.add({
            'exerciseName': row.nameController.text.trim(),
            'orderIndex': i,
            'sets': int.tryParse(row.setsController.text.trim()) ?? 1,
            'reps': row.repsController.text.trim().isEmpty
                ? '1'
                : row.repsController.text.trim(),
            'weight': double.tryParse(row.weightController.text.trim()),
            'notes': row.notesController.text.trim().isEmpty
                ? null
                : row.notesController.text.trim(),
          });
        }
        await apiClient.postVoid(
          ApiEndpoints.sessionExerciseLogs(widget.sessionId),
          data: {'items': payload},
        );
      }

      if (mounted) context.go(AppRoutes.home);
    } catch (e) {
      if (mounted) {
        setState(() => _isSubmitting = false);
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final sessionAsync =
        ref.watch(sessionDetailProvider(widget.sessionId));

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقرير الجلسة' : 'Session Summary'),
        automaticallyImplyLeading: false,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: AppColors.success.withOpacity(0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Column(
              children: [
                const Icon(Icons.check_circle,
                    color: AppColors.success, size: 48),
                const SizedBox(height: 8),
                Text(
                  isArabic ? 'تمت الجلسة بنجاح!' : 'Session completed!',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w700,
                    color: AppColors.success,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),

          sessionAsync.when(
            data: (session) {
              final names = ref.watch(clientNameByIdProvider);
              final clientName = names[session.memberId] ??
                  (isArabic ? 'عميل' : 'Client');
              return Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isArabic ? 'ملخص الجلسة' : 'Session Summary',
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w600),
                      ),
                      const SizedBox(height: 12),
                      _SummaryRow(
                        icon: Icons.person,
                        label: isArabic ? 'العميل' : 'Client',
                        value: clientName,
                      ),
                      _SummaryRow(
                        icon: Icons.access_time,
                        label: isArabic ? 'الوقت' : 'Time',
                        value: DateFormat('hh:mm a', isArabic ? 'ar' : 'en')
                            .format(session.scheduledAt),
                      ),
                      _SummaryRow(
                        icon: Icons.timer,
                        label: isArabic ? 'المدة' : 'Duration',
                        value:
                            '${session.durationMinutes} ${isArabic ? 'دقيقة' : 'min'}',
                      ),
                    ],
                  ),
                ),
              );
            },
            loading: () => const ShimmerCard(height: 150),
            error: (_, __) => const SizedBox.shrink(),
          ),
          const SizedBox(height: 16),

          // Per-exercise log
          Row(
            children: [
              Expanded(
                child: Text(
                  isArabic ? 'سجل التمارين' : 'Exercise log',
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
              ),
              TextButton.icon(
                onPressed: _addExercise,
                icon: const Icon(Icons.add),
                label: Text(isArabic ? 'تمرين' : 'Exercise'),
              ),
            ],
          ),
          if (_exercises.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Text(
                isArabic
                    ? 'اضغط على «+ تمرين» لإضافة تمارين الجلسة (اختياري)'
                    : 'Tap "+ Exercise" to record per-exercise actuals (optional)',
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.hintColor),
              ),
            ),
          for (var i = 0; i < _exercises.length; i++) ...[
            _ExerciseEditor(
              row: _exercises[i],
              index: i,
              isArabic: isArabic,
              onRemove: () => _removeExercise(i),
            ),
            const SizedBox(height: 8),
          ],

          const SizedBox(height: 16),

          // Trainer notes for next time
          Text(
            isArabic ? 'ملاحظات للجلسة القادمة' : 'Notes for Next Session',
            style: theme.textTheme.titleMedium
                ?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          AppTextField(
            label: isArabic ? 'ملاحظات' : 'Notes',
            hint: isArabic
                ? 'ما الذي تريد التركيز عليه في الجلسة القادمة؟'
                : 'What to focus on next time?',
            controller: _trainerNotesController,
            maxLines: 4,
          ),
          const SizedBox(height: 24),

          AppButton(
            text: isArabic ? 'اغلاق الجلسة' : 'Close Session',
            onPressed: _isSubmitting ? null : _submit,
            isLoading: _isSubmitting,
          ),
          const SizedBox(height: 12),
          AppButton.outlined(
            text: isArabic ? 'العودة للرئيسية' : 'Go to Dashboard',
            onPressed: () => context.go(AppRoutes.home),
          ),
        ],
      ),
    );
  }
}

class _ExerciseRow {
  final TextEditingController nameController = TextEditingController();
  final TextEditingController setsController =
      TextEditingController(text: '3');
  final TextEditingController repsController =
      TextEditingController(text: '10');
  final TextEditingController weightController = TextEditingController();
  final TextEditingController notesController = TextEditingController();

  void dispose() {
    nameController.dispose();
    setsController.dispose();
    repsController.dispose();
    weightController.dispose();
    notesController.dispose();
  }
}

class _ExerciseEditor extends StatelessWidget {
  final _ExerciseRow row;
  final int index;
  final bool isArabic;
  final VoidCallback onRemove;

  const _ExerciseEditor({
    required this.row,
    required this.index,
    required this.isArabic,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 12,
                backgroundColor: theme.colorScheme.primary,
                child: Text(
                  '${index + 1}',
                  style: theme.textTheme.labelSmall
                      ?.copyWith(color: theme.colorScheme.onPrimary),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  controller: row.nameController,
                  decoration: InputDecoration(
                    isDense: true,
                    border: const OutlineInputBorder(),
                    labelText:
                        isArabic ? 'اسم التمرين' : 'Exercise name',
                  ),
                ),
              ),
              IconButton(
                icon: const Icon(Icons.close),
                color: theme.colorScheme.error,
                tooltip: isArabic ? 'حذف' : 'Remove',
                onPressed: onRemove,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: row.setsController,
                  keyboardType: TextInputType.number,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  decoration: InputDecoration(
                    isDense: true,
                    border: const OutlineInputBorder(),
                    labelText: isArabic ? 'المجموعات' : 'Sets',
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  controller: row.repsController,
                  decoration: InputDecoration(
                    isDense: true,
                    border: const OutlineInputBorder(),
                    labelText: isArabic ? 'التكرارات' : 'Reps',
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  controller: row.weightController,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  decoration: InputDecoration(
                    isDense: true,
                    border: const OutlineInputBorder(),
                    labelText: isArabic ? 'الوزن (كجم)' : 'Weight (kg)',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          TextField(
            controller: row.notesController,
            maxLines: 2,
            decoration: InputDecoration(
              isDense: true,
              border: const OutlineInputBorder(),
              labelText: isArabic ? 'ملاحظات' : 'Notes',
            ),
          ),
        ],
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _SummaryRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Icon(icon, size: 16, color: AppColors.grey400),
          const SizedBox(width: 8),
          Text(
            '$label: ',
            style: theme.textTheme.bodySmall
                ?.copyWith(color: AppColors.grey500),
          ),
          Expanded(
            child: Text(
              value,
              style: theme.textTheme.bodySmall
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}
