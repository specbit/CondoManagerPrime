using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddAssignedToIdToConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatorId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "InitiatorId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Conversations");
        }
    }
}
