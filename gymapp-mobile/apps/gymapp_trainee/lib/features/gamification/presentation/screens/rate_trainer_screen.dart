import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/gamification_providers.dart';

class RateTrainerScreen extends ConsumerStatefulWidget {
  final String sessionId;

  const RateTrainerScreen({super.key, required this.sessionId});

  @override
  ConsumerState<RateTrainerScreen> createState() =>
      _RateTrainerScreenState();
}

class _RateTrainerScreenState extends ConsumerState<RateTrainerScreen> {
  int _stars = 5;
  final _commentController = TextEditingController();
  bool _isSaving = false;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final authState = ref.watch(authProvider);
    final trainerId = authState.valueOrNull?.user?.gymMemberships
        ?.firstOrNull?.assignedTrainerId;

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تقييم المدرب' : 'Rate Trainer'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            const SizedBox(height: 16),
            Text(
              isArabic
                  ? 'كيف كانت تجربتك مع المدرب؟'
                  : 'How was your experience with the trainer?',
              style: Theme.of(context).textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),

            // Stars
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(
                5,
                (index) => GestureDetector(
                  onTap: () => setState(() => _stars = index + 1),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 6),
                    child: Icon(
                      index < _stars ? Icons.star : Icons.star_border,
                      color: Colors.amber,
                      size: 44,
                    ),
                  ),
                ),
              ),
            ),

            const SizedBox(height: 8),
            Text(
              _starLabel(_stars, isArabic),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Colors.amber,
                    fontWeight: FontWeight.w600,
                  ),
            ),

            const SizedBox(height: 24),

            AppTextField(
              label: isArabic
                  ? 'تعليق (اختياري)'
                  : 'Comment (optional)',
              controller: _commentController,
              maxLines: 4,
              hint: isArabic
                  ? 'شارك تجربتك مع الآخرين'
                  : 'Share your experience',
            ),

            const Spacer(),

            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: (_isSaving || trainerId == null)
                    ? null
                    : () => _submit(trainerId),
                child: _isSaving
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : Text(isArabic ? 'إرسال التقييم' : 'Submit Rating'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _starLabel(int stars, bool isArabic) {
    final labels = isArabic
        ? ['رديء', 'مقبول', 'جيد', 'جيد جداً', 'ممتاز']
        : ['Poor', 'Fair', 'Good', 'Very Good', 'Excellent'];
    return labels[stars - 1];
  }

  Future<void> _submit(String trainerId) async {
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final gymId = authState.valueOrNull?.currentGymId;
      final clientId = authState.valueOrNull?.user?.id;
      if (gymId == null || clientId == null) {
        throw Exception('No active gym or user');
      }
      final repo = ref.read(gamificationRepositoryProvider);
      await repo.rateTrainer(RateTrainerRequest(
        gymId: gymId,
        trainerId: trainerId,
        clientId: clientId,
        sessionId: widget.sessionId,
        stars: _stars,
        comment: _commentController.text.isEmpty
            ? null
            : _commentController.text,
      ));
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
