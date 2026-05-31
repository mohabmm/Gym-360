using ClosedXML.Excel;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Infrastructure.Services;

namespace GymApp.Tests.Application.Gyms;

/// <summary>
/// Covers the NationalId column added to the Members import sheet (col 10):
/// valid 14-digit text, malformed values (logged but non-blocking), blank
/// (optional), and the common case of a NID typed as a numeric Excel cell.
/// </summary>
public class ExcelImportParserNationalIdTests
{
    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid GymId = Guid.NewGuid();

    private static Stream BuildWorkbook(Action<IXLWorksheet> writeMembers)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Members");
        // Header row (parser skips row 1).
        var headers = new[]
        {
            "MemberNumber", "FirstName", "LastName", "Email", "Phone",
            "DateOfBirth", "Gender", "JoinedAt", "Section", "NationalId"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        writeMembers(ws);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>Writes the required Members fields for a row, returning the row for NID customization.</summary>
    private static IXLRow Member(IXLWorksheet ws, int row, string memberNumber, string phone)
    {
        ws.Cell(row, 1).Value = memberNumber;
        ws.Cell(row, 2).Value = "Ahmed";
        ws.Cell(row, 5).Value = phone;
        return ws.Row(row);
    }

    private static async Task<ParsedImport> ParseAsync(Stream content)
    {
        var parser = new ExcelImportParser();
        return await parser.ParseAsync(JobId, GymId, content);
    }

    [Fact]
    public async Task Valid_14_digit_text_is_kept()
    {
        var content = BuildWorkbook(ws =>
        {
            Member(ws, 2, "M001", "201000000001");
            ws.Cell(2, 10).Value = "29001011234567"; // 14 digits as text
        });

        var result = await ParseAsync(content);

        result.Members.Should().ContainSingle();
        result.Members[0].NationalId.Should().Be("29001011234567");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Numeric_cell_preserves_all_14_digits()
    {
        var content = BuildWorkbook(ws =>
        {
            Member(ws, 2, "M001", "201000000001");
            // NID typed as a NUMBER — GetString() would yield scientific notation.
            ws.Cell(2, 10).Value = 29001011234567d;
        });

        var result = await ParseAsync(content);

        result.Members.Should().ContainSingle();
        result.Members[0].NationalId.Should().Be("29001011234567");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Thirteen_digits_is_rejected_but_member_still_imports()
    {
        var content = BuildWorkbook(ws =>
        {
            Member(ws, 2, "M001", "201000000001");
            ws.Cell(2, 10).Value = "1234567890123"; // 13 digits
        });

        var result = await ParseAsync(content);

        result.Members.Should().ContainSingle();
        result.Members[0].NationalId.Should().BeNull();
        result.Errors.Should().ContainSingle(e => e.Sheet == "Members" && e.Column == "NationalId");
    }

    [Fact]
    public async Task Non_numeric_is_rejected()
    {
        var content = BuildWorkbook(ws =>
        {
            Member(ws, 2, "M001", "201000000001");
            ws.Cell(2, 10).Value = "29001011ABCDEF";
        });

        var result = await ParseAsync(content);

        result.Members.Should().ContainSingle();
        result.Members[0].NationalId.Should().BeNull();
        result.Errors.Should().ContainSingle(e => e.Sheet == "Members" && e.Column == "NationalId");
    }

    [Fact]
    public async Task Blank_is_allowed_and_yields_null()
    {
        var content = BuildWorkbook(ws =>
        {
            Member(ws, 2, "M001", "201000000001");
            // col 10 left empty
        });

        var result = await ParseAsync(content);

        result.Members.Should().ContainSingle();
        result.Members[0].NationalId.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }
}
