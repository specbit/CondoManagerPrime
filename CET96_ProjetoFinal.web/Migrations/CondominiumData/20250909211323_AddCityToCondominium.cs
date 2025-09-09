using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddCityToCondominium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Condominiums",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Condominiums");
        }
    }
}
