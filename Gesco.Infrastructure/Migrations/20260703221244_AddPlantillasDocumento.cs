using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gesco.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantillasDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantillasDocumento",
                columns: table => new
                {
                    PlantillaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariablesSoportadas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasDocumento", x => x.PlantillaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantillasDocumento");
        }
    }
}
