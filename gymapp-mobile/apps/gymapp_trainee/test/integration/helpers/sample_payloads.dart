/// Canned JSON payloads shaped like the real BE responses. Kept here so every
/// feature test can reach for the same shapes without duplicating model
/// contracts in test code.
class Samples {
  Samples._();

  static Map<String, dynamic> user({String id = 'user-1'}) => {
        'id': id,
        'email': 'member@test.local',
        'firstName': 'Mona',
        'lastName': 'Ali',
        'phone': '+201000000000',
        'role': 'Member',
        'gender': 'Female',
        'status': 'Active',
        'gymId': 'gym-1',
        'gymName': 'Tameeni Test Gym',
      };

  static Map<String, dynamic> authResponse() => {
        'accessToken': 'new-access-token',
        'refreshToken': 'new-refresh-token',
        'user': user(),
        'expiresAt': '2026-05-16T10:00:00.000Z',
      };

  // Mirrors BE TodaysWorkoutResponse — assignment summary, no exercises.
  static Map<String, dynamic> todaysWorkoutResponse({
    String workoutId = 'workout-1',
    String assignmentId = 'asgn-1',
  }) => {
        'assignmentId': assignmentId,
        'workoutId': workoutId,
        'workoutName': 'Push Day A',
        'workoutDescription': 'Chest + triceps',
        'estimatedDurationMinutes': 55,
        'targetMuscleGroups': <String>['Chest', 'Triceps'],
        'isRemoteMode': true,
        'startDate': '2026-05-01',
        'endDate': null,
      };

  static Map<String, dynamic> workout({String id = 'workout-1'}) => {
        'id': id,
        'name': 'Push Day A',
        'nameAr': 'يوم الدفع أ',
        'description': 'Chest + triceps',
        'trainerId': 'trainer-1',
        'trainerName': 'Coach Omar',
        'exercises': [
          {
            'id': 'we-1',
            'exerciseId': 'ex-1',
            'orderIndex': 0,
            'sets': 4,
            'reps': 10,
            'weight': 60.0,
            'restSeconds': 90,
          },
        ],
        'isTemplate': false,
        'estimatedDurationMinutes': 55,
      };

  static Map<String, dynamic> workoutLog({String id = 'log-1'}) => {
        'id': id,
        'workoutId': 'workout-1',
        'workoutName': 'Push Day A',
        'userId': 'user-1',
        'loggedAt': '2026-04-16T09:00:00.000Z',
        'durationMinutes': 55,
        'sets': [
          {
            'id': 'set-1',
            'exerciseId': 'ex-1',
            'exerciseName': 'Bench Press',
            'setNumber': 1,
            'reps': 10,
            'weight': 60.0,
            'rpe': 7,
          },
        ],
        'isAdHoc': false,
      };

  static Map<String, dynamic> session({String id = 'session-1'}) => {
        'id': id,
        'trainerId': 'trainer-1',
        'trainerName': 'Coach Omar',
        'traineeId': 'user-1',
        'traineeName': 'Mona Ali',
        'scheduledAt': '2026-04-20T09:00:00.000Z',
        'durationMinutes': 60,
        'status': 'Scheduled',
        'type': 'PT',
      };

  // Mirrors the BE ProgressDashboardResponse shape (Modules/Progress):
  // latestMetrics + change-from-first deltas + photo count + streak counters.
  static Map<String, dynamic> progressDashboard() => {
        'latestMetrics': {
          'id': 'bm-1',
          'userId': 'user-1',
          'gymId': 'gym-1',
          'date': '2026-04-15',
          'weight': 72.5,
          'weightUnit': 'kg',
          'bodyFatPercentage': 22.0,
          'measurements': null,
          'source': 'Member',
          'loggedBy': 'user-1',
          'notes': null,
          'createdAt': '2026-04-15T08:00:00.000Z',
        },
        'weightChangeFromFirst': -0.7,
        'bodyFatChangeFromFirst': -0.5,
        'photoCount': 4,
        'currentWorkoutStreak': 4,
        'longestWorkoutStreak': 12,
        'currentCheckInStreak': 3,
        'longestCheckInStreak': 9,
      };

  static Map<String, dynamic> bodyMetric({String id = 'bm-1'}) => {
        'id': id,
        'userId': 'user-1',
        'metricType': 'Weight',
        'value': 72.5,
        'unit': 'kg',
        'recordedAt': '2026-04-15T08:00:00.000Z',
      };

  static Map<String, dynamic> progressPhotoSet() => {
        'date': '2026-04-15T00:00:00.000Z',
        'front': {
          'id': 'ph-1',
          'userId': 'user-1',
          'type': 'Front',
          'imageUrl': 'https://cdn.test/ph-1.jpg',
          'isSharedWithTrainer': false,
        },
      };

  static Map<String, dynamic> waterLog() => {
        'id': 'wl-1',
        'userId': 'user-1',
        'date': '2026-04-16T00:00:00.000Z',
        'totalMl': 1500,
        'targetMl': 3000,
        'entries': [
          {
            'id': 'we-1',
            'amountMl': 500,
            'loggedAt': '2026-04-16T09:00:00.000Z',
          },
        ],
      };

  /// Mirrors backend `FoodLogResponse` — grouped `meals`, each containing
  /// its own `foodItems` list. The client datasource flattens this into
  /// `FoodLogModel.entries`, synthesising `"<mealId>:<index>"` ids.
  static Map<String, dynamic> foodLog() => {
        'id': 'fl-1',
        'userId': 'user-1',
        'date': '2026-04-16',
        'totalCalories': 320,
        'totalProtein': 14.0,
        'totalCarbs': 40.0,
        'totalFat': 8.0,
        'meals': [
          {
            'id': 'meal-1',
            'mealType': 'Breakfast',
            'totalCalories': 320,
            'totalProtein': 14.0,
            'totalCarbs': 40.0,
            'totalFat': 8.0,
            'foodItems': [
              {
                'name': 'Ful medames',
                'quantity': 200.0,
                'unit': 'g',
                'calories': 320,
                'protein': 14.0,
                'carbs': 40.0,
                'fat': 8.0,
              },
            ],
          },
        ],
      };

  /// Mirrors backend `FoodDatabaseItemDto` — per-serving macros +
  /// `nameArabic`, not the per-100g/`nameAr` shape `FoodItemModel` declares.
  /// Clients must go through `FoodItemModel.fromFoodDatabaseJson` to parse it.
  static Map<String, dynamic> foodItem({String id = 'food-1'}) => {
        'id': id,
        'name': 'Ful medames',
        'nameArabic': 'فول مدمس',
        'servingSize': 100.0,
        'servingUnit': 'g',
        'caloriesPerServing': 160.0,
        'proteinPerServing': 7.0,
        'carbsPerServing': 20.0,
        'fatPerServing': 4.0,
        'category': 'Protein',
      };

  static Map<String, dynamic> mealPlan() => {
        'id': 'mp-1',
        'name': 'Cut plan',
        'trainerId': '',
        'traineeId': '',
        'status': 'Active',
        'assignedByTrainerId': 'trainer-1',
        'assignedByTrainerName': 'Coach Omar',
        'dailyCalorieTarget': 2000,
        'macroTargets': {
          'proteinGrams': 150,
          'carbsGrams': 200,
          'fatGrams': 60,
        },
        'days': [],
      };

  static Map<String, dynamic> conversation() => {
        'id': 'conv-1',
        'gymId': 'gym-1',
        'type': 'Direct',
        'participants': [
          {
            'userId': 'user-1',
            'name': 'Mona Ali',
            'photoUrl': null,
            'role': 'Member',
          },
          {
            'userId': 'trainer-1',
            'name': 'Coach Omar',
            'photoUrl': null,
            'role': 'Trainer',
          },
        ],
        'unreadCount': 2,
        'createdAt': '2026-04-15T10:00:00.000Z',
        'updatedAt': '2026-04-15T18:00:00.000Z',
        'lastMessageAt': '2026-04-15T18:00:00.000Z',
      };

  static Map<String, dynamic> message({String id = 'msg-1'}) => {
        'id': id,
        'conversationId': 'conv-1',
        'senderId': 'trainer-1',
        'senderName': 'Coach Omar',
        'senderPhotoUrl': null,
        'content': 'Great session today!',
        'mediaType': 'None',
        'isRead': false,
        'sentAt': '2026-04-15T18:00:00.000Z',
      };

  static Map<String, dynamic> announcement({String id = 'ann-1'}) => {
        'id': id,
        'gymId': 'gym-1',
        'title': 'Ramadan hours',
        'content': 'Gym closes at 2am during Ramadan.',
        'category': 'Operations',
        'status': 'Published',
      };

  static Map<String, dynamic> notification({String id = 'notif-1'}) => {
        'id': id,
        'userId': 'user-1',
        'title': 'Session reminder',
        'body': 'Your session starts in 1 hour.',
        'type': 'session_reminder',
        'isRead': false,
        'createdAt': '2026-04-16T08:00:00.000Z',
      };

  static Map<String, dynamic> notificationPrefs() => {
        'id': 'np-1',
        'userId': 'user-1',
        'sessionReminders': true,
        'expiryWarnings': true,
        'announcements': true,
        'chatMessages': true,
        'promotions': false,
        'workoutReminders': true,
        'progressReminders': true,
      };

  static Map<String, dynamic> checkIn({String id = 'ci-1'}) => {
        'id': id,
        'gymId': 'gym-1',
        'memberId': 'user-1',
        'memberName': 'Mona Ali',
        'method': 'qr',
        'checkedInAt': '2026-04-16T08:30:00.000Z',
        'status': 'Active',
      };

  static Map<String, dynamic> leaderboard({String type = 'workouts_per_month'}) => {
        'type': type,
        'period': 'monthly',
        'entries': [
          {
            'position': 1,
            'memberId': 'user-2',
            'memberName': 'Top Alex',
            'score': 22.0,
            'scoreLabel': '22 workouts',
          },
          {
            'position': 2,
            'memberId': 'user-1',
            'memberName': 'Mona Ali',
            'score': 18.0,
            'scoreLabel': '18 workouts',
          },
        ],
        'myPosition': {
          'position': 2,
          'memberId': 'user-1',
          'memberName': 'Mona Ali',
          'score': 18.0,
          'scoreLabel': '18 workouts',
        },
        'totalParticipants': 42,
      };

  static Map<String, dynamic> trainerPublicProfile() => {
        'trainerId': 'trainer-1',
        'trainerName': 'Coach Omar',
        'trainerPhotoUrl': 'https://cdn.test/coach.jpg',
        'averageRating': 4.7,
        'totalReviews': 31,
        'badges': [],
        'testimonials': [],
        'totalClients': 12,
        'totalSessions': 220,
        'specialization': 'Strength',
      };

  static Map<String, dynamic> sessionPackage({String id = 'pkg-1'}) => {
        'id': id,
        'gymId': 'gym-1',
        'memberId': 'user-1',
        'trainerId': 'trainer-1',
        'templateId': 'tpl-1',
        'totalSessions': 10,
        'remainingSessions': 7,
        'usedSessions': 3,
        'price': 3000.0,
        'currency': 'EGP',
        'status': 'Active',
        'startDate': '2026-04-01T00:00:00.000Z',
        'expiryDate': '2026-07-01T00:00:00.000Z',
      };

  static Map<String, dynamic> payment({String id = 'pay-1'}) => {
        'id': id,
        'gymId': 'gym-1',
        'memberId': 'user-1',
        'amount': 500.0,
        'currency': 'EGP',
        'method': 'Cash',
        'purpose': 'Package',
        'status': 'Completed',
        'paidAt': '2026-04-01T10:00:00.000Z',
      };

  static Map<String, dynamic> gymClass({String id = 'class-1'}) => {
        'id': id,
        'gymId': 'gym-1',
        'name': 'HIIT Morning',
        'instructorName': 'Coach Nour',
        'capacity': 20,
        'bookedCount': 12,
        'startTime': '2026-04-17T07:00:00.000Z',
        'endTime': '2026-04-17T08:00:00.000Z',
        'status': 'Scheduled',
      };

  static Map<String, dynamic> availabilitySlot({String id = 'av-1'}) => {
        'id': id,
        'trainerId': 'trainer-1',
        'slots': [
          {
            'id': 'slot-1',
            'dayOfWeek': 1,
            'startTime': '09:00',
            'endTime': '12:00',
            'isActive': true,
          },
        ],
        'bufferMinutes': 15,
      };

  static Map<String, dynamic> exercise({String id = 'ex-1'}) => {
        'id': id,
        'name': 'Bench Press',
        'muscleGroup': 'Chest',
        'equipment': 'Barbell',
        'category': 'Compound',
        'difficulty': 'Intermediate',
        'isCustom': false,
      };
}
