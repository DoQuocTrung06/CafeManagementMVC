using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeManagementMVC.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSizeProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Size",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
