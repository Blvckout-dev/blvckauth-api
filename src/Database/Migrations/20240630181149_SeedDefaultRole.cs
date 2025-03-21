﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blvckout.BlvckAuth.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
