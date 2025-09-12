using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToUnitNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_CondominiumId",
                table: "Units");

            migrationBuilder.CreateIndex(
                name: "IX_Units_CondominiumId_UnitNumber",
                table: "Units",
                columns: new[] { "CondominiumId", "UnitNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_CondominiumId_UnitNumber",
                table: "Units");

            migrationBuilder.CreateIndex(
                name: "IX_Units_CondominiumId",
                table: "Units",
                column: "CondominiumId");
        }
    }
}
