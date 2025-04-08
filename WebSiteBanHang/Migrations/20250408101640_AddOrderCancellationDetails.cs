using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSiteBanHang.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCancellationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationType",
                table: "Orders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancellationType",
                table: "Orders");
        }
    }
}
