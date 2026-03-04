using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Application.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFinancailObligation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GmailMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenderHandle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawContentJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedDonationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawTransactions_Donations_ResolvedDonationId",
                        column: x => x.ResolvedDonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawTransactions_ProviderTransactionId_Unique",
                table: "RawTransactions",
                column: "ProviderTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawTransactions_Provider_Status",
                table: "RawTransactions",
                columns: new[] { "Provider", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RawTransactions_ResolvedDonationId",
                table: "RawTransactions",
                column: "ResolvedDonationId");

            migrationBuilder.CreateIndex(
                name: "IX_RawTransactions_Status",
                table: "RawTransactions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawTransactions");
        }
    }
}
