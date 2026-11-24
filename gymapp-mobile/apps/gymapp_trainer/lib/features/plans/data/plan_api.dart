import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';
import 'plan_draft.dart';

/// pt.training_plan: gym templates first, then the coach's own (the first call seeds three samples).
final planTemplatesProvider = FutureProvider.autoDispose<List<PlanTemplateSummaryModel>>((ref) async {
  return ref.watch(apiClientProvider).get<List<PlanTemplateSummaryModel>>(
        ApiEndpoints.planTemplates,
        parser: (data) => [
          for (final item in data as List) PlanTemplateSummaryModel.fromJson(Map<String, dynamic>.from(item as Map))
        ],
      );
});

final planTemplateProvider = FutureProvider.autoDispose.family<PlanTemplateModel, String>((ref, id) async {
  return ref.watch(apiClientProvider).get<PlanTemplateModel>(
        ApiEndpoints.planTemplateById(id),
        parser: (data) => PlanTemplateModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// pt.training_plan: the built-in ready programs (classics, Egyptian staples, lifestyle, cardio).
final planProgramsProvider = FutureProvider.autoDispose<List<PlanProgramSummaryModel>>((ref) async {
  return ref.watch(apiClientProvider).get<List<PlanProgramSummaryModel>>(
        ApiEndpoints.planPrograms,
        parser: (data) => [
          for (final item in data as List) PlanProgramSummaryModel.fromJson(Map<String, dynamic>.from(item as Map))
        ],
      );
});

final planProgramProvider = FutureProvider.autoDispose.family<PlanProgramModel, String>((ref, key) async {
  return ref.watch(apiClientProvider).get<PlanProgramModel>(
        ApiEndpoints.planProgramByKey(key),
        parser: (data) => PlanProgramModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// A client's active plan and history.
final traineePlansProvider = FutureProvider.autoDispose.family<TraineePlansModel, String>((ref, clientId) async {
  return ref.watch(apiClientProvider).get<TraineePlansModel>(
        ApiEndpoints.traineePlans(clientId),
        parser: (data) => TraineePlansModel.fromJson(Map<String, dynamic>.from(data as Map)),
      );
});

/// A library exercise the builder can link to.
class LibraryExercise {
  final String id;
  final String name;
  final String? nameAr;
  final String? demoMediaUrl;

  const LibraryExercise({required this.id, required this.name, this.nameAr, this.demoMediaUrl});

  String displayName(bool isArabic) => isArabic && (nameAr?.isNotEmpty ?? false) ? nameAr! : name;

  factory LibraryExercise.fromJson(Map<String, dynamic> json) => LibraryExercise(
        id: json['id'] as String,
        name: json['name'] as String? ?? '',
        nameAr: json['nameAr'] as String?,
        demoMediaUrl: json['demoMediaUrl'] as String?,
      );
}

final planExerciseSearchProvider =
    FutureProvider.autoDispose.family<List<LibraryExercise>, String>((ref, query) async {
  if (query.trim().length < 2) return const [];
  return ref.watch(apiClientProvider).get<List<LibraryExercise>>(
        ApiEndpoints.planExercises,
        queryParameters: {'search': query.trim(), 'pageSize': 20},
        parser: (data) => [
          for (final item in (Map<String, dynamic>.from(data as Map)['items'] as List? ?? const []))
            LibraryExercise.fromJson(Map<String, dynamic>.from(item as Map))
        ],
      );
});

/// Writes plans and templates, then refreshes what the screens show.
class PlanApi {
  final Ref _ref;

  PlanApi(this._ref);

  ApiClient get _api => _ref.read(apiClientProvider);

  static TrainingPlanModel _plan(dynamic data) => TrainingPlanModel.fromJson(Map<String, dynamic>.from(data as Map));

  static PlanTemplateModel _template(dynamic data) => PlanTemplateModel.fromJson(Map<String, dynamic>.from(data as Map));

  /// Gives [clientId] the plan (copied from [templateId] when given) starting [startDate].
  Future<TrainingPlanModel> assign({
    required String clientId,
    required PlanDraft draft,
    required DateTime startDate,
    String? templateId,
  }) async {
    final plan = await _api.post<TrainingPlanModel>(ApiEndpoints.trainingPlans,
        data: {
          'traineeId': clientId,
          'startDate': _date(startDate),
          'templateId': templateId,
          'content': draft.toContentJson(),
        },
        parser: _plan);
    _ref.invalidate(traineePlansProvider(clientId));
    return plan;
  }

  /// Saves the coach's edit; a 409 means the plan changed since [version] was loaded.
  Future<TrainingPlanModel> update({required TrainingPlanSummaryModel plan, required PlanDraft draft}) async {
    final saved = await _api.put<TrainingPlanModel>(ApiEndpoints.trainingPlanById(plan.id),
        data: {'expectedVersion': plan.version, 'content': draft.toContentJson()}, parser: _plan);
    _ref.invalidate(traineePlansProvider(plan.traineeId));
    return saved;
  }

  Future<PlanTemplateModel> saveTemplate({String? templateId, required PlanDraft draft, bool asGymTemplate = false}) async {
    final content = draft.toContentJson();
    final saved = templateId == null
        ? await _api.post<PlanTemplateModel>(ApiEndpoints.planTemplates,
            data: {'asGymTemplate': asGymTemplate, 'content': content}, parser: _template)
        : await _api.put<PlanTemplateModel>(ApiEndpoints.planTemplateById(templateId), data: content, parser: _template);
    _ref.invalidate(planTemplatesProvider);
    return saved;
  }

  Future<PlanTemplateModel> savePlanAsTemplate(String planId, String name) async {
    final saved = await _api.post<PlanTemplateModel>(ApiEndpoints.savePlanAsTemplate(planId),
        data: {'asGymTemplate': false, 'name': name}, parser: _template);
    _ref.invalidate(planTemplatesProvider);
    return saved;
  }

  Future<PlanTemplateModel> duplicateTemplate(String templateId, String name) async {
    final saved = await _api.post<PlanTemplateModel>(ApiEndpoints.duplicatePlanTemplate(templateId),
        data: {'asGymTemplate': false, 'name': name}, parser: _template);
    _ref.invalidate(planTemplatesProvider);
    return saved;
  }

  static String _date(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
}

final planApiProvider = Provider<PlanApi>(PlanApi.new);
