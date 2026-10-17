import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../../../clients/presentation/providers/client_providers.dart';
import '../../../home/presentation/providers/trainer_dashboard_provider.dart';
import '../../../scheduling/presentation/providers/scheduling_providers.dart';
import '../providers/session_providers.dart';

class LiveSessionScreen extends ConsumerStatefulWidget {
  final String sessionId;

  const LiveSessionScreen({super.key, required this.sessionId});

  @override
  ConsumerState<LiveSessionScreen> createState() =>
      _LiveSessionScreenState();
}

class _LiveSessionScreenState extends ConsumerState<LiveSessionScreen> {
  late Timer _timer;
  Duration _elapsed = Duration.zero;
  DateTime? _startedAt;
  // The session is only live once the BE accepted /start. Until then the
  // timer stays at zero; if /start is rejected the screen shows why instead
  // of pretending the session is running.
  bool _isStarting = true;
  String? _startError;
  final List<_LiveSetLog> _sets = [];
  final _notesController = TextEditingController();
  bool _isCompleting = false;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      final startedAt = _startedAt;
      if (mounted && startedAt != null) {
        setState(() {
          _elapsed = DateTime.now().difference(startedAt);
        });
      }
    });
    WidgetsBinding.instance.addPostFrameCallback((_) => _startSession());
  }

  Future<void> _startSession() async {
    final apiClient = ref.read(apiClientProvider);
    final testMode = ref.read(testModeProvider);

    // Test mode bypasses the booking-request accept step: auto-confirm a
    // Pending session so /start succeeds immediately. BE rejects confirm on
    // any non-Pending status — swallow that, the session is already past
    // Pending in that case.
    if (testMode) {
      try {
        await apiClient.postVoid(
          ApiEndpoints.sessionConfirm(SessionId(widget.sessionId)),
          data: {},
        );
      } catch (_) {}
    }

    if (mounted) {
      setState(() {
        _isStarting = true;
        _startError = null;
      });
    }

    try {
      await apiClient.postVoid(
        ApiEndpoints.sessionStart(SessionId(widget.sessionId)),
        data: {},
      );
    } catch (e) {
      // Re-entering a session that is already InProgress is fine. Anything
      // else (too early, not confirmed, network) means the session is NOT
      // live — surface it rather than running a timer over a session the BE
      // will then refuse to complete.
      var alreadyRunning = false;
      try {
        final session = await apiClient.get<SessionModel>(
          ApiEndpoints.sessionById(SessionId(widget.sessionId)),
          parser: (data) =>
              SessionModel.fromJson(data as Map<String, dynamic>),
        );
        alreadyRunning = session.status.toLowerCase() == 'inprogress';
      } catch (_) {}

      if (!alreadyRunning) {
        if (mounted) {
          setState(() {
            _isStarting = false;
            _startError = _describeStartError(e);
          });
        }
        return;
      }
    }

    if (mounted) {
      setState(() {
        _isStarting = false;
        _startedAt = DateTime.now();
      });
    }
  }

  String _describeStartError(Object error) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    if (error is ApiException) {
      switch (error.errorCode) {
        case 'Scheduling.SessionTooEarly':
          return isArabic
              ? 'لا يمكن بدء الجلسة الآن. يمكنك البدء قبل موعدها بـ 15 دقيقة كحد أقصى.'
              : 'This session cannot be started yet. You can start it up to 15 minutes before its scheduled time.';
        case 'Scheduling.InvalidStatusTransition':
          return isArabic
              ? 'لا يمكن بدء هذه الجلسة في حالتها الحالية.'
              : 'This session cannot be started in its current status.';
      }
      return error.message;
    }
    return isArabic
        ? 'تعذر بدء الجلسة. تحقق من الاتصال وحاول مرة أخرى.'
        : 'Could not start the session. Check your connection and try again.';
  }

  @override
  void dispose() {
    _timer.cancel();
    _notesController.dispose();
    super.dispose();
  }

  String _formatDuration(Duration d) {
    final h = d.inHours.toString().padLeft(2, '0');
    final m = (d.inMinutes % 60).toString().padLeft(2, '0');
    final s = (d.inSeconds % 60).toString().padLeft(2, '0');
    return '$h:$m:$s';
  }

  Future<void> _completeSession() async {
    final isArabic =
        Localizations.localeOf(context).languageCode == 'ar';
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'اتمام الجلسة؟' : 'Complete session?'),
        content: Text(
          isArabic
              ? 'هل انت متاكد من انهاء الجلسة؟'
              : 'Are you sure you want to end this session?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'نعم، انهي' : 'Yes, complete'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() => _isCompleting = true);
    try {
      final apiClient = ref.read(apiClientProvider);

      // Order matters: BE Session.AddExerciseLog rejects unless
      // Status == Completed. Complete the session FIRST, then push
      // exercise logs.
      await apiClient.postVoid(
        ApiEndpoints.sessionComplete(SessionId(widget.sessionId)),
        data: {},
      );

      // Aggregate live `_LiveSetLog` rows per exercise. BE SessionExerciseLog
      // schema is one row per exercise (sets count + reps string), so encode
      // per-set reps as CSV "10,8,8" — trainee detail splits on comma to
      // render the breakdown. Weight = max recorded (working weight).
      final byExercise = <String, List<_LiveSetLog>>{};
      for (final s in _sets) {
        final key = s.exerciseName.trim();
        if (key.isEmpty) continue;
        byExercise.putIfAbsent(key, () => []).add(s);
      }
      if (byExercise.isNotEmpty) {
        final items = <Map<String, dynamic>>[];
        var i = 0;
        for (final entry in byExercise.entries) {
          final rows = entry.value..sort((a, b) => a.setNumber.compareTo(b.setNumber));
          final reps = rows.map((r) => r.reps).join(',');
          final weights = rows.map((r) => r.weight).whereType<double>();
          final maxWeight = weights.isEmpty
              ? null
              : weights.reduce((a, b) => a > b ? a : b);
          // Per-set weight breakdown captured in notes when not uniform so the
          // trainee can see actual progression: "60,60,65 kg".
          final weightVals = rows.map((r) => r.weight).toList();
          final allSameWeight = weightVals.every((w) => w == weightVals.first);
          final notes = (!allSameWeight && weightVals.any((w) => w != null))
              ? 'Per-set weight (kg): ${weightVals.map((w) => w == null ? '-' : (w == w.truncateToDouble() ? w.toStringAsFixed(0) : w.toStringAsFixed(1))).join(', ')}'
              : null;
          items.add({
            'exerciseName': entry.key,
            'orderIndex': i++,
            'sets': rows.length,
            'reps': reps.isEmpty ? '0' : reps,
            'weight': maxWeight,
            'notes': notes,
          });
        }
        try {
          await apiClient.postVoid(
            ApiEndpoints.sessionExerciseLogs(SessionId(widget.sessionId)),
            data: {'items': items},
          );
        } catch (_) {
          // Non-fatal — trainer can re-enter on the post-session screen.
        }
      }

      if (mounted) {
        // Forward live notes to post-session screen so trainer doesn't retype.
        // PostSessionLogScreen reads it via GoRouterState.extra.
        ref.invalidate(trainerDashboardProvider);
        ref.invalidate(upcomingSessionsProvider);
        context.pushReplacement(
          '/sessions/${widget.sessionId}/log',
          extra: _notesController.text.trim(),
        );
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isCompleting = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e is ApiException ? e.message : e.toString())),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final theme = Theme.of(context);
    final sessionAsync =
        ref.watch(sessionDetailProvider(widget.sessionId));

    final startError = _startError;
    if (startError != null) {
      return Scaffold(
        appBar: AppBar(
          title: Text(isArabic ? 'تعذر بدء الجلسة' : 'Session not started'),
        ),
        body: AppErrorWidget(
          message: startError,
          onRetry: _startSession,
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'جلسة نشطة' : 'Live Session'),
        backgroundColor: AppColors.success,
        foregroundColor: Colors.white,
        actions: [
          if (_isCompleting)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Colors.white)),
            )
          else
            TextButton(
              onPressed: _isStarting ? null : _completeSession,
              style:
                  TextButton.styleFrom(foregroundColor: Colors.white),
              child: Text(isArabic ? 'اتمام' : 'Complete'),
            ),
        ],
      ),
      body: Column(
        children: [
          // Timer banner
          Container(
            color: AppColors.success,
            padding: const EdgeInsets.symmetric(vertical: 16),
            child: Center(
              child: Text(
                _formatDuration(_elapsed),
                style: theme.textTheme.displaySmall?.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 4,
                ),
              ),
            ),
          ),

          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Client info
                sessionAsync.when(
                  data: (session) {
                    final names = ref.watch(clientNameByIdProvider);
                    final clientName = names[session.memberId] ??
                        (isArabic ? 'عميل' : 'Client');
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            AvatarWidget(
                              name: clientName,
                              size: AppConstants.avatarSizeSmall,
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment:
                                    CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    clientName,
                                    style: theme.textTheme.titleSmall
                                        ?.copyWith(
                                            fontWeight: FontWeight.w600),
                                  ),
                                  Text(
                                    DateFormat('hh:mm a', isArabic ? 'ar' : 'en')
                                        .format(session.scheduledAt),
                                    style: theme.textTheme.bodySmall,
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                  loading: () => const ShimmerCard(height: 70),
                  error: (_, __) => const SizedBox.shrink(),
                ),
                const SizedBox(height: 16),

                // Log sets section
                Row(
                  mainAxisAlignment:
                      MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      isArabic ? 'تسجيل المجموعات' : 'Log Sets',
                      style: theme.textTheme.titleMedium
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    TextButton.icon(
                      onPressed: () =>
                          _showAddSetDialog(context, isArabic),
                      icon: const Icon(Icons.add, size: 16),
                      label:
                          Text(isArabic ? 'اضافة' : 'Add Set'),
                    ),
                  ],
                ),
                ..._sets.map((s) => Card(
                      margin: const EdgeInsets.only(bottom: 4),
                      child: ListTile(
                        dense: true,
                        leading: CircleAvatar(
                          radius: 16,
                          child: Text(
                            '${s.setNumber}',
                            style: const TextStyle(fontSize: 12),
                          ),
                        ),
                        title: Text(s.exerciseName),
                        subtitle: Text(
                          '${s.reps} reps${s.weight != null ? ' • ${s.weight}kg' : ''}',
                        ),
                      ),
                    )),

                const SizedBox(height: 16),

                // Notes
                AppTextField(
                  label: isArabic ? 'ملاحظات الجلسة' : 'Session Notes',
                  hint: isArabic
                      ? 'اي ملاحظات او مشاهدات...'
                      : 'Any notes or observations...',
                  controller: _notesController,
                  maxLines: 3,
                ),

                const SizedBox(height: 24),
                AppButton(
                  text: isArabic ? 'اتمام الجلسة' : 'Complete Session',
                  onPressed:
                      _isCompleting || _isStarting ? null : _completeSession,
                  isLoading: _isCompleting,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  void _showAddSetDialog(BuildContext context, bool isArabic) {
    final exerciseController = TextEditingController();
    final repsController = TextEditingController(text: '12');
    final weightController = TextEditingController();
    final setNumber = _sets.length + 1;

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'تسجيل مجموعة' : 'Log Set'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            AppTextField(
              label: isArabic ? 'التمرين' : 'Exercise',
              controller: exerciseController,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(
                  child: AppTextField(
                    label: isArabic ? 'تكرار' : 'Reps',
                    controller: repsController,
                    keyboardType: TextInputType.number,
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: AppTextField(
                    label: isArabic ? 'وزن (كج)' : 'Weight (kg)',
                    controller: weightController,
                    keyboardType: TextInputType.number,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(isArabic ? 'الغاء' : 'Cancel'),
          ),
          ElevatedButton(
            onPressed: () {
              final reps = int.tryParse(repsController.text) ?? 0;
              final weight = double.tryParse(weightController.text);
              setState(() {
                _sets.add(_LiveSetLog(
                  exerciseId: exerciseController.text,
                  exerciseName: exerciseController.text,
                  setNumber: setNumber,
                  reps: reps,
                  weight: weight,
                ));
              });
              Navigator.pop(ctx);
            },
            child: Text(isArabic ? 'تسجيل' : 'Log'),
          ),
        ],
      ),
    );
  }
}

class _LiveSetLog {
  final String exerciseId;
  final String exerciseName;
  final int setNumber;
  final int reps;
  final double? weight;
  final String? notes;

  const _LiveSetLog({
    required this.exerciseId,
    required this.exerciseName,
    required this.setNumber,
    required this.reps,
    this.weight,
  }) : notes = null;
}
