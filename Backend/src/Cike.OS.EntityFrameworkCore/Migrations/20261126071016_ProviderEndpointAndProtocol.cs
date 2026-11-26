using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ProviderEndpointAndProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelProviderMappings_ModelProviders_ModelProviderId",
                table: "ModelProviderMappings");

            migrationBuilder.DropColumn(
                name: "Setting",
                table: "ModelProviders");

            migrationBuilder.RenameColumn(
                name: "ModelProviderId",
                table: "ModelProviderMappings",
                newName: "ModelProviderEndpointId");

            migrationBuilder.RenameIndex(
                name: "IX_ModelProviderMappings_ModelProviderId",
                table: "ModelProviderMappings",
                newName: "IX_ModelProviderMappings_ModelProviderEndpointId");

            migrationBuilder.CreateTable(
                name: "ModelProviderEndpoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ModelProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Protocol = table.Column<int>(type: "int", nullable: false),
                    BaseUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Setting = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
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

            migrationBuilder.AddForeignKey(
                name: "FK_ModelProviderMappings_ModelProviderEndpoints_ModelProviderEn~",
                table: "ModelProviderMappings",
                column: "ModelProviderEndpointId",
                principalTable: "ModelProviderEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelProviderMappings_ModelProviderEndpoints_ModelProviderEn~",
                table: "ModelProviderMappings");

            migrationBuilder.DropTable(
                name: "ModelProviderEndpoints");

            migrationBuilder.RenameColumn(
                name: "ModelProviderEndpointId",
                table: "ModelProviderMappings",
                newName: "ModelProviderId");

            migrationBuilder.RenameIndex(
                name: "IX_ModelProviderMappings_ModelProviderEndpointId",
                table: "ModelProviderMappings",
                newName: "IX_ModelProviderMappings_ModelProviderId");

            migrationBuilder.AddColumn<string>(
                name: "Setting",
                table: "ModelProviders",
                type: "json",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelProviderMappings_ModelProviders_ModelProviderId",
                table: "ModelProviderMappings",
                column: "ModelProviderId",
                principalTable: "ModelProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
