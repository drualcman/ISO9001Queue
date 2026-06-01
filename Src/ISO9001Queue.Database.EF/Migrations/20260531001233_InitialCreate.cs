using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISO9001Queue.Database.EF.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Iso9001AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    EntityId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso9001AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Iso9001CustomerFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso9001CustomerFeedbacks", x => x.Id);
                    table.CheckConstraint("CK_CustomerFeedback_Rating", "[Rating] BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "Iso9001IncidentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CompanyId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    AffectedProcess = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso9001IncidentReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Iso9001NonConformities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AffectedProcess = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Cause = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso9001NonConformities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Iso9001NonConformityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NonConformityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Iso9001NonConformityDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Iso9001NonConformityDetails_Iso9001NonConformities_NonConformityId",
                        column: x => x.NonConformityId,
                        principalTable: "Iso9001NonConformities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001AuditLogs_CompanyId",
                table: "Iso9001AuditLogs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001AuditLogs_EntityId",
                table: "Iso9001AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001CustomerFeedbacks_CompanyId",
                table: "Iso9001CustomerFeedbacks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001CustomerFeedbacks_CustomerId",
                table: "Iso9001CustomerFeedbacks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001IncidentReports_CompanyId",
                table: "Iso9001IncidentReports",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001IncidentReports_EntityId",
                table: "Iso9001IncidentReports",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001NonConformities_CompanyId",
                table: "Iso9001NonConformities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001NonConformities_Status",
                table: "Iso9001NonConformities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Iso9001NonConformityDetails_NonConformityId",
                table: "Iso9001NonConformityDetails",
                column: "NonConformityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Iso9001AuditLogs");

            migrationBuilder.DropTable(
                name: "Iso9001CustomerFeedbacks");

            migrationBuilder.DropTable(
                name: "Iso9001IncidentReports");

            migrationBuilder.DropTable(
                name: "Iso9001NonConformityDetails");

            migrationBuilder.DropTable(
                name: "Iso9001NonConformities");
        }
    }
}
