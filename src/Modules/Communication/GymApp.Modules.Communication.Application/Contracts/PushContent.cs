namespace GymApp.Modules.Communication.Application.Contracts;

/// <summary>What a push notification shows (title/body) plus its data payload.</summary>
public sealed record PushContent(string Title, string Body, Dictionary<string, string>? Data = null);
