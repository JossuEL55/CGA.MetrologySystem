using System;
using CGA.MetrologySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGA.MetrologySystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260701120000_AddUserAccessTracking")]
    public partial class AddUserAccessTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcceso",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UltimoAcceso",
                table: "AspNetUsers");
        }
    }
}
