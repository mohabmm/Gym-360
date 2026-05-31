import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../constants/app_constants.dart';

/// Reusable text field with RTL support and consistent styling.
class AppTextField extends StatelessWidget {
  final String? label;
  final String? hint;
  final String? errorText;
  final TextEditingController? controller;
  final String? Function(String?)? validator;
  final void Function(String)? onChanged;
  final void Function(String)? onSubmitted;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final bool obscureText;
  final bool readOnly;
  final bool autofocus;
  final int? maxLines;
  final int? maxLength;
  final Widget? prefixIcon;
  final Widget? suffixIcon;
  final List<TextInputFormatter>? inputFormatters;
  final FocusNode? focusNode;
  final bool enabled;
  final EdgeInsetsGeometry? contentPadding;
  final bool autocorrect;
  final bool enableSuggestions;

  const AppTextField({
    super.key,
    this.label,
    this.hint,
    this.errorText,
    this.controller,
    this.validator,
    this.onChanged,
    this.onSubmitted,
    this.keyboardType,
    this.textInputAction,
    this.obscureText = false,
    this.readOnly = false,
    this.autofocus = false,
    this.maxLines = 1,
    this.maxLength,
    this.prefixIcon,
    this.suffixIcon,
    this.inputFormatters,
    this.focusNode,
    this.enabled = true,
    this.contentPadding,
    this.autocorrect = true,
    this.enableSuggestions = true,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        if (label != null)
          Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Text(
              label!,
              style: Theme.of(context).textTheme.titleSmall,
            ),
          ),
        TextFormField(
          controller: controller,
          validator: validator,
          onChanged: onChanged,
          onFieldSubmitted: onSubmitted,
          keyboardType: keyboardType,
          textInputAction: textInputAction,
          obscureText: obscureText,
          readOnly: readOnly,
          autofocus: autofocus,
          maxLines: maxLines,
          maxLength: maxLength,
          inputFormatters: inputFormatters,
          focusNode: focusNode,
          enabled: enabled,
          autocorrect: autocorrect,
          enableSuggestions: enableSuggestions,
          decoration: InputDecoration(
            hintText: hint,
            errorText: errorText,
            prefixIcon: prefixIcon,
            suffixIcon: suffixIcon,
            contentPadding: contentPadding,
          ),
        ),
      ],
    );
  }
}

/// Password text field with visibility toggle.
class AppPasswordField extends StatefulWidget {
  final String? label;
  final String? hint;
  final TextEditingController? controller;
  final String? Function(String?)? validator;
  final void Function(String)? onChanged;
  final TextInputAction? textInputAction;

  const AppPasswordField({
    super.key,
    this.label,
    this.hint,
    this.controller,
    this.validator,
    this.onChanged,
    this.textInputAction,
  });

  @override
  State<AppPasswordField> createState() => _AppPasswordFieldState();
}

class _AppPasswordFieldState extends State<AppPasswordField> {
  bool _obscure = true;

  @override
  Widget build(BuildContext context) {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    final tooltip = _obscure
        ? (isArabic ? 'إظهار كلمة المرور' : 'Show password')
        : (isArabic ? 'إخفاء كلمة المرور' : 'Hide password');
    return AppTextField(
      label: widget.label,
      hint: widget.hint,
      controller: widget.controller,
      validator: widget.validator,
      onChanged: widget.onChanged,
      textInputAction: widget.textInputAction,
      obscureText: _obscure,
      keyboardType: TextInputType.visiblePassword,
      suffixIcon: IconButton(
        icon: Icon(
          _obscure ? Icons.visibility_off_outlined : Icons.visibility_outlined,
          size: AppConstants.iconSizeMedium,
          // Explicit semanticLabel so screen readers announce the action
          // even when tooltip isn't surfaced (common on iOS VoiceOver).
          semanticLabel: tooltip,
        ),
        tooltip: tooltip,
        onPressed: () => setState(() => _obscure = !_obscure),
      ),
    );
  }
}

/// Search text field with clear button.
class AppSearchField extends StatelessWidget {
  final TextEditingController? controller;
  final String? hint;
  final void Function(String)? onChanged;
  final VoidCallback? onClear;

  const AppSearchField({
    super.key,
    this.controller,
    this.hint,
    this.onChanged,
    this.onClear,
  });

  @override
  Widget build(BuildContext context) {
    return AppTextField(
      controller: controller,
      hint: hint,
      onChanged: onChanged,
      prefixIcon: const Icon(Icons.search, size: AppConstants.iconSizeMedium),
      suffixIcon: controller?.text.isNotEmpty == true
          ? Builder(
              builder: (ctx) {
                final isArabic =
                    Localizations.localeOf(ctx).languageCode == 'ar';
                return IconButton(
                  icon: const Icon(Icons.clear,
                      size: AppConstants.iconSizeMedium),
                  tooltip: isArabic ? 'مسح' : 'Clear',
                  onPressed: () {
                    controller?.clear();
                    onClear?.call();
                  },
                );
              },
            )
          : null,
      textInputAction: TextInputAction.search,
    );
  }
}
