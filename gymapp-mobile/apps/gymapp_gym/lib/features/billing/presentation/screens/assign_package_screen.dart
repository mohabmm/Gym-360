import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/billing_provider.dart';
import '../../../members/presentation/providers/members_provider.dart';
import '../../../trainers/presentation/providers/trainers_provider.dart';
import '../../../../core/error_text.dart';

class AssignPackageScreen extends ConsumerStatefulWidget {
  const AssignPackageScreen({super.key});

  @override
  ConsumerState<AssignPackageScreen> createState() =>
      _AssignPackageScreenState();
}

class _AssignPackageScreenState extends ConsumerState<AssignPackageScreen> {
  final _memberIdController = TextEditingController();
  String? _selectedTemplateId;
  String? _selectedTrainerId;
  bool _loading = false;

  @override
  void dispose() {
    _memberIdController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_memberIdController.text.isEmpty ||
        _selectedTemplateId == null ||
        _selectedTrainerId == null) return;
    setState(() => _loading = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      final gymId = ref.read(currentGymIdProvider);
      final userId = ref.read(authProvider).valueOrNull?.user?.id ?? '';

      final memberInput = _memberIdController.text.trim();
      final match = await findMemberByNumber(apiClient, gymId, memberInput);
      if (match == null) {
        throw Exception(
          Localizations.localeOf(context).languageCode == 'ar'
              ? 'العضو غير موجود'
              : 'Member not found',
        );
      }

      await apiClient.post<void>(
        ApiEndpoints.assignPackage,
        data: {
          'gymId': gymId,
          'memberId': match.id,
          'trainerId': _selectedTrainerId,
          'templateId': _selectedTemplateId,
          'assignedBy': userId,
        },
        parser: (_) {},
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'تم تعيين الباقة'
                  : 'Package assigned',
            ),
          ),
        );
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(staffErrorText(e))));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final templates = ref.watch(packageTemplatesProvider);
    final trainers = ref.watch(trainersProvider);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'تعيين باقة' : 'Assign Package'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          AppTextField(
            controller: _memberIdController,
            label: isArabic ? 'رقم العضو' : 'Member Number / ID',
          ),
          const SizedBox(height: 16),
          Text(
            isArabic ? 'اختر المدرب' : 'Select Trainer',
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
          ),
          const SizedBox(height: 8),
          trainers.when(
            data: (items) => DropdownButtonFormField<String>(
              value: _selectedTrainerId,
              hint: Text(isArabic ? 'اختر المدرب' : 'Choose trainer'),
              items: items
                  .map((t) => DropdownMenuItem(
                        value: t.id,
                        child: Text('${t.firstName} ${t.lastName}'),
                      ))
                  .toList(),
              onChanged: (v) => setState(() => _selectedTrainerId = v),
            ),
            loading: () => const LinearProgressIndicator(),
            error: (_, __) => Text(isArabic ? 'تعذر التحميل' : 'Failed to load'),
          ),
          const SizedBox(height: 20),
          Text(
            isArabic ? 'اختر الباقة' : 'Select Package',
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
          ),
          const SizedBox(height: 12),
          templates.when(
            data: (items) => Column(
              children: items.map((pkg) {
                final isSelected = _selectedTemplateId == pkg.id;
                return AppCard(
                  margin: const EdgeInsets.only(bottom: 8),
                  onTap: () => setState(() => _selectedTemplateId = pkg.id),
                  border: isSelected
                      ? Border.all(
                          color: Theme.of(context).colorScheme.primary,
                          width: 2,
                        )
                      : null,
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: isSelected
                        ? Icon(Icons.check_circle,
                            color: Theme.of(context).colorScheme.primary)
                        : const Icon(Icons.radio_button_unchecked),
                    title: Text(pkg.name),
                    subtitle: Text(
                        '${pkg.sessionCount} ${isArabic ? 'حصة' : 'sessions'}'),
                    trailing: Text(NumberFormatter.formatCurrency(pkg.totalPrice)),
                  ),
                );
              }).toList(),
            ),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (_, __) => Text(isArabic ? 'تعذر التحميل' : 'Failed to load'),
          ),
          const SizedBox(height: 32),
          AppButton(
            text: isArabic ? 'تعيين الباقة' : 'Assign Package',
            isLoading: _loading,
            onPressed: _selectedTemplateId != null && _selectedTrainerId != null
                ? _submit
                : null,
          ),
        ],
      ),
    );
  }
}
