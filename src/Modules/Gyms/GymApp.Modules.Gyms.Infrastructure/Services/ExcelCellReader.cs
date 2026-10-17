using System.Globalization;
using ClosedXML.Excel;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

/// <summary>Tolerant readers for the loosely-typed cells of an import workbook.</summary>
internal static class ExcelCellReader
{
    public static string TrimmedString(this IXLCell cell) => cell.GetString().Trim();

    public static string? OptionalString(this IXLCell cell)
    {
        var value = cell.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string StringOr(this IXLCell cell, string fallback) => cell.OptionalString() ?? fallback;

    public static DateTime? DateTimeOrNull(this IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        if (DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d))
            return d;
        return null;
    }

    public static decimal? DecimalOrNull(this IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        if (decimal.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
        return null;
    }

    // National IDs are long digit strings. When a user types one into Excel it is
    // often stored as a NUMBER, and GetString() then yields scientific notation
    // ("1.23457E+13"). Read numeric cells as a 0-decimal integer so the 14 digits
    // survive intact; otherwise fall back to the trimmed string.
    public static string? IdNumberOrNull(this IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number)
            return ((long)Math.Round(cell.GetDouble())).ToString(CultureInfo.InvariantCulture);
        return cell.OptionalString();
    }
}
