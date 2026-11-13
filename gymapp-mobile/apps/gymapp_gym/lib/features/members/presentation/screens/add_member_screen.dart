import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/error_text.dart';
import '../../../../core/providers.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';
import '../providers/members_provider.dart';
import '../widgets/assign_trainer_form.dart';

/// The desk adds a member and, in the same step, assigns their coach and number of
/// sessions. A member has no sessions in the member app until this assignment exists.
class AddMemberScreen extends ConsumerStatefulWidget {
  const AddMemberScreen({super.key});

  @override
  ConsumerState<AddMemberScreen> createState() => _AddMemberScreenState();
}

class _AddMemberScreenState extends ConsumerState<AddMemberScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();
  final _nationalIdController = TextEditingController();
  final _assignment = AssignTrainerDraft();
  bool _loading = false;

  bool get _isArabic => Localizations.localeOf(context).languageCode == 'ar';

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _nationalIdController.dispose();
    _assignment.dispose();
    super.dispose();
  }

  void _showError(Object e) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(staffErrorText(e, isArabic: _isArabic))));
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final memberId = await _createOrLinkMember();
      if (memberId != null) await _assignCoach(memberId);
    } catch (e) {
      _showError(e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// Creates the member (or re-links an existing one). Null when staff cancelled.
  Future<String?> _createOrLinkMember() async {
    final apiClient = ref.read(apiClientProvider);
    final phone = _phoneController.text.trim();
    final Map<String, dynamic> created;
    try {
      created = await apiClient.post<Map<String, dynamic>>(
        ApiEndpoints.identityStaffCreateUser,
        data: _newMemberBody(phone),
        parser: (data) => data as Map<String, dynamic>,
      );
    } on ApiException catch (e) {
      // 409 = phone/email already belongs to an existing member. Offer to re-link
      // them to this gym instead of creating a duplicate (matches staff web).
      if (e.statusCode == 409) return _offerLinkExisting(phone);
      rethrow;
    }

    final userId = created['userId'] as String;
    // staff-create already links the new user to the caller's gym and answers with
    // the member number. Only link explicitly when it did not.
    if (created['memberNumber'] == null) {
      await apiClient.post<void>(
        ApiEndpoints.gymMembers(GymId(ref.read(currentGymIdProvider))),
        data: {'userId': userId},
        parser: (_) {},
      );
    }
    return userId;
  }

  Map<String, dynamic> _newMemberBody(String phone) {
    final email = _emailController.text.trim();
    final nationalId = _nationalIdController.text.trim();
    return {
      'firstName': _firstNameController.text.trim(),
      'lastName': _lastNameController.text.trim(),
      'phone': phone,
      if (email.isNotEmpty) 'email': email,
      if (nationalId.isNotEmpty) 'nationalId': nationalId,
    };
  }

  /// Assigns the chosen coach and sessions. If that fails the member already exists, so
  /// staff get the assign dialog again and land on the member's profile — never a
  /// member silently left without a coach.
  Future<void> _assignCoach(String memberId) async {
    ref.invalidate(membersProvider);
    try {
      await submitQuickAssign(ref, memberId: memberId, draft: _assignment);
      if (mounted) context.pop();
    } catch (e) {
      _showError(e);
      if (!mounted) return;
      await showAssignTrainerDialog(context, ref, memberId: memberId);
      if (mounted) context.pushReplacement('/members/$memberId');
    }
  }

  /// Resolves the existing trainee behind a duplicate phone, asks staff to confirm,
  /// then re-links them to this gym via staff-link-existing (no duplicate created).
  /// Returns the member's user id once linked, or null when nothing was linked.
  Future<String?> _offerLinkExisting(String phone) async {
    final apiClient = ref.read(apiClientProvider);
    final match = await apiClient.get<Map<String, dynamic>>(
      ApiEndpoints.identityStaffLookupUser,
      queryParameters: {'phone': phone},
      parser: (data) => data as Map<String, dynamic>,
    );
    if (!mounted) return null;

    final name = '${match['firstName'] ?? ''} ${match['lastName'] ?? ''}'.trim();
    if (match['isAlreadyMemberOfThisGym'] == true) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(_isArabic ? '$name عضو بالفعل في هذه الصالة' : '$name is already a member of this gym'),
      ));
      return null;
    }

    if (await _confirmLink(name, match['email']) != true) return null;
    await apiClient.post<void>(
      ApiEndpoints.identityStaffLinkExisting,
      data: {'phone': phone},
      parser: (_) {},
    );
    return match['userId'] as String?;
  }

  Future<bool?> _confirmLink(String name, Object? email) {
    final isArabic = _isArabic;
    return showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isArabic ? 'عضو موجود' : 'Existing member'),
        content: Text(isArabic
            ? 'يوجد عضو مسجل بهذا الرقم: $name (${email ?? ''}). إضافته إلى صالتك؟'
            : 'A member with this phone already exists: $name (${email ?? ''}). Add them to your gym?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text(isArabic ? 'إلغاء' : 'Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text(isArabic ? 'إضافة' : 'Add'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = _isArabic;
    final trainersAsync = ref.watch(trainersProvider);
    final trainers = assignableTrainers(trainersAsync.valueOrNull ?? const <UserModel>[]);
    final canSubmit = trainersAsync.hasValue && trainers.isNotEmpty;

    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'اضافة عضو' : 'Add Member')),
      body: Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  ..._memberFields(isArabic),
                  const SizedBox(height: 24),
                  _CoachSection(
                    trainersAsync: trainersAsync,
                    trainers: trainers,
                    draft: _assignment,
                    isArabic: isArabic,
                    onChanged: () => setState(() {}),
                    onRetry: () => ref.invalidate(trainersProvider),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'اضافة العضو' : 'Add Member',
              isLoading: _loading,
              onPressed: canSubmit ? _submit : null,
            ),
          ),
        ],
      ),
    );
  }

  String? _required(String? v) => v == null || v.trim().isEmpty ? (_isArabic ? 'مطلوب' : 'Required') : null;

  // Optional, but if provided it must be a valid address — the backend rejects
  // malformed emails with a 400, so validate inline instead.
  String? _optionalEmail(String? v) {
    final t = v?.trim() ?? '';
    if (t.isEmpty || RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(t)) return null;
    return _isArabic ? 'بريد إلكتروني غير صالح' : 'Invalid email address';
  }

  String? _optionalNationalId(String? v) {
    final t = v?.trim() ?? '';
    if (t.isEmpty || RegExp(r'^\d{14}$').hasMatch(t)) return null;
    return _isArabic ? 'يجب أن يكون 14 رقمًا' : 'Must be 14 digits';
  }

  List<Widget> _memberFields(bool isArabic) => [
        AppTextField(
          controller: _firstNameController,
          label: isArabic ? 'الاسم الاول' : 'First Name',
          validator: _required,
        ),
        const SizedBox(height: 16),
        AppTextField(
          controller: _lastNameController,
          label: isArabic ? 'الاسم الاخير' : 'Last Name',
          validator: _required,
        ),
        const SizedBox(height: 16),
        AppTextField(
          controller: _phoneController,
          label: isArabic ? 'رقم الهاتف' : 'Phone Number',
          keyboardType: TextInputType.phone,
          validator: _required,
        ),
        const SizedBox(height: 16),
        AppTextField(
          controller: _emailController,
          label: isArabic ? 'البريد الالكتروني' : 'Email (optional)',
          keyboardType: TextInputType.emailAddress,
          validator: _optionalEmail,
        ),
        const SizedBox(height: 16),
        AppTextField(
          controller: _nationalIdController,
          label: isArabic ? 'الرقم القومي (اختياري)' : 'National ID (optional)',
          keyboardType: TextInputType.number,
          validator: _optionalNationalId,
        ),
      ];
}

/// "Coach and sessions" part of the form: required, so every new member starts with a
/// coach and a session count chosen by the desk.
class _CoachSection extends StatelessWidget {
  final AsyncValue<List<UserModel>> trainersAsync;
  final List<UserModel> trainers;
  final AssignTrainerDraft draft;
  final bool isArabic;
  final VoidCallback onChanged;
  final VoidCallback onRetry;

  const _CoachSection({
    required this.trainersAsync,
    required this.trainers,
    required this.draft,
    required this.isArabic,
    required this.onChanged,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          isArabic ? 'المدرب والحصص' : 'Coach and sessions',
          style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 8),
        _body(theme),
      ],
    );
  }

  Widget _body(ThemeData theme) {
    if (trainersAsync.isLoading) return const Center(child: CircularProgressIndicator());
    if (trainersAsync.hasError) {
      return AppErrorWidget(
        message: isArabic ? 'تعذر تحميل المدربين' : 'Could not load coaches',
        onRetry: onRetry,
      );
    }
    if (trainers.isEmpty) {
      return Text(noCoachesText(isArabic), style: TextStyle(color: theme.colorScheme.error));
    }
    return AssignTrainerFields(
      draft: draft,
      trainers: trainers,
      isArabic: isArabic,
      onChanged: onChanged,
    );
  }
}
