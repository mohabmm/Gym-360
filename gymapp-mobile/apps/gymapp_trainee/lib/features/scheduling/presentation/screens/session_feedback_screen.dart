import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/scheduling_providers.dart';

class SessionFeedbackScreen extends ConsumerStatefulWidget {
  final String sessionId;

  const SessionFeedbackScreen({super.key, required this.sessionId});

  @override
  ConsumerState<SessionFeedbackScreen> createState() =>
      _SessionFeedbackScreenState();
}

class _SessionFeedbackScreenState
    extends ConsumerState<SessionFeedbackScreen> {
  int _rating = 5;
  int _satisfaction = 4;
  int _rpe = 7;
  final _notesController = TextEditingController();
  bool _isSaving = false;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final asyncSession = ref.watch(sessionDetailProvider(widget.sessionId));
    final alreadyRated =
        asyncSession.valueOrNull?.memberLog != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقييم الجلسة' : 'Session Feedback'),
      ),
      body: alreadyRated
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  isArabic
                      ? 'تم تسجيل تقييمك مسبقاً ولا يمكن تعديله'
                      : 'You already submitted feedback for this session and it cannot be changed',
                  textAlign: TextAlign.center,
                ),
              ),
            )
          : SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Stars rating
            Text(
              isArabic ? 'تقييم المدرب' : 'Rate Your Trainer',
              style: Theme.of(context)
                  .textTheme
                  .titleSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(
                5,
                (index) => IconButton(
                  icon: Icon(
                    index < _rating ? Icons.star : Icons.star_border,
                    color: Colors.amber,
                    size: 36,
                  ),
                  onPressed: () => setState(() => _rating = index + 1),
                ),
              ),
            ),

            const SizedBox(height: 24),

            // Satisfaction slider
            Text(
              isArabic
                  ? 'الرضا عن الجلسة: $_satisfaction/5'
                  : 'Session Satisfaction: $_satisfaction/5',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 8),
            Slider(
              value: _satisfaction.toDouble(),
              min: 1,
              max: 5,
              divisions: 4,
              label: _satisfaction.toString(),
              onChanged: (v) => setState(() => _satisfaction = v.round()),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(isArabic ? 'غير راضٍ' : 'Poor',
                    style: Theme.of(context).textTheme.bodySmall),
                Text(isArabic ? 'ممتاز' : 'Excellent',
                    style: Theme.of(context).textTheme.bodySmall),
              ],
            ),

            const SizedBox(height: 24),

            // RPE slider
            Text(
              isArabic
                  ? 'مستوى الجهد (RPE): $_rpe/10'
                  : 'Effort Level (RPE): $_rpe/10',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 8),
            Slider(
              value: _rpe.toDouble(),
              min: 1,
              max: 10,
              divisions: 9,
              label: _rpe.toString(),
              onChanged: (v) => setState(() => _rpe = v.round()),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(isArabic ? 'سهل' : 'Easy',
                    style: Theme.of(context).textTheme.bodySmall),
                Text(isArabic ? 'شديد' : 'Max',
                    style: Theme.of(context).textTheme.bodySmall),
              ],
            ),

            const SizedBox(height: 24),

            AppTextField(
              label: isArabic ? 'ملاحظات (اختياري)' : 'Notes (optional)',
              controller: _notesController,
              maxLines: 4,
              hint: isArabic
                  ? 'كيف كانت الجلسة؟'
                  : 'How was the session?',
            ),

            const SizedBox(height: 32),

            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _isSaving ? null : _submit,
                child: _isSaving
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : Text(isArabic ? 'إرسال التقييم' : 'Submit Feedback'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    final already = ref.read(sessionDetailProvider(widget.sessionId)).valueOrNull?.memberLog != null;
    if (already) {
      if (mounted) context.pop();
      return;
    }
    setState(() => _isSaving = true);
    try {
      final repo = ref.read(schedulingRepositoryProvider);
      await repo.logMemberFeedback(widget.sessionId, {
        'rating': _rating,
        'satisfaction': _satisfaction,
        'perceivedExertion': _rpe,
        if (_notesController.text.isNotEmpty) 'notes': _notesController.text,
      });
      ref.invalidate(upcomingSessionsListProvider);
      ref.invalidate(mySessionsAllProvider);
      ref.invalidate(sessionDetailProvider(widget.sessionId));
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }
}
