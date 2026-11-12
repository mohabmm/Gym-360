import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../sessions/presentation/pt/pt_coach_widgets.dart';

import '../providers/client_providers.dart';

class ClientRosterScreen extends ConsumerStatefulWidget {
  const ClientRosterScreen({super.key});

  @override
  ConsumerState<ClientRosterScreen> createState() => _ClientRosterScreenState();
}

class _ClientRosterScreenState extends ConsumerState<ClientRosterScreen> {
  String _searchQuery = '';
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final clientsAsync = ref.watch(clientRosterProvider);
    // pt.packages: remaining sessions and trial badge per client.
    final packagesByMember =
        ref.watch(isFeatureEnabledProvider(FeatureFlagKeys.ptPackages))
            ? {
                for (final p
                    in ref.watch(activeClientPackagesProvider).valueOrNull ??
                        const <SessionPackageModel>[])
                  p.memberId: p,
              }
            : const <String, SessionPackageModel>{};

    return Scaffold(
      appBar: AppBar(
        title: Text(isArabic ? 'العملاء' : 'Clients'),
      ),
      body: Column(
        children: [
          // Search bar
          Padding(
            padding: const EdgeInsets.all(16),
            child: AppSearchField(
              controller: _searchController,
              hint: isArabic ? 'ابحث عن عميل...' : 'Search clients...',
              onChanged: (value) => setState(() => _searchQuery = value),
              onClear: () => setState(() {
                _searchQuery = '';
                _searchController.clear();
              }),
            ),
          ),

          // Client list
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async {
                ref.invalidate(clientRosterProvider);
              },
              child: clientsAsync.when(
                data: (clients) {
                  final filtered = _searchQuery.isEmpty
                      ? clients
                      : clients.where((c) {
                          final name = '${c.user.firstName} ${c.user.lastName}'
                              .toLowerCase();
                          return name.contains(_searchQuery.toLowerCase());
                        }).toList();

                  if (filtered.isEmpty) {
                    return EmptyState(
                      icon: Icons.people_outline,
                      title: isArabic ? 'لا يوجد عملاء' : 'No clients found',
                      subtitle: _searchQuery.isNotEmpty
                          ? (isArabic
                              ? 'جرب بحثا مختلفا'
                              : 'Try a different search')
                          : (isArabic
                              ? 'سيظهر عملاؤك هنا'
                              : 'Your clients will appear here'),
                    );
                  }

                  return ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    itemCount: filtered.length,
                    itemBuilder: (context, index) {
                      final client = filtered[index];
                      return _ClientCard(
                        client: client,
                        isArabic: isArabic,
                        package: packagesByMember[client.user.id],
                        onTap: () => context.push('/clients/${client.user.id}'),
                      );
                    },
                  );
                },
                loading: () => ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: 6,
                  itemBuilder: (_, __) => const Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: ShimmerCard(height: 80),
                  ),
                ),
                error: (e, _) => AppErrorWidget(
                  message: e.toString(),
                  onRetry: () => ref.invalidate(clientRosterProvider),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ClientCard extends StatelessWidget {
  final ClientSummary client;
  final bool isArabic;
  final VoidCallback onTap;
  final SessionPackageModel? package;

  const _ClientCard({
    required this.client,
    required this.isArabic,
    required this.onTap,
    this.package,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final name = '${client.user.firstName} ${client.user.lastName}';

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        borderRadius: BorderRadius.circular(AppConstants.borderRadius),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              AvatarWidget(
                imageUrl: client.user.photoUrl,
                name: name,
                size: AppConstants.avatarSizeMedium,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      style: theme.textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    if (package != null)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Row(children: [
                          Text(
                            isArabic
                                ? 'متبقي ${package!.remainingSessions} من ${package!.totalSessions}'
                                : '${package!.remainingSessions} of ${package!.totalSessions} left',
                            style: theme.textTheme.bodySmall,
                          ),
                          if (package!.isTrial) ...[
                            const SizedBox(width: 6),
                            TrialBadge(isArabic: isArabic),
                          ],
                        ]),
                      ),
                    if (client.goalTags.isNotEmpty)
                      Wrap(
                        spacing: 4,
                        runSpacing: 4,
                        children: client.goalTags
                            .take(3)
                            .map((tag) => Container(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 6,
                                    vertical: 2,
                                  ),
                                  decoration: BoxDecoration(
                                    color: theme.colorScheme.primary
                                        .withValues(alpha: 0.1),
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                  child: Text(
                                    tag,
                                    style: TextStyle(
                                      fontSize: 10,
                                      color: theme.colorScheme.primary,
                                      fontWeight: FontWeight.w500,
                                    ),
                                  ),
                                ))
                            .toList(),
                      ),
                  ],
                ),
              ),
              if (package != null && package!.remainingSessions > 0)
                FeatureFlagGate(
                  flag: FeatureFlagKeys.ptDualSessionLogging,
                  child:
                      QuickLogButton(memberId: client.user.id, compact: true),
                )
              else
                const Icon(Icons.chevron_right, color: AppColors.grey400),
            ],
          ),
        ),
      ),
    );
  }
}
