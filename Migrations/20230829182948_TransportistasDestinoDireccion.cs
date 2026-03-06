using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class TransportistasDestinoDireccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "TransportistaId",
            //    table: "EnvioDTO");

            //migrationBuilder.RenameColumn(
            //    name: "TransportistaCodigoDestino",
            //    table: "Envios",
            //    newName: "TransportistaDestinoCodigo");

            //migrationBuilder.RenameColumn(
            //    name: "TransportistaCodigoDestino",
            //    table: "EnvioDTO",
            //    newName: "TransportistaDestinoCodigo");

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "EnviosAudit",
                type: "nvarchar(max)",
                nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TransportistaDestino",
            //    table: "EnvioDTO",
            //    type: "nvarchar(250)",
            //    maxLength: 250,
            //    nullable: true);

            migrationBuilder.CreateTable(
                name: "Transportistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transportistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transportistas_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transportistas_EstadoId",
                table: "Transportistas",
                column: "EstadoId");



            migrationBuilder.Sql(@"CREATE OR ALTER VIEW [dbo].[vwTransportistas] 
AS
	SELECT IDPROVLOGI, tl.CODIGO, tl.CODOPERA, tl.NOMBRE, ACTIVO, t.Direccion, t.EstadoId
	FROM Presea_MAS_Migracion.dbo.PROVLOGI tl
		LEFT JOIN Transportistas t on t.Codigo =tl.CODIGO 
"); 

            migrationBuilder.Sql(@"set identity_insert Menues on
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  16  , 16  ,'Transportistas' ,'Transportistas' , NULL  ,'fas fa-truck-pickup' , 40  ,  0, 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  17  , 16  ,'Listado Transportistas' ,'Transportistas' ,'index' ,'fas fa-list-ul' , 40  , 0  , 0  ) 
set identity_insert Menues off"
);

            migrationBuilder.Sql(@"set identity_insert ParametricosHeader on
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  6  ,'Transportista Estado' , 0  ) 
set identity_insert ParametricosHeader off");

            migrationBuilder.Sql(@"
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Transportista Estado' ,'Deshabilitado' , NULL  , 70  , 0  ,'#a91a10' , 6  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Transportista Estado' ,'Activo' , NULL  , 60  , 0  , NULL  , 6  )");

        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transportistas");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "EnviosAudit");

            migrationBuilder.DropColumn(
                name: "TransportistaDestino",
                table: "EnvioDTO");

            migrationBuilder.RenameColumn(
                name: "TransportistaDestinoCodigo",
                table: "Envios",
                newName: "TransportistaCodigoDestino");

            migrationBuilder.RenameColumn(
                name: "TransportistaDestinoCodigo",
                table: "EnvioDTO",
                newName: "TransportistaCodigoDestino");

            migrationBuilder.AddColumn<int>(
                name: "TransportistaId",
                table: "EnvioDTO",
                type: "int",
                nullable: true);
        }
    }
}
