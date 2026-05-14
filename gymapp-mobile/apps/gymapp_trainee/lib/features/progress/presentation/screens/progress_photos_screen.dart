import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../../../../core/providers.dart';
import '../providers/progress_providers.dart';

class ProgressPhotosScreen extends ConsumerWidget {
  const ProgressPhotosScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final photosAsync = ref.watch(progressPhotosProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'صور التقدم' : 'Progress Photos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_a_photo),
            onPressed: () => _pickAndUpload(context, ref, isArabic),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(progressPhotosProvider),
        child: photosAsync.when(
          data: (photoSets) {
            if (photoSets.isEmpty) {
              return EmptyState(
                icon: Icons.photo_library_outlined,
                title: isArabic ? 'لا توجد صور بعد' : 'No photos yet',
                subtitle: isArabic
                    ? 'ارفع صورتك الأولى لمتابعة تقدمك'
                    : 'Upload your first photo to track your progress',
              );
            }
            return GridView.builder(
              padding: const EdgeInsets.all(8),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                crossAxisSpacing: 4,
                mainAxisSpacing: 4,
              ),
              itemCount: photoSets.length,
              itemBuilder: (context, index) {
                final set = photoSets[index];
                final photo = set.front ?? set.side ?? set.back;
                if (photo == null) return const SizedBox.shrink();

                return GestureDetector(
                  onTap: () => _showPhotoDetail(context, set, isArabic),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: Stack(
                      fit: StackFit.expand,
                      children: [
                        Image.network(
                          photo.thumbnailUrl ?? photo.imageUrl,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => Container(
                            color: Theme.of(context)
                                .colorScheme
                                .surfaceVariant,
                            child: const Icon(Icons.image_not_supported),
                          ),
                        ),
                        Positioned(
                          bottom: 4,
                          left: 4,
                          right: 4,
                          child: Text(
                            DateFormat('dd/MM').format(set.date),
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 10,
                              shadows: [
                                Shadow(
                                    color: Colors.black,
                                    offset: Offset(0, 1),
                                    blurRadius: 2),
                              ],
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              },
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (error, _) => AppErrorWidget(
            message: error.toString(),
            onRetry: () => ref.invalidate(progressPhotosProvider),
          ),
        ),
      ),
    );
  }

  Future<void> _pickAndUpload(
      BuildContext context, WidgetRef ref, bool isArabic) async {
    final picker = ImagePicker();
    final XFile? file =
        await picker.pickImage(source: ImageSource.gallery, imageQuality: 80);
    if (file == null) return;

    // Show type picker
    if (!context.mounted) return;
    final type = await showDialog<String>(
      context: context,
      builder: (ctx) => SimpleDialog(
        title: Text(isArabic ? 'نوع الصورة' : 'Photo Type'),
        children: [
          SimpleDialogOption(
            onPressed: () => Navigator.pop(ctx, 'front'),
            child: Text(isArabic ? 'أمامية' : 'Front'),
          ),
          SimpleDialogOption(
            onPressed: () => Navigator.pop(ctx, 'side'),
            child: Text(isArabic ? 'جانبية' : 'Side'),
          ),
          SimpleDialogOption(
            onPressed: () => Navigator.pop(ctx, 'back'),
            child: Text(isArabic ? 'خلفية' : 'Back'),
          ),
        ],
      ),
    );
    if (type == null) return;

    try {
      final authState = ref.read(authProvider).valueOrNull;
      final userId = authState?.user?.id;
      final gymId = authState?.currentGymId;
      if (userId == null || gymId == null) {
        throw Exception('No authenticated user or active gym');
      }
      final now = DateTime.now();
      final dateStr = '${now.year.toString().padLeft(4, '0')}-'
          '${now.month.toString().padLeft(2, '0')}-'
          '${now.day.toString().padLeft(2, '0')}';

      // In real impl would upload file bytes; here we post the path as URL.
      final repo = ref.read(progressRepositoryProvider);
      await repo.uploadProgressPhoto({
        'userId': userId,
        'gymId': gymId,
        'photoUrl': file.path,
        'thumbnailUrl': file.path,
        'angle': type,
        'date': dateStr,
      });
      ref.invalidate(progressPhotosProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }

  void _showPhotoDetail(
      BuildContext context, ProgressPhotoSetModel set, bool isArabic) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => _PhotoDetailSheet(set: set, isArabic: isArabic),
    );
  }
}

class _PhotoDetailSheet extends StatelessWidget {
  final ProgressPhotoSetModel set;
  final bool isArabic;

  const _PhotoDetailSheet({required this.set, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.8,
      maxChildSize: 0.95,
      minChildSize: 0.5,
      expand: false,
      builder: (ctx, scrollController) {
        return Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  decoration: BoxDecoration(
                    color: Colors.grey[300],
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              const SizedBox(height: 12),
              Text(
                DateFormat(isArabic ? 'dd MMMM yyyy' : 'MMMM dd, yyyy')
                    .format(set.date),
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 16),
              Expanded(
                child: Row(
                  children: [
                    if (set.front != null)
                      Expanded(
                          child: _PhotoView(
                              photo: set.front!,
                              label: isArabic ? 'أمامية' : 'Front')),
                    if (set.side != null)
                      Expanded(
                          child: _PhotoView(
                              photo: set.side!,
                              label: isArabic ? 'جانبية' : 'Side')),
                    if (set.back != null)
                      Expanded(
                          child: _PhotoView(
                              photo: set.back!,
                              label: isArabic ? 'خلفية' : 'Back')),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _PhotoView extends StatelessWidget {
  final ProgressPhotoModel photo;
  final String label;

  const _PhotoView({required this.photo, required this.label});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(4),
      child: Column(
        children: [
          Expanded(
            child: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: Image.network(
                photo.imageUrl,
                fit: BoxFit.cover,
                width: double.infinity,
                errorBuilder: (_, __, ___) => Container(
                  color: Theme.of(context).colorScheme.surfaceVariant,
                  child: const Icon(Icons.image_not_supported),
                ),
              ),
            ),
          ),
          const SizedBox(height: 4),
          Text(label, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}
