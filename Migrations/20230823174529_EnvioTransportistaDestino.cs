using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class EnvioTransportistaDestino : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TransportistaDestinoCodigo",
                table: "Envios",
                type: "decimal(18,2)",
                nullable: true);

            //migrationBuilder.AddColumn<decimal>(
            //    name: "TransportistaCodigoDestino",
            //    table: "EnvioDTO",
            //    type: "decimal(18,2)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "TransportistaId",
            //    table: "EnvioDTO",
            //    type: "int",
            //    nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransportistaDestinoCodigo",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "TransportistaCodigoDestino",
                table: "EnvioDTO");

            migrationBuilder.DropColumn(
                name: "TransportistaId",
                table: "EnvioDTO");
        }
    }
}
