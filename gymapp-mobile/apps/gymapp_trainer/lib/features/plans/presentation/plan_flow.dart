import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import '../data/plan_access.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'plan_builder_screen.dart';
import 'start_plan_sheet.dart';
import '../../nutrition/diet/diet_flow.dart';

/// "Give a plan": pick blank, a ready program or a template, then the builder opens ready to assign.
/// pt.starter_plan: a 3-day starter plan (blank or a ready program's first days, no template)
/// until the desk unlocks this client; each unlock allows one week from a template or program.
Future<void> startPlanForClient(BuildContext context, WidgetRef ref, String clientId) async {
  final access = await ref.read(coachPlanAccessProvider(clientId).future);
  if (!context.mounted) return;
  final choice = await showStartPlanSheet(context, limit: access.limit);
  if (choice == null || !context.mounted) return;
  final draft = await _draftFor(ref, choice);
  if (!context.mounted) return;
  draft.limitTo(access.limit);
  await _assign(
      context,
      ref,
      clientId,
      PlanBuilderArgs(
        mode: PlanBuilderMode.assign,
        draft: draft,
        clientId: clientId,
        templateId: access.isStarter ? null : choice.template?.id,
      ));
}

/// The builder's starting draft for what the coach picked.
Future<PlanDraft> _draftFor(WidgetRef ref, StartChoice choice) async {
  final program = choice.program;
  if (program != null) return PlanDraft.fromProgram(await ref.read(planProgramProvider(program.key).future));
  final template = choice.template;
  if (template == null) return PlanDraft.blank();
  return PlanDraft.fromTemplate(await ref.read(planTemplateProvider(template.id).future));
}

/// Opens the builder; once the plan is given, onboarding goes on to the diet (when nutrition is on).
Future<void> _assign(BuildContext context, WidgetRef ref, String clientId, PlanBuilderArgs args) async {
  final given = await context.push<bool>(AppRoutes.planBuilder, extra: args);
  ref.invalidate(coachPlanAccessProvider(clientId));
  if (given == true && context.mounted) await offerDietAfterPlan(context, ref, clientId);
}

/// Opens the client's active plan in the builder to edit it. A 3-day plan or one-week program
/// stays within its limit.
Future<void> editClientPlan(BuildContext context, TrainingPlanModel plan) {
  final draft = PlanDraft.fromPlan(plan)..limitTo(PlanLimit.parse(plan.summary.limit));
  return context.push(AppRoutes.planBuilder,
      extra: PlanBuilderArgs(mode: PlanBuilderMode.editPlan, draft: draft, plan: plan.summary));
}
