using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ProviderAsConnectionAndModelSubEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelProviderMappings");

            migrationBuilder.DropTable(
                name: "ModelProviderEndpoints");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Models");

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

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "ModelProviders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "ModelProviders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "ModelProviders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Setting",
                table: "ModelProviders",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelProviderId",
                table: "Models",
                column: "ModelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_TenantId",
                table: "Models",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Models_ModelProviders_ModelProviderId",
                table: "Models",
                column: "ModelProviderId",
                principalTable: "ModelProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Models_ModelProviders_ModelProviderId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_ModelProviderId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_TenantId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ModelProviderId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "ModelProviders");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "ModelProviders");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "ModelProviders");

            migrationBuilder.DropColumn(
                name: "Setting",
                table: "ModelProviders");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Models",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Models",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelProviderEndpoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ApiKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaseUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModelProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Protocol = table.Column<int>(type: "int", nullable: false),
                    Setting = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelProviderEndpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelProviderEndpoints_ModelProviders_ModelProviderId",
                        column: x => x.ModelProviderId,
                        principalTable: "ModelProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelProviderMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModelId = table.Column<long>(type: "bigint", nullable: false),
                    ModelProviderEndpointId = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ProviderModelName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelProviderMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelProviderMappings_ModelProviderEndpoints_ModelProviderEn~",
                        column: x => x.ModelProviderEndpointId,
                        principalTable: "ModelProviderEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderEndpoints_CreatedBy",
                table: "ModelProviderEndpoints",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderEndpoints_IsDeleted",
                table: "ModelProviderEndpoints",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderEndpoints_ModelProviderId",
                table: "ModelProviderEndpoints",
                column: "ModelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderEndpoints_TenantId",
                table: "ModelProviderEndpoints",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderEndpoints_UpdatedBy",
                table: "ModelProviderEndpoints",
                column: "UpdatedBy");

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
                name: "IX_ModelProviderMappings_ModelProviderEndpointId",
                table: "ModelProviderMappings",
                column: "ModelProviderEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_TenantId",
                table: "ModelProviderMappings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviderMappings_UpdatedBy",
                table: "ModelProviderMappings",
                column: "UpdatedBy");
        }
    }
}
