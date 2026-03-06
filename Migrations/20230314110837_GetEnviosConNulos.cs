using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class GetEnviosConNulos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[EnviosGet] 
@desde date = null,
@hasta date = null, 
@numero bigint = 0, 
@guiaNumero bigint = 0, 
@estado int = null,
@pageSize int = 10,
@skip int = 0
as
	
	select 
		e.NUMENVIO as Numero, 
		t.Codigo as TransportistaCodigo, t.Nombre as Transportista, 
		co.ApellidoNombre as Chofer, co.Id as ChoferId,
		v.Id as VehiculoId, v.Patente, v.Descripcion as Vehiculo,  tipoVehiculo.id as VehiculoTipoId,  tipoVehiculo.Descripcion as VehiculoTipo,
		deEstado.Descripcion as Estado, deEstado.Color as EstadoColor, deEstado.id as EstadoId,
		de.Id as EnvioId, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov, 
		(select count(*) from dbo.vwGuias where NumeroEnvio = e.NUMENVIO) as CantidadGuias,
		RecordsTotal = COUNT(*) OVER(), --para el paginador
		ROW_NUMBER() OVER(ORDER BY e.NUMENVIO ASC) AS Id -- esto es porque lo necesita el Entity Framework
	from dbo.vwEnvios e
		left join dbo.vwGuias g on g.NumeroEnvio = e.NUMENVIO
		left join dbo.vwTransportistas t on t.codigo = g.PROVLOGI
		left join Envios de on de.Numero = e.NUMENVIO
		left join Vehiculos v on v.Id = de.VehiculoId
		left join Parametricos tipoVehiculo on tipoVehiculo.Id  = v.TipoId
		LEFT join Parametricos deEstado on deEstado.id = de.EstadoId
		LEFT join Choferes co on co.Id = de.ChoferId
		where   (( @desde is null) OR ( CAST(e.FECINGRE As Date) between @desde and @hasta))
		and (( @numero =0 ) or ( e.NUMENVIO = @numero))
		and (( @guiaNumero =0 ) or ( g.NumeroGuia = @guiaNumero))
		and (( @estado is null or @estado = 0 ) or ( de.EstadoId = @estado))
	
	
	group by e.NUMENVIO, t.codigo, t.nombre, co.Id, co.ApellidoNombre, 
		     v.id, v.patente, v.Descripcion, tipoVehiculo.id, tipoVehiculo.Descripcion,
			 deEstado.Descripcion, deEstado.Color, deEstado.id, 
			 de.Id, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov
	ORDER BY  e.NUMENVIO, t.nombre
	OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
         
        }
    }
}
