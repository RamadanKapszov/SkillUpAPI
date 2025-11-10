using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillUpAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Badges",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Badges_Title",
                table: "Badges",
                newName: "IX_Badges_Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Badges",
                newName: "Title");

            migrationBuilder.RenameIndex(
                name: "IX_Badges_Name",
                table: "Badges",
                newName: "IX_Badges_Title");
        }
    }
}
