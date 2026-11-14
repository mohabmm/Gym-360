import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../../../members/presentation/providers/members_provider.dart';
import '../../../members/presentation/widgets/current_coach.dart';
import '../../../members/presentation/widgets/member_picker.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';
import '../providers/billing_provider.dart';
import '../widgets/assign_package_sections.dart';

/// Sell a session package at the desk: 1) find the member, 2) the coach (locked to the member's
/// current coach while they have sessions left), 3) the package; then confirm.
/// Opened from More (search the member) or from a member's profile ([initialMemberId]).
class AssignPackageScreen extends ConsumerStatefulWidget {
  final String? initialMemberId;

  const AssignPackageScreen({super.key, this.initialMemberId});

  @override
  ConsumerState<AssignPackageScreen> createState() => _AssignPackageScreenState();
}

class _AssignPackageScreenState extends ConsumerState<AssignPackageScreen> {
  UserModel? _picked;
  bool _initialCleared = false;
  String? _pickedTrainerId;
  String? _templateId;
  bool _loading = false;

  bool get _ar => Localizations.localeOf(context).languageCode == 'ar';

  /// The picked member, or (from a member's profile) that member until staff tap "Change".
  UserModel? get _member {
    final initial = widget.initialMemberId;
    if (_picked != null || initial == null || _initialCleared) return _picked;
    return ref.watch(memberDetailProvider(initial)).valueOrNull;
  }

  /// The member's current coach package (sessions left), or null.
  SessionPackageModel? get _currentCoach {
    final m = _member;
    return m == null ? null : ref.watch(memberCurrentCoachProvider(m.id)).valueOrNull;
  }

  String? get _trainerId => _currentCoach?.trainerId ?? _pickedTrainerId;

  SessionPackageTemplateModel? get _template =>
      ref.watch(packageTemplatesProvider).valueOrNull?.where((t) => t.id == _templateId).firstOrNull;

  String? _trainerName(String? id) {
    final coach = _currentCoach;
    if (coach != null) return coach.trainerName;
    final t = ref.read(trainersProvider).valueOrNull?.where((t) => t.id == id).firstOrNull;
    return t == null ? null : '${t.firstName} ${t.lastName}';
  }

  void _selectMember(UserModel? m) => setState(() {
        _picked = m;
        _initialCleared = true;
        _pickedTrainerId = null;
      });

  Future<void> _confirmAndSubmit() async {
    final member = _member!;
    final template = _template!;
    final coach = _trainerName(_trainerId) ?? '';
    final price = NumberFormatter.formatCurrency(Egp(template.totalPrice));
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(_ar ? 'تأكيد تعيين الباقة' : 'Confirm package'),
        content: Text(_ar
            ? '${template.sessionCount} حصة مع الكابتن $coach للعضو ${memberName(member)} مقابل $price.'
            : '${template.sessionCount} sessions with Coach $coach for ${memberName(member)} — $price.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: Text(_ar ? 'رجوع' : 'Back')),
          FilledButton(onPressed: () => Navigator.pop(dialogContext, true), child: Text(_ar ? 'تأكيد' : 'Confirm')),
        ],
      ),
    );
    if (ok == true) await _submit();
  }

  Future<void> _submit() async {
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    final ar = _ar;
    setState(() => _loading = true);
    try {
      // The backend records the signed-in staff member as "assigned by".
      await ref.read(apiClientProvider).post<void>(
        ApiEndpoints.assignPackage,
        data: {
          'gymId': ref.read(currentGymIdProvider),
          'memberId': _member!.id,
          'trainerId': _trainerId,
          'templateId': _templateId,
        },
        parser: (_) {},
      );
      ref.invalidate(memberCurrentCoachProvider(_member!.id));
      messenger.showSnackBar(SnackBar(content: Text(ar ? 'تم تعيين الباقة' : 'Package assigned')));
      navigator.pop();
    } catch (e) {
      messenger.showSnackBar(SnackBar(content: Text(staffErrorText(e, isArabic: ar))));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final canSubmit = _member != null && _trainerId != null && _templateId != null && !_loading;
    return Scaffold(
      appBar: AppBar(title: Text(_ar ? 'تعيين باقة' : 'Assign package')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
        children: [
          StepHint(_ar
              ? 'بيع باقة حصص تدريب شخصي لعضو: اختر العضو، ثم المدرب، ثم الباقة.'
              : 'Sell a personal-training package: pick the member, then the coach, then the package.'),
          StepTitle(number: 1, title: _ar ? 'العضو' : 'Member'),
          MemberPicker(selected: _member, onChanged: _selectMember, isArabic: _ar),
          StepTitle(number: 2, title: _ar ? 'المدرب' : 'Coach', enabled: _member != null),
          _coachStep(),
          StepTitle(number: 3, title: _ar ? 'الباقة' : 'Package'),
          _templatesStep(),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
          child: AppButton(
            key: const ValueKey('assign-package'),
            text: _buttonLabel(),
            isLoading: _loading,
            onPressed: canSubmit ? _confirmAndSubmit : null,
          ),
        ),
      ),
    );
  }

  String _buttonLabel() {
    final t = _template;
    if (t == null) return _ar ? 'تعيين الباقة' : 'Assign package';
    final price = NumberFormatter.formatCurrency(Egp(t.totalPrice));
    return _ar ? 'تعيين الباقة · $price' : 'Assign package · $price';
  }

  Widget _coachStep() {
    final member = _member;
    if (member == null) return StepHint(_ar ? 'اختر العضو أولاً' : 'Pick the member first');
    final current = ref.watch(memberCurrentCoachProvider(member.id));
    if (current.isLoading) return const LinearProgressIndicator();
    final coach = current.valueOrNull;
    if (coach != null) return LockedCoachTile(current: coach, isArabic: _ar);
    return ref.watch(trainersProvider).when(
          data: (trainers) => DropdownButtonFormField<String>(
            key: const ValueKey('trainer-dropdown'),
            initialValue: _pickedTrainerId,
            hint: Text(_ar ? 'اختر المدرب' : 'Choose coach'),
            items: [
              for (final t in trainers) DropdownMenuItem(value: t.id, child: Text('${t.firstName} ${t.lastName}')),
            ],
            onChanged: (v) => setState(() => _pickedTrainerId = v),
          ),
          loading: () => const LinearProgressIndicator(),
          error: (_, __) => StepHint(_ar ? 'تعذر تحميل المدربين' : "Couldn't load coaches"),
        );
  }

  Widget _templatesStep() => ref.watch(packageTemplatesProvider).when(
        data: (all) {
          final templates = all.where((t) => t.isActive).toList();
          if (templates.isEmpty) {
            return StepHint(_ar
                ? 'لا توجد باقات بعد. أنشئ باقة من «قوالب الباقات».'
                : 'No packages yet. Create one in "Package Templates".');
          }
          return Column(children: [
            for (final t in templates)
              TemplateCard(
                template: t,
                selected: t.id == _templateId,
                isArabic: _ar,
                onTap: () => setState(() => _templateId = t.id),
              ),
          ]);
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => StepHint(_ar ? 'تعذر تحميل الباقات' : "Couldn't load packages"),
      );
}
