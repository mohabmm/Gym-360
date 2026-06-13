/// Localizes the BE SessionType enum (PersonalTraining/Assessment/FollowUp)
/// for the Arabic-first UI. Several trainer screens printed the raw enum
/// string ("PersonalTraining"), leaking English. Labels mirror the trainee
/// booking sheet (تدريب / تقييم / متابعة).
String sessionTypeLabel(String type, bool isArabic) {
  switch (type.toLowerCase().replaceAll(RegExp(r'[\s_\-]'), '')) {
    case 'personaltraining':
      return isArabic ? 'تدريب شخصي' : 'Personal Training';
    case 'assessment':
      return isArabic ? 'تقييم' : 'Assessment';
    case 'followup':
      return isArabic ? 'متابعة' : 'Follow-up';
    default:
      return type;
  }
}
