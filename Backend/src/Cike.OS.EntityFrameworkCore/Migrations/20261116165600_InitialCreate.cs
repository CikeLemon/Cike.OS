using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ToolPackageId = table.Column<long>(type: "bigint", nullable: false),
                    Parameters = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserToolConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToolId = table.Column<long>(type: "bigint", nullable: false),
                    Values = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToolConfigurations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_CreatedBy",
                table: "Tools",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_IsDeleted",
                table: "Tools",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_TenantId",
                table: "Tools",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_ToolPackageId",
                table: "Tools",
                column: "ToolPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_UpdatedBy",
                table: "Tools",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_CreatedBy",
                table: "UserToolConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_IsDeleted",
                table: "UserToolConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_TenantId",
                table: "UserToolConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_TenantId_UserId_ToolId",
                table: "UserToolConfigurations",
                columns: new[] { "TenantId", "UserId", "ToolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_ToolId",
                table: "UserToolConfigurations",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_UpdatedBy",
                table: "UserToolConfigurations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserToolConfigurations_UserId",
                table: "UserToolConfigurations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tools");

            migrationBuilder.DropTable(
                name: "UserToolConfigurations");
        }
    }
}
