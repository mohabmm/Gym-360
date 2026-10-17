/// Canned JSON payloads shaped like the real BE responses.
/// Every trainer-app integration test pulls shapes from here so model
/// contracts stay in one place.
class Samples {
  Samples._();

  static Map<String, dynamic> trainerUser() => {
        'id': 'trainer-1',
        'email': 'coach@test.local',
        'firstName': 'Omar',
        'lastName': 'Hassan',
        'phone': '+201000000001',
        'role': 'Trainer',
        'gender': 'Male',
        'status': 'Active',
        'gymId': 'gym-1',
        'gymName': 'Tameeni Test Gym',
        'gymMemberships': [
          {
            'id': 'mem-1',
            'gymId': 'gym-1',
            'gymName': 'Tameeni Test Gym',
            'membershipType': 'TrainerStaff',
            'status': 'Active',
          },
          {
            'id': 'mem-2',
            'gymId': 'gym-2',
            'gymName': 'Rasan Downtown',
            'membershipType': 'TrainerStaff',
            'status': 'Active',
          },
        ],
      };

  static Map<String, dynamic> clientUser({String id = 'client-1'}) => {
        'id': id,
        'email': 'mona@test.local',
        'firstName': 'Mona',
        'lastName': 'Ali',
        'role': 'Member',
        'status': 'Active',
        'gymId': 'gym-1',
        'goals': ['WeightLoss', 'Toning'],
      };

  static Map<String, dynamic> authResponse() => {
        'accessToken': 'new-access-token',
        'refreshToken': 'new-refresh-token',
        'user': {
          ...trainerUser(),
          'role': 'Trainer',
        },
        'expiresAt': '2026-05-16T10:00:00.000Z',
      };

  static Map<String, dynamic> session({
    String id = 'session-1',
    String status = 'Scheduled',
  }) =>
      {
        'id': id,
        'gymId': 'gym-1',
        'trainerId': 'trainer-1',
        'memberId': 'client-1',
        'scheduledAt': '2026-04-20T09:00:00.000Z',
        'durationMinutes': 60,
        'sessionType': 'PersonalTraining',
        'status': status,
        'isRecurring': false,
        'createdAt': '2026-04-10T08:00:00.000Z',
      };

  static Map<String, dynamic> workout({String id = 'workout-1'}) => {
        'id': id,
        'name': 'Push Day A',
        'nameAr': 'يوم الدفع أ',
        'description': 'Chest + triceps',
        'trainerId': 'trainer-1',
        'exercises': [
          {
            'id': 'we-1',
            'exerciseId': 'ex-1',
            'orderIndex': 0,
            'sets': 4,
            'reps': '10',
            'weight': 60.0,
            'restSeconds': 90,
          },
        ],
        'isTemplate': false,
        'estimatedDurationMinutes': 55,
      };

  static Map<String, dynamic> exercise({String id = 'ex-1'}) => {
        'id': id,
        'name': 'Bench Press',
        'muscleGroups': ['Chest'],
        'equipment': 'Barbell',
        'category': 'Compound',
        'scope': 'Global',
        'isActive': true,
      };

  static Map<String, dynamic> program() => {
        'id': 'prog-1',
        'name': '8-Week Strength',
        'trainerId': 'trainer-1',
        'durationWeeks': 8,
        'days': [
          {
            'id': 'pd-1',
            'weekNumber': 1,
            'dayOfWeek': 1,
            'workoutId': 'workout-1',
          },
        ],
      };

  static Map<String, dynamic> availability() => {
        'id': 'av-1',
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
        'blockedDates': [
          {
            'id': 'bd-1',
            'date': '2026-04-25T00:00:00.000Z',
            'allDay': true,
          },
        ],
      };

  static Map<String, dynamic> conversation() => {
        'id': 'conv-1',
        'gymId': 'gym-1',
        'type': 'Direct',
        'participants': [
          {
            'userId': 'trainer-1',
            'name': 'Coach Omar',
            'photoUrl': null,
            'role': 'Trainer',
          },
          {
            'userId': 'client-1',
            'name': 'Mona Ali',
            'photoUrl': null,
            'role': 'Member',
          },
        ],
        'unreadCount': 2,
        'createdAt': '2026-04-15T10:00:00.000Z',
        'updatedAt': '2026-04-15T18:00:00.000Z',
        'lastMessageAt': '2026-04-15T18:00:00.000Z',
      };

  static Map<String, dynamic> message() => {
        'id': 'msg-1',
        'conversationId': 'conv-1',
        'senderId': 'client-1',
        'senderName': 'Mona Ali',
        'senderPhotoUrl': null,
        'content': 'Thanks coach!',
        'mediaType': 'None',
        'isRead': false,
        'sentAt': '2026-04-15T18:00:00.000Z',
      };

  static Map<String, dynamic> mealPlan() => {
        'id': 'mp-1',
        'name': 'Cut plan',
        'trainerId': 'trainer-1',
        'traineeId': '',
        'status': 'Active',
        'assignedByTrainerId': 'trainer-1',
        'dailyCalorieTarget': 2000,
        'macroTargets': {
          'proteinGrams': 150,
          'carbsGrams': 200,
          'fatGrams': 60,
        },
        'days': <Map<String, dynamic>>[],
      };

  /// Mirrors backend `FoodDatabaseItemDto` — per-serving macros +
  /// `nameArabic`, not the per-100g/`nameAr` shape `FoodItemModel` declares.
  /// Clients must go through `FoodItemModel.fromFoodDatabaseJson` to parse it.
  static Map<String, dynamic> foodItem() => {
        'id': 'food-1',
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

  static Map<String, dynamic> bodyMetric() => {
        'id': 'bm-1',
        'userId': 'client-1',
        'gymId': 'gym-1',
        'date': '2026-04-15',
        'weight': 72.5,
        'weightUnit': 'kg',
        'source': 'SelfReported',
        'loggedBy': 'client-1',
        'createdAt': '2026-04-15T08:00:00.000Z',
      };

  static Map<String, dynamic> remoteWorkoutStatus() => {
        'clientId': 'client-1',
        'trainerId': 'trainer-1',
        'isActive': true,
        'activatedAt': '2026-04-15T08:00:00.000Z',
        'reason': 'Out of town',
      };

  static Map<String, dynamic> sessionPackage() => {
        'id': 'pkg-1',
        'gymId': 'gym-1',
        'memberId': 'client-1',
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

  static Map<String, dynamic> clientNote() => {
        'id': 'note-1',
        'content': 'Client has right-shoulder tightness',
        'createdAt': '2026-04-10T14:00:00.000Z',
      };

  static Map<String, dynamic> trainerEarnings() => {
        'totalSessions': 24,
        'totalEarningsEgp': 4800.0,
        'currency': 'EGP',
        'records': [
          {
            'sessionId': 'session-1',
            'memberId': 'client-1',
            'rateApplied': 200.0,
            'currency': 'EGP',
            'earnedAt': '2026-04-10T10:00:00.000Z',
          },
        ],
      };

  static Map<String, dynamic> trainerPublicProfile() => {
        'trainerId': 'trainer-1',
        'trainerName': 'Coach Omar',
        'averageRating': 4.7,
        'displayRating': 4.7,
        'totalReviews': 31,
        'badges': <dynamic>[],
        'testimonials': [
          {'memberInitials': 'M.A.', 'comment': 'Great coach!', 'rating': 5},
        ],
        'totalClients': 12,
        'totalSessions': 220,
        'specialization': 'Strength',
      };

  static Map<String, dynamic> gym() => {
        'id': 'gym-1',
        'name': 'Tameeni Test Gym',
        'gymCode': 'TAMEENI-1',
        'status': 'Active',
        'address': {
          'street': '1 Test St',
          'city': 'Cairo',
          'country': 'EG',
        },
      };

  static Map<String, dynamic> planFeatures() => {
        'gymId': 'gym-1',
        'planCode': 'pro',
        'planName': 'Pro',
        'memberLimit': 500,
        'trainerLimit': 10,
        'staffLimit': 5,
        'features': [
          {
            'featureCode': 'remote_workouts',
            'featureName': 'Remote workouts',
            'isEnabled': true,
            'limitValue': null,
            'isOverridden': false,
          },
          {
            'featureCode': 'meal_plans',
            'featureName': 'Meal plans',
            'isEnabled': true,
            'limitValue': null,
            'isOverridden': false,
          },
          {
            'featureCode': 'broadcasts',
            'featureName': 'Broadcasts',
            'isEnabled': true,
            'limitValue': null,
            'isOverridden': false,
          },
        ],
      };
}
