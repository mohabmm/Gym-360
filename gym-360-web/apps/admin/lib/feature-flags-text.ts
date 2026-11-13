'use client';

import { useDocumentLocale, type Locale } from '@gym360/ui';

/**
 * Arabic + English labels for the Feature Flags section. Arabic is the default locale
 * (RTL); the LocaleToggle in the header switches both the text and the direction.
 */
const TEXT = {
  en: {
    navLabel: 'Feature Flags',
    gymsTitle: 'Feature flags',
    gymsDescription: 'Choose which features each gym sees. A gym follows its preset; overrides change single flags.',
    presetsTitle: 'Presets',
    presetsDescription: 'Which flags each preset turns on. Changing a preset changes every gym on it.',
    auditTitle: 'Audit log',
    auditDescription: 'Every preset and flag change: who, when, gym, flag, old value and new value.',
    tabGyms: 'Gyms',
    tabPresets: 'Presets',
    tabAudit: 'Audit log',
    searchGyms: 'Search gyms…',
    allPresets: 'All presets',
    gym: 'Gym',
    status: 'Status',
    preset: 'Preset',
    overrides: 'Overrides',
    selected: (n: number) => `${n} selected`,
    applyPreset: 'Apply preset',
    applyPresetTo: (n: number) => `Apply a preset to ${n} gym(s)`,
    choosePreset: 'Choose a preset',
    flag: 'Flag',
    effective: 'Effective',
    source: 'Source',
    sourcePreset: 'Preset',
    sourceOverride: 'Override',
    on: 'On',
    off: 'Off',
    presetValue: (on: boolean) => `Preset: ${on ? 'on' : 'off'}`,
    resetToPreset: 'Reset to preset',
    setting: 'Setting',
    apps: 'Apps',
    reason: 'Reason (optional)',
    confirm: 'Confirm',
    cancel: 'Cancel',
    save: 'Save',
    confirmPresetTitle: 'Change preset?',
    confirmPresetBody: (gyms: number, preset: string) =>
      `${gyms} gym(s) will switch to “${preset}”. Their overrides are kept.`,
    confirmOverrideTitle: 'Change this flag for this gym?',
    confirmOverrideBody: (flag: string, on: boolean, gym: string) =>
      `“${flag}” will be turned ${on ? 'ON' : 'OFF'} for ${gym} only. Other gyms are not affected.`,
    confirmResetTitle: 'Reset to preset?',
    confirmResetBody: (flag: string) => `The override on “${flag}” is removed and the preset value applies again.`,
    confirmPresetEditTitle: 'Save preset changes?',
    confirmPresetEditBody: (changes: number, gyms: number) =>
      `${changes} flag change(s) will apply to ${gyms} gym(s) on this preset.`,
    affectsGyms: (gyms: number) => `Warning: ${gyms} gym(s) use this preset and will be affected.`,
    unsaved: (n: number) => `${n} unsaved change(s)`,
    saved: 'Saved.',
    savedPropagation: 'Saved. The API applies it immediately; apps pick it up within 5 minutes or when reopened.',
    propagationNote:
      'How fast changes reach the apps: the API enforces a change immediately. The mobile apps and the staff web refresh flags when opened, when brought back to the foreground, and every 5 minutes while open. No new app release is needed.',
    error: 'Something went wrong. Please try again.',
    loadError: 'Could not load feature flags.',
    empty: 'Nothing here yet.',
    when: 'When',
    who: 'Who',
    change: 'Change',
    oldValue: 'Old',
    newValue: 'New',
    allGyms: 'All gyms',
    allFlags: 'All flags',
    previous: 'Previous',
    next: 'Next',
    changeTypes: {
      preset_assigned: 'Preset assigned',
      override_set: 'Override set',
      override_cleared: 'Override cleared',
      preset_edited: 'Preset edited',
    } as Record<string, string>,
    backToGyms: 'Back to gyms',
  },
  ar: {
    navLabel: 'التحكم في الميزات',
    gymsTitle: 'التحكم في الميزات',
    gymsDescription: 'اختر الميزات التي يراها كل نادٍ. يتبع النادي الإعداد المسبق، والاستثناءات تغيّر ميزات محددة.',
    presetsTitle: 'الإعدادات المسبقة',
    presetsDescription: 'الميزات التي يفعّلها كل إعداد مسبق. تغيير الإعداد يغيّر كل الأندية التي تستخدمه.',
    auditTitle: 'سجل التغييرات',
    auditDescription: 'كل تغيير في الإعدادات والميزات: من، ومتى، والنادي، والميزة، والقيمة القديمة والجديدة.',
    tabGyms: 'الأندية',
    tabPresets: 'الإعدادات المسبقة',
    tabAudit: 'سجل التغييرات',
    searchGyms: 'ابحث عن نادٍ…',
    allPresets: 'كل الإعدادات',
    gym: 'النادي',
    status: 'الحالة',
    preset: 'الإعداد المسبق',
    overrides: 'الاستثناءات',
    selected: (n: number) => `تم اختيار ${n}`,
    applyPreset: 'تطبيق إعداد مسبق',
    applyPresetTo: (n: number) => `تطبيق إعداد مسبق على ${n} نادٍ`,
    choosePreset: 'اختر إعدادًا مسبقًا',
    flag: 'الميزة',
    effective: 'الحالة الفعلية',
    source: 'المصدر',
    sourcePreset: 'إعداد مسبق',
    sourceOverride: 'استثناء',
    on: 'مفعّلة',
    off: 'متوقفة',
    presetValue: (on: boolean) => `في الإعداد: ${on ? 'مفعّلة' : 'متوقفة'}`,
    resetToPreset: 'العودة للإعداد المسبق',
    setting: 'إعداد',
    apps: 'التطبيقات',
    reason: 'السبب (اختياري)',
    confirm: 'تأكيد',
    cancel: 'إلغاء',
    save: 'حفظ',
    confirmPresetTitle: 'تغيير الإعداد المسبق؟',
    confirmPresetBody: (gyms: number, preset: string) =>
      `سيتحول ${gyms} نادٍ إلى «${preset}». تبقى الاستثناءات كما هي.`,
    confirmOverrideTitle: 'تغيير هذه الميزة لهذا النادي؟',
    confirmOverrideBody: (flag: string, on: boolean, gym: string) =>
      `سيتم ${on ? 'تفعيل' : 'إيقاف'} «${flag}» لنادي ${gym} فقط. لن تتأثر الأندية الأخرى.`,
    confirmResetTitle: 'العودة للإعداد المسبق؟',
    confirmResetBody: (flag: string) => `سيُحذف الاستثناء على «${flag}» وتُطبق قيمة الإعداد المسبق.`,
    confirmPresetEditTitle: 'حفظ تغييرات الإعداد المسبق؟',
    confirmPresetEditBody: (changes: number, gyms: number) =>
      `سيُطبق ${changes} تغيير على ${gyms} نادٍ يستخدم هذا الإعداد.`,
    affectsGyms: (gyms: number) => `تنبيه: ${gyms} نادٍ يستخدم هذا الإعداد وسيتأثر.`,
    unsaved: (n: number) => `${n} تغيير غير محفوظ`,
    saved: 'تم الحفظ.',
    savedPropagation: 'تم الحفظ. تطبّقه الواجهة البرمجية فورًا، وتلتقطه التطبيقات خلال ٥ دقائق أو عند إعادة فتحها.',
    propagationNote:
      'سرعة وصول التغيير للتطبيقات: تطبّق الواجهة البرمجية التغيير فورًا. تحدّث تطبيقات الجوال وموقع الموظفين الميزات عند الفتح، وعند العودة للتطبيق، وكل ٥ دقائق أثناء الاستخدام. لا حاجة لإصدار جديد من التطبيق.',
    error: 'حدث خطأ. حاول مرة أخرى.',
    loadError: 'تعذّر تحميل الميزات.',
    empty: 'لا يوجد شيء بعد.',
    when: 'متى',
    who: 'من',
    change: 'التغيير',
    oldValue: 'القديم',
    newValue: 'الجديد',
    allGyms: 'كل الأندية',
    allFlags: 'كل الميزات',
    previous: 'السابق',
    next: 'التالي',
    changeTypes: {
      preset_assigned: 'تعيين إعداد مسبق',
      override_set: 'إضافة استثناء',
      override_cleared: 'حذف استثناء',
      preset_edited: 'تعديل إعداد مسبق',
    } as Record<string, string>,
    backToGyms: 'العودة للأندية',
  },
} as const;

export type FlagsText = (typeof TEXT)['en'];

export function flagsText(locale: Locale): FlagsText {
  return (locale === 'ar' ? TEXT.ar : TEXT.en) as FlagsText;
}

/** Labels for the current locale plus the localized name helpers. */
export function useFlagsText() {
  const locale = useDocumentLocale();
  const t = flagsText(locale);
  return {
    t,
    locale,
    flagName: (f: { nameEn: string; nameAr: string }) => (locale === 'ar' ? f.nameAr : f.nameEn),
    flagDescription: (f: { descriptionEn: string; descriptionAr: string }) =>
      locale === 'ar' ? f.descriptionAr : f.descriptionEn,
    presetName: (p: { nameEn: string; nameAr: string } | undefined, code: string) =>
      p ? (locale === 'ar' ? p.nameAr : p.nameEn) : code,
    changeType: (type: string) => t.changeTypes[type] ?? type,
  };
}
