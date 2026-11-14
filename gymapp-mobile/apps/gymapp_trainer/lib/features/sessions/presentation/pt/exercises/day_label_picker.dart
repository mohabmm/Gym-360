import 'package:flutter/material.dart';
import 'package:gymapp_core/gymapp_core.dart';

/// Push / Pull / Legs … chips plus "Other" for a custom label.
class DayLabelPicker extends StatelessWidget {
  final String? selected;
  final bool isArabic;
  final ValueChanged<String?> onChanged;

  const DayLabelPicker({super.key, required this.selected, required this.isArabic, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    final custom = selected != null && !ExerciseDays.presets.containsKey(selected);
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final label in ExerciseDays.presets.keys)
          ChoiceChip(
            label: Text(ExerciseDays.display(label, isArabic)),
            selected: selected == label,
            onSelected: (on) => onChanged(on ? label : null),
          ),
        ChoiceChip(
          label: Text(custom ? selected! : (isArabic ? 'أخرى…' : 'Other…')),
          selected: custom,
          onSelected: (_) => _askCustom(context),
        ),
      ],
    );
  }

  Future<void> _askCustom(BuildContext context) async {
    final controller = TextEditingController();
    final label = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(isArabic ? 'اسم اليوم' : 'Day name'),
        content: TextField(
          controller: controller,
          autofocus: true,
          maxLength: 40,
          decoration: InputDecoration(hintText: isArabic ? 'مثال: أكتاف وذراعين' : 'e.g. Shoulders & arms'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: Text(isArabic ? 'إلغاء' : 'Cancel')),
          FilledButton(
            onPressed: () => Navigator.pop(context, controller.text.trim()),
            child: Text(isArabic ? 'تم' : 'Done'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (label != null && label.isNotEmpty) onChanged(label);
  }
}
