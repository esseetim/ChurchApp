using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Application.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationDomainEventsAndSummaryFamilySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Summaries_Type_PeriodType_StartDate_EndDate_GeneratedAtUtc",
                table: "Summaries");

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Summaries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "Donations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Summaries_FamilyId",
                table: "Summaries",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Summaries_Type_PeriodType_StartDate_EndDate_MemberId_FamilyId_ServiceName",
                table: "Summaries",
                columns: new[] { "Type", "PeriodType", "StartDate", "EndDate", "MemberId", "FamilyId", "ServiceName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Summaries_Families_FamilyId",
                table: "Summaries",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Summaries_Families_FamilyId",
                table: "Summaries");

            migrationBuilder.DropIndex(
                name: "IX_Summaries_FamilyId",
                table: "Summaries");

            migrationBuilder.DropIndex(
                name: "IX_Summaries_Type_PeriodType_StartDate_EndDate_MemberId_FamilyId_ServiceName",
                table: "Summaries");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Summaries");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "Donations");

            migrationBuilder.CreateIndex(
                name: "IX_Summaries_Type_PeriodType_StartDate_EndDate_GeneratedAtUtc",
                table: "Summaries",
                columns: new[] { "Type", "PeriodType", "StartDate", "EndDate", "GeneratedAtUtc" });
        }
    }
}
