using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOrganizationalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LoginName",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Users",
                newName: "AcademicTitle");

            migrationBuilder.AlterColumn<string>(
                name: "LoginName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonnelNumber",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityIdentifier",
                table: "Users",
                type: "nvarchar(184)",
                maxLength: 184,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LoginName",
                table: "Users",
                column: "LoginName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonnelNumber",
                table: "Users",
                column: "PersonnelNumber",
                unique: true,
                filter: "[PersonnelNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SecurityIdentifier",
                table: "Users",
                column: "SecurityIdentifier",
                unique: true,
                filter: "[SecurityIdentifier] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_LoginName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonnelNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SecurityIdentifier",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonnelNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityIdentifier",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "AcademicTitle",
                table: "Users",
                newName: "Title");

            migrationBuilder.AlterColumn<string>(
                name: "LoginName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LoginName",
                table: "Users",
                column: "LoginName",
                unique: true,
                filter: "[LoginName] IS NOT NULL");
        }
    }
}
