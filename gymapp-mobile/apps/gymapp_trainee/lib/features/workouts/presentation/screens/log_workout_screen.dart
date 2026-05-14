import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../providers/workouts_providers.dart';

class LogWorkoutScreen extends ConsumerStatefulWidget {
  const LogWorkoutScreen({super.key});

  @override
  ConsumerState<LogWorkoutScreen> createState() => _LogWorkoutScreenState();
}

class _LogWorkoutScreenState extends ConsumerState<LogWorkoutScreen> {
  String? _selectedWorkoutId;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final workoutsAsync = ref.watch(workoutsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ابدأ التمرين' : 'Start Workout'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isArabic
                  ? 'اختر نموذج تمرين أو سجّل تمرينًا حرًا'
                  : 'Pick a workout template or log ad-hoc',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 16),
            Text(
              isArabic ? 'نموذج التمرين' : 'Workout template',
              style: Theme.of(context).textTheme.titleSmall,
            ),
            const SizedBox(height: 8),
            workoutsAsync.when(
              data: (workouts) => DropdownButtonFormField<String>(
                value: _selectedWorkoutId,
                decoration: InputDecoration(
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  hintText: isArabic ? 'تمرين حر' : 'Ad-hoc (no template)',
                ),
                items: [
                  DropdownMenuItem(
                    value: null,
                    child: Text(isArabic ? 'تمرين حر' : 'Ad-hoc workout'),
                  ),
                  ...workouts.map(
                    (w) => DropdownMenuItem(
                      value: w.id,
                      child: Text(
                        isArabic && w.nameAr != null ? w.nameAr! : w.name,
                      ),
                    ),
                  ),
                ],
                onChanged: (value) {
                  setState(() => _selectedWorkoutId = value);
                },
              ),
              loading: () => const LinearProgressIndicator(),
              error: (_, __) => const SizedBox.shrink(),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _start,
                child: Text(isArabic ? 'ابدأ' : 'Start'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _start() {
    if (_selectedWorkoutId != null) {
      context.push('/workouts/guided?workoutId=$_selectedWorkoutId');
    } else {
      context.push('/workouts/ad-hoc');
    }
  }
}
