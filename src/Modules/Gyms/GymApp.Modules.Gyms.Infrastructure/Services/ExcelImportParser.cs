using System.Globalization;
using ClosedXML.Excel;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class ExcelImportParser : IImportParser
{
    public bool CanHandle(DataImportSource source) =>
        source is DataImportSource.Excel or DataImportSource.Odoo or DataImportSource.Daftra;

    public Task<ParsedImport> ParseAsync(Guid importJobId, Guid gymId, Stream content, CancellationToken cancellationToken = default)
    {
        var members = new List<DataImportMemberIntegrationEvent>();
        var payments = new List<DataImportPaymentIntegrationEvent>();
        var packages = new List<DataImportSessionPackageIntegrationEvent>();
        var inBody = new List<DataImportInBodyIntegrationEvent>();
        var errors = new List<ImportRowError>();

        using var workbook = new XLWorkbook(content);

        ParseMembers(workbook, importJobId, gymId, members, errors);
        ParsePayments(workbook, importJobId, gymId, payments, errors);
        ParseSessions(workbook, importJobId, gymId, packages, errors);
        ParseInBody(workbook, importJobId, gymId, inBody, errors);

        return Task.FromResult(new ParsedImport(members, payments, packages, inBody, errors));
    }

    private static void ParseMembers(XLWorkbook wb, Guid jobId, Guid gymId,
        List<DataImportMemberIntegrationEvent> members, List<ImportRowError> errors)
    {
        if (!wb.TryGetWorksheet("Members", out var ws)) return;

        var used = ws.RangeUsed();
        if (used is null) return;

        var rows = used.RowsUsed().Skip(1);
        foreach (var row in rows)
        {
            var rowNum = row.RowNumber();
            try
            {
                var memberNumber = row.Cell(1).GetString().Trim();
                var firstName = row.Cell(2).GetString().Trim();
                var lastName = row.Cell(3).GetString().Trim();
                var email = EmptyToNull(row.Cell(4).GetString());
                var phone = row.Cell(5).GetString().Trim();
                var dob = TryGetDateTime(row.Cell(6));
                var gender = row.Cell(7).GetString().Trim();
                var joinedAt = TryGetDateTime(row.Cell(8)) ?? DateTime.UtcNow;
                var section = EmptyToNull(row.Cell(9).GetString());
                var nationalId = ReadIdNumber(row.Cell(10));

                if (string.IsNullOrWhiteSpace(memberNumber))
                { errors.Add(new("Members", rowNum, "MemberNumber", "required")); continue; }
                if (string.IsNullOrWhiteSpace(firstName))
                { errors.Add(new("Members", rowNum, "FirstName", "required")); continue; }
                if (string.IsNullOrWhiteSpace(phone))
                { errors.Add(new("Members", rowNum, "Phone", "required")); continue; }

                // National ID is optional. If present it must be exactly 14 digits
                // (Egyptian NID). A malformed value is logged but does NOT block the
                // member row — the member imports with NationalId=null so staff can
                // fix the value later from the errors report.
                if (nationalId is not null && !IsValidNationalId(nationalId))
                {
                    errors.Add(new("Members", rowNum, "NationalId", $"invalid '{nationalId}' (must be 14 digits)"));
                    nationalId = null;
                }

                members.Add(new DataImportMemberIntegrationEvent(
                    jobId, rowNum, gymId, memberNumber, firstName, lastName,
                    email, phone, dob, gender, joinedAt, section, nationalId));
            }
            catch (Exception ex)
            {
                errors.Add(new("Members", rowNum, "Row", ex.Message));
            }
        }
    }

    private static void ParsePayments(XLWorkbook wb, Guid jobId, Guid gymId,
        List<DataImportPaymentIntegrationEvent> payments, List<ImportRowError> errors)
    {
        if (!wb.TryGetWorksheet("Payments", out var ws)) return;
        var used = ws.RangeUsed(); if (used is null) return;

        foreach (var row in used.RowsUsed().Skip(1))
        {
            var rowNum = row.RowNumber();
            try
            {
                var memberNumber = row.Cell(1).GetString().Trim();
                var amount = row.Cell(2).GetDouble();
                var currency = EmptyOr(row.Cell(3).GetString(), "EGP");
                var method = EmptyOr(row.Cell(4).GetString(), "Cash");
                var purpose = EmptyOr(row.Cell(5).GetString(), "MembershipRenewal");
                var receipt = row.Cell(6).GetString().Trim();
                var paidAt = TryGetDateTime(row.Cell(7)) ?? DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(memberNumber))
                { errors.Add(new("Payments", rowNum, "MemberNumber", "required")); continue; }
                if (amount <= 0)
                { errors.Add(new("Payments", rowNum, "Amount", "must be > 0")); continue; }

                payments.Add(new DataImportPaymentIntegrationEvent(
                    jobId, rowNum, gymId, memberNumber, (decimal)amount,
                    currency, method, purpose,
                    string.IsNullOrWhiteSpace(receipt) ? $"IMP-{jobId:N}-{rowNum}" : receipt,
                    paidAt));
            }
            catch (Exception ex)
            {
                errors.Add(new("Payments", rowNum, "Row", ex.Message));
            }
        }
    }

    private static void ParseSessions(XLWorkbook wb, Guid jobId, Guid gymId,
        List<DataImportSessionPackageIntegrationEvent> packages, List<ImportRowError> errors)
    {
        if (!wb.TryGetWorksheet("Sessions", out var ws)) return;
        var used = ws.RangeUsed(); if (used is null) return;

        foreach (var row in used.RowsUsed().Skip(1))
        {
            var rowNum = row.RowNumber();
            try
            {
                var memberNumber = row.Cell(1).GetString().Trim();
                var trainerNumber = EmptyToNull(row.Cell(2).GetString());
                var name = EmptyOr(row.Cell(3).GetString(), "Imported Package");
                var total = (int)row.Cell(4).GetDouble();
                var remaining = (int)row.Cell(5).GetDouble();
                var price = (decimal)row.Cell(6).GetDouble();
                var currency = EmptyOr(row.Cell(7).GetString(), "EGP");
                var purchasedAt = TryGetDateTime(row.Cell(8)) ?? DateTime.UtcNow;
                var expiresAt = TryGetDateTime(row.Cell(9));

                if (string.IsNullOrWhiteSpace(memberNumber))
                { errors.Add(new("Sessions", rowNum, "MemberNumber", "required")); continue; }
                if (total <= 0)
                { errors.Add(new("Sessions", rowNum, "TotalSessions", "must be > 0")); continue; }

                packages.Add(new DataImportSessionPackageIntegrationEvent(
                    jobId, rowNum, gymId, memberNumber, trainerNumber,
                    name, total, Math.Clamp(remaining, 0, total),
                    price, currency, purchasedAt, expiresAt));
            }
            catch (Exception ex)
            {
                errors.Add(new("Sessions", rowNum, "Row", ex.Message));
            }
        }
    }

    private static void ParseInBody(XLWorkbook wb, Guid jobId, Guid gymId,
        List<DataImportInBodyIntegrationEvent> inBody, List<ImportRowError> errors)
    {
        if (!wb.TryGetWorksheet("InBody", out var ws)) return;
        var used = ws.RangeUsed(); if (used is null) return;

        foreach (var row in used.RowsUsed().Skip(1))
        {
            var rowNum = row.RowNumber();
            try
            {
                var memberNumber = row.Cell(1).GetString().Trim();
                var measuredAt = TryGetDateTime(row.Cell(2)) ?? DateTime.UtcNow;
                var weight = TryDecimal(row.Cell(3));
                var bodyFat = TryDecimal(row.Cell(4));
                var muscle = TryDecimal(row.Cell(5));
                var visceral = TryDecimal(row.Cell(6));
                var bmi = TryDecimal(row.Cell(7));
                var bmr = TryDecimal(row.Cell(8));
                var water = TryDecimal(row.Cell(9));
                var notes = EmptyToNull(row.Cell(10).GetString());

                if (string.IsNullOrWhiteSpace(memberNumber))
                { errors.Add(new("InBody", rowNum, "MemberNumber", "required")); continue; }

                inBody.Add(new DataImportInBodyIntegrationEvent(
                    jobId, rowNum, gymId, memberNumber, measuredAt,
                    weight, bodyFat, muscle, visceral, bmi, bmr, water, notes));
            }
            catch (Exception ex)
            {
                errors.Add(new("InBody", rowNum, "Row", ex.Message));
            }
        }
    }

    private static DateTime? TryGetDateTime(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        if (DateTime.TryParse(cell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d))
            return d;
        return null;
    }

    private static decimal? TryDecimal(IXLCell cell)
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
    private static string? ReadIdNumber(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        if (cell.DataType == XLDataType.Number)
            return ((long)Math.Round(cell.GetDouble())).ToString(CultureInfo.InvariantCulture);
        return EmptyToNull(cell.GetString());
    }

    private static bool IsValidNationalId(string value) =>
        value.Length == 14 && value.All(char.IsDigit);

    private static string? EmptyToNull(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string EmptyOr(string s, string fallback) => string.IsNullOrWhiteSpace(s) ? fallback : s.Trim();
}
