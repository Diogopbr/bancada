using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancada.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccesses_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccesses_OrderId",
                table: "CustomerAccesses",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccesses_TokenHash",
                table: "CustomerAccesses",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAccesses");
        }
    }
}
