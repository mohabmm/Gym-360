import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/core/push_routes.dart';

void main() {
  test('pt.starter_plan notifications open the member', () {
    expect(trainerPushRoute({'type': 'new_client_assigned', 'route': '/clients/m-1'}), '/clients/m-1');
    expect(trainerPushRoute({'type': 'plan_programs_unlocked', 'route': '/clients/m-1'}), '/clients/m-1');
  });

  test('other notifications and foreign routes stay put', () {
    expect(trainerPushRoute({'type': 'meal_plan_assigned', 'route': '/more/nutrition/meal-plan'}), isNull);
    expect(trainerPushRoute({'type': 'new_client_assigned', 'route': '/plan'}), isNull);
    expect(trainerPushRoute(null), isNull);
  });
}
