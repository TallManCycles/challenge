using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActivityModelForFitFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns for FIT file support
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "Activities",
                type: "text",
                nullable: false,
                defaultValue: "cycling");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Activities",
                type: "text",
                nullable: false,
                defaultValue: "Manual");

            // Add new metric columns
            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Activities",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ElevationGainM",
                table: "Activities",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Add timing columns
            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Activities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Activities",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Add performance metric columns
            migrationBuilder.AddColumn<double>(
                name: "AverageSpeed",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxSpeed",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageHeartRate",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxHeartRate",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AveragePower",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxPower",
                table: "Activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AverageCadence",
                table: "Activities",
                type: "double precision",
                nullable: true);

            // Migrate existing data from old columns to new ones
            migrationBuilder.Sql(@"
                UPDATE ""Activities"" 
                SET 
                    ""DistanceKm"" = CAST(""Distance"" AS double precision),
                    ""ElevationGainM"" = CAST(""ElevationGain"" AS double precision),
                    ""DurationMinutes"" = ""MovingTime"",
                    ""StartTime"" = ""ActivityDate"",
                    ""EndTime"" = ""ActivityDate"" + (""MovingTime"" * INTERVAL '1 minute')
                WHERE ""Distance"" IS NOT NULL OR ""ElevationGain"" IS NOT NULL OR ""MovingTime"" IS NOT NULL;
            ");

            // Make GarminActivityId nullable since we now support other sources
            migrationBuilder.AlterColumn<string>(
                name: "GarminActivityId",
                table: "Activities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove new columns
            migrationBuilder.DropColumn(name: "ExternalId", table: "Activities");
            migrationBuilder.DropColumn(name: "ActivityType", table: "Activities");
            migrationBuilder.DropColumn(name: "Source", table: "Activities");
            migrationBuilder.DropColumn(name: "DistanceKm", table: "Activities");
            migrationBuilder.DropColumn(name: "ElevationGainM", table: "Activities");
            migrationBuilder.DropColumn(name: "DurationMinutes", table: "Activities");
            migrationBuilder.DropColumn(name: "StartTime", table: "Activities");
            migrationBuilder.DropColumn(name: "EndTime", table: "Activities");
            migrationBuilder.DropColumn(name: "AverageSpeed", table: "Activities");
            migrationBuilder.DropColumn(name: "MaxSpeed", table: "Activities");
            migrationBuilder.DropColumn(name: "AverageHeartRate", table: "Activities");
            migrationBuilder.DropColumn(name: "MaxHeartRate", table: "Activities");
            migrationBuilder.DropColumn(name: "AveragePower", table: "Activities");
            migrationBuilder.DropColumn(name: "MaxPower", table: "Activities");
            migrationBuilder.DropColumn(name: "AverageCadence", table: "Activities");

            // Restore GarminActivityId as required
            migrationBuilder.AlterColumn<string>(
                name: "GarminActivityId",
                table: "Activities",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
