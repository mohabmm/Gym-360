import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import '../utils/extensions.dart';

bool _isValidImageUrl(String url) {
  final uri = Uri.tryParse(url);
  if (uri == null) return false;
  if (uri.scheme != 'http' && uri.scheme != 'https') return false;
  final host = uri.host;
  if (host.isEmpty) return false;
  // Reject bare TLD-less hostnames (e.g. "example" with no dot)
  if (!host.contains('.')) return false;
  return true;
}

/// Avatar widget with image loading, placeholder, and initials fallback.
class AvatarWidget extends StatelessWidget {
  final String? imageUrl;
  final String name;
  final double size;
  final double? borderWidth;
  final Color? borderColor;
  final VoidCallback? onTap;
  final bool showOnlineBadge;

  const AvatarWidget({
    super.key,
    this.imageUrl,
    required this.name,
    this.size = 48,
    this.borderWidth,
    this.borderColor,
    this.onTap,
    this.showOnlineBadge = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final initials = name.initials;

    Widget avatar = Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: borderWidth != null
            ? Border.all(
                color: borderColor ?? theme.colorScheme.primary,
                width: borderWidth!,
              )
            : null,
      ),
      child: ClipOval(
        child: imageUrl != null && imageUrl!.isNotEmpty && _isValidImageUrl(imageUrl!)
            ? CachedNetworkImage(
                imageUrl: imageUrl!,
                width: size,
                height: size,
                fit: BoxFit.cover,
                placeholder: (_, __) => _InitialsAvatar(
                  initials: initials,
                  size: size,
                  theme: theme,
                ),
                errorWidget: (_, __, ___) => _InitialsAvatar(
                  initials: initials,
                  size: size,
                  theme: theme,
                ),
              )
            : _InitialsAvatar(
                initials: initials,
                size: size,
                theme: theme,
              ),
      ),
    );

    if (showOnlineBadge) {
      avatar = Stack(
        children: [
          avatar,
          Positioned(
            right: 0,
            bottom: 0,
            child: Container(
              width: size * 0.25,
              height: size * 0.25,
              decoration: BoxDecoration(
                color: AppColors.success,
                shape: BoxShape.circle,
                border: Border.all(
                  color: theme.scaffoldBackgroundColor,
                  width: 2,
                ),
              ),
            ),
          ),
        ],
      );
    }

    if (onTap != null) {
      return GestureDetector(
        onTap: onTap,
        child: avatar,
      );
    }

    return avatar;
  }
}

class _InitialsAvatar extends StatelessWidget {
  final String initials;
  final double size;
  final ThemeData theme;

  const _InitialsAvatar({
    required this.initials,
    required this.size,
    required this.theme,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      color: theme.colorScheme.primary.withOpacity(0.15),
      alignment: Alignment.center,
      child: Text(
        initials,
        style: TextStyle(
          color: theme.colorScheme.primary,
          fontSize: size * 0.35,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
