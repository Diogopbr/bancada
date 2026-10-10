using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bancada.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReadableOrderNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "OrderNumbers",
                startValue: 10001L);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValueSql: "'BC-' || nextval('\"OrderNumbers\"')::text",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "OrderNumbers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValueSql: "'BC-' || nextval('\"OrderNumbers\"')::text");
        }
    }
}
