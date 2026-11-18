import 'package:flutter/material.dart';

import '../../../widgets/app_card.dart';
import '../measurement_form_strings.dart';

/// "Same as last": fills the empty fields with the last readings in one tap.
/// Disabled ([onCopy] null) once every field with a last reading is filled.
class CopyLastButton extends StatelessWidget {
  final MeasurementFormStrings strings;
  final VoidCallback? onCopy;

  const CopyLastButton({super.key, required this.strings, required this.onCopy});

  @override
  Widget build(BuildContext context) => AppCard(
        key: const ValueKey('copy-last'),
        onTap: onCopy,
        padding: EdgeInsets.zero,
        child: ListTile(
          enabled: onCopy != null,
          leading: const Icon(Icons.history),
          title: Text(strings.copyLast),
          subtitle: Text(strings.copyLastHint),
          trailing: const Icon(Icons.content_copy, size: 18),
        ),
      );
}

/// Coach quick notes: one tap adds a line to the note.
class QuickNoteChips extends StatelessWidget {
  final List<String> notes;
  final ValueChanged<String> onPick;

  const QuickNoteChips({super.key, required this.notes, required this.onPick});

  @override
  Widget build(BuildContext context) => Wrap(
        spacing: 8,
        runSpacing: 4,
        children: [
          for (final note in notes)
            ActionChip(label: Text(note), visualDensity: VisualDensity.compact, onPressed: () => onPick(note)),
        ],
      );
}
