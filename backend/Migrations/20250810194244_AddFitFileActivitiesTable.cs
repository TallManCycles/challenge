using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFitFileActivitiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FitFileActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ZwiftUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivityName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: false),
                    ElevationGainM = table.Column<double>(type: "double precision", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AverageSpeed = table.Column<double>(type: "double precision", nullable: true),
                    MaxSpeed = table.Column<double>(type: "double precision", nullable: true),
                    AverageHeartRate = table.Column<int>(type: "integer", nullable: true),
                    MaxHeartRate = table.Column<int>(type: "integer", nullable: true),
                    AveragePower = table.Column<int>(type: "integer", nullable: true),
                    MaxPower = table.Column<int>(type: "integer", nullable: true),
                    AverageCadence = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastProcessingAttempt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ChallengesProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ChallengesProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitFileActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitFileActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_ActivityDate",
                table: "FitFileActivities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_CreatedAt",
                table: "FitFileActivities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_FileName",
                table: "FitFileActivities",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_Status",
                table: "FitFileActivities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_UserId",
                table: "FitFileActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FitFileActivities_ZwiftUserId",
                table: "FitFileActivities",
                column: "ZwiftUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FitFileActivities");
        }
    }
}
