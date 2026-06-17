using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISO9001Queue.Database.EF.Migrations
{
    /// <inheritdoc />
    public partial class LimitDataColumnTo4000 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows may exceed 4000 chars; trim them first or the ALTER COLUMN fails.
            migrationBuilder.Sql(
                "UPDATE Iso9001IncidentReports SET Data = LEFT(Data, 4000) WHERE LEN(Data) > 4000;");
            migrationBuilder.Sql(
                "UPDATE Iso9001AuditLogs SET Data = LEFT(Data, 4000) WHERE LEN(Data) > 4000;");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Iso9001IncidentReports",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Iso9001AuditLogs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Iso9001IncidentReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Iso9001AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }
    }
}
