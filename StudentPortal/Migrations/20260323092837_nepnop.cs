using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentPortal.Migrations
{
    /// <inheritdoc />
    public partial class nepnop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "O2Esdae1BIpDX7bsgeUv+S1teVqLWpwXBw9qY8l6U7I=");
        }
    }
}
