using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeTargetsAndProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add target fields to Challenge table
            migrationBuilder.AddColumn<double>(
                name: "TargetDistance",
                table: "Challenges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TargetElevation",
                table: "Challenges",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetTime",
                table: "Challenges",
                type: "integer",
                nullable: true);

            // Add progress tracking fields to ChallengeParticipants table
            migrationBuilder.AddColumn<double>(
                name: "CurrentDistance",
                table: "ChallengeParticipants",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurrentElevation",
                table: "ChallengeParticipants",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentTime",
                table: "ChallengeParticipants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "ChallengeParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ChallengeParticipants",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "ChallengeParticipants",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Update LastUpdated to current time for existing records
            migrationBuilder.Sql(@"
                UPDATE ""ChallengeParticipants"" 
                SET ""LastUpdated"" = NOW()
                WHERE ""LastUpdated"" = '0001-01-01 00:00:00';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove target fields from Challenge table
            migrationBuilder.DropColumn(name: "TargetDistance", table: "Challenges");
            migrationBuilder.DropColumn(name: "TargetElevation", table: "Challenges");
            migrationBuilder.DropColumn(name: "TargetTime", table: "Challenges");

            // Remove progress tracking fields from ChallengeParticipants table
            migrationBuilder.DropColumn(name: "CurrentDistance", table: "ChallengeParticipants");
            migrationBuilder.DropColumn(name: "CurrentElevation", table: "ChallengeParticipants");
            migrationBuilder.DropColumn(name: "CurrentTime", table: "ChallengeParticipants");
            migrationBuilder.DropColumn(name: "IsCompleted", table: "ChallengeParticipants");
            migrationBuilder.DropColumn(name: "CompletedAt", table: "ChallengeParticipants");
            migrationBuilder.DropColumn(name: "LastUpdated", table: "ChallengeParticipants");
        }
    }
}
