using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameDueDateToStartDateAddIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "Subscriptions",
                newName: "StartDate");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Subscriptions",
                newName: "DueDate");
        }
    }
}
