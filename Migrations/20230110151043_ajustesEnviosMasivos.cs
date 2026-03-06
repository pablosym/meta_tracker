using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class ajustesEnviosMasivos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {


			migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GuiasGet] 
@numeroEnvio bigint = 0, 
@numeroGuia bigint = 0, 
@pageSize int = 10,
@skip int = 0
as
		
	select g.NumeroGuia as Numero, g.FECHAHORA as Fecha, 	
		cliente.Codigo AS ClienteCodigo,  case  isnull(cliente.N_Fantasia,'') when '' then cliente.Nombre else cliente.N_Fantasia end AS ClienteNombre, 
		cliente.DIRECCION as ClienteDireccion, cliente.TELEFONO as ClienteTelefono,
		datosAdicionalCliente.DOMI_LAT as DestinoLatitud, 
		datosAdicionalCliente.DOMI_LON AS DestinoLongitud, 
		guiaEstado.Descripcion as Estado, guiaEstado.Color as EstadoColor, guiaEstado.id as EstadoId, eg.envioId,
		(select count(*) FROM [Presea_Mas_Migracion].[dbo].[CTACTE] as cabeceraComprobantes where cabeceraComprobantes.NUMGUIA = g.NumeroGuia) as CantidadComprobantes,
		RecordsTotal = COUNT(*) OVER(), --para el paginador
		ROW_NUMBER() OVER(ORDER BY g.NumeroGuia ASC) AS Id -- esto es porque lo necesita el Entity Framework
	from dbo.vwGuias g
		-- inner join dbo.vwTransportistas t on t.codigo = g.PROVLOGI
		LEFT JOIN Presea_Mas_Migracion.[dbo].[CLIENTES] AS cliente ON 
			g.ClienteCodigo = cliente.CODIGO 
		LEFT JOIN Presea_Mas_Migracion.[dbo].[CLIENADI] AS datosAdicionalCliente ON 
			cliente.CODIGO = datosAdicionalCliente.CODIGO
		left join EnviosGuias eg on eg.Numero = g.NumeroGuia
		left join Parametricos guiaEstado on guiaEstado.Id = eg.EstadoId
	where (@numeroEnvio = 0) or (	g.NumeroEnvio = @numeroEnvio)
			and ((@numeroGuia = 0) or (g.numeroGuia = @numeroGuia))

	group by g.NumeroGuia, g.FECHAHORA, 
			cliente.Codigo, cliente.Nombre, cliente.N_Fantasia, cliente.DIRECCION, cliente.TELEFONO, 
			datosAdicionalCliente.DOMI_LAT, datosAdicionalCliente.DOMI_LON, guiaEstado.Id, guiaEstado.Descripcion, guiaEstado.color, eg.envioId
	ORDER BY  g.FECHAHORA, g.NumeroGuia 
		OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");

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
		inner join dbo.vwGuias g on g.NumeroEnvio = e.NUMENVIO
		inner join dbo.vwTransportistas t on t.codigo = g.PROVLOGI
		left join Envios de on de.Numero = e.NUMENVIO
		left join Vehiculos v on v.Id = de.VehiculoId
		left join Parametricos tipoVehiculo on tipoVehiculo.Id  = v.TipoId
		LEFT join Parametricos deEstado on deEstado.id = de.EstadoId
		LEFT join Choferes co on co.Id = de.ChoferId
	where   (( @desde is null) OR ( CAST(g.fechaHora As Date) between @desde and @hasta))
		and (( @numero =0 ) or ( e.NUMENVIO = @numero))
		and (( @guiaNumero =0 ) or ( g.NumeroGuia = @guiaNumero))
		and (( @estado is null or @estado = 0 ) or ( de.EstadoId = @estado))
	
	
	group by e.NUMENVIO, t.codigo, t.nombre, co.Id, co.ApellidoNombre, 
		     v.id, v.patente, v.Descripcion, tipoVehiculo.id, tipoVehiculo.Descripcion,
			 deEstado.Descripcion, deEstado.Color, deEstado.id, 
			 de.Id, de.Observaciones, de.FechaInicio, de.FechaTurno, de.FechaUltimoMov
	ORDER BY  e.NUMENVIO, t.nombre
	OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");

            migrationBuilder.Sql(@"insert into Usuarios (Baja, Nombre, Clave, Correo, FlgAdmin) values (0, 'Jordana Schmid', 'clave', 'jschmid@meta.com.ar', 0) 
                insert into Usuarios (Baja, Nombre, Clave, Correo, FlgAdmin) values (0, 'Mariano Neira', 'clave', 'mneira@meta.com.ar', 0)
                insert into Usuarios (Baja, Nombre, Clave, Correo, FlgAdmin) values (0, 'Diego Suarez', 'clave', 'dsuarez@meta.com.ar', 0)
                insert into Usuarios (Baja, Nombre, Clave, Correo, FlgAdmin) values (0, 'Diego Brizuela', 'clave', 'dbrizuela@meta.com.ar', 1)");

            migrationBuilder.Sql(@"
update ParametricosHeader set Descripcion ='Vehiculo Estado' where id = 4
update ParametricosHeader set Descripcion ='Tipo Vehiculo' where id = 5
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('MBS', 'MERCEDES BENZ SPRINTER', 10, 0, 5) 
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('FF', 'FIAT FIORINO', 20, 0, 5) 
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('RK', 'RENAULT KANGOO', 30, 0, 5) 
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('CB', 'CITROEN BERLINGO', 40, 0, 5) 
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('XR 250', 'MOTO HONDA XR 250', 50, 0, 5) 
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('7RD 125', 'MOTO 7RD 125 MONDIAL', 60, 0, 5)
insert into Parametricos (codigo, descripcion, orden, baja, parametricosHeaderId)  values ('YBR 125 Z', 'MOTO YAMAHA YBR 125 Z', 70, 0, 5)");


            migrationBuilder.Sql(@"insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MERCEDES BENZ SPRINTER','MOI 881',17)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MERCEDES BENZ SPRINTER','AA 382 VY',17)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MERCEDES BENZ SPRINTER','AE 216 TD',17)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MERCEDES BENZ SPRINTER','AF 338 LU',17)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','OBI 359',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AA 890 PA',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AA 890 PB',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AE 188 YJ',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AC 861 GC',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AD 229 KQ',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','AB 388 KH',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','KHV 392',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','IQW 206',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'FIAT FIORINO','PDY 257',18)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'RENAULT KANGOO','AE 229 DH',19)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'RENAULT KANGOO','AD 064 QV',19)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'CITROEN BERLINGO','AE 976 SE',20)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MOTO HONDA XR 250','111LIJ',21)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MOTO 7RD 125 MONDIAL','782 CDW',22)
insert into Vehiculos (EstadoId, Descripcion, Patente, TipoId ) values (3, 'MOTO YAMAHA YBR 125 Z','A 141 AMN',23)");

            migrationBuilder.Sql(@"insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('BAILAO ANGIE HERMINIA','39490375','NL LOGÍSTICA S.R.L.','39490375',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('BARRIONUEVO KEVIN EMMANUEL','37184570','NL LOGÍSTICA S.R.L.','37184570',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('BERLANGA OMAR DANIEL ','14140733','NL LOGÍSTICA S.R.L.','14140733',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CALDERON MAURO ARIEL ','33028307','NL LOGÍSTICA S.R.L.','33028307',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CARDOZO HUGO RICARDO','20581567','NL LOGÍSTICA S.R.L.','20581567',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CARUGATTI DIEGO ARIEL ','42832273','NL LOGÍSTICA S.R.L.','42832273',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CINKO JONATAN LUIS DANIEL','33606957','NL LOGÍSTICA S.R.L.','33606957',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CORTEZ CRISTIAN DAVID','30684663','NL LOGÍSTICA S.R.L.','30684663',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('CORVALAN MAURO ARIEL ','33028307','NL LOGÍSTICA S.R.L.','33028307',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('D´AMORE CLAUDIO ALEJANDRO','25058353','NL LOGÍSTICA S.R.L.','25058353',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('GAITAN ERNESTO CAMILO','33465198','NL LOGÍSTICA S.R.L.','33465198',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('LEZCANO CARLOS ABEL','17874089','NL LOGÍSTICA S.R.L.','17874089',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('MASTROBERTI MARCELO ALEJANDRO','17602324','NL LOGÍSTICA S.R.L.','17602324',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('MAZZEO JORGE OSVALDO','16544644','NL LOGÍSTICA S.R.L.','16544644',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('NUÑEZ BENJAMÍN','20728549','NL LOGÍSTICA S.R.L.','20728549',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('PONCE CESAR LUIS','13943070','NL LOGÍSTICA S.R.L.','13943070',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('RECABARREN OSCAR ALEJANDRO','18590631','NL LOGÍSTICA S.R.L.','18590631',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('RESCIGNO ARIEL VICENTE','16810773','NL LOGÍSTICA S.R.L.','16810773',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('RIOS ROBERTO FERNANDO','27277689','NL LOGÍSTICA S.R.L.','27277689',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('RODRIGUEZ AYRTON AGUSTÍN','39507845','NL LOGÍSTICA S.R.L.','39507845',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('ROLDAN JOAQUÍN','35754674','NL LOGÍSTICA S.R.L.','35754674',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('SCHOEN DAVID MISAEL ','38682860','NL LOGÍSTICA S.R.L.','38682860',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('TORRES LUCAS FABIAN','39166252','NL LOGÍSTICA S.R.L.','39166252',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('TRIBULO ALEJANDRO CESAR','24272352','NL LOGÍSTICA S.R.L.','24272352',1,0)
insert into choferes (ApellidoNombre, Legajo, Observacion,  Dni, EstadoId, Baja) values ('VALDAZO JUAN EMILIO','35657836','NL LOGÍSTICA S.R.L.','35657836',1,0)
");



        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observacion",
                table: "EnviosAudit");
        }
    }
}
