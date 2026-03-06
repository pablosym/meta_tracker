using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    public partial class Inicial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.CreateTable(
                name: "Menues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuPadreId = table.Column<int>(type: "int", nullable: false),
                    Item = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AspController = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AspAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Baja = table.Column<bool>(type: "bit", nullable: false),
                    AccesoDirecto = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametricosHeader",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Baja = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametricosHeader", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baja = table.Column<bool>(type: "bit", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlgAdmin = table.Column<bool>(type: "bit", nullable: false),
                    FechaUltimoIngreso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametricos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Baja = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParametricosHeaderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parametricos_ParametricosHeader_ParametricosHeaderId",
                        column: x => x.ParametricosHeaderId,
                        principalTable: "ParametricosHeader",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Choferes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazonSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legajo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoId = table.Column<int>(type: "int", nullable: false),
                    Baja = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choferes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Choferes_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuesRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuesRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuesRoles_Menues_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuesRoles_Parametricos_RolId",
                        column: x => x.RolId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Parametricos_RolId",
                        column: x => x.RolId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Patente = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoId = table.Column<int>(type: "int", nullable: true),
                    EstadoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Parametricos_TipoId",
                        column: x => x.TipoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Envios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    TransportistaCodigo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaTurno = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUltimoMov = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioUltimoMovId = table.Column<int>(type: "int", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChoferId = table.Column<int>(type: "int", nullable: true),
                    VehiculoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Envios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Envios_Choferes_ChoferId",
                        column: x => x.ChoferId,
                        principalTable: "Choferes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Envios_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Envios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Envios_Usuarios_UsuarioUltimoMovId",
                        column: x => x.UsuarioUltimoMovId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Envios_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnviosGuias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    EnvioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstadoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnviosGuias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnviosGuias_Envios_EnvioId",
                        column: x => x.EnvioId,
                        principalTable: "Envios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnviosGuias_Parametricos_EstadoId",
                        column: x => x.EstadoId,
                        principalTable: "Parametricos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Choferes_EstadoId",
                table: "Choferes",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_ChoferId",
                table: "Envios",
                column: "ChoferId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_EstadoId",
                table: "Envios",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_UsuarioId",
                table: "Envios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_UsuarioUltimoMovId",
                table: "Envios",
                column: "UsuarioUltimoMovId");

            migrationBuilder.CreateIndex(
                name: "IX_Envios_VehiculoId",
                table: "Envios",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_EnviosGuias_EnvioId",
                table: "EnviosGuias",
                column: "EnvioId");

            migrationBuilder.CreateIndex(
                name: "IX_EnviosGuias_EstadoId",
                table: "EnviosGuias",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuesRoles_MenuId",
                table: "MenuesRoles",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuesRoles_RolId",
                table: "MenuesRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Parametricos_ParametricosHeaderId",
                table: "Parametricos",
                column: "ParametricosHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRoles_RolId",
                table: "UsuariosRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRoles_UsuarioId",
                table: "UsuariosRoles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_EstadoId",
                table: "Vehiculos",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_TipoId",
                table: "Vehiculos",
                column: "TipoId");



            
            migrationBuilder.Sql(@"INSERT INTO dbo.usuarios (Baja ,Nombre ,Clave ,Correo ,FlgAdmin ,FechaUltimoIngreso ) VALUES (  0  ,'admin' ,'2d+lxZnVl0ElUoRWu+shT/78F6yzPq7xmRt7Ds7SV4M=' ,'admin@meta.com.ar' , 1  , null ) ");


            migrationBuilder.Sql(@"set identity_insert Menues on
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  1  , 1  ,'Seguridad' ,'Auth' , NULL  ,'fas fa-shield-alt' , 1  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  2  , 1  ,'Usuarios' ,'usuarios' ,'index' ,'fas fa-users-cog' , 10  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  3  , 1  ,'Menu x Rol' ,'menuesRol' ,'index' ,'fas fa-user-shield' , 20  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  4  , 4  ,'Ajustes' ,'Ajustes' , NULL  ,'fas fa-cogs' , 900  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  5  , 4  ,'Parametricos Ad' ,'parametrico' ,'index' ,'fas fa-cog' , 10  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  6  , 6  ,'Choferes' ,'Choferes' , NULL  ,'fas fa-user-friends' , 10  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  7  , 6  ,'Listado Choferes' ,'Choferes' ,'Index' ,'fas fa-list-ul' , 15  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  8  , 8  ,'Envios' ,'Envios' , NULL  ,'fas fa-archive' , 15  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  9  , 8  ,'Listado Envios' ,'Envios' ,'Index' ,'fas fa-boxes' , 20  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  10  , 10  ,'Vehiculos' ,'Vehiculos' , NULL  ,'fas fa-car-alt' , 25  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  11  , 10  ,'Listado Vehiculos' ,'Vehiculos' ,'Index' ,'fas fa-list-ul' , 30  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  12  , 12  ,'Informacion' ,'Home' , NULL ,'far fa-lightbulb' , 1000  , 0  , 0  ) 
INSERT INTO dbo.menues (Id ,MenuPadreId ,Item ,AspController ,AspAction ,Icono ,Orden ,Baja ,AccesoDirecto ) VALUES (  13  , 12  ,'Manual de Usuario' ,'Home' ,'ManualDeUsuario' ,'far fa-file-pdf' , 1001  , 0  , 1  ) 
set identity_insert Menues off");

            migrationBuilder.Sql(@"set identity_insert ParametricosHeader on
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  1  ,'Roles' , 0  ) 
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  2  ,'Envios' , 0  ) 
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  3  ,'Chofer Estados' , 0  ) 
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  4  ,'Tipo Vehiculo' , 0  ) 
INSERT INTO dbo.ParametricosHeader (Id ,Descripcion ,Baja ) VALUES (  5  ,'Vehiculo Estado' , 0  ) 
set identity_insert ParametricosHeader off");


            migrationBuilder.Sql(@"INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Chofer Estado' ,'Activo' , NULL  , 10  , 0  , NULL  , 3  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Chofer Estado' ,'Deshabilitado' , NULL  , 20  , 0  ,'#a91a10' , 3  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Vehiculo Estado' ,'Activo' , NULL  , 50  , 0  , NULL  , 4  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Vehiculo Estado' ,'Deshabilitado' , NULL  , 60  , 0  , '#a91a10'  , 4  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( '01' ,'Camion' , NULL  , 30  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( '02' ,'Tractor' , NULL  , 35  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( '03' ,'Chasis' , NULL  , 40  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'G' ,'Generico' , NULL  , 45  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'moto' ,'Moto' , NULL  , 50  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'UT' ,'Utilitario' , NULL  , 55  , 0  , NULL  , 5  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Envio Pendiente' ,'Envio Pendiente' , NULL  , 10  , 0  , NULL  , 2  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Envio Correcto' ,'Envio Correcto' , NULL  , 20  , 0  , NULL  , 2  ) 
INSERT INTO dbo.Parametricos (Codigo ,Descripcion ,Valor ,Orden ,Baja ,Color ,ParametricosHeaderId ) VALUES ( 'Envio Con Error' ,'Envio Con Error' , NULL  , 30  , 0  , '#e88f89'  , 2  ) ");


            migrationBuilder.Sql(@"CREATE or ALTER  VIEW [dbo].[vwEnvios]
AS
	SELECT IDENVIO, NUMENVIO, EMPRESA, OBSERVACIO, FECINGRE
	FROM [Presea_Mas_Migracion].[dbo].[ENVIO] ");

            migrationBuilder.Sql(@"CREATE  or ALTER VIEW [dbo].[vwGuias]
AS
	SELECT IDGUIA, NUMENVIO as NumeroEnvio, NUMGUIA as NumeroGuia, FECHAHORA as FechaHora, FECHENVIO as FechaEnvio, guia.PROVLOGI, 
		guia.OPERADOR as Operador, guia.OPERENVIO as OperadorEnvio, 
		cli.CODIGO as  ClienteCodigo,  case  isnull(cli.N_FANTASIA, cli.NOMBRE) when '' then cli.NOMBRE else cli.N_FANTASIA end as Cliente,  cli.DOMICILIO as  ClienteDomicilio, cli.TELEFONO as ClienteTelefono, cli.E_MAIL as ClienteEmail, 

		 (select isnull(count (*),0) from [Presea_Mas_Migracion].[dbo].[CTACTE] cta where cta.numguia = guia.NUMGUIA) as CantidadComprobantes
	FROM [Presea_Mas_Migracion].[dbo].[GUIA] guia
		inner join [Presea_Mas_Migracion].[dbo].CLIENTES cli on cli.CODIGO = guia.CLIENTE ");

            migrationBuilder.Sql(@"CREATE or alter  VIEW [dbo].[vwTransportistas]
AS
	SELECT IDPROVLOGI, CODIGO, CODOPERA, NOMBRE, ACTIVO
	FROM Presea_MAS_Migracion.dbo.PROVLOGI");


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
		co.RazonSocial as Chofer, co.Id as ChoferId,
		v.Id as VehiculoId, v.Patente, v.Descripcion as Vehiculo,  tipoVehiculo.id as VehiculoTipoId,  tipoVehiculo.Descripcion as VehiculoTipo,
		deEstado.Descripcion as Estado, deEstado.Color as EstadoColor, deEstado.id as EstadoId,
		de.Id as EnvioId, de.Observaciones, de.FechaInicio, de.FechaTurno, 
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
	
	
	group by e.NUMENVIO, t.codigo, t.nombre, co.Id, co.RazonSocial, 
		     v.id, v.patente, v.Descripcion, tipoVehiculo.id, tipoVehiculo.Descripcion,
			 deEstado.Descripcion, deEstado.Color, deEstado.id, 
			 de.Id, de.Observaciones, de.FechaInicio, de.FechaTurno
	ORDER BY  e.NUMENVIO, t.nombre
	OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");


            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GetArticulosPorGuia]
@numeroGuia BIGINT =0,
@pageSize int = 10,
@skip int = 0
AS
BEGIN

	
	SELECT cabeceraComprobantes.NUMERO AS NumeroComprobante, 
		cabeceraComprobantes.AFILIADO AS CabeceraComprobantesAfiliado, 
		detalleComprobante.codigo AS ArticuloCodigo, articulo.detalle AS ArticuloDescripcion, detalleComprobante.CANTIDAD AS CantidadSolicitada, 
		RecordsTotal = COUNT(*) OVER(), --para el paginador
		ROW_NUMBER() OVER(ORDER BY cabeceraComprobantes.numguia ASC) AS Id -- esto es porque lo necesita el Entity Framework
	FROM [Presea_Mas_Migracion].[dbo].[CTACTE] as cabeceraComprobantes 
		JOIN [Presea_Mas_Migracion].[dbo].[GUIA] AS guia ON 
			cabeceraComprobantes.numguia = guia.NUMGUIA
		JOIN [Presea_Mas_Migracion].[dbo].[MOVIMIEN] AS detalleComprobante ON
			cabeceraComprobantes.EMPRESA = detalleComprobante.EMPRESA AND  
			cabeceraComprobantes.FORMULARIO = detalleComprobante.FORMULARIO AND  
			cabeceraComprobantes.NUMERO = detalleComprobante.NNUMERO
		JOIN [Presea_MAS_Migracion].[dbo].[STOCK] AS articulo ON
			detalleComprobante.CODIGO = articulo.CODIGO
		JOIN [Presea_Mas_Migracion].[dbo].[PROVLOGI] transportista ON
			guia.PROVLOGI = transportista.CODIGO

	WHERE
		cabeceraComprobantes.formulario LIKE 'Remito%' AND
		cabeceraComprobantes.NUMGUIA = @numeroGuia 

		order by cabeceraComprobantes.NUMERO
	END");


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
	where g.NumeroEnvio = @numeroEnvio
			and ((@numeroGuia = 0) or (g.numeroGuia = @numeroGuia))

	group by g.NumeroGuia, g.FECHAHORA, 
			cliente.Codigo, cliente.Nombre, cliente.N_Fantasia, cliente.DIRECCION, cliente.TELEFONO, 
			datosAdicionalCliente.DOMI_LAT, datosAdicionalCliente.DOMI_LON, guiaEstado.Id, guiaEstado.Descripcion, guiaEstado.color, eg.envioId
	ORDER BY  g.FECHAHORA, g.NumeroGuia 
		OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticuloDTO");

            migrationBuilder.DropTable(
                name: "EnvioDTO");

            migrationBuilder.DropTable(
                name: "EnviosGuias");

            migrationBuilder.DropTable(
                name: "GuiaDTO");

            migrationBuilder.DropTable(
                name: "MenuesRoles");

            migrationBuilder.DropTable(
                name: "UsuariosRoles");

            migrationBuilder.DropTable(
                name: "Envios");

            migrationBuilder.DropTable(
                name: "Menues");

            migrationBuilder.DropTable(
                name: "Choferes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Parametricos");

            migrationBuilder.DropTable(
                name: "ParametricosHeader");
        }
    }
}
