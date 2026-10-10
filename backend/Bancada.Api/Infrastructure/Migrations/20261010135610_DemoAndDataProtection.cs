using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bancada.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoAndDataProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoRole",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoVisitors",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoVisitors", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_DemoVisitors_DemoWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "DemoWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_WorkspaceId",
                table: "Orders",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_WorkspaceId",
                table: "AspNetUsers",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoVisitors_WorkspaceId",
                table: "DemoVisitors",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoWorkspaces_ExpiresAt",
                table: "DemoWorkspaces",
                column: "ExpiresAt");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_DemoWorkspaces_WorkspaceId",
                table: "AspNetUsers",
                column: "WorkspaceId",
                principalTable: "DemoWorkspaces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DemoWorkspaces_WorkspaceId",
                table: "Orders",
                column: "WorkspaceId",
                principalTable: "DemoWorkspaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_DemoWorkspaces_WorkspaceId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DemoWorkspaces_WorkspaceId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "DemoVisitors");

            migrationBuilder.DropTable(
                name: "DemoWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_Orders_WorkspaceId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WorkspaceId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DemoRole",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AspNetUsers");
        }
    }
}
