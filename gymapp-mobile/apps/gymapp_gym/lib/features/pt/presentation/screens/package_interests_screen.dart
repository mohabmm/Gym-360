import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../../core/providers.dart';
import '../providers/pt_providers.dart';

/// pt.package_interest — trainees who tapped "I'm interested". The desk calls or WhatsApps
/// them, then sells the package from the member profile (the lead closes itself), or
/// dismisses a lead that will not convert.
class PackageInterestsScreen extends ConsumerWidget {
  const PackageInterestsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final leads = ref.watch(pendingInterestsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(isArabic ? 'مهتمون بالباقات' : 'Interested in a package')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(pendingInterestsProvider.future),
        child: leads.when(
          data: (list) => list.isEmpty
              ? ListView(children: [
                  const SizedBox(height: 60),
                  EmptyState(
                    icon: Icons.inbox_outlined,
                    title: isArabic ? 'لا طلبات جديدة' : 'No new requests',
                    subtitle: isArabic
                        ? 'عندما يضغط متدرب «مهتم» يظهر هنا.'
                        : 'When a trainee taps “I’m interested”, they show up here.',
                  ),
                ])
              : ListView.separated(
                  itemCount: list.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (_, i) => _LeadTile(lead: list[i], isArabic: isArabic),
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => AppErrorWidget(
            message: isArabic ? 'تعذر تحميل القائمة' : 'Could not load the list',
            onRetry: () => ref.invalidate(pendingInterestsProvider),
          ),
        ),
      ),
    );
  }
}

class _LeadTile extends ConsumerWidget {
  final PackageInterestLead lead;
  final bool isArabic;
  const _LeadTile({required this.lead, required this.isArabic});

  String get _name => lead.memberName.isEmpty ? (isArabic ? 'متدرب' : 'Trainee') : lead.memberName;

  String _subtitle() {
    final when = DateFormat('d MMM · HH:mm', isArabic ? 'ar' : 'en').format(lead.createdAt.toLocal());
    final size = lead.recommendedSessions;
    final what = switch (lead.source) {
      'Renewal' => isArabic ? 'تجديد' : 'Renewal',
      _ => isArabic ? 'بعد التجربة' : 'After trial',
    };
    final sizeText = size == null ? '' : (isArabic ? ' · $size حصة' : ' · $size sessions');
    return isArabic
        ? '$what$sizeText · المدرب: ${lead.trainerName} · $when'
        : '$what$sizeText · Coach: ${lead.trainerName} · $when';
  }

  Future<void> _dismiss(BuildContext context, WidgetRef ref) async {
    final messenger = ScaffoldMessenger.of(context);
    try {
      await ref.read(apiClientProvider).postVoid(ApiEndpoints.packageInterestDismiss(lead.id));
      ref.invalidate(pendingInterestsProvider);
    } catch (_) {
      messenger.showSnackBar(SnackBar(
        content: Text(isArabic ? 'تعذّر الإغلاق، حاول مرة أخرى' : 'Could not close the lead, please try again'),
      ));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final phone = lead.memberPhone;
    return ListTile(
      leading: CircleAvatar(child: Text(_name.characters.first)),
      title: Text(_name, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(_subtitle()),
      onTap: () => context.push('/members/${lead.memberId}'),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (phone != null && phone.isNotEmpty) ...[
            IconButton(
              tooltip: isArabic ? 'اتصال' : 'Call',
              icon: const Icon(Icons.call_outlined),
              onPressed: () => launchUrl(Uri(scheme: 'tel', path: phone)),
            ),
            IconButton(
              tooltip: 'WhatsApp',
              icon: const Icon(Icons.chat_outlined),
              onPressed: () => launchUrl(whatsAppUri(phone), mode: LaunchMode.externalApplication),
            ),
          ],
          PopupMenuButton<String>(
            onSelected: (_) => _dismiss(context, ref),
            itemBuilder: (_) => [
              PopupMenuItem(value: 'dismiss', child: Text(isArabic ? 'إغلاق الطلب' : 'Close lead')),
            ],
          ),
        ],
      ),
    );
  }
}

/// wa.me link for an Egyptian number: digits only, local 01x… becomes 201x….
Uri whatsAppUri(String phone) {
  var digits = phone.replaceAll(RegExp(r'\D'), '');
  if (digits.startsWith('0')) digits = '2$digits';
  return Uri.parse('https://wa.me/$digits');
}
