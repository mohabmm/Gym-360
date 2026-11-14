import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../providers/members_provider.dart';

String memberName(UserModel m) => '${m.firstName} ${m.lastName}'.trim();

/// "#0012 · 0100 123 4567" — whatever identifies the member at the desk.
String memberSubtitle(UserModel m) =>
    [if ((m.memberNumber ?? '').isNotEmpty) '#${m.memberNumber}', if ((m.phone ?? '').isNotEmpty) m.phone!]
        .join(' · ');

/// Search the roster by name, phone or member number and pick one member.
/// Once picked, shows the member with a "Change" button instead of the search box.
class MemberPicker extends ConsumerStatefulWidget {
  final UserModel? selected;
  final ValueChanged<UserModel?> onChanged;
  final bool isArabic;

  const MemberPicker({super.key, required this.selected, required this.onChanged, required this.isArabic});

  @override
  ConsumerState<MemberPicker> createState() => _MemberPickerState();
}

class _MemberPickerState extends ConsumerState<MemberPicker> {
  final _controller = TextEditingController();
  Timer? _debounce;
  String _query = '';

  @override
  void dispose() {
    _debounce?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _onTyped(String text) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (mounted) setState(() => _query = text.trim());
    });
  }

  void _pick(UserModel? m) {
    _controller.clear();
    setState(() => _query = '');
    widget.onChanged(m);
  }

  @override
  Widget build(BuildContext context) {
    final selected = widget.selected;
    if (selected != null) {
      return SelectedMemberTile(member: selected, isArabic: widget.isArabic, onChange: () => _pick(null));
    }
    final ar = widget.isArabic;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          key: const ValueKey('member-search'),
          controller: _controller,
          autofocus: true,
          onChanged: _onTyped,
          textInputAction: TextInputAction.search,
          decoration: InputDecoration(
            prefixIcon: const Icon(Icons.search),
            hintText: ar ? 'ابحث بالاسم أو الموبايل أو رقم العضو' : 'Search by name, phone or member number',
          ),
        ),
        if (_query.length >= 2) _Results(query: _query, isArabic: ar, onPick: _pick),
      ],
    );
  }
}

class _Results extends ConsumerWidget {
  final String query;
  final bool isArabic;
  final ValueChanged<UserModel> onPick;

  const _Results({required this.query, required this.isArabic, required this.onPick});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ref.watch(memberSearchProvider(query)).when(
          data: (members) => members.isEmpty
              ? Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text(isArabic ? 'لا يوجد عضو بهذا البحث' : 'No member matches this search',
                      textAlign: TextAlign.center),
                )
              : Column(children: [
                  for (final m in members)
                    ListTile(
                      leading: _Initials(member: m),
                      title: Text(memberName(m)),
                      subtitle: Text(memberSubtitle(m)),
                      onTap: () => onPick(m),
                    ),
                ]),
          loading: () => const Padding(padding: EdgeInsets.all(12), child: LinearProgressIndicator()),
          error: (_, __) => Padding(
            padding: const EdgeInsets.all(12),
            child: Text(isArabic ? 'تعذر البحث، حاول مرة أخرى' : "Couldn't search, try again"),
          ),
        );
  }
}

/// The chosen member, with a button to pick someone else.
class SelectedMemberTile extends StatelessWidget {
  final UserModel member;
  final bool isArabic;
  final VoidCallback? onChange;

  const SelectedMemberTile({super.key, required this.member, required this.isArabic, this.onChange});

  @override
  Widget build(BuildContext context) => AppCard(
        key: const ValueKey('selected-member'),
        child: ListTile(
          contentPadding: EdgeInsets.zero,
          leading: _Initials(member: member),
          title: Text(memberName(member), style: const TextStyle(fontWeight: FontWeight.w700)),
          subtitle: Text(memberSubtitle(member)),
          trailing: onChange == null
              ? null
              : TextButton(onPressed: onChange, child: Text(isArabic ? 'تغيير' : 'Change')),
        ),
      );
}

class _Initials extends StatelessWidget {
  final UserModel member;

  const _Initials({required this.member});

  @override
  Widget build(BuildContext context) {
    final first = member.firstName.isEmpty ? '' : member.firstName[0];
    final last = member.lastName.isEmpty ? '' : member.lastName[0];
    return CircleAvatar(child: Text('$first$last'.toUpperCase()));
  }
}
