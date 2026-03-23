using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortal.Migrations
{
    /// <inheritdoc />
    public partial class nepnep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "O2Esdae1BIpDX7bsgeUv+S1teVqLWpwXBw9qY8l6U7I=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$npnhaXhxOCrO51AzZRm8QO4H.gPCUHvvaSN5WWnuh0Sqp4S55MCUm");
        }
    }
}
