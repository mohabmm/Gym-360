import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/scheduling_providers.dart';
import 'availability_parser.dart';

class AvailabilityScreen extends ConsumerStatefulWidget {
  const AvailabilityScreen({super.key});

  @override
  ConsumerState<AvailabilityScreen> createState() =>
      _AvailabilityScreenState();
}

class _AvailabilityScreenState extends ConsumerState<AvailabilityScreen> {
  // weekday (0=Sunday … 6=Saturday) -> list of (start, end) time slots
  final Map<int, List<_TimeSlot>> _slots = {
    for (int i = 0; i < 7; i++) i: [],
  };
  final List<DateTime> _blockedDates = [];
  bool _isSaving = false;
  bool _isHydrating = true;
  Object? _hydrationError;
  int _sessionDurationMinutes = 60;
  int _bufferMinutes = 15;

  static const List<int> _durationOptions = [15, 30, 45, 60, 75, 90, 120];
  static const List<int> _bufferOptions = [0, 5, 10, 15, 20, 30];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _tryHydrate());
  }

  // Why retry: activeGymIdProvider hydrates from secure storage asynchronously.
  // If the user reaches this screen before that future resolves, ref.read
  // returns null once and we'd silently render an empty form. Poll up to
  // ~3 seconds for both trainerId and gymId to become available before
  // giving up.
  int _hydrateAttempts = 0;
  static const int _maxHydrateAttempts = 15;
  static const Duration _hydrateRetryDelay = Duration(milliseconds: 200);

  Future<void> _tryHydrate() async {
    if (!mounted) return;
    final trainerId = ref.read(authProvider).user?.id;
    final gymId = ref.read(activeGymIdProvider);
    if (trainerId == null || gymId == null) {
      _hydrateAttempts++;
      if (_hydrateAttempts >= _maxHydrateAttempts) {
        if (mounted) setState(() => _isHydrating = false);
        return;
      }
      await Future.delayed(_hydrateRetryDelay);
      return _tryHydrate();
    }
    await _hydrate(trainerId, gymId);
  }

  Future<void> _hydrate(String trainerId, String gymId) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      final raw = await apiClient.get<Map<String, dynamic>>(
        ApiEndpoints.trainerAvailability,
        queryParameters: {'trainerId': trainerId, 'gymId': gymId},
        // Be permissive: Dio decodes JSON into Map<String, dynamic> but the
        // previous narrower `is Map<String, dynamic>` check failed in the
        // field, leaving the form blank. `is Map` + from(...) handles
        // whatever concrete Map type the codec produces.
        parser: (data) => data is Map
            ? Map<String, dynamic>.from(data)
            : <String, dynamic>{},
      );
      if (!mounted) return;
      setState(() {
        _sessionDurationMinutes =
            (raw['sessionDurationMinutes'] as num?)?.toInt() ?? 60;
        _bufferMinutes = (raw['bufferMinutes'] as num?)?.toInt() ?? 15;
        // Snap to picker options if BE returned a custom value.
        if (!_durationOptions.contains(_sessionDurationMinutes)) {
          _durationOptions
              .reduce((a, b) => (a - _sessionDurationMinutes).abs() <
                      (b - _sessionDurationMinutes).abs()
                  ? a
                  : b);
        }
        for (final list in _slots.values) {
          list.clear();
        }
        final weekly = raw['weeklySlots'];
        if (weekly is List) {
          for (final entry in weekly) {
            if (entry is! Map) continue;
            final dayIndex =
                AvailabilityParser.parseDayOfWeek(entry['dayOfWeek']);
            if (dayIndex == null) continue;
            final start = AvailabilityParser.parseTime(entry['startTime']);
            final end = AvailabilityParser.parseTime(entry['endTime']);
            if (start == null || end == null) continue;
            _slots[dayIndex]!
                .add(_TimeSlot(startTime: start, endTime: end));
          }
        }
        _blockedDates.clear();
        final blocked = raw['blockedDates'];
        if (blocked is List) {
          for (final entry in blocked) {
            String? dateStr;
            if (entry is Map) {
              dateStr = (entry['date'] ?? entry['Date'])?.toString();
            } else if (entry is String) {
              dateStr = entry;
            }
            if (dateStr == null) continue;
            final parsed = DateTime.tryParse(dateStr);
            if (parsed != null) _blockedDates.add(parsed);
          }
          _blockedDates.sort();
        }
        _isHydrating = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _hydrationError = e;
        _isHydrating = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);

    final dayNames = isArabic
        ? ['الاحد', 'الاثنين', 'الثلاثاء', 'الاربعاء', 'الخميس', 'الجمعة', 'السبت']
        : ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اوقات التوفر' : 'Availability'),
        actions: [
          if (_isSaving || _isHydrating)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2)),
            )
          else
            TextButton(
              onPressed: _save,
              child: Text(isArabic ? 'حفظ' : 'Save'),
            ),
        ],
      ),
      body: _isHydrating
          ? const Center(child: CircularProgressIndicator())
          : _hydrationError != null
              ? AppErrorWidget(
                  message: _hydrationError.toString(),
                  onRetry: () {
                    setState(() {
                      _isHydrating = true;
                      _hydrationError = null;
                      _hydrateAttempts = 0;
                    });
                    _tryHydrate();
                  },
                )
              : _buildForm(context, isArabic, theme, dayNames),
    );
  }

  Widget _buildForm(BuildContext context, bool isArabic, ThemeData theme,
      List<String> dayNames) {
    return RefreshIndicator(
        onRefresh: () async => ref.invalidate(availabilitySlotsProvider),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _buildSessionSettingsCard(theme, isArabic),
            const SizedBox(height: 16),
            Text(
              isArabic
                  ? 'حدد اوقات توفرك الاسبوعية'
                  : 'Set your weekly availability',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: AppColors.grey500,
              ),
            ),
            const SizedBox(height: 8),
            ...List.generate(7, (dayIndex) {
              final slots = _slots[dayIndex]!;
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment:
                            MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            dayNames[dayIndex],
                            style: theme.textTheme.titleSmall?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          TextButton.icon(
                            onPressed: () =>
                                _addSlot(context, dayIndex, isArabic),
                            icon: const Icon(Icons.add, size: 16),
                            label: Text(
                                isArabic ? 'اضافة' : 'Add'),
                          ),
                        ],
                      ),
                      if (slots.isEmpty)
                        Text(
                          isArabic ? 'غير متاح' : 'Not available',
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: AppColors.grey400,
                          ),
                        )
                      else
                        ...slots.asMap().entries.map((entry) {
                          final slot = entry.value;
                          return Row(
                            children: [
                              Icon(Icons.access_time,
                                  size: 16, color: AppColors.grey400),
                              const SizedBox(width: 4),
                              Text(
                                '${slot.startTime.format(context)} - ${slot.endTime.format(context)}',
                                style: theme.textTheme.bodySmall,
                              ),
                              const Spacer(),
                              IconButton(
                                icon: const Icon(Icons.close,
                                    size: 16, color: AppColors.error),
                                onPressed: () {
                                  setState(() =>
                                      _slots[dayIndex]!.removeAt(entry.key));
                                },
                                padding: EdgeInsets.zero,
                                constraints: const BoxConstraints(),
                              ),
                            ],
                          );
                        }),
                    ],
                  ),
                ),
              );
            }),
            const SizedBox(height: 16),
            _buildBlockedDatesCard(theme, isArabic),
            const SizedBox(height: 24),
            AppButton(
              text: isArabic ? 'حفظ الاوقات' : 'Save Availability',
              onPressed: _isSaving ? null : _save,
              isLoading: _isSaving,
            ),
          ],
        ),
      );
  }

  Widget _buildSessionSettingsCard(ThemeData theme, bool isArabic) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isArabic ? 'اعدادات الجلسة' : 'Session settings',
              style: theme.textTheme.titleSmall
                  ?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: _LabeledDropdown<int>(
                    label: isArabic ? 'مدة الجلسة' : 'Session length',
                    value: _sessionDurationMinutes,
                    items: _durationOptions
                        .map((m) => DropdownMenuItem<int>(
                              value: m,
                              child: Text(isArabic ? '$m د' : '$m min'),
                            ))
                        .toList(),
                    onChanged: (v) =>
                        setState(() => _sessionDurationMinutes = v ?? 60),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _LabeledDropdown<int>(
                    label: isArabic ? 'فاصل بين الجلسات' : 'Buffer',
                    value: _bufferMinutes,
                    items: _bufferOptions
                        .map((m) => DropdownMenuItem<int>(
                              value: m,
                              child: Text(isArabic ? '$m د' : '$m min'),
                            ))
                        .toList(),
                    onChanged: (v) =>
                        setState(() => _bufferMinutes = v ?? 15),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBlockedDatesCard(ThemeData theme, bool isArabic) {
    final fmt = DateFormat('dd/MM/yyyy', isArabic ? 'ar' : 'en');
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  isArabic ? 'الاجازات وايام التوقف' : 'Time off',
                  style: theme.textTheme.titleSmall
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
                TextButton.icon(
                  onPressed: () => _addBlockedDates(context, isArabic),
                  icon: const Icon(Icons.event_busy, size: 16),
                  label: Text(isArabic ? 'اضافة اجازة' : 'Add leave'),
                ),
              ],
            ),
            if (_blockedDates.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  isArabic
                      ? 'لا توجد ايام محجوبة'
                      : 'No blocked dates',
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: AppColors.grey400),
                ),
              )
            else
              ..._blockedDates.map((date) => Row(
                    children: [
                      Icon(Icons.event_busy,
                          size: 16, color: AppColors.grey500),
                      const SizedBox(width: 6),
                      Text(fmt.format(date),
                          style: theme.textTheme.bodySmall),
                      const Spacer(),
                      IconButton(
                        icon: const Icon(Icons.close,
                            size: 16, color: AppColors.error),
                        onPressed: () => _removeBlockedDate(date, isArabic),
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(),
                      ),
                    ],
                  )),
          ],
        ),
      ),
    );
  }

  Future<void> _addBlockedDates(BuildContext context, bool isArabic) async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: now.add(const Duration(days: 365)),
      helpText: isArabic ? 'اختر فترة الاجازة' : 'Select leave period',
    );
    if (picked == null) return;

    final trainerId = ref.read(authProvider).user?.id;
    final gymId = ref.read(activeGymIdProvider);
    if (trainerId == null || gymId == null) return;
    final apiClient = ref.read(apiClientProvider);

    setState(() => _isSaving = true);
    try {
      for (var day = picked.start;
          !day.isAfter(picked.end);
          day = day.add(const Duration(days: 1))) {
        // Skip duplicates so re-picking an existing range is a no-op rather
        // than a bunch of "DateAlreadyBlocked" 400s.
        if (_blockedDates.any((d) =>
            d.year == day.year && d.month == day.month && d.day == day.day)) {
          continue;
        }
        final dateStr = day.toIso8601String().split('T')[0];
        await apiClient.postVoid(
          ApiEndpoints.blockDate,
          data: {
            'gymId': gymId,
            'date': dateStr,
            'autoNotifyClients': true,
          },
        );
        _blockedDates.add(DateTime(day.year, day.month, day.day));
      }
      _blockedDates.sort();
      setState(() {});
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException ? e.message : e.toString();
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  Future<void> _removeBlockedDate(DateTime date, bool isArabic) async {
    final gymId = ref.read(activeGymIdProvider);
    if (gymId == null) return;
    final apiClient = ref.read(apiClientProvider);

    setState(() => _isSaving = true);
    try {
      final dateStr = date.toIso8601String().split('T')[0];
      await apiClient.postVoid(
        ApiEndpoints.unblockDate,
        data: {'gymId': gymId, 'date': dateStr},
      );
      _blockedDates.removeWhere((d) =>
          d.year == date.year && d.month == date.month && d.day == date.day);
      setState(() {});
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException ? e.message : e.toString();
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  Future<void> _addSlot(
      BuildContext context, int dayIndex, bool isArabic) async {
    final startTime = await showTimePicker(
      context: context,
      initialTime: const TimeOfDay(hour: 9, minute: 0),
      helpText: isArabic ? 'وقت البداية' : 'Start time',
    );
    if (startTime == null || !mounted) return;

    final endTime = await showTimePicker(
      context: context,
      initialTime: TimeOfDay(hour: startTime.hour + 1, minute: 0),
      helpText: isArabic ? 'وقت النهاية' : 'End time',
    );
    if (endTime == null) return;

    if (!_isAfter(endTime, startTime)) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'وقت النهاية يجب ان يكون بعد البداية'
              : 'End time must be after start time'),
        ));
      }
      return;
    }

    final newSlot = _TimeSlot(startTime: startTime, endTime: endTime);
    if (_overlapsExisting(dayIndex, newSlot)) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(isArabic
              ? 'يتداخل مع وقت اخر في نفس اليوم'
              : 'Slot overlaps another on the same day'),
        ));
      }
      return;
    }

    setState(() => _slots[dayIndex]!.add(newSlot));
  }

  bool _isAfter(TimeOfDay a, TimeOfDay b) =>
      a.hour > b.hour || (a.hour == b.hour && a.minute > b.minute);

  bool _overlapsExisting(int dayIndex, _TimeSlot s) {
    int toMin(TimeOfDay t) => t.hour * 60 + t.minute;
    final start = toMin(s.startTime);
    final end = toMin(s.endTime);
    for (final existing in _slots[dayIndex]!) {
      final es = toMin(existing.startTime);
      final ee = toMin(existing.endTime);
      if (start < ee && es < end) return true;
    }
    return false;
  }

  Future<void> _save() async {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final hasAnySlot = _slots.values.any((l) => l.isNotEmpty);
    if (!hasAnySlot) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(isArabic
            ? 'اضف وقت واحد على الاقل قبل الحفظ'
            : 'Add at least one slot before saving'),
      ));
      return;
    }

    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }

      final apiClient = ref.read(apiClientProvider);
      final availability = <Map<String, dynamic>>[];

      for (final entry in _slots.entries) {
        for (final slot in entry.value) {
          availability.add({
            'dayOfWeek': entry.key,
            'startTime':
                '${slot.startTime.hour.toString().padLeft(2, '0')}:${slot.startTime.minute.toString().padLeft(2, '0')}:00',
            'endTime':
                '${slot.endTime.hour.toString().padLeft(2, '0')}:${slot.endTime.minute.toString().padLeft(2, '0')}:00',
          });
        }
      }

      await apiClient.putVoid(
        ApiEndpoints.trainerAvailability,
        data: {
          'trainerId': trainerId,
          'gymId': gymId,
          'sessionDurationMinutes': _sessionDurationMinutes,
          'bufferMinutes': _bufferMinutes,
          'weeklySlots': availability,
        },
      );

      ref.invalidate(availabilitySlotsProvider);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        final msg = e is ApiException ? e.message : e.toString();
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg)));
      }
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }
}

class _LabeledDropdown<T> extends StatelessWidget {
  final String label;
  final T value;
  final List<DropdownMenuItem<T>> items;
  final ValueChanged<T?> onChanged;

  const _LabeledDropdown({
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  color: AppColors.grey500,
                )),
        const SizedBox(height: 4),
        DropdownButtonFormField<T>(
          value: value,
          isExpanded: true,
          items: items,
          onChanged: onChanged,
          decoration: const InputDecoration(
            isDense: true,
            contentPadding:
                EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            border: OutlineInputBorder(),
          ),
        ),
      ],
    );
  }
}

class _TimeSlot {
  final TimeOfDay startTime;
  final TimeOfDay endTime;

  _TimeSlot({required this.startTime, required this.endTime});
}
