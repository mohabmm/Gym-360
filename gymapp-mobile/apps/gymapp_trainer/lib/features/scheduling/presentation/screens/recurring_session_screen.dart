import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';

class RecurringSessionScreen extends ConsumerStatefulWidget {
  const RecurringSessionScreen({super.key});

  @override
  ConsumerState<RecurringSessionScreen> createState() =>
      _RecurringSessionScreenState();
}

class _RecurringSessionScreenState
    extends ConsumerState<RecurringSessionScreen> {
  String? _selectedClientId;
  int _selectedDayOfWeek = 1;
  TimeOfDay _selectedTime = const TimeOfDay(hour: 10, minute: 0);
  int _durationMinutes = 60;
  int _recurrenceCount = 4;
  bool _isSaving = false;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final clientsAsync = ref.watch(clientRosterProvider);

    final dayNames = isArabic
        ? ['الاحد', 'الاثنين', 'الثلاثاء', 'الاربعاء', 'الخميس', 'الجمعة', 'السبت']
        : ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    return Scaffold(
      appBar: AppBar(
        title:
            Text(isArabic ? 'جلسات متكررة' : 'Recurring Sessions'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(
            isArabic ? 'اختر العميل' : 'Select Client',
            style: theme.textTheme.titleSmall,
          ),
          const SizedBox(height: 8),
          clientsAsync.when(
            data: (clients) => DropdownButtonFormField<String>(
              value: _selectedClientId,
              hint: Text(isArabic ? 'اختر عميلا' : 'Select a client'),
              items: clients
                  .map((c) => DropdownMenuItem(
                        value: c.user.id,
                        child: Text(
                            '${c.user.firstName} ${c.user.lastName}'),
                      ))
                  .toList(),
              onChanged: (v) => setState(() => _selectedClientId = v),
              decoration:
                  const InputDecoration(border: OutlineInputBorder()),
            ),
            loading: () => const LinearProgressIndicator(),
            error: (_, __) => const SizedBox.shrink(),
          ),
          const SizedBox(height: 16),

          Text(
            isArabic ? 'يوم الاسبوع' : 'Day of Week',
            style: theme.textTheme.titleSmall,
          ),
          const SizedBox(height: 8),
          DropdownButtonFormField<int>(
            value: _selectedDayOfWeek,
            items: List.generate(
              7,
              (i) => DropdownMenuItem(
                value: i,
                child: Text(dayNames[i]),
              ),
            ),
            onChanged: (v) =>
                setState(() => _selectedDayOfWeek = v ?? 1),
            decoration:
                const InputDecoration(border: OutlineInputBorder()),
          ),
          const SizedBox(height: 16),

          ListTile(
            contentPadding: EdgeInsets.zero,
            title: Text(isArabic ? 'الوقت' : 'Time',
                style: theme.textTheme.titleSmall),
            subtitle: Text(_selectedTime.format(context)),
            trailing: const Icon(Icons.access_time),
            onTap: () async {
              final t = await showTimePicker(
                context: context,
                initialTime: _selectedTime,
              );
              if (t != null) setState(() => _selectedTime = t);
            },
          ),
          const Divider(),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(isArabic ? 'المدة (دقائق)' : 'Duration (min)',
                  style: theme.textTheme.titleSmall),
              Row(children: [
                IconButton(
                  icon: const Icon(Icons.remove_circle_outline),
                  onPressed: _durationMinutes > 30
                      ? () =>
                          setState(() => _durationMinutes -= 15)
                      : null,
                ),
                Text('$_durationMinutes',
                    style: theme.textTheme.titleSmall),
                IconButton(
                  icon: const Icon(Icons.add_circle_outline),
                  onPressed: () =>
                      setState(() => _durationMinutes += 15),
                ),
              ]),
            ],
          ),
          const Divider(),

          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(isArabic ? 'عدد الجلسات' : 'Session Count',
                  style: theme.textTheme.titleSmall),
              Row(children: [
                IconButton(
                  icon: const Icon(Icons.remove_circle_outline),
                  onPressed: _recurrenceCount > 1
                      ? () =>
                          setState(() => _recurrenceCount--)
                      : null,
                ),
                Text('$_recurrenceCount',
                    style: theme.textTheme.titleSmall),
                IconButton(
                  icon: const Icon(Icons.add_circle_outline),
                  // BE caps WeeksAhead at 12 (rejects more with a 400); keep
                  // the stepper in range so the trainer never hits that error.
                  onPressed: _recurrenceCount < 12
                      ? () => setState(() => _recurrenceCount++)
                      : null,
                ),
              ]),
            ],
          ),

          const SizedBox(height: 32),
          AppButton(
            text: isArabic ? 'انشاء الجلسات' : 'Create Sessions',
            onPressed:
                (_isSaving || _selectedClientId == null) ? null : _save,
            isLoading: _isSaving,
          ),
        ],
      ),
    );
  }

  Future<void> _save() async {
    if (_selectedClientId == null) return;
    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }

      final apiClient = ref.read(apiClientProvider);
      await apiClient.postVoid(
        ApiEndpoints.recurringSessions,
        data: {
          'gymId': gymId,
          'trainerId': trainerId,
          'memberId': _selectedClientId,
          'dayOfWeek': _selectedDayOfWeek,
          'time':
              '${_selectedTime.hour.toString().padLeft(2, '0')}:${_selectedTime.minute.toString().padLeft(2, '0')}:00',
          'durationMinutes': _durationMinutes,
          'sessionType': 'PersonalTraining',
          'weeksAhead': _recurrenceCount,
        },
      );
      if (mounted) Navigator.of(context).pop();
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
