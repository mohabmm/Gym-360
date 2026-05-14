import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';
import 'package:table_calendar/table_calendar.dart';

import '../../../../core/router.dart';
import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../providers/scheduling_providers.dart';

class CalendarScreen extends ConsumerStatefulWidget {
  const CalendarScreen({super.key});

  @override
  ConsumerState<CalendarScreen> createState() => _CalendarScreenState();
}

class _CalendarScreenState extends ConsumerState<CalendarScreen> {
  DateTime _focusedDay = DateTime.now();
  DateTime _selectedDay = DateTime.now();
  CalendarFormat _calendarFormat = CalendarFormat.week;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final sessionsAsync = ref.watch(upcomingSessionsProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'الجدول' : 'Schedule'),
        actions: [
          IconButton(
            icon: const Icon(Icons.schedule_outlined),
            tooltip: isArabic ? 'الاوقات المتاحة' : 'Availability',
            onPressed: () => context.push(AppRoutes.availability),
          ),
          IconButton(
            icon: const Icon(Icons.pending_actions_outlined),
            tooltip: isArabic ? 'طلبات الحجز' : 'Booking Requests',
            onPressed: () => context.push(AppRoutes.bookingRequests),
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              switch (value) {
                case 'recurring':
                  context.push(AppRoutes.recurringSession);
                  break;
                case 'leave':
                  context.push(AppRoutes.leaveManagement);
                  break;
              }
            },
            itemBuilder: (context) => [
              PopupMenuItem(
                value: 'recurring',
                child: ListTile(
                  dense: true,
                  leading: const Icon(Icons.repeat),
                  title: Text(
                      isArabic ? 'جلسات متكررة' : 'Recurring Sessions'),
                ),
              ),
              PopupMenuItem(
                value: 'leave',
                child: ListTile(
                  dense: true,
                  leading: const Icon(Icons.event_busy),
                  title: Text(isArabic ? 'ادارة الاجازات' : 'Leave Management'),
                ),
              ),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          // Calendar widget
          sessionsAsync.when(
            data: (sessions) => TableCalendar(
              locale: isArabic ? 'ar' : 'en',
              firstDay: DateTime.now().subtract(const Duration(days: 30)),
              lastDay: DateTime.now().add(const Duration(days: 365)),
              focusedDay: _focusedDay,
              selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
              calendarFormat: _calendarFormat,
              availableCalendarFormats: {
                CalendarFormat.month: isArabic ? 'شهر' : 'Month',
                CalendarFormat.week: isArabic ? 'اسبوع' : 'Week',
              },
              eventLoader: (day) => sessions
                  .where((s) => isSameDay(s.scheduledAt.toLocal(), day))
                  .toList(),
              onDaySelected: (selectedDay, focusedDay) {
                setState(() {
                  _selectedDay = selectedDay;
                  _focusedDay = focusedDay;
                });
              },
              onFormatChanged: (format) {
                setState(() => _calendarFormat = format);
              },
              onPageChanged: (focusedDay) {
                _focusedDay = focusedDay;
              },
              calendarStyle: CalendarStyle(
                todayDecoration: BoxDecoration(
                  color: theme.colorScheme.primary.withOpacity(0.3),
                  shape: BoxShape.circle,
                ),
                selectedDecoration: BoxDecoration(
                  color: theme.colorScheme.primary,
                  shape: BoxShape.circle,
                ),
                markerDecoration: BoxDecoration(
                  color: theme.colorScheme.secondary,
                  shape: BoxShape.circle,
                ),
              ),
            ),
            loading: () => const ShimmerCard(height: 300),
            error: (_, __) => const SizedBox(height: 100),
          ),

          const Divider(height: 1),

          // Sessions for selected day
          Expanded(
            child: sessionsAsync.when(
              data: (sessions) {
                final daySessions = sessions
                    .where((s) => isSameDay(s.scheduledAt, _selectedDay))
                    .toList();

                if (daySessions.isEmpty) {
                  return EmptyState(
                    icon: Icons.event_available,
                    title: isArabic ? 'لا توجد جلسات' : 'No sessions',
                    subtitle: isArabic
                        ? 'لا توجد جلسات في هذا اليوم'
                        : 'No sessions on this day',
                  );
                }

                final names = ref.watch(clientNameByIdProvider);
                return RefreshIndicator(
                  onRefresh: () async =>
                      ref.invalidate(upcomingSessionsProvider),
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: daySessions.length,
                    itemBuilder: (context, index) {
                      final session = daySessions[index];
                      return _SessionCard(
                        session: session,
                        isArabic: isArabic,
                        memberName: names[session.memberId],
                      );
                    },
                  ),
                );
              },
              loading: () => ListView.builder(
                itemCount: 3,
                itemBuilder: (_, __) => const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  child: ShimmerCard(height: 80),
                ),
              ),
              error: (e, _) => AppErrorWidget(
                message: e.toString(),
                onRetry: () => ref.invalidate(upcomingSessionsProvider),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _SessionCard extends ConsumerWidget {
  final SessionModel session;
  final bool isArabic;
  final String? memberName;

  const _SessionCard({
    required this.session,
    required this.isArabic,
    this.memberName,
  });

  // Backend returns 'Confirmed' or 'InProgress' for startable sessions.
  // AppConstants.sessionScheduled ('scheduled') was the old name — never returned.
  // Test mode also enables Pending so QA can start without going through
  // Booking Requests; live_session_screen auto-confirms before /start.
  static bool _canStartLive(String status, {bool testMode = false}) {
    final s = status.toLowerCase();
    if (testMode && s == 'pending') return true;
    return s == 'confirmed' || s == 'inprogress' || s == 'scheduled';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final timeStr = DateFormat('hh:mm a', isArabic ? 'ar' : 'en').format(session.scheduledAt);
    final testMode = ref.watch(testModeProvider);

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppConstants.borderRadius),
        onTap: () => _canStartLive(session.status, testMode: testMode)
            ? context.push('/sessions/${session.id}/live')
            : context.push('/clients/${session.memberId}'),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                    horizontal: 10, vertical: 8),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primary.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  timeStr,
                  style: TextStyle(
                    color: theme.colorScheme.primary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      (memberName != null && memberName!.isNotEmpty)
                          ? memberName!
                          : (isArabic ? 'عميل' : 'Client'),
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    Text(
                      '${session.durationMinutes} ${isArabic ? 'دقيقة' : 'min'}',
                      style: theme.textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              if (_canStartLive(session.status, testMode: testMode))
                const Icon(Icons.play_circle_outline,
                    color: AppColors.success),
            ],
          ),
        ),
      ),
    );
  }
}
