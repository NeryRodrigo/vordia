using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gesco.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoHistoricoToContrato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoHistorico",
                table: "ContratosInquilino",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoHistorico",
                table: "ContratosInquilino");
        }
    }
}
