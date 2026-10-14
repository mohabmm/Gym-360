import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/classes_provider.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';
import '../../../../core/error_text.dart';

class CreateClassScreen extends ConsumerStatefulWidget {
  const CreateClassScreen({super.key});

  @override
  ConsumerState<CreateClassScreen> createState() => _CreateClassScreenState();
}

class _CreateClassScreenState extends ConsumerState<CreateClassScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _descController = TextEditingController();
  final _capacityController = TextEditingController(text: '20');
  String? _selectedTrainerId;
  DateTime _startTime = DateTime.now().add(const Duration(hours: 1));
  DateTime _endTime = DateTime.now().add(const Duration(hours: 2));
  bool _loading = false;

  @override
  void dispose() {
    _nameController.dispose();
    _descController.dispose();
    _capacityController.dispose();
    super.dispose();
  }

  Future<void> _pickTime(bool isStart) async {
    final picked = await showDateTimePicker(context, isStart ? _startTime : _endTime);
    if (picked == null || !mounted) return;
    if (isStart) {
      setState(() {
        _startTime = picked;
        if (!_endTime.isAfter(_startTime)) {
          _endTime = _startTime.add(const Duration(hours: 1));
        }
      });
    } else {
      // Guard: the end time must be strictly after the start time. Without
      // this the negative difference was sent to the backend as
      // durationMinutes.
      if (!picked.isAfter(_startTime)) {
        final isArabic =
            Localizations.localeOf(context).languageCode == 'ar';
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'يجب أن يكون وقت الانتهاء بعد وقت البدء'
              : 'End time must be after start time'),
        ));
        return;
      }
      setState(() => _endTime = picked);
    }
  }

  Future<DateTime?> showDateTimePicker(BuildContext ctx, DateTime initial) async {
    final date = await showDatePicker(
      context: ctx,
      initialDate: initial,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date == null) return null;
    if (!mounted) return null;
    final time = await showTimePicker(
      context: ctx,
      initialTime: TimeOfDay.fromDateTime(initial),
    );
    if (time == null) return null;
    return DateTime(date.year, date.month, date.day, time.hour, time.minute);
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final gymId = ref.read(currentGymIdProvider);
      if (gymId.isEmpty) {
        throw Exception('No active gym selected');
      }
      if (_selectedTrainerId == null) {
        throw Exception(
          Localizations.localeOf(context).languageCode == 'ar'
              ? 'يرجى اختيار المدرب'
              : 'Please select a trainer',
        );
      }
      final trainerIdText = _selectedTrainerId!;
      final duration = _endTime.difference(_startTime).inMinutes;
      if (duration <= 0) {
        throw Exception(
          Localizations.localeOf(context).languageCode == 'ar'
              ? 'يجب أن يكون وقت الانتهاء بعد وقت البدء'
              : 'End time must be after start time',
        );
      }
      final startTime = TimeOfDay.fromDateTime(_startTime);
      final startStr =
          '${startTime.hour.toString().padLeft(2, '0')}:${startTime.minute.toString().padLeft(2, '0')}:00';
      final dayOfWeek = _startTime.weekday % 7; // Dart Mon=1..Sun=7 → .NET Sun=0..Sat=6

      final apiClient = ref.read(apiClientProvider);
      await apiClient.post<void>(
        ApiEndpoints.gymClasses(gymId),
        data: {
          'name': _nameController.text.trim(),
          'description': _descController.text.trim(),
          'trainerId': trainerIdText,
          'schedule': {
            'dayOfWeek': dayOfWeek,
            'startTime': startStr,
            'durationMinutes': duration,
            'isRecurring': false,
          },
          'capacity': int.tryParse(_capacityController.text) ?? 20,
          'waitlistEnabled': false,
        },
        parser: (_) {},
      );
      if (mounted) {
        ref.invalidate(classesProvider);
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final trainers = ref.watch(trainersProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'حصة جديدة' : 'New Class'),
      ),
      body: Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AppTextField(
                    controller: _nameController,
                    label: isArabic ? 'اسم الحصة' : 'Class Name',
                    hint: isArabic ? 'مثال: يوجا للمبتدئين' : 'e.g. Beginner Yoga',
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  trainers.when(
                    data: (items) => DropdownButtonFormField<String>(
                      value: _selectedTrainerId,
                      decoration: InputDecoration(
                        labelText: isArabic ? 'المدرب' : 'Instructor',
                      ),
                      hint: Text(isArabic ? 'اختر المدرب' : 'Choose trainer'),
                      items: items
                          .map((t) => DropdownMenuItem(
                                value: t.id,
                                child: Text('${t.firstName} ${t.lastName}'),
                              ))
                          .toList(),
                      validator: (v) =>
                          v == null ? (isArabic ? 'مطلوب' : 'Required') : null,
                      onChanged: (v) => setState(() => _selectedTrainerId = v),
                    ),
                    loading: () => const LinearProgressIndicator(),
                    error: (_, __) =>
                        Text(isArabic ? 'تعذر تحميل المدربين' : 'Failed to load trainers'),
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _capacityController,
                    label: isArabic ? 'السعة' : 'Capacity',
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      if (v == null || v.isEmpty) return isArabic ? 'مطلوب' : 'Required';
                      final n = int.tryParse(v);
                      if (n == null) return isArabic ? 'رقم غير صالح' : 'Invalid number';
                      if (n < 1) {
                        return isArabic
                            ? 'يجب أن تكون السعة 1 على الأقل'
                            : 'Capacity must be at least 1';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _descController,
                    label: isArabic ? 'الوصف (اختياري)' : 'Description (optional)',
                    maxLines: 3,
                  ),
                  const SizedBox(height: 16),
                  InkWell(
                    onTap: () => _pickTime(true),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: 8),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(isArabic ? 'وقت البدء' : 'Start Time',
                                    style: Theme.of(context).textTheme.bodyMedium),
                                Text(
                                  DateFormatter.formatDateTime(_startTime,
                                      locale: isArabic ? 'ar' : 'en'),
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.calendar_today),
                        ],
                      ),
                    ),
                  ),
                  const Divider(),
                  InkWell(
                    onTap: () => _pickTime(false),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(vertical: 8),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(isArabic ? 'وقت الانتهاء' : 'End Time',
                                    style: Theme.of(context).textTheme.bodyMedium),
                                Text(
                                  DateFormatter.formatDateTime(_endTime,
                                      locale: isArabic ? 'ar' : 'en'),
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            ),
                          ),
                          const Icon(Icons.calendar_today),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'اضافة الحصة' : 'Add Class',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}
