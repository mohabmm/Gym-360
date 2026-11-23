import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'plan_builder_screen.dart';
import 'start_plan_sheet.dart';

/// "Give a plan": pick blank or a template, then the builder opens ready to assign.
Future<void> startPlanForClient(BuildContext context, WidgetRef ref, String clientId) async {
  final choice = await showStartPlanSheet(context);
  if (choice == null || !context.mounted) return;
  final template = choice.template;
  if (template == null) {
    await context.push(AppRoutes.planBuilder,
        extra: PlanBuilderArgs(mode: PlanBuilderMode.assign, draft: PlanDraft.blank(), clientId: clientId));
    return;
  }
  final detail = await ref.read(planTemplateProvider(template.id).future);
  if (!context.mounted) return;
  await context.push(AppRoutes.planBuilder,
      extra: PlanBuilderArgs(
        mode: PlanBuilderMode.assign,
        draft: PlanDraft.fromTemplate(detail),
        clientId: clientId,
        templateId: detail.summary.id,
      ));
}

/// Opens the client's active plan in the builder to edit it.
Future<void> editClientPlan(BuildContext context, TrainingPlanModel plan) => context.push(AppRoutes.planBuilder,
    extra: PlanBuilderArgs(
      mode: PlanBuilderMode.editPlan,
      draft: PlanDraft.fromPlan(plan),
      plan: plan.summary,
    ));
