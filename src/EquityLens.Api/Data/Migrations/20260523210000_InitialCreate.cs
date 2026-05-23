using System;
using EquityLens.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquityLens.Api.Data.Migrations;

[DbContext(typeof(EquityLensDbContext))]
[Migration("20260523210000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApiRequestLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Provider = table.Column<string>(type: "TEXT", nullable: false),
                EndpointName = table.Column<string>(type: "TEXT", nullable: false),
                Ticker = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                Success = table.Column<bool>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: false),
                RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiRequestLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ResearchSnapshots",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Ticker = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                RiskScore = table.Column<int>(type: "INTEGER", nullable: false),
                OneYearReturn = table.Column<decimal>(type: "TEXT", nullable: false),
                Summary = table.Column<string>(type: "TEXT", nullable: false),
                DashboardJson = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResearchSnapshots", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WatchlistItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Ticker = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false),
                AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastKnownRiskScore = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WatchlistItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WatchlistItems_Ticker",
            table: "WatchlistItems",
            column: "Ticker",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApiRequestLogs");
        migrationBuilder.DropTable(name: "ResearchSnapshots");
        migrationBuilder.DropTable(name: "WatchlistItems");
    }
}
