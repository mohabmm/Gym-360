import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Lightweight gym membership record used by [GymSwitcher].
/// Mirrors `UserGymMembership` from the Identity module.
class GymMembershipOption {
  final String gymId;
  final String gymName;
  final String? gymNameAr;
  final String? role;
  final String? city;
  final bool suspended;

  const GymMembershipOption({
    required this.gymId,
    required this.gymName,
    this.gymNameAr,
    this.role,
    this.city,
    this.suspended = false,
  });
}

/// Gym switcher pill — taps open a modal bottom sheet listing all gyms the
/// user belongs to. Trainers who work at multiple gyms (FOUNDER_DECISIONS Q3)
/// use this to change the active X-Gym-Id header without re-logging in.
///
/// Degrades to a read-only badge when the user has only one gym.
class GymSwitcher extends StatelessWidget {
  final List<GymMembershipOption> gyms;
  final String currentGymId;
  final ValueChanged<String> onSwitch;
  final bool compact;

  const GymSwitcher({
    super.key,
    required this.gyms,
    required this.currentGymId,
    required this.onSwitch,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    if (gyms.isEmpty) return const SizedBox.shrink();
    final current = gyms.firstWhere(
      (g) => g.gymId == currentGymId,
      orElse: () => gyms.first,
    );
    final isSingle = gyms.length == 1;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        borderRadius: BorderRadius.circular(24),
        onTap: isSingle ? null : () => _openSheet(context),
        child: Container(
          padding: EdgeInsets.symmetric(
            horizontal: compact ? 10 : 12,
            vertical: 6,
          ),
          decoration: BoxDecoration(
            color: AppColors.primary.withOpacity(0.08),
            borderRadius: BorderRadius.circular(24),
            border: Border.all(color: AppColors.primary.withOpacity(0.18)),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.apartment_rounded, size: 16, color: AppColors.primary),
              if (!compact) ...[
                const SizedBox(width: 6),
                ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 140),
                  child: Text(
                    current.gymName,
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.primary,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
              if (!isSingle) ...[
                const SizedBox(width: 4),
                Icon(Icons.expand_more, size: 16, color: AppColors.primary),
              ],
            ],
          ),
        ),
      ),
    );
  }

  void _openSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => _GymSwitcherSheet(
        gyms: gyms,
        currentGymId: currentGymId,
        onSelect: (id) {
          Navigator.of(ctx).pop();
          if (id != currentGymId) onSwitch(id);
        },
      ),
    );
  }
}

class _GymSwitcherSheet extends StatelessWidget {
  final List<GymMembershipOption> gyms;
  final String currentGymId;
  final ValueChanged<String> onSelect;

  const _GymSwitcherSheet({
    required this.gyms,
    required this.currentGymId,
    required this.onSelect,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.only(bottom: 12, left: 4),
              child: Text(
                'Switch gym',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                itemCount: gyms.length,
                separatorBuilder: (_, __) => const SizedBox(height: 6),
                itemBuilder: (_, i) => _GymRow(
                  gym: gyms[i],
                  active: gyms[i].gymId == currentGymId,
                  onTap: () => onSelect(gyms[i].gymId),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _GymRow extends StatelessWidget {
  final GymMembershipOption gym;
  final bool active;
  final VoidCallback onTap;

  const _GymRow({required this.gym, required this.active, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final initials = gym.gymName
        .split(' ')
        .map((s) => s.isNotEmpty ? s[0] : '')
        .take(2)
        .join()
        .toUpperCase();

    return Material(
      color: active ? AppColors.primary.withOpacity(0.06) : Colors.transparent,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Row(
            children: [
              Container(
                height: 40,
                width: 40,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: active ? AppColors.primary : AppColors.primary.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(
                  initials,
                  style: TextStyle(
                    color: active ? Colors.white : AppColors.primary,
                    fontWeight: FontWeight.w700,
                    fontSize: 13,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Flexible(
                          child: Text(
                            gym.gymName,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (gym.suspended) ...[
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: AppColors.error.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Text(
                              'suspended',
                              style: TextStyle(
                                fontSize: 10,
                                fontWeight: FontWeight.w700,
                                color: AppColors.error,
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                    if (gym.gymNameAr != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 2),
                        child: Directionality(
                          textDirection: TextDirection.rtl,
                          child: Text(
                            gym.gymNameAr!,
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.textTheme.bodySmall?.color?.withOpacity(0.7),
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ),
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Row(
                        children: [
                          if (gym.role != null)
                            Text(
                              gym.role!,
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                                color: AppColors.primary,
                              ),
                            ),
                          if (gym.role != null && gym.city != null)
                            Text(
                              ' · ',
                              style: theme.textTheme.bodySmall,
                            ),
                          if (gym.city != null)
                            Text(
                              gym.city!,
                              style: theme.textTheme.bodySmall,
                            ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              if (active)
                Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20),
            ],
          ),
        ),
      ),
    );
  }
}
