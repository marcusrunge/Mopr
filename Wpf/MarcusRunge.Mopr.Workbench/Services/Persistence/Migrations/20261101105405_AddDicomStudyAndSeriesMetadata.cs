using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDicomStudyAndSeriesMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PatientBirthDate",
                table: "Studies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientId",
                table: "Studies",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "Studies",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudyDate",
                table: "Studies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesNumber",
                table: "Series",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientBirthDate",
                table: "Studies");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Studies");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "Studies");

            migrationBuilder.DropColumn(
                name: "StudyDate",
                table: "Studies");

            migrationBuilder.DropColumn(
                name: "SeriesNumber",
                table: "Series");
        }
    }
}
