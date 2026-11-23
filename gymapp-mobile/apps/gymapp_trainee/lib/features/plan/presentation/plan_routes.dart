/// pt.training_plan trainee routes (registered in core/router.dart, guarded by the flag).
class PlanRoutes {
  PlanRoutes._();

  static const String plan = '/plan';
  static const String workoutPattern = '/plan/workout/:dayKey';

  static String workout(String dayKey) => '/plan/workout/${Uri.encodeComponent(dayKey)}';
}
