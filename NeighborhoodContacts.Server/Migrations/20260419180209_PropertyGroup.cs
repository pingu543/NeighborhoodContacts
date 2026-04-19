using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeighborhoodContacts.Server.Migrations
{
    /// <inheritdoc />
    public partial class PropertyGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PropertyGroupId",
                table: "Properties",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PropertyGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_PropertyGroupId",
                table: "Properties",
                column: "PropertyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyGroups_Name",
                table: "PropertyGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyGroups_PropertyGroupId",
                table: "Properties",
                column: "PropertyGroupId",
                principalTable: "PropertyGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyGroups_PropertyGroupId",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "PropertyGroups");

            migrationBuilder.DropIndex(
                name: "IX_Properties_PropertyGroupId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PropertyGroupId",
                table: "Properties");
        }
    }
}
