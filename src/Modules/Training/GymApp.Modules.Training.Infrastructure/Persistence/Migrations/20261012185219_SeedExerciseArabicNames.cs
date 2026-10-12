using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymApp.Modules.Training.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedExerciseArabicNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c300-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "القفز مع فتح الذراعين والأرجل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c301-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "العدو السريع");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c302-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الجري في الهواء الطلق");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c303-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ركوب الدراجة في الهواء الطلق");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c304-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "نط الحبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c305-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "نط الحبل المزدوج");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c306-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "بيربي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c307-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع الركبتين عالياً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c308-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة ثنيات الورك");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c309-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة الصدر (عند الباب)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة الكتف (عبر الجسم)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة الترايسبس (فوق الرأس)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السباحة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "حبال المعركة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة العضلة الخلفية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة العضلة الأمامية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c310-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ثبات الجسم المجوف");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c311-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الجري على المشاية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c312-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "المشي على المشاية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c313-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "المشي المائل على المشاية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c314-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين المعدة على مقعد مائل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c315-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "علم التنين");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c316-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ركلات الرفرفة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c317-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "لمس أصابع القدم");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c318-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز صعود السلالم");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c319-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ستير ماستر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "دراجة الهواء");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز التزلج");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الدراجة الثابتة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الدراجة المستلقية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز الأوربتراك");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز التجديف");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c324-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرير الكرة الطبية من الصدر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c325-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رمي الكرة الطبية بالدوران");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c326-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "قفزة المتزلج");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c330-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الانحناء للأمام جالساً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c331-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة السمانة واقفاً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c332-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الفوم رولر (الجسم كامل)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c333-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الفوم رولر (جانب الفخذ)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c334-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة عضلة الظهر العريضة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c335-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إطالة القطة والبقرة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c336-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "وضعية الطفل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c337-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "وضعية الحمامة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c338-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "القفز مع ضم الركبتين");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c339-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الوثب الجانبي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط الانفجاري");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رمي الكرة الطبية على الأرض");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الفوم رولر (الظهر)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "القفز على الصندوق");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "قفزة العمق");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الوثب الطويل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c340-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ثني الخلفية النوردي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c341-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب بين الأرجل بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c342-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة واقفاً (جهاز)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c343-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة جالساً (جهاز)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c344-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة الرومانية بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c345-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة الرومانية برجل واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c346-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين صباح الخير");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c347-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع الجلوت والخلفية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c348-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c349-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة برجل واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة بوزن الجسم");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "دفع الورك بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة على جهاز ضغط الأرجل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة على جهاز سميث");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة بوضع الانحناء");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع السمانة بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c350-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز في سكوات");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c351-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات البندول");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c352-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سيسي سكوات");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c353-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات على رجل واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c354-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأرجل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c355-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز هاك سكوات");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c356-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز تمديد الأرجل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c357-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات على جهاز سميث");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c358-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز ثني الأرجل مستلقياً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c359-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز ثني الأرجل جالساً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز ثني الأرجل واقفاً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة بأرجل مستقيمة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الجلوس على الحائط");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات مع القفز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "القفز على الصندوق");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات الكأس بالكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c360-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع الأرجل (مستلقياً)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c361-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع الأرجل معلقاً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c362-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "رفع الركبتين معلقاً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c363-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "اللف الروسي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c364-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "كرانش الدراجة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c365-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الكرانش العكسي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c366-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين المعدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c367-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين V للبطن");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c368-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "كرانش بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c369-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الحطاب بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط بالوف بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "كرانش على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تسلق الجبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الحشرة الميتة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الطائر والكلب");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "عجلة البطن");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c370-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "إبعاد الورك بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c371-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "دفع الورك على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c372-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز إبعاد الورك");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c373-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز ضم الورك");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c374-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "دفع الورك بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c375-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جسر الجلوت");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c376-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جسر الجلوت برجل واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c377-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الركلة الخلفية بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c378-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضفدع");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c379-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "البلانك");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "البلانك الجانبي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين البطن (كرانش)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين المحارة بالمطاط");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "المشي الجانبي بالمطاط");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات سومو");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة سومو بالكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c380-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل المطرقة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c381-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالدمبل على مقعد مائل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c382-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل التركيز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c383-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل زوتمان");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c384-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل على المسند (بار)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c385-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل معكوس بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c386-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل السحب");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c387-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c388-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالكابل العالي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c389-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل على جهاز المسند");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل البايسبس على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل العنكبوت");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل المطرقة عبر الجسم");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل على المسند (دمبل)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل المطرقة بالكابل (حبل)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c390-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأكتاف على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c391-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الجانبي على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c392-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الكتف الخلفي على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c393-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأكتاف على جهاز سميث");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c394-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الجانبي بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c395-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الأمامي بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c396-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب الوجه بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c397-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الكتف الخلفي بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c398-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "شد المطاط للخارج");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c399-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالبار المتعرج");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط لاند ماين");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط بوضع القمة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط بالوقوف على اليدين");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الأمامي بالطارة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات خلفي بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات أمامي بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات فوق الرأس");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات زيرشر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الترايسبس على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الغطس على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط جي إم");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الترايسبس بالكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الطعن المتحرك بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الصعود على الصندوق بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3aa-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات بلغاري بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ab-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات الكأس");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ac-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الطعن بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ad-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الصعود على الصندوق بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ae-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكوات بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3af-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الطعن بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الترايسبس فوق الرأس بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الكابل للأسفل بذراع واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكل كراشر (بار متعرج)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط البار بقبضة ضيقة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل 21");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تبادل بالكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الترايسبس للأسفل (كابل)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الترايسبس بالحبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط تيت");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "متوازي للترايسبس");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ba-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الغطس على المقعد");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط الماسي (ترايسبس)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الترايسبس فوق الرأس بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الترايسبس فوق الرأس بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3be-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الركلة الخلفية بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bf-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سكل كراشر بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط الواسع");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط الماسي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط المائل للأسفل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "متوازي للصدر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر العلوي على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز الفراشة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر على جهاز سميث");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمرين الضغط");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب البار مع الانحناء");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب بندلاي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ca-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب تي بار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب الدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط سفيند");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة التقليدية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ce-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة سومو");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cf-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة الرومانية");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر بقبضة ضيقة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر العلوي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر السفلي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر العلوي بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر السفلي بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تقاطع الكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تقاطع الكابل السفلي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3da-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الصدر بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3db-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الصدر على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3dc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الصدر بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3dd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الصدر العلوي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3de-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الصدر السفلي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3df-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "بول أوفر بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "أرجحة الكيتل بل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الضغط فوق الرأس (الضغط العسكري)");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأكتاف بالبار جالساً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "بوش برس");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "العقلة بقبضة محايدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب المقلوب");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تمديد الظهر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "هايبر إكستنشن");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الجانبي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفع الأمامي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ea-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "تفتيح الكتف الخلفي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3eb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العمودي بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ec-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العمودي بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ed-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأكتاف بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ee-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط الأكتاف بالدمبل جالساً");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ef-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "ضغط أرنولد");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العالي بقبضة واسعة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العالي بقبضة ضيقة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العالي بقبضة معكوسة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب الأرضي بالكابل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "الرفعة الميتة بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "هز الأكتاف بالدمبل");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "هز الأكتاف بالبار");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العالي");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "جهاز السحب بدعم الصدر");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب مع الانحناء على جهاز سميث");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fa-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "العقلة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "العقلة بقبضة معكوسة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "سحب الكابل بذراع واحدة");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب بذراعين مستقيمتين");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fe-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب على الجهاز");

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ff-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: "السحب العالي على الجهاز");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c300-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c301-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c302-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c303-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c304-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c305-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c306-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c307-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c308-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c309-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c30f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c310-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c311-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c312-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c313-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c314-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c315-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c316-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c317-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c318-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c319-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c31f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c324-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c325-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c326-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c330-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c331-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c332-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c333-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c334-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c335-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c336-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c337-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c338-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c339-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c33f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c340-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c341-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c342-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c343-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c344-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c345-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c346-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c347-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c348-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c349-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c34f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c350-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c351-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c352-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c353-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c354-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c355-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c356-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c357-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c358-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c359-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c35f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c360-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c361-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c362-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c363-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c364-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c365-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c366-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c367-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c368-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c369-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c36f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c370-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c371-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c372-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c373-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c374-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c375-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c376-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c377-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c378-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c379-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c37f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c380-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c381-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c382-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c383-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c384-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c385-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c386-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c387-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c388-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c389-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c38f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c390-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c391-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c392-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c393-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c394-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c395-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c396-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c397-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c398-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c399-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39a-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39b-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39c-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39d-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39e-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c39f-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3a9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3aa-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ab-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ac-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ad-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ae-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3af-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3b9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ba-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3be-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3bf-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3c9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ca-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ce-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3cf-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3da-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3db-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3dc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3dd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3de-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3df-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3e9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ea-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3eb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ec-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ed-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ee-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ef-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f0-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f1-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f2-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f3-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f4-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f5-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f6-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f7-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f8-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3f9-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fa-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fb-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fc-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fd-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3fe-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);

            migrationBuilder.UpdateData(
                schema: "training",
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3ff-e5f6-7890-abcd-ef1234567890"),
                column: "NameAr",
                value: null);
        }
    }
}
