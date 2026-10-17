using System.Text;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Infrastructure.Jobs;

/// <summary>
/// Reads a data import job's stored upload through the matching parser and
/// writes the CSV error report for rows that could not be imported.
/// </summary>
internal sealed class DataImportFileProcessor
{
    private readonly IImportFileStorage _fileStorage;
    private readonly IEnumerable<IImportParser> _parsers;

    public DataImportFileProcessor(IImportFileStorage fileStorage, IEnumerable<IImportParser> parsers)
    {
        _fileStorage = fileStorage;
        _parsers = parsers;
    }

    public async Task<ParsedImport> ParseAsync(DataImportJob job, CancellationToken ct)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanHandle(job.Source))
            ?? throw new InvalidOperationException($"No parser registered for {job.Source}");

        await using var content = await _fileStorage.OpenAsync(job.StoragePath, ct);
        return await parser.ParseAsync(job.Id, job.GymId, content, ct);
    }

    /// <summary>Saves the error report and returns its path, or null when there were no errors.</summary>
    public async Task<string?> SaveErrorReportAsync(Guid jobId, IReadOnlyList<ImportRowError> errors, CancellationToken ct)
    {
        if (errors.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("Sheet,RowNumber,Column,Message");
        foreach (var e in errors)
        {
            sb.Append(Csv(e.Sheet)).Append(',')
              .Append(e.RowNumber).Append(',')
              .Append(Csv(e.Column)).Append(',')
              .AppendLine(Csv(e.Message));
        }

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        return await _fileStorage.SaveErrorReportAsync(jobId, ms, ct);
    }

    private static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
}
