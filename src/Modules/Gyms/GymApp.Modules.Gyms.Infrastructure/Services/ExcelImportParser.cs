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
        var context = new ImportContext(importJobId, gymId);

        using var workbook = new XLWorkbook(content);

        var members = ParseSheet(workbook, "Members", context, ParseMember);
        var payments = ParseSheet(workbook, "Payments", context, ParsePayment);
        var packages = ParseSheet(workbook, "Sessions", context, ParseSessionPackage);
        var inBody = ParseSheet(workbook, "InBody", context, ParseInBody);

        return Task.FromResult(new ParsedImport(members, payments, packages, inBody, context.Errors));
    }

    private static List<T> ParseSheet<T>(XLWorkbook workbook, string sheet, ImportContext context, Func<ImportRow, T?> parseRow)
        where T : class
    {
        var parsed = new List<T>();
        if (!workbook.TryGetWorksheet(sheet, out var ws)) return parsed;

        var used = ws.RangeUsed();
        if (used is null) return parsed;

        foreach (var cells in used.RowsUsed().Skip(1))
        {
            var row = new ImportRow(sheet, cells, context);
            try
            {
                var item = parseRow(row);
                if (item is not null) parsed.Add(item);
            }
            catch (Exception ex)
            {
                row.AddError("Row", ex.Message);
            }
        }

        return parsed;
    }

    private static DataImportMemberIntegrationEvent? ParseMember(ImportRow row)
    {
        var memberNumber = row.Cell(1).TrimmedString();
        var firstName = row.Cell(2).TrimmedString();
        var lastName = row.Cell(3).TrimmedString();
        var email = row.Cell(4).OptionalString();
        var phone = row.Cell(5).TrimmedString();
        var dob = row.Cell(6).DateTimeOrNull();
        var gender = row.Cell(7).TrimmedString();
        var joinedAt = row.Cell(8).DateTimeOrNull() ?? DateTime.UtcNow;
        var section = row.Cell(9).OptionalString();

        if (string.IsNullOrWhiteSpace(memberNumber))
        { row.AddError("MemberNumber", "required"); return null; }
        if (string.IsNullOrWhiteSpace(firstName))
        { row.AddError("FirstName", "required"); return null; }
        if (string.IsNullOrWhiteSpace(phone))
        { row.AddError("Phone", "required"); return null; }

        var nationalId = ReadNationalId(row);

        return new DataImportMemberIntegrationEvent(
            row.Context.JobId, row.Number, row.Context.GymId, memberNumber, firstName, lastName,
            email, phone, dob, gender, joinedAt, section, nationalId);
    }

    // National ID is optional. If present it must be exactly 14 digits
    // (Egyptian NID). A malformed value is logged but does NOT block the
    // member row — the member imports with NationalId=null so staff can
    // fix the value later from the errors report.
    private static string? ReadNationalId(ImportRow row)
    {
        var nationalId = row.Cell(10).IdNumberOrNull();
        if (nationalId is null) return null;
        if (nationalId.Length == 14 && nationalId.All(char.IsDigit)) return nationalId;

        row.AddError("NationalId", $"invalid '{nationalId}' (must be 14 digits)");
        return null;
    }

    private static DataImportPaymentIntegrationEvent? ParsePayment(ImportRow row)
    {
        var memberNumber = row.Cell(1).TrimmedString();
        var amount = row.Cell(2).GetDouble();
        var currency = row.Cell(3).StringOr("EGP");
        var method = row.Cell(4).StringOr("Cash");
        var purpose = row.Cell(5).StringOr("MembershipRenewal");
        var receipt = row.Cell(6).OptionalString();
        var paidAt = row.Cell(7).DateTimeOrNull() ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(memberNumber))
        { row.AddError("MemberNumber", "required"); return null; }
        if (amount <= 0)
        { row.AddError("Amount", "must be > 0"); return null; }

        var jobId = row.Context.JobId;
        return new DataImportPaymentIntegrationEvent(
            jobId, row.Number, row.Context.GymId, memberNumber, (decimal)amount,
            currency, method, purpose,
            receipt ?? $"IMP-{jobId:N}-{row.Number}",
            paidAt);
    }

    private static DataImportSessionPackageIntegrationEvent? ParseSessionPackage(ImportRow row)
    {
        var memberNumber = row.Cell(1).TrimmedString();
        var trainerNumber = row.Cell(2).OptionalString();
        var name = row.Cell(3).StringOr("Imported Package");
        var total = (int)row.Cell(4).GetDouble();
        var remaining = (int)row.Cell(5).GetDouble();
        var price = (decimal)row.Cell(6).GetDouble();
        var currency = row.Cell(7).StringOr("EGP");
        var purchasedAt = row.Cell(8).DateTimeOrNull() ?? DateTime.UtcNow;
        var expiresAt = row.Cell(9).DateTimeOrNull();

        if (string.IsNullOrWhiteSpace(memberNumber))
        { row.AddError("MemberNumber", "required"); return null; }
        if (total <= 0)
        { row.AddError("TotalSessions", "must be > 0"); return null; }

        return new DataImportSessionPackageIntegrationEvent(
            row.Context.JobId, row.Number, row.Context.GymId, memberNumber, trainerNumber,
            name, total, Math.Clamp(remaining, 0, total),
            price, currency, purchasedAt, expiresAt);
    }

    private static DataImportInBodyIntegrationEvent? ParseInBody(ImportRow row)
    {
        var memberNumber = row.Cell(1).TrimmedString();
        var measuredAt = row.Cell(2).DateTimeOrNull() ?? DateTime.UtcNow;
        var weight = row.Cell(3).DecimalOrNull();
        var bodyFat = row.Cell(4).DecimalOrNull();
        var muscle = row.Cell(5).DecimalOrNull();
        var visceral = row.Cell(6).DecimalOrNull();
        var bmi = row.Cell(7).DecimalOrNull();
        var bmr = row.Cell(8).DecimalOrNull();
        var water = row.Cell(9).DecimalOrNull();
        var notes = row.Cell(10).OptionalString();

        if (string.IsNullOrWhiteSpace(memberNumber))
        { row.AddError("MemberNumber", "required"); return null; }

        return new DataImportInBodyIntegrationEvent(
            row.Context.JobId, row.Number, row.Context.GymId, memberNumber, measuredAt,
            weight, bodyFat, muscle, visceral, bmi, bmr, water, notes);
    }
}
