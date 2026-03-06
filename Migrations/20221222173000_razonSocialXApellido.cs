using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class razonSocialXApellido : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RazonSocial",
                table: "Choferes",
                newName: "ApellidoNombre");

            migrationBuilder.RenameColumn(
                name: "Descripcion",
                table: "Choferes",
                newName: "Observacion");


            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[EnviosGet] 
@desde date = null,
@hasta date = null, 
@numero bigint = 0, 
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
		inner join dbo.vwGuias g on g.NumeroEnvio = e.NUMENVIO
		inner join dbo.vwTransportistas t on t.codigo = g.PROVLOGI
		left join Envios de on de.Numero = e.NUMENVIO
		left join Vehiculos v on v.Id = de.VehiculoId
		left join Parametricos tipoVehiculo on tipoVehiculo.Id  = v.TipoId
		LEFT join Parametricos deEstado on deEstado.id = de.EstadoId
		LEFT join Choferes co on co.Id = de.ChoferId
	where   (( @desde is null) OR (g.fechaHora between @desde and @hasta))
		and (( @numero =0 ) or ( e.NUMENVIO = @numero))
		and (( @estado is null or @estado = 0 ) or ( de.EstadoId = @estado))
	
	
	group by e.NUMENVIO, t.codigo, t.nombre, co.Id, co.ApellidoNombre, 
		     v.id, v.patente, v.Descripcion, tipoVehiculo.id, tipoVehiculo.Descripcion,
			 deEstado.Descripcion, deEstado.Color, deEstado.id, 
			 de.Id, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov
	ORDER BY  e.NUMENVIO, t.nombre
	OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");

            migrationBuilder.Sql(@"set identity_insert Menues on
                    INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  14  , 8  ,'Masivo' ,'Envios' ,'Masivos' ,'fas fa-barcode' , 20  , 0  , 1  )
                    set identity_insert Menues off");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnvioId",
                table: "GuiaDTO");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "GuiaDTO");

            migrationBuilder.DropColumn(
                name: "EstadoColor",
                table: "GuiaDTO");

            migrationBuilder.DropColumn(
                name: "EstadoId",
                table: "GuiaDTO");

            migrationBuilder.RenameColumn(
                name: "Observacion",
                table: "Choferes",
                newName: "Descripcion");

            migrationBuilder.RenameColumn(
                name: "ApellidoNombre",
                table: "Choferes",
                newName: "RazonSocial");
        }
    }
}
