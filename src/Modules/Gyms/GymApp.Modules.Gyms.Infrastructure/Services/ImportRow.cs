using ClosedXML.Excel;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>The import job a workbook belongs to and the row errors collected while parsing it.</summary>
internal sealed record ImportContext(Guid JobId, Guid GymId)
{
    public List<ImportRowError> Errors { get; } = new();
}

/// <summary>One data row of an import sheet, able to record errors against itself.</summary>
internal sealed class ImportRow
{
    private readonly string _sheet;
    private readonly IXLRangeRow _cells;

    public ImportRow(string sheet, IXLRangeRow cells, ImportContext context)
    {
        _sheet = sheet;
        _cells = cells;
        Context = context;
        Number = cells.RowNumber();
    }

    public ImportContext Context { get; }
    public int Number { get; }

    public IXLCell Cell(int column) => _cells.Cell(column);

    public void AddError(string column, string message) =>
        Context.Errors.Add(new ImportRowError(_sheet, Number, column, message));
}
