using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameDurationMinutesToDurationSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Simply rename the column - Garmin data is already in seconds
            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "Activities",
                newName: "DurationSeconds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Simply rename the column back
            migrationBuilder.RenameColumn(
                name: "DurationSeconds",
                table: "Activities",
                newName: "DurationMinutes");
        }
    }
}
