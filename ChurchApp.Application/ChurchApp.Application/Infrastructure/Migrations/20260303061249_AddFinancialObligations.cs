using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Application.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ObligationId",
                table: "Donations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialObligations", x => x.Id);
                    table.CheckConstraint("CK_FinancialObligations_TotalAmount_Positive", "\"TotalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancialObligations_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_ObligationId",
                table: "Donations",
                column: "ObligationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialObligations_DueDate",
                table: "FinancialObligations",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialObligations_MemberId_Status",
                table: "FinancialObligations",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_FinancialObligations_ObligationId",
                table: "Donations",
                column: "ObligationId",
                principalTable: "FinancialObligations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donations_FinancialObligations_ObligationId",
                table: "Donations");

            migrationBuilder.DropTable(
                name: "FinancialObligations");

            migrationBuilder.DropIndex(
                name: "IX_Donations_ObligationId",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "ObligationId",
                table: "Donations");
        }
    }
}
