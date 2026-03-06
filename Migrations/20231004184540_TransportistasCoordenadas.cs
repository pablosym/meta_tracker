using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class TransportistasCoordenadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Coordenadas",
                table: "Transportistas",
                type: "nvarchar(max)",
                nullable: true);


            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[EnviosGet] 
@desde date = null,
@hasta date = null, 
@numero bigint = 0, 
@guiaNumero bigint = 0, 
@estado int = null,
@pageSize int = 10,
@skip int = 0
as
	
	SELECT 
		e.NUMENVIO as Numero, 
		t.Codigo as TransportistaCodigo, t.Nombre as Transportista, 
		co.ApellidoNombre as Chofer, co.Id as ChoferId,
		v.Id as VehiculoId, v.Patente, v.Descripcion as Vehiculo,  tipoVehiculo.id as VehiculoTipoId,  tipoVehiculo.Descripcion as VehiculoTipo,
		deEstado.Descripcion as Estado, deEstado.Color as EstadoColor, deEstado.id as EstadoId,
		de.Id as EnvioId, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov, 
		(select count(*) from dbo.vwGuias where NumeroEnvio = e.NUMENVIO) as CantidadGuias,
		de.TransportistaDestinoCodigo, tDestino.Nombre as TransportistaDestino,
		RecordsTotal = COUNT(*) OVER(), --para el paginador
		ROW_NUMBER() OVER(ORDER BY e.NUMENVIO ASC) AS Id -- esto es porque lo necesita el Entity Framework
	FROM dbo.vwEnvios e
		LEFT JOIN  dbo.vwGuias g on g.NumeroEnvio = e.NUMENVIO
		LEFT JOIN  dbo.vwTransportistas t on t.codigo = g.PROVLOGI
		LEFT JOIN  Envios de on de.Numero = e.NUMENVIO
		LEFT JOIN  dbo.vwTransportistas tDestino on tDestino.CODIGO = de.TransportistaDestinoCodigo
		LEFT JOIN  Vehiculos v on v.Id = de.VehiculoId
		LEFT JOIN  Parametricos tipoVehiculo on tipoVehiculo.Id  = v.TipoId
		LEFT JOIN  Parametricos deEstado on deEstado.id = de.EstadoId
		LEFT JOIN  Choferes co on co.Id = de.ChoferId
	WHERE   
		 (( @desde is null) OR ( CAST(e.FECINGRE As Date) between @desde and @hasta))
		and (( @numero =0 ) or ( e.NUMENVIO = @numero))
		and (( @guiaNumero =0 ) or ( g.NumeroGuia = @guiaNumero))
		and (( @estado is null or @estado = 0 ) or ( de.EstadoId = @estado))
	
	
	GROUP BY e.NUMENVIO, t.codigo, t.nombre, co.Id, co.ApellidoNombre, 
		     v.id, v.patente, v.Descripcion, tipoVehiculo.id, tipoVehiculo.Descripcion,
			 deEstado.Descripcion, deEstado.Color, deEstado.id, 
			 de.Id, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov, 
			 de.TransportistaDestinoCodigo, 
			 tDestino.NOMBRE
	ORDER BY  e.NUMENVIO desc, t.nombre
	OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY
");



            migrationBuilder.Sql(@"CREATE OR ALTER VIEW [dbo].[vwTransportistas]
AS
	SELECT IDPROVLOGI, tl.CODIGO, tl.CODOPERA, tl.NOMBRE, ACTIVO, t.Direccion, t.EstadoId, t.Coordenadas
	FROM Presea_MAS_Migracion.dbo.PROVLOGI tl
		LEFT JOIN Transportistas t on t.Codigo =tl.CODIGO 
");




        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Coordenadas",
                table: "Transportistas");
        }
    }
}
