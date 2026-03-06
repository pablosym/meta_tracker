using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class migrationEnvioAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "EnviosAudit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Envio = table.Column<long>(type: "bigint", nullable: false),
                    Guia = table.Column<long>(type: "bigint", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosAudit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnviosAudit_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnviosAudit_EstadoId",
                table: "EnviosAudit",
                column: "EstadoId");

            migrationBuilder.Sql(@"set identity_insert Menues on
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  14  , 8  ,'Masivo' ,'Envios' , 'Masivos' ,'fas fa-barcode' , 20  , 0  , 1  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  15  , 8  ,'Auditoria' ,'EnviosAudit' , 'index' ,'fas fa-user-secret' , 30  , 0  , 0  ) 
set identity_insert Menues off");


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnviosAudit");

            migrationBuilder.DropColumn(
                name: "FechaUltimoMov",
                table: "EnvioDTO");
        }
    }
}
