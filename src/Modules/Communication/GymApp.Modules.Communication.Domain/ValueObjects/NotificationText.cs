namespace GymApp.Modules.Communication.Domain.ValueObjects;

/// <summary>Arabic (primary) and English text of a system notification.</summary>
public sealed record NotificationText(string TitleAr, string BodyAr, string TitleEn, string BodyEn);
