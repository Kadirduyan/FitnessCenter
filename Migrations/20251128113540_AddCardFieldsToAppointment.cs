using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddCardFieldsToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardCVV",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardExpiryDate",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "Appointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardCVV",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CardExpiryDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "Appointments");
        }
    }
}
