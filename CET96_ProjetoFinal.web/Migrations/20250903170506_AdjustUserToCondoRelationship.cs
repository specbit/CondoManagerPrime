using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations
{
    /// <inheritdoc />
    public partial class AdjustUserToCondoRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Condominium_CondominiumId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Condominium");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CondominiumId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Condominium",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CondominiumManagerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContractValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeePerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumberOfUnits = table.Column<int>(type: "int", nullable: false),
                    PropertyRegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserCreatedId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserDeletedId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserUpdatedId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Condominium", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CondominiumId",
                table: "AspNetUsers",
                column: "CondominiumId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Condominium_CondominiumId",
                table: "AspNetUsers",
                column: "CondominiumId",
                principalTable: "Condominium",
                principalColumn: "Id");
        }
    }
}
