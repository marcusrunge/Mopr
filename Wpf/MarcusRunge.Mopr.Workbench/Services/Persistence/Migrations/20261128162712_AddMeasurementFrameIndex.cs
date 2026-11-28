using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261128162712_AddMeasurementFrameIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "FrameIndex",
            table: "Measurements",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FrameIndex",
            table: "Measurements");
    }
}
