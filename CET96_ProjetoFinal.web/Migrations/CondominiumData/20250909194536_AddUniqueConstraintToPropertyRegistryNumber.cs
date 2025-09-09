using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToPropertyRegistryNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Condominiums",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Condominiums_CompanyId_PropertyRegistryNumber",
                table: "Condominiums",
                columns: new[] { "CompanyId", "PropertyRegistryNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Condominiums_CompanyId_PropertyRegistryNumber",
                table: "Condominiums");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Condominiums");
        }
    }
}
