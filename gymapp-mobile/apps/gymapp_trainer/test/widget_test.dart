import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/home/presentation/providers/trainer_dashboard_provider.dart';

void main() {
  group('TrainerDashboardData', () {
    test('default constructor yields empty dashboard', () {
      const data = TrainerDashboardData();
      expect(data.todaySessions, isEmpty);
      expect(data.pendingBookings, 0);
      expect(data.unreadMessages, 0);
      expect(data.totalClients, 0);
      expect(data.sessionsCompletedThisMonth, 0);
      expect(data.newClientsThisMonth, 0);
      expect(data.averageRating, 0.0);
    });

    test('holds passed-in values', () {
      final session = SessionModel(
        id: 's1',
        trainerId: 't1',
        traineeId: 'm1',
        scheduledAt: DateTime(2026, 4, 14, 10),
        durationMinutes: 60,
        status: 'scheduled',
      );
      final data = TrainerDashboardData(
        todaySessions: [session],
        pendingBookings: 2,
        unreadMessages: 5,
        totalClients: 42,
        sessionsCompletedThisMonth: 18,
        newClientsThisMonth: 3,
        averageRating: 4.6,
      );
      expect(data.todaySessions, hasLength(1));
      expect(data.todaySessions.first.id, 's1');
      expect(data.pendingBookings, 2);
      expect(data.unreadMessages, 5);
      expect(data.totalClients, 42);
      expect(data.sessionsCompletedThisMonth, 18);
      expect(data.averageRating, 4.6);
    });
  });
}
