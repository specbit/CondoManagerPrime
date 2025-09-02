using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddDeletedAtToCondominiums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Condominiums",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Condominiums");
        }
    }
}
