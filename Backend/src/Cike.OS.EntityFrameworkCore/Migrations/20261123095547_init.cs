using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cike.OS.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instructions = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Options = table.Column<string>(type: "json", nullable: false)
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
                    table.PrimaryKey("PK_Agents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AgentToolPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<long>(type: "bigint", nullable: false),
                    ToolPackageId = table.Column<long>(type: "bigint", nullable: false),
                    IsFull = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_AgentToolPackages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConversationFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    Sender = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoragePath = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationFiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConversationMessageEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationMessageId = table.Column<long>(type: "bigint", nullable: false),
                    ActionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Duration = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ConversationMessageEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    SenderRole = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenderId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelProviders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Setting = table.Column<string>(type: "json", nullable: false)
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
                    table.PrimaryKey("PK_ModelProviders", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ModelProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TagIds = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
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
                    table.PrimaryKey("PK_Tags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ToolPackageCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
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
                    table.PrimaryKey("PK_ToolPackageCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ToolPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Readme = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToolPackageCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ToolPackages", x => x.Id);
                })
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
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ToolPackageId = table.Column<long>(type: "bigint", nullable: false),
                    Parameters = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Setting = table.Column<string>(type: "json", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AgentTools",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    AgentToolPackageId = table.Column<long>(type: "bigint", nullable: false),
                    ToolId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, comment: "CreatedAt"),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "CreatedBy"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: false, comment: "UpdatedBy"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentTools_AgentToolPackages_AgentToolPackageId",
                        column: x => x.AgentToolPackageId,
                        principalTable: "AgentToolPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_CreatedBy",
                table: "Agents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_IsDeleted",
                table: "Agents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TenantId",
                table: "Agents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_UpdatedBy",
                table: "Agents",
                column: "UpdatedBy");

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

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_AgentToolPackageId",
                table: "AgentTools",
                column: "AgentToolPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_CreatedBy",
                table: "AgentTools",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_IsDeleted",
                table: "AgentTools",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_TenantId",
                table: "AgentTools",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_ToolId",
                table: "AgentTools",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTools_UpdatedBy",
                table: "AgentTools",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_ConversationId",
                table: "ConversationFiles",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_CreatedBy",
                table: "ConversationFiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_IsDeleted",
                table: "ConversationFiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_Sender",
                table: "ConversationFiles",
                column: "Sender");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_TenantId",
                table: "ConversationFiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFiles_UpdatedBy",
                table: "ConversationFiles",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_ConversationId",
                table: "ConversationMembers",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_CreatedBy",
                table: "ConversationMembers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_IsDeleted",
                table: "ConversationMembers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_MemberId",
                table: "ConversationMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_TenantId",
                table: "ConversationMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_TenantId_ConversationId_MemberId",
                table: "ConversationMembers",
                columns: new[] { "TenantId", "ConversationId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_UpdatedBy",
                table: "ConversationMembers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_ConversationId",
                table: "ConversationMessageEvents",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_ConversationMessageId",
                table: "ConversationMessageEvents",
                column: "ConversationMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_CreatedBy",
                table: "ConversationMessageEvents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_IsDeleted",
                table: "ConversationMessageEvents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_TenantId",
                table: "ConversationMessageEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessageEvents_UpdatedBy",
                table: "ConversationMessageEvents",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationId",
                table: "ConversationMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_CreatedBy",
                table: "ConversationMessages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_IsDeleted",
                table: "ConversationMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_SenderId",
                table: "ConversationMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_TenantId",
                table: "ConversationMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_UpdatedBy",
                table: "ConversationMessages",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AgentId",
                table: "Conversations",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_CreatedBy",
                table: "Conversations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_IsDeleted",
                table: "Conversations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TenantId",
                table: "Conversations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UpdatedBy",
                table: "Conversations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviders_CreatedBy",
                table: "ModelProviders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviders_IsDeleted",
                table: "ModelProviders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviders_TenantId",
                table: "ModelProviders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelProviders_UpdatedBy",
                table: "ModelProviders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Models_CreatedBy",
                table: "Models",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Models_IsDeleted",
                table: "Models",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Models_ModelProviderId",
                table: "Models",
                column: "ModelProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_TenantId",
                table: "Models",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_UpdatedBy",
                table: "Models",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CreatedBy",
                table: "Tags",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_IsDeleted",
                table: "Tags",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TenantId",
                table: "Tags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UpdatedBy",
                table: "Tags",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackageCategories_CreatedBy",
                table: "ToolPackageCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackageCategories_IsDeleted",
                table: "ToolPackageCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackageCategories_TenantId",
                table: "ToolPackageCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackageCategories_UpdatedBy",
                table: "ToolPackageCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackages_CreatedBy",
                table: "ToolPackages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackages_IsDeleted",
                table: "ToolPackages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackages_TenantId",
                table: "ToolPackages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackages_ToolPackageCategoryId",
                table: "ToolPackages",
                column: "ToolPackageCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPackages_UpdatedBy",
                table: "ToolPackages",
                column: "UpdatedBy");

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
                columns: new[] { "TenantId", "UserId", "ToolId" });

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
                name: "Agents");

            migrationBuilder.DropTable(
                name: "AgentTools");

            migrationBuilder.DropTable(
                name: "ConversationFiles");

            migrationBuilder.DropTable(
                name: "ConversationMembers");

            migrationBuilder.DropTable(
                name: "ConversationMessageEvents");

            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "ModelProviders");

            migrationBuilder.DropTable(
                name: "Models");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "ToolPackageCategories");

            migrationBuilder.DropTable(
                name: "ToolPackages");

            migrationBuilder.DropTable(
                name: "Tools");

            migrationBuilder.DropTable(
                name: "UserToolConfigurations");

            migrationBuilder.DropTable(
                name: "AgentToolPackages");
        }
    }
}
