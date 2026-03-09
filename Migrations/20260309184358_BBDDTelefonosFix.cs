using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDTelefonosFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GetTelefonosGuias]
@NumGuiasCSV varchar(MAX)
AS
BEGIN
    SET NOCOUNT ON;


--1 Si hay teléfono de domicilio válido → DOMICILI
--2 Si mismo NUMGUIA + CLIENTE + AFILIADO tiene más de un celular → AFILIADO_MULTIPLES_TEL
--3 Si tiene 1 celular → AFILIADO
--4 Si no hay nada → SIN_TELEFONO


    CREATE TABLE #Guias (NumGuia bigint PRIMARY KEY);

    INSERT INTO #Guias (NumGuia)
    SELECT CAST(Item AS bigint)
    FROM dbo.SplitString(@NumGuiasCSV, ',');

    ;WITH Base AS
    (
        SELECT DISTINCT
            cta.NUMGUIA,
            cta.CLIENTE,
            cta.AFILIADO,
            cta.LISTAPRE,
            dom.TEL_CEL AS TelefonoDomicili,
            tuni.CEL    AS TelefonoAfiliado,
            tuni.CantCelAfiliado
        FROM #Guias g

        INNER JOIN Presea_Mas_Migracion.dbo.CTACTE cta 
            ON cta.NUMGUIA = g.NumGuia 

        INNER JOIN Parametricos p 
            ON p.Codigo = cta.LISTAPRE 
           AND p.ParametricosHeaderId = 7
           AND p.Baja = 0

        LEFT JOIN Presea_Mas_Migracion.dbo.DOMICILI dom 
            ON CAST(dom.CODIGO  AS VARCHAR(50)) = CAST(cta.DOMI_ENTRE AS VARCHAR(50))
           AND CAST(dom.CLIENTE AS VARCHAR(50)) = CAST(cta.CLIENTE   AS VARCHAR(50))

        OUTER APPLY (
            SELECT
                MAX(d.CEL) AS CEL,
                COUNT(*)   AS CantCelAfiliado
            FROM (
                SELECT DISTINCT a.CEL
                FROM Presea_Mas_Migracion.dbo.AFILIADO a
                WHERE a.CODIGO  = cta.AFILIADO
                  AND a.CLIENTE = cta.CLIENTE
                  AND a.CEL IS NOT NULL
                  AND LTRIM(RTRIM(a.CEL)) <> ''
            ) d
        ) tuni

        WHERE cta.NUMGUIA  > 0 
          AND cta.AFILIADO > 0
    )

    SELECT
        b.NUMGUIA,
        b.CLIENTE,
        b.AFILIADO,
        b.LISTAPRE,
        b.TelefonoDomicili,
        b.TelefonoAfiliado,
        CASE
            WHEN b.TelefonoDomicili IS NOT NULL 
                 AND LTRIM(RTRIM(b.TelefonoDomicili)) <> ''
            THEN 'DOMICILI'

            WHEN b.CantCelAfiliado > 1
            THEN 'AFILIADO_MULTIPLES_TEL'

            WHEN b.CantCelAfiliado = 1
            THEN 'AFILIADO'

            ELSE 'SIN_TELEFONO'
        END AS TelefonoEstado
    FROM Base b;

    DROP TABLE #Guias;
END");
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GetArticulosPorGuia] 
    @numeroGuia BIGINT = 0,
    @pageSize INT = 10,
    @skip INT = 0
AS
BEGIN

    ;WITH Datos AS
    (
        SELECT DISTINCT
            cabeceraComprobantes.NUMERO AS NumeroComprobante, 
            cabeceraComprobantes.AFILIADO AS CabeceraComprobantesAfiliado, 
            detalleComprobante.codigo AS ArticuloCodigo, 
            articulo.detalle AS ArticuloDescripcion, 
            detalleComprobante.CANTIDAD AS CantidadSolicitada, 
            cabeceraComprobantes.NRORECETA AS NroReceta,
            cabeceraComprobantes.LISTAPRE AS ListaPrecio, 
		    guia.NUMGUIA AS NumeroGuia,   --Lo necesita el modelo, sino da error 
		    cabeceraComprobantes.CLIENTE AS ClienteCodigo, --Lo necesita el modelo, sino da error 
			'' AS Telefono,		  --Lo necesita el modelo, sino da error 	
			'' AS TelefonoOrigen  --Lo necesita el modelo, sino da error 
        FROM [Presea_Mas_Migracion].[dbo].[CTACTE] cabeceraComprobantes

        JOIN [Presea_Mas_Migracion].[dbo].[GUIA] guia 
            ON cabeceraComprobantes.numguia = guia.NUMGUIA

        JOIN [Presea_Mas_Migracion].[dbo].[MOVIMIEN] detalleComprobante WITH(NOLOCK)
            ON cabeceraComprobantes.EMPRESA = detalleComprobante.EMPRESA
            AND cabeceraComprobantes.FORMULARIO = detalleComprobante.FORMULARIO
            AND cabeceraComprobantes.NUMERO = detalleComprobante.NNUMERO

        JOIN [Presea_MAS_Migracion].[dbo].[STOCK] articulo 
            ON detalleComprobante.CODIGO = articulo.CODIGO

        JOIN [Presea_Mas_Migracion].[dbo].[PROVLOGI] transportista 
            ON guia.PROVLOGI = transportista.CODIGO

        WHERE
            cabeceraComprobantes.formulario LIKE 'Remito%'
            AND cabeceraComprobantes.NUMGUIA = @numeroGuia
    )

    SELECT
        *,
        COUNT(*) OVER() AS RecordsTotal,
        ROW_NUMBER() OVER(ORDER BY NumeroComprobante) AS Id
    FROM Datos
    ORDER BY NumeroComprobante
    OFFSET @skip ROWS
    FETCH NEXT @pageSize ROWS ONLY;

END");

            //migrationBuilder.AddColumn<Guid>(
            //    name: "CodigoViaje",
            //    table: "EnviosAudit",
            //    type: "uniqueidentifier",
            //    nullable: true);

            //migrationBuilder.AddColumn<Guid>(
            //    name: "CodigoViaje",
            //    table: "EnvioDTO",
            //    type: "uniqueidentifier",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "ClienteCodigo",
            //    table: "ArticuloDTO",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<string>(
            //    name: "ListaPrecio",
            //    table: "ArticuloDTO",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<string>(
            //    name: "NroReceta",
            //    table: "ArticuloDTO",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<long>(
            //    name: "NumeroGuia",
            //    table: "ArticuloDTO",
            //    type: "bigint",
            //    nullable: false,
            //    defaultValue: 0L);

            //migrationBuilder.AddColumn<string>(
            //    name: "Telefono",
            //    table: "ArticuloDTO",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "TelefonoOrigen",
            //    table: "ArticuloDTO",
            //    type: "nvarchar(max)",
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoViaje",
                table: "EnviosAudit");

            migrationBuilder.DropColumn(
                name: "CodigoViaje",
                table: "EnvioDTO");

            migrationBuilder.DropColumn(
                name: "ClienteCodigo",
                table: "ArticuloDTO");

            migrationBuilder.DropColumn(
                name: "ListaPrecio",
                table: "ArticuloDTO");

            migrationBuilder.DropColumn(
                name: "NroReceta",
                table: "ArticuloDTO");

            migrationBuilder.DropColumn(
                name: "NumeroGuia",
                table: "ArticuloDTO");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "ArticuloDTO");

            migrationBuilder.DropColumn(
                name: "TelefonoOrigen",
                table: "ArticuloDTO");
        }
    }
}
