using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGarminWebhookTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarminActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SummaryId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ActivityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartTimeOffsetInSeconds = table.Column<int>(type: "integer", nullable: false),
                    DurationInSeconds = table.Column<int>(type: "integer", nullable: false),
                    DistanceInMeters = table.Column<double>(type: "double precision", nullable: true),
                    TotalElevationGainInMeters = table.Column<double>(type: "double precision", nullable: true),
                    TotalElevationLossInMeters = table.Column<double>(type: "double precision", nullable: true),
                    ActiveKilocalories = table.Column<int>(type: "integer", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    IsWebUpload = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseData = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarminActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GarminActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GarminWebhookPayloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WebhookType = table.Column<string>(type: "text", nullable: false),
                    RawPayload = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarminWebhookPayloads", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_ActivityType",
                table: "GarminActivities",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_IsProcessed",
                table: "GarminActivities",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_StartTime",
                table: "GarminActivities",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_SummaryId",
                table: "GarminActivities",
                column: "SummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_UserId",
                table: "GarminActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivities_UserId_ActivityType",
                table: "GarminActivities",
                columns: new[] { "UserId", "ActivityType" });

            migrationBuilder.CreateIndex(
                name: "IX_GarminWebhookPayloads_IsProcessed",
                table: "GarminWebhookPayloads",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_GarminWebhookPayloads_NextRetryAt",
                table: "GarminWebhookPayloads",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_GarminWebhookPayloads_ReceivedAt",
                table: "GarminWebhookPayloads",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GarminWebhookPayloads_WebhookType",
                table: "GarminWebhookPayloads",
                column: "WebhookType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarminActivities");

            migrationBuilder.DropTable(
                name: "GarminWebhookPayloads");
        }
    }
}
