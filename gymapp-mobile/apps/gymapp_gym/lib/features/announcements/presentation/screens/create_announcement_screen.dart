import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../providers/announcements_provider.dart';
import '../../../../core/error_text.dart';

class CreateAnnouncementScreen extends ConsumerStatefulWidget {
  const CreateAnnouncementScreen({super.key});

  @override
  ConsumerState<CreateAnnouncementScreen> createState() =>
      _CreateAnnouncementScreenState();
}

class _CreateAnnouncementScreenState
    extends ConsumerState<CreateAnnouncementScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _bodyController = TextEditingController();
  String _category = 'general';
  String _targetAudience = 'All';
  bool _loading = false;

  @override
  void dispose() {
    _titleController.dispose();
    _bodyController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final apiClient = ref.read(apiClientProvider);
      // migrated to currentGymIdProvider
      final gymId = ref.read(currentGymIdProvider);
      if (gymId.isEmpty) {
        throw const ApiException(
          message: 'No active gym selected.',
          errorCode: 'NO_ACTIVE_GYM',
        );
      }
      final userId = ref.read(authProvider).valueOrNull?.user?.id;
      if (userId == null || userId.isEmpty) {
        throw const ApiException(
          message: 'User not authenticated.',
          errorCode: 'NO_USER',
        );
      }
      await apiClient.post<void>(
        ApiEndpoints.gymAnnouncements(gymId),
        data: {
          'title': _titleController.text.trim(),
          'content': _bodyController.text.trim(),
          'category': _category,
          'targetAudience': _targetAudience,
          'createdBy': userId,
        },
        parser: (_) {},
      );
      // Force the list screen to refetch — without this the autoDispose
      // FutureProvider would only refresh on next mount, which the user
      // misreads as "create failed".
      ref.invalidate(announcementsProvider);
      if (mounted) {
        context.pop();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              Localizations.localeOf(context).languageCode == 'ar'
                  ? 'تم ارسال الاعلان'
                  : 'Announcement sent',
            ),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(staffErrorText(e))),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'اعلان جديد' : 'New Announcement'),
      ),
      body: Column(
        children: [
          Expanded(
            child: Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AppTextField(
                    controller: _titleController,
                    label: isArabic ? 'العنوان' : 'Title',
                    hint: isArabic ? 'ادخل عنوان الاعلان' : 'Enter announcement title',
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  AppTextField(
                    controller: _bodyController,
                    label: isArabic ? 'المحتوى' : 'Content',
                    hint: isArabic ? 'ادخل محتوى الاعلان' : 'Enter announcement content',
                    maxLines: 5,
                    validator: (v) =>
                        v == null || v.isEmpty ? (isArabic ? 'مطلوب' : 'Required') : null,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    isArabic ? 'التصنيف' : 'Category',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          fontWeight: FontWeight.w500,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      // Codes MUST match the BE AnnouncementCategory enum
                      // (General/Schedule/Offer/Maintenance). Previously the FE
                      // sent 'promotion'/'event', which fail Enum.TryParse on
                      // the backend and were silently saved as General.
                      'general',
                      'offer',
                      'schedule',
                      'maintenance',
                    ].map((cat) {
                      final labels = {
                        'general': isArabic ? 'عام' : 'General',
                        'offer': isArabic ? 'عرض' : 'Offer',
                        'schedule': isArabic ? 'الجدول' : 'Schedule',
                        'maintenance': isArabic ? 'صيانة' : 'Maintenance',
                      };
                      return ChoiceChip(
                        label: Text(labels[cat] ?? cat),
                        selected: _category == cat,
                        onSelected: (_) => setState(() => _category = cat),
                      );
                    }).toList(),
                  ),
                  const SizedBox(height: 16),
                  Text(
                    isArabic ? 'الجمهور المستهدف' : 'Target Audience',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          fontWeight: FontWeight.w500,
                        ),
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      'All',
                      'Members',
                      'Trainers',
                      'Staff',
                    ].map((aud) {
                      final labels = {
                        'All': isArabic ? 'الكل' : 'All',
                        'Members': isArabic ? 'الأعضاء' : 'Members',
                        'Trainers': isArabic ? 'المدربون' : 'Trainers',
                        'Staff': isArabic ? 'الموظفون' : 'Staff',
                      };
                      return ChoiceChip(
                        label: Text(labels[aud] ?? aud),
                        selected: _targetAudience == aud,
                        onSelected: (_) => setState(() => _targetAudience = aud),
                      );
                    }).toList(),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppButton(
              text: isArabic ? 'ارسال الاعلان' : 'Send Announcement',
              isLoading: _loading,
              onPressed: _submit,
            ),
          ),
        ],
      ),
    );
  }
}
