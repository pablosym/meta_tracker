using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDTelefonoGuiaAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "TelefonosGuiasLog",
                type: "nvarchar(max)",
                nullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "TelefonoEstado",
            //    table: "TelefonoGuiaResultado",
            //    type: "nvarchar(max)",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)");

            //migrationBuilder.AddColumn<string>(
            //    name: "AfiliadoNombre",
            //    table: "TelefonoGuiaResultado",
            //    type: "nvarchar(max)",
            //    nullable: true);


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
			afiliado.NOMBRE AS AfiliadoNombre,
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

		   LEFT JOIN Presea_Mas_Migracion.dbo.AFILIADO afiliado 
				on afiliado.CODIGO = cta.AFILIADO

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
		b.AfiliadoNombre,
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
END
");

            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GetArticulosPorGuias]
    @NumGuiasCsv VARCHAR(MAX),
    @pageSize INT = 2147483647,
    @skip INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH GuiasFiltradas AS
    (
        SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS BIGINT) AS NumeroGuia
        FROM STRING_SPLIT(@NumGuiasCsv, ',')
        WHERE TRY_CAST(LTRIM(RTRIM(value)) AS BIGINT) IS NOT NULL
    ),
    Datos AS
    (
        SELECT DISTINCT
            cabeceraComprobantes.NUMERO AS NumeroComprobante,
            cabeceraComprobantes.AFILIADO AS CabeceraComprobantesAfiliado,
            detalleComprobante.CODIGO AS ArticuloCodigo,
            articulo.DETALLE AS ArticuloDescripcion,
            detalleComprobante.CANTIDAD AS CantidadSolicitada,
            cabeceraComprobantes.NRORECETA AS NroReceta,
            cabeceraComprobantes.LISTAPRE AS ListaPrecio,
            guia.NUMGUIA AS NumeroGuia,
            cabeceraComprobantes.CLIENTE AS ClienteCodigo,
            '' AS Telefono,
            '' AS TelefonoOrigen
        FROM [Presea_Mas_Migracion].[dbo].[CTACTE] cabeceraComprobantes
        INNER JOIN [Presea_Mas_Migracion].[dbo].[GUIA] guia
            ON cabeceraComprobantes.NUMGUIA = guia.NUMGUIA
        INNER JOIN GuiasFiltradas gf
            ON gf.NumeroGuia = cabeceraComprobantes.NUMGUIA
        INNER JOIN [Presea_Mas_Migracion].[dbo].[MOVIMIEN] detalleComprobante WITH (NOLOCK)
            ON cabeceraComprobantes.EMPRESA = detalleComprobante.EMPRESA
            AND cabeceraComprobantes.FORMULARIO = detalleComprobante.FORMULARIO
            AND cabeceraComprobantes.NUMERO = detalleComprobante.NNUMERO
        INNER JOIN [Presea_MAS_Migracion].[dbo].[STOCK] articulo
            ON detalleComprobante.CODIGO = articulo.CODIGO
        INNER JOIN [Presea_Mas_Migracion].[dbo].[PROVLOGI] transportista
            ON guia.PROVLOGI = transportista.CODIGO
        WHERE cabeceraComprobantes.FORMULARIO LIKE 'Remito%'
    )
    SELECT
        *,
        COUNT(*) OVER() AS RecordsTotal,
        ROW_NUMBER() OVER(ORDER BY NumeroGuia, NumeroComprobante) AS Id
    FROM Datos
    ORDER BY NumeroGuia, NumeroComprobante
    OFFSET @skip ROWS
    FETCH NEXT @pageSize ROWS ONLY;
END
GO");


            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "TelefonosGuiasLog");

            migrationBuilder.DropColumn(
                name: "AfiliadoNombre",
                table: "TelefonoGuiaResultado");

            migrationBuilder.AlterColumn<string>(
                name: "TelefonoEstado",
                table: "TelefonoGuiaResultado",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
