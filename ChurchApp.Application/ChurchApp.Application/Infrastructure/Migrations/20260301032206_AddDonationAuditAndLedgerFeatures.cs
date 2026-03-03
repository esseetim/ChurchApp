using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChurchApp.Application.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationAuditAndLedgerFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Donations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Donations",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Donations",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Donations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Donations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Donations",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAtUtc",
                table: "Donations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedBy",
                table: "Donations",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DonationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DonationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationAudits_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_IdempotencyKey",
                table: "Donations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonationAudits_DonationId_OccurredAtUtc",
                table: "DonationAudits",
                columns: new[] { "DonationId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationAudits");

            migrationBuilder.DropIndex(
                name: "IX_Donations_IdempotencyKey",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "VoidedAtUtc",
                table: "Donations");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "Donations");
        }
    }
}
