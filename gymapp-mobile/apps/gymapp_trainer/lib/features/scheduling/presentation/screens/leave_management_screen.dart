import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';

class LeaveManagementScreen extends ConsumerStatefulWidget {
  const LeaveManagementScreen({super.key});

  @override
  ConsumerState<LeaveManagementScreen> createState() =>
      _LeaveManagementScreenState();
}

class _LeaveManagementScreenState
    extends ConsumerState<LeaveManagementScreen> {
  bool _isSaving = false;

  // BE has no GET /availability/blocked-dates; it returns blocked dates inside
  // the GET /scheduling/availability response.
  final _blockedDatesProvider =
      FutureProvider.autoDispose<List<DateTime>>((ref) async {
    final apiClient = ref.watch(apiClientProvider);
    final gymId = ref.watch(activeGymIdProvider);
    final trainerId = ref.watch(authProvider).user?.id;
    if (gymId == null || trainerId == null) return <DateTime>[];

    final dates = await apiClient.get<List<String>>(
      ApiEndpoints.trainerAvailability,
      queryParameters: {'trainerId': trainerId, 'gymId': gymId},
      parser: (data) {
        if (data is Map<String, dynamic>) {
          final blocked =
              data['blockedDates'] ?? data['blocked_dates'] ?? const [];
          if (blocked is List) {
            return blocked
                .map((e) {
                  if (e is Map) {
                    final d = e['date'] ?? e['Date'];
                    return d?.toString() ?? '';
                  }
                  return e.toString();
                })
                .where((s) => s.isNotEmpty)
                .toList();
          }
        }
        return const <String>[];
      },
    );
    return dates
        .map((s) => DateTime.tryParse(s))
        .whereType<DateTime>()
        .toList();
  });

  Future<void> _addBlock(BuildContext context, bool isArabic) async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: now,
      lastDate: now.add(const Duration(days: 365)),
      helpText: isArabic ? 'اختر فترة الاجازة' : 'Select leave period',
    );
    if (picked == null) return;

    setState(() => _isSaving = true);
    try {
      final authState = ref.read(authProvider);
      final trainerId = authState.user?.id;
      final gymId = ref.read(activeGymIdProvider);
      if (trainerId == null || gymId == null) {
        throw Exception('No authenticated trainer or active gym');
      }
      final apiClient = ref.read(apiClientProvider);

      // BE endpoint blocks a single date at a time; iterate through the range.
      for (var day = picked.start;
          !day.isAfter(picked.end);
          day = day.add(const Duration(days: 1))) {
        final dateStr = day.toIso8601String().split('T')[0];
        await apiClient.postVoid(
          ApiEndpoints.blockDate,
          data: {
            'trainerId': trainerId,
            'gymId': gymId,
            'date': dateStr,
            'autoNotifyClients': true,
          },
        );
      }
      ref.invalidate(_blockedDatesProvider);
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

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final blockedAsync = ref.watch(_blockedDatesProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'ادارة الاجازات' : 'Leave Management'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: isArabic ? 'اضافة اجازة' : 'Add leave',
            onPressed: () => _addBlock(context, isArabic),
          ),
        ],
      ),
      body: blockedAsync.when(
        data: (dates) {
          if (dates.isEmpty) {
            return EmptyState(
              icon: Icons.event_available,
              title: isArabic ? 'لا توجد اجازات محجوزة' : 'No blocked dates',
              subtitle: isArabic
                  ? 'اضف اجازات لمنع الحجز في تلك الايام'
                  : 'Add leave dates to block bookings',
            );
          }

          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: dates.length,
            itemBuilder: (context, index) {
              final date = dates[index];
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  leading: const Icon(Icons.event_busy),
                  title: Text(DateFormat('dd/MM/yyyy').format(date)),
                  trailing: IconButton(
                    icon: const Icon(Icons.delete_outline,
                        color: AppColors.error),
                    onPressed: () {
                      // BE exposes no unblock-date endpoint yet; show a hint.
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            isArabic
                                ? 'الغاء الحجب غير متاح حاليا'
                                : 'Removing a blocked date is not supported yet',
                          ),
                        ),
                      );
                    },
                  ),
                ),
              );
            },
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => AppErrorWidget(
          message: e.toString(),
          onRetry: () => ref.invalidate(_blockedDatesProvider),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _addBlock(context, isArabic),
        icon: const Icon(Icons.event_busy),
        label: Text(isArabic ? 'اضافة اجازة' : 'Add Leave'),
      ),
    );
  }
}
