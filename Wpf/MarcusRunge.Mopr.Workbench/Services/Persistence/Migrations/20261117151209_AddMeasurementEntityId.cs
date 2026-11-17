using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261117151209_AddMeasurementEntityId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "EntityId",
            table: "Measurements",
            type: "uniqueidentifier",
            nullable: false,
            defaultValueSql: "NEWSEQUENTIALID()");

        migrationBuilder.CreateIndex(
            name: "IX_Measurements_EntityId",
            table: "Measurements",
            column: "EntityId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Measurements_EntityId",
            table: "Measurements");

        migrationBuilder.DropColumn(
            name: "EntityId",
            table: "Measurements");
    }
}
