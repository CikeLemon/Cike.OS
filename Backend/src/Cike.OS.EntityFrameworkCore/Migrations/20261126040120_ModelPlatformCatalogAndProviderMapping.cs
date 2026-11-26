using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ModelPlatformCatalogAndProviderMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_TenantId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Models_ModelProviderId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_TenantId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ModelProviderId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Models");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Models",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Models",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelProviderMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ModelProviderId = table.Column<long>(type: "bigint", nullable: false),
                    ModelId = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProviderModelName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelProviderMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelProviderMappings_ModelProviders_ModelProviderId",
                        column: x => x.ModelProviderId,
                        principalTable: "ModelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_CreatedBy",
                table: "ModelProviderMappings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_IsDeleted",
                table: "ModelProviderMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_ModelId",
                table: "ModelProviderMappings",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_ModelProviderId",
                table: "ModelProviderMappings",
                column: "ModelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_TenantId",
                table: "ModelProviderMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_UpdatedBy",
                table: "ModelProviderMappings",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelProviderMappings");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Models");

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "Tags",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ModelProviderId",
                table: "Models",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                table: "Models",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TenantId",
                table: "Tags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelProviderId",
                table: "Models",
                column: "ModelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_TenantId",
                table: "Models",
                column: "TenantId");
        }
    }
}
