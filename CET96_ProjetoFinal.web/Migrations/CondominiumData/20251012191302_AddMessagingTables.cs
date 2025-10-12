using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CET96_ProjetoFinal.web.Migrations.CondominiumData
{
    /// <inheritdoc />
    public partial class AddMessagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* WE ARE COMMENTING THIS PART OUT BECAUSE THE TABLE ALREADY EXISTS
            migrationBuilder.CreateTable(
                name: "Condominiums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CondominiumManagerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyRegistryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContractValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeePerUnit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserCreatedId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserUpdatedId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserDeletedId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Condominiums", x => x.Id);
                });
            */

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            /* WE ARE COMMENTING THIS PART OUT BECAUSE THE TABLE ALREADY EXISTS
           migrationBuilder.CreateTable(
               name: "Units",
               columns: table => new
               {
                   Id = table.Column<int>(type: "int", nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   UnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                   CondominiumId = table.Column<int>(type: "int", nullable: false),
                   OwnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                   IsActive = table.Column<bool>(type: "bit", nullable: false),
                   CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                   UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                   DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Units", x => x.Id);
                   table.ForeignKey(
                       name: "FK_Units_Condominiums_CondominiumId",
                       column: x => x.CondominiumId,
                       principalTable: "Condominiums",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
               });
              */

            migrationBuilder.CreateTable(
               name: "Messages",
               columns: table => new
               {
                   Id = table.Column<int>(type: "int", nullable: false)
                       .Annotation("SqlServer:Identity", "1, 1"),
                   Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                   SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                   SenderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                   ReceiverId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                   ConversationId = table.Column<int>(type: "int", nullable: false),
                   IsRead = table.Column<bool>(type: "bit", nullable: false),
                   Status = table.Column<int>(type: "int", nullable: false)
               },
               constraints: table =>
               {
                   table.PrimaryKey("PK_Messages", x => x.Id);
                   table.ForeignKey(
                       name: "FK_Messages_Conversations_ConversationId",
                       column: x => x.ConversationId,
                       principalTable: "Conversations",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
               });

           /* WE ARE COMMENTING THIS PART OUT BECAUSE THE INDEX ALREADY EXISTS
           migrationBuilder.CreateIndex(
               name: "IX_Condominiums_CompanyId_PropertyRegistryNumber",
               table: "Condominiums",
               columns: new[] { "CompanyId", "PropertyRegistryNumber" },
               unique: true);
           */

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            /* WE ARE COMMENTING THIS PART OUT BECAUSE THE INDEX ALREADY EXISTS
            migrationBuilder.CreateIndex(
                name: "IX_Units_CondominiumId_UnitNumber",
                table: "Units",
                columns: new[] { "CondominiumId", "UnitNumber" },
                unique: true);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            /* WE ARE COMMENTING THIS PART OUT
            migrationBuilder.DropTable(
                name: "Units");
            */

            migrationBuilder.DropTable(
                name: "Conversations");

            /* WE ARE COMMENTING THIS PART OUT
            migrationBuilder.DropTable(
                name: "Condominiums");
            */
        }
    }
}
