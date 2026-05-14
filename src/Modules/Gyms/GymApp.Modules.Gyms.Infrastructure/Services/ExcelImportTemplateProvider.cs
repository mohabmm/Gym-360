using ClosedXML.Excel;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class ExcelImportTemplateProvider : IImportTemplateProvider
{
    public Task<Stream> GenerateAsync(CancellationToken cancellationToken = default)
    {
        using var wb = new XLWorkbook();

        WriteHeaders(wb.Worksheets.Add("Members"), new[]
        {
            "MemberNumber", "FirstName", "LastName", "Email", "Phone",
            "DateOfBirth", "Gender", "JoinedAt", "Section"
        });

        WriteHeaders(wb.Worksheets.Add("Payments"), new[]
        {
            "MemberNumber", "Amount", "Currency", "PaymentMethod",
            "Purpose", "ReceiptNumber", "PaidAt"
        });

        WriteHeaders(wb.Worksheets.Add("Sessions"), new[]
        {
            "MemberNumber", "TrainerMemberNumber", "PackageName", "TotalSessions",
            "RemainingSessions", "TotalPrice", "Currency", "PurchasedAt", "ExpiresAt"
        });

        WriteHeaders(wb.Worksheets.Add("InBody"), new[]
        {
            "MemberNumber", "MeasuredAt", "WeightKg", "BodyFatPercentage",
            "SkeletalMuscleMassKg", "VisceralFatLevel", "Bmi", "BasalMetabolicRate",
            "BodyWaterKg", "Notes"
        });

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return Task.FromResult<Stream>(ms);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        ws.Columns().AdjustToContents();
    }
}
