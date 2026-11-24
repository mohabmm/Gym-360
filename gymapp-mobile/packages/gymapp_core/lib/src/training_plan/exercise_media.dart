import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../network/api_base_url.dart';

/// The API serves exercise demo animations (looping WebP, public domain) under a relative path
/// such as `/media/exercises/barbell-back-squat.webp`; a coach's own link is already absolute.
String? exerciseMediaUrl(String? url, {String? baseUrl}) {
  if (url == null || url.trim().isEmpty) return null;
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  final base = (baseUrl ?? ApiBaseUrl.resolve()).replaceAll(RegExp(r'/+$'), '');
  return '$base${url.startsWith('/') ? '' : '/'}$url';
}

/// A looping demo of the exercise (start ↔ end position), or a quiet placeholder when the
/// exercise has none, e.g. a free-text exercise.
class ExerciseMediaView extends StatelessWidget {
  final String? url;
  final double size;

  const ExerciseMediaView({super.key, required this.url, this.size = 56});

  @override
  Widget build(BuildContext context) {
    final resolved = exerciseMediaUrl(url);
    final colors = Theme.of(context).colorScheme;
    final placeholder = Container(
      width: size,
      height: size,
      color: colors.surfaceContainerHighest,
      alignment: Alignment.center,
      child: Icon(Icons.fitness_center, size: size * 0.45, color: colors.onSurfaceVariant),
    );
    return ClipRRect(
      borderRadius: BorderRadius.circular(size > 100 ? 16 : 10),
      child: resolved == null
          ? placeholder
          : CachedNetworkImage(
              imageUrl: resolved,
              width: size,
              height: size,
              fit: BoxFit.cover,
              placeholder: (_, __) => placeholder,
              errorWidget: (_, __, ___) => placeholder,
            ),
    );
  }
}

/// What the exercise sheet shows: the animation, how to do it, and the coach's own video.
class ExerciseMediaDetails {
  final String title;
  final String? subtitle;
  final String? mediaUrl;
  final String? notes;
  final String? videoUrl;

  const ExerciseMediaDetails({required this.title, this.subtitle, this.mediaUrl, this.notes, this.videoUrl});
}

/// Opens the exercise: the big looping animation, the coach's notes and their video link.
Future<void> showExerciseMediaSheet(BuildContext context, ExerciseMediaDetails details, {required bool isArabic}) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => Directionality(
        textDirection: isArabic ? TextDirection.rtl : TextDirection.ltr,
        child: _ExerciseMediaSheet(details: details, isArabic: isArabic),
      ),
    );

class _ExerciseMediaSheet extends StatelessWidget {
  final ExerciseMediaDetails details;
  final bool isArabic;

  const _ExerciseMediaSheet({required this.details, required this.isArabic});

  @override
  Widget build(BuildContext context) {
    final text = Theme.of(context).textTheme;
    final video = exerciseMediaUrl(details.videoUrl);
    final width = MediaQuery.sizeOf(context).width - 32;
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text(details.title, style: text.titleLarge),
          if (details.subtitle != null) Text(details.subtitle!, style: text.bodyMedium),
          const SizedBox(height: 12),
          Center(child: ExerciseMediaView(url: details.mediaUrl, size: width.clamp(120, 360).toDouble())),
          if (details.mediaUrl == null)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Text(isArabic ? 'لا يوجد عرض لهذا التمرين بعد' : 'No demo for this exercise yet',
                  textAlign: TextAlign.center, style: text.bodySmall),
            ),
          if (details.notes?.trim().isNotEmpty ?? false) ...[
            const SizedBox(height: 16),
            Text(isArabic ? 'ملاحظات المدرب' : "Coach's notes", style: text.titleSmall),
            Text(details.notes!.trim()),
          ],
          if (video != null) ...[
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: () => launchUrl(Uri.parse(video), mode: LaunchMode.externalApplication),
              icon: const Icon(Icons.play_circle_outline),
              label: Text(isArabic ? 'فيديو المدرب' : "Coach's video"),
            ),
          ],
        ]),
      ),
    );
  }
}
