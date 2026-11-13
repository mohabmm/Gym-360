import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';

/// An inclusive range of session counts the desk may pick.
class SessionRange {
  final int min;
  final int max;
  const SessionRange(this.min, this.max);

  bool contains(int? count) => count != null && count >= min && count <= max;
}

/// Desk-chosen session counts: any number in this range (free or paid).
const SessionRange kAssignSessionRange = SessionRange(1, 100);

/// Free trial (pt.trial_sessions): 1–3 sessions, once per trainee.
const SessionRange kTrialSessionRange = SessionRange(1, 3);

/// Payment methods the backend accepts (wallets/InstaPay are 'Online'), with (Arabic, English) labels.
const Map<String, (String, String)> kAssignPaymentMethods = {
  'Cash': ('نقدي', 'Cash'),
  'Card': ('بطاقة', 'Card'),
  'BankTransfer': ('تحويل بنكي', 'Bank Transfer'),
  'Online': ('محفظة الكترونية / انستاباي', 'Mobile wallet / InstaPay'),
};

/// Trainers the desk can assign: everyone except pending or rejected sign-ups.
List<UserModel> assignableTrainers(List<UserModel> trainers) => trainers.where((t) {
      final status = (t.status ?? '').toLowerCase();
      return status != 'pending_approval' && status != 'rejected';
    }).toList();

/// What the desk picked: coach, session count, optional price, and trial mode.
class AssignTrainerDraft {
  String? trainerId;
  bool isTrial = false;
  String paymentMethod = 'Cash';
  final TextEditingController sessions = TextEditingController();
  final TextEditingController price = TextEditingController();

  int? get sessionCount => int.tryParse(sessions.text.trim());

  /// Null when blank (free) or not a number.
  double? get totalPrice => double.tryParse(price.text.trim());

  bool get isPaid => !isTrial && (totalPrice ?? 0) > 0;

  void dispose() {
    sessions.dispose();
    price.dispose();
  }
}

/// Validation for the session count: trial 1–3, otherwise 1–100.
String? validateSessionCount(String? value, {required bool isTrial, required bool isArabic}) {
  final range = isTrial ? kTrialSessionRange : kAssignSessionRange;
  if (range.contains(int.tryParse(value?.trim() ?? ''))) return null;
  return isArabic ? 'أدخل عددًا من ${range.min} إلى ${range.max}' : 'Enter a number from ${range.min} to ${range.max}';
}

/// Validation for the optional total price: blank means free.
String? validatePrice(String? value, {required bool isArabic}) {
  final text = value?.trim() ?? '';
  if (text.isEmpty) return null;
  final price = double.tryParse(text);
  return price == null || price < 0 ? (isArabic ? 'سعر غير صالح' : 'Invalid price') : null;
}

/// Who the package is for, in which gym, and which desk user assigns it.
typedef QuickAssignTarget = ({String gymId, String memberId, String assignedBy});

/// Body for POST /billing/packages/quick-assign.
Map<String, dynamic> quickAssignPayload(QuickAssignTarget target, AssignTrainerDraft draft) => {
      'gymId': target.gymId,
      'trainerId': draft.trainerId,
      'memberId': target.memberId,
      'sessionCount': draft.sessionCount,
      'assignedBy': target.assignedBy,
      if (draft.isTrial) 'isTrial': true,
      if (draft.isPaid) 'totalPrice': draft.totalPrice,
      if (draft.isPaid) 'paymentMethod': draft.paymentMethod,
    };

/// Creates the member's package with the chosen coach (the only way a member gets sessions).
Future<void> submitQuickAssign(WidgetRef ref, {required String memberId, required AssignTrainerDraft draft}) async {
  final assignedBy = ref.read(authProvider).valueOrNull?.user?.id;
  if (assignedBy == null) {
    throw const ApiException(message: 'No active session.', errorCode: 'NO_SESSION');
  }
  await ref.read(apiClientProvider).post<void>(
        ApiEndpoints.quickAssignPackage,
        data: quickAssignPayload(
          (gymId: ref.read(currentGymIdProvider), memberId: memberId, assignedBy: assignedBy),
          draft,
        ),
        parser: (_) {},
      );
}

/// Coach picker, session count, and optional price/payment fields. Place inside a [Form].
class AssignTrainerFields extends StatelessWidget {
  final AssignTrainerDraft draft;
  final List<UserModel> trainers;
  final bool isArabic;
  final bool allowTrial;
  final VoidCallback onChanged;

  const AssignTrainerFields({
    super.key,
    required this.draft,
    required this.trainers,
    required this.isArabic,
    required this.onChanged,
    this.allowTrial = false,
  });

  void _update(void Function() change) {
    change();
    onChanged();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _trainerPicker(),
        if (allowTrial) _trialSwitch(),
        const SizedBox(height: 12),
        _sessionsField(),
        if (!draft.isTrial) ..._priceFields(),
      ],
    );
  }

  Widget _trainerPicker() => DropdownButtonFormField<String>(
        key: const ValueKey('assign-trainer'),
        initialValue: draft.trainerId,
        isExpanded: true,
        decoration: InputDecoration(labelText: isArabic ? 'المدرب' : 'Coach'),
        hint: Text(isArabic ? 'اختر المدرب' : 'Pick a coach'),
        items: [
          for (final t in trainers)
            DropdownMenuItem(value: t.id, child: Text('${t.firstName} ${t.lastName}'.trim())),
        ],
        validator: (v) => v == null ? (isArabic ? 'اختر المدرب' : 'Pick a coach') : null,
        onChanged: (v) => _update(() => draft.trainerId = v),
      );

  Widget _trialSwitch() => Padding(
        padding: const EdgeInsets.only(top: 8),
        child: SwitchListTile(
          contentPadding: EdgeInsets.zero,
          value: draft.isTrial,
          title: Text(isArabic ? 'حصص تجريبية مجانية' : 'Free trial'),
          subtitle: Text(isArabic ? 'من ١ إلى ٣ حصص، مرة واحدة لكل متدرب' : '1–3 sessions, once per trainee'),
          onChanged: (v) => _update(() => draft.isTrial = v),
        ),
      );

  Widget _sessionsField() => TextFormField(
        key: const ValueKey('assign-sessions'),
        controller: draft.sessions,
        keyboardType: TextInputType.number,
        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
        decoration: InputDecoration(
          labelText: isArabic ? 'عدد الحصص' : 'Number of sessions',
          hintText: draft.isTrial
              ? (isArabic ? 'من ١ إلى ٣' : '1 to 3')
              : (isArabic ? 'من ١ إلى ١٠٠' : '1 to 100'),
        ),
        validator: (v) => validateSessionCount(v, isTrial: draft.isTrial, isArabic: isArabic),
      );

  List<Widget> _priceFields() => [
        const SizedBox(height: 12),
        TextFormField(
          key: const ValueKey('assign-price'),
          controller: draft.price,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(
            labelText: isArabic ? 'السعر الإجمالي (ج.م)' : 'Total price (EGP)',
            hintText: isArabic ? 'اتركه فارغًا لحصص مجانية' : 'Leave blank for free sessions',
          ),
          validator: (v) => validatePrice(v, isArabic: isArabic),
          onChanged: (_) => onChanged(),
        ),
        if (draft.isPaid) ...[
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            initialValue: draft.paymentMethod,
            isExpanded: true,
            decoration: InputDecoration(labelText: isArabic ? 'طريقة الدفع' : 'Payment method'),
            items: [
              for (final MapEntry(key: method, value: (ar, en)) in kAssignPaymentMethods.entries)
                DropdownMenuItem(value: method, child: Text(isArabic ? ar : en)),
            ],
            onChanged: (v) => _update(() => draft.paymentMethod = v ?? 'Cash'),
          ),
        ],
      ];
}

/// The member-profile "Assign Trainer" dialog. Returns true when a package was created.
Future<bool> showAssignTrainerDialog(BuildContext context, WidgetRef ref, {required String memberId}) async {
  final isArabic = Localizations.localeOf(context).languageCode == 'ar';
  final trainers = assignableTrainers(
    await ref.read(trainersProvider.future).catchError((_) => <UserModel>[]),
  );
  if (!context.mounted) return false;
  final messenger = ScaffoldMessenger.of(context);
  if (trainers.isEmpty) {
    messenger.showSnackBar(SnackBar(content: Text(noCoachesText(isArabic))));
    return false;
  }

  final draft = AssignTrainerDraft();
  final allowTrial = ref.read(featureFlagsProvider).isEnabled(FeatureFlagKeys.ptTrialSessions);
  try {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => _AssignTrainerDialog(draft: draft, trainers: trainers, allowTrial: allowTrial),
    );
    if (confirmed != true) return false;
    await submitQuickAssign(ref, memberId: memberId, draft: draft);
    messenger.showSnackBar(SnackBar(
      content: Text(draft.isTrial
          ? (isArabic ? 'تم تعيين الحصص التجريبية' : 'Free trial assigned')
          : (isArabic ? 'تم التعيين' : 'Trainer assigned')),
    ));
    return true;
  } catch (e) {
    messenger.showSnackBar(SnackBar(content: Text(staffErrorText(e, isArabic: isArabic))));
    return false;
  } finally {
    draft.dispose();
  }
}

/// Shown when the gym has no approved coach to assign.
String noCoachesText(bool isArabic) =>
    isArabic ? 'لا يوجد مدربون معتمدون. أضف مدربًا أولاً.' : 'No approved coaches yet. Add a coach first.';

class _AssignTrainerDialog extends StatefulWidget {
  final AssignTrainerDraft draft;
  final List<UserModel> trainers;
  final bool allowTrial;

  const _AssignTrainerDialog({required this.draft, required this.trainers, required this.allowTrial});

  @override
  State<_AssignTrainerDialog> createState() => _AssignTrainerDialogState();
}

class _AssignTrainerDialogState extends State<_AssignTrainerDialog> {
  final _formKey = GlobalKey<FormState>();

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    return AlertDialog(
      title: Text(isArabic ? 'تعيين مدرب' : 'Assign Trainer'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: AssignTrainerFields(
            draft: widget.draft,
            trainers: widget.trainers,
            isArabic: isArabic,
            allowTrial: widget.allowTrial,
            onChanged: () => setState(() {}),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: Text(isArabic ? 'الغاء' : 'Cancel'),
        ),
        TextButton(
          onPressed: () {
            if (_formKey.currentState!.validate()) Navigator.of(context).pop(true);
          },
          child: Text(isArabic ? 'تعيين' : 'Assign'),
        ),
      ],
    );
  }
}
