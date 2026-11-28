using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AgentToolAsAgentSubEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentTools_AgentToolPackages_AgentToolPackageId",
                table: "AgentTools");

            migrationBuilder.DropTable(
                name: "AgentToolPackages");

            migrationBuilder.RenameColumn(
                name: "ToolId",
                table: "AgentTools",
                newName: "TargetId");

            migrationBuilder.RenameColumn(
                name: "AgentToolPackageId",
                table: "AgentTools",
                newName: "AgentId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentTools_ToolId",
                table: "AgentTools",
                newName: "IX_AgentTools_TargetId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentTools_AgentToolPackageId",
                table: "AgentTools",
                newName: "IX_AgentTools_AgentId");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "AgentTools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentTools_Agents_AgentId",
                table: "AgentTools",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentTools_Agents_AgentId",
                table: "AgentTools");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AgentTools");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                table: "AgentTools",
                newName: "ToolId");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                table: "AgentTools",
                newName: "AgentToolPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentTools_TargetId",
                table: "AgentTools",
                newName: "IX_AgentTools_ToolId");

            migrationBuilder.RenameIndex(
                name: "IX_AgentTools_AgentId",
                table: "AgentTools",
                newName: "IX_AgentTools_AgentToolPackageId");

            migrationBuilder.CreateTable(
                name: "AgentToolPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<long>(type: "bigint", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsFull = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ToolPackageId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolPackages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_AgentId",
                table: "AgentToolPackages",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_CreatedBy",
                table: "AgentToolPackages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_IsDeleted",
                table: "AgentToolPackages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_TenantId",
                table: "AgentToolPackages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_ToolPackageId",
                table: "AgentToolPackages",
                column: "ToolPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolPackages_UpdatedBy",
                table: "AgentToolPackages",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentTools_AgentToolPackages_AgentToolPackageId",
                table: "AgentTools",
                column: "AgentToolPackageId",
                principalTable: "AgentToolPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
