import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/router.dart';
import '../data/plan_api.dart';
import '../data/plan_draft.dart';
import 'plan_builder_screen.dart';
import 'start_plan_sheet.dart';
import 'trainer_plan_strings.dart';

/// The coach's template library: gym templates (read-only for coaches) and their own.
class PlanTemplatesScreen extends ConsumerWidget {
  const PlanTemplatesScreen({super.key});

  Future<void> _open(BuildContext context, WidgetRef ref, TrainerPlanStrings s, PlanTemplateSummaryModel t) async {
    if (!t.canEdit) {
      await _offerCopy(context, ref, s, t);
      return;
    }
    final detail = await ref.read(planTemplateProvider(t.id).future);
    if (!context.mounted) return;
    await context.push(AppRoutes.planBuilder,
        extra: PlanBuilderArgs(
          mode: PlanBuilderMode.editTemplate,
          templateId: t.id,
          draft: PlanDraft.fromTemplate(detail),
        ));
  }

  Future<void> _offerCopy(BuildContext context, WidgetRef ref, TrainerPlanStrings s, PlanTemplateSummaryModel t) async {
    final copy = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(t.displayName(s.isArabic)),
        content: Text(s.gymTemplateReadOnly),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: Text(MaterialLocalizations.of(context).cancelButtonLabel)),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: Text(s.makeMyCopy)),
        ],
      ),
    );
    if (copy != true || !context.mounted) return;
    await ref.read(planApiProvider).duplicateTemplate(t.id, s.templateCopyName(t.name));
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final s = TrainerPlanStrings(isArabic);
    final templates = ref.watch(planTemplatesProvider);
    return RtlScaffold(
      isArabic: isArabic,
      appBar: AppBar(title: Text(s.templates)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.planBuilder,
            extra: PlanBuilderArgs(mode: PlanBuilderMode.newTemplate, draft: PlanDraft.blank())),
        icon: const Icon(Icons.add),
        label: Text(s.newPlan),
      ),
      body: templates.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => Center(child: Text(s.saveFailed)),
        data: (list) => RefreshIndicator(
          onRefresh: () => ref.refresh(planTemplatesProvider.future),
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            children: [
              for (final t in list) PlanTemplateTile(template: t, strings: s, onTap: () => _open(context, ref, s, t)),
            ],
          ),
        ),
      ),
    );
  }
}
