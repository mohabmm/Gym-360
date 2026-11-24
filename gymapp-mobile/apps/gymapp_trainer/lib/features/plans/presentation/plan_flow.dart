import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'plan_builder_screen.dart';
import 'start_plan_sheet.dart';
import '../../nutrition/diet/diet_flow.dart';

/// "Give a plan": pick blank, a ready program or a template, then the builder opens ready to assign.
Future<void> startPlanForClient(BuildContext context, WidgetRef ref, String clientId) async {
  final choice = await showStartPlanSheet(context);
  if (choice == null || !context.mounted) return;
  final program = choice.program;
  if (program != null) {
    final detail = await ref.read(planProgramProvider(program.key).future);
    if (!context.mounted) return;
    await _assign(context, ref, clientId,
        PlanBuilderArgs(mode: PlanBuilderMode.assign, draft: PlanDraft.fromProgram(detail), clientId: clientId));
    return;
  }
  final template = choice.template;
  if (template == null) {
    await _assign(context, ref, clientId, PlanBuilderArgs(mode: PlanBuilderMode.assign, draft: PlanDraft.blank(), clientId: clientId));
    return;
  }
  final detail = await ref.read(planTemplateProvider(template.id).future);
  if (!context.mounted) return;
  await _assign(
      context,
      ref,
      clientId,
      PlanBuilderArgs(
        mode: PlanBuilderMode.assign,
        draft: PlanDraft.fromTemplate(detail),
        clientId: clientId,
        templateId: detail.summary.id,
      ));
}

/// Opens the builder; once the plan is given, onboarding goes on to the diet (when nutrition is on).
Future<void> _assign(BuildContext context, WidgetRef ref, String clientId, PlanBuilderArgs args) async {
  final given = await context.push<bool>(AppRoutes.planBuilder, extra: args);
  if (given == true && context.mounted) await offerDietAfterPlan(context, ref, clientId);
}

/// Opens the client's active plan in the builder to edit it.
Future<void> editClientPlan(BuildContext context, TrainingPlanModel plan) => context.push(AppRoutes.planBuilder,
    extra: PlanBuilderArgs(
      mode: PlanBuilderMode.editPlan,
      draft: PlanDraft.fromPlan(plan),
      plan: plan.summary,
    ));
