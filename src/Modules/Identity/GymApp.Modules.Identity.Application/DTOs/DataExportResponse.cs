namespace GymApp.Modules.Identity.Application.DTOs;

public sealed record DataExportResponse(string DownloadUrl, DateTime ExpiresAt);
