import 'package:flutter/material.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// "1  Member" — numbered step title; greyed out until the previous step is done.
class StepTitle extends StatelessWidget {
  final int number;
  final String title;
  final bool enabled;

  const StepTitle({super.key, required this.number, required this.title, this.enabled = true});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = enabled ? theme.colorScheme.primary : AppColors.grey400;
    return Padding(
      padding: const EdgeInsets.only(top: 20, bottom: 8),
      child: Row(children: [
        CircleAvatar(
          radius: 12,
          backgroundColor: color,
          child: Text('$number', style: const TextStyle(color: AppColors.white, fontSize: 12)),
        ),
        const SizedBox(width: 8),
        Text(title,
            style: theme.textTheme.titleSmall?.copyWith(
              fontWeight: FontWeight.w700,
              color: enabled ? null : AppColors.grey400,
            )),
      ]),
    );
  }
}

/// The member's current coach: a new package stays with them (one coach at a time).
class LockedCoachTile extends StatelessWidget {
  final SessionPackageModel current;
  final bool isArabic;

  const LockedCoachTile({super.key, required this.current, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final name = current.trainerName ?? (isArabic ? 'المدرب الحالي' : 'Current coach');
    return AppCard(
      key: const ValueKey('locked-coach'),
      child: ListTile(
        contentPadding: EdgeInsets.zero,
        leading: const Icon(Icons.lock_outline),
        title: Text(isArabic ? 'الكابتن $name' : 'Coach $name', style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text(isArabic
            ? 'متبقي له ${current.remainingSessions} حصة مع هذا المدرب. الباقة الجديدة تكون مع نفس المدرب (مدرب واحد في كل مرة).'
            : '${current.remainingSessions} sessions left with this coach. The new package stays with the same coach (one coach at a time).'),
      ),
    );
  }
}

/// One package template as a selectable card: name, sessions, per-session and total price.
class TemplateCard extends StatelessWidget {
  final SessionPackageTemplateModel template;
  final bool selected;
  final bool isArabic;
  final VoidCallback onTap;

  const TemplateCard({
    super.key,
    required this.template,
    required this.selected,
    required this.isArabic,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final perSession = template.sessionCount == 0 ? 0.0 : template.totalPrice / template.sessionCount;
    final name = isArabic && (template.nameAr ?? '').isNotEmpty ? template.nameAr! : template.name;
    return AppCard(
      key: ValueKey('template-${template.id}'),
      margin: const EdgeInsets.only(bottom: 8),
      onTap: onTap,
      border: Border.all(
        color: selected ? theme.colorScheme.primary : AppColors.grey200,
        width: selected ? 2 : 1,
      ),
      child: ListTile(
        contentPadding: EdgeInsets.zero,
        leading: Icon(selected ? Icons.check_circle : Icons.radio_button_unchecked,
            color: selected ? theme.colorScheme.primary : AppColors.grey400),
        title: Text(name, style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text(isArabic
            ? '${template.sessionCount} حصة · ${NumberFormatter.formatCurrency(Egp(perSession))} للحصة'
            : '${template.sessionCount} sessions · ${NumberFormatter.formatCurrency(Egp(perSession))} / session'),
        trailing: Text(NumberFormatter.formatCurrency(Egp(template.totalPrice)),
            style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w800)),
      ),
    );
  }
}

/// Grey hint shown in a step that can't be filled yet.
class StepHint extends StatelessWidget {
  final String text;

  const StepHint(this.text, {super.key});

  @override
  Widget build(BuildContext context) =>
      Text(text, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.grey500));
}
