using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDNroTelefono : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            //migrationBuilder.CreateTable(
            //    name: "TelefonoGuiaResultado",
            //    columns: table => new
            //    {
            //        NumGuia = table.Column<long>(type: "bigint", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Cliente = table.Column<int>(type: "int", nullable: false),
            //        Afiliado = table.Column<long>(type: "bigint", nullable: false),
            //        Listapre = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        TelefonoDomicili = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        TelefonoAfiliado = table.Column<string>(type: "nvarchar(max)", nullable: true),
            //        TelefonoEstado = table.Column<string>(type: "nvarchar(max)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_TelefonoGuiaResultado", x => x.NumGuia);
            //    });


            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GetTelefonosGuias]
    @NumGuiasCSV varchar(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Tabla temporal con los IDs de las guías
    CREATE TABLE #Guias (NumGuia bigint PRIMARY KEY);

    INSERT INTO #Guias (NumGuia)
    SELECT CAST(Item AS bigint)
    FROM dbo.SplitString(@NumGuiasCSV, ',');

    ----------------------------------------------------------------------------
    -- 2. Base: una fila por guía/cliente/afiliado con teléfonos
    ----------------------------------------------------------------------------
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

        --LEFT JOIN Presea_Mas_Migracion.dbo.AFILIADO afiliado 
        --    ON afiliado.CODIGO = cta.AFILIADO

        LEFT JOIN Presea_Mas_Migracion.dbo.DOMICILI dom 
            ON CAST(dom.CODIGO  AS VARCHAR(50)) = CAST(cta.DOMI_ENTRE AS VARCHAR(50))
           AND CAST(dom.CLIENTE AS VARCHAR(50)) = CAST(cta.CLIENTE   AS VARCHAR(50))

        -- Teléfono del afiliado + cantidad de celulares distintos de ese afiliado
        OUTER APPLY (
            SELECT
                MAX(d.CEL) AS CEL,            -- un celular elegido (cualquiera)
                COUNT(*)   AS CantCelAfiliado -- cantidad de celulares distintos
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
    ),

    ----------------------------------------------------------------------------
    -- 3. Conteo de AFILIADOS por guía+cliente
    ----------------------------------------------------------------------------
    AfiliadosPorGuia AS
    (
        SELECT
            NUMGUIA,
            CLIENTE,
            COUNT(DISTINCT AFILIADO) AS CantAfiliadosGuia
        FROM Base
        GROUP BY NUMGUIA, CLIENTE
    )

    ----------------------------------------------------------------------------
    -- 4. Resultado final con TelefonoEstado
    ----------------------------------------------------------------------------
    SELECT
        b.NUMGUIA,
        b.CLIENTE,
        b.AFILIADO,
        b.LISTAPRE,
        b.TelefonoDomicili,
        b.TelefonoAfiliado,
        CASE
            -- 1) Si hay teléfono de domicilio válido, usamos domicilio
            WHEN b.TelefonoDomicili IS NOT NULL 
                 AND LTRIM(RTRIM(b.TelefonoDomicili)) <> '' THEN 'DOMICILI'

            -- 2) Mismo NUMGUIA + CLIENTE con varios AFILIADO → ¿a quién informo?
            WHEN apg.CantAfiliadosGuia > 1 THEN 'GUIA_MULTIPLES_AFILIADOS'

            -- 3) Un afiliado con varios celulares
            WHEN b.CantCelAfiliado > 1 THEN 'AFILIADO_MULTIPLES'

            -- 4) Un afiliado con un solo celular
            WHEN b.CantCelAfiliado = 1 THEN 'AFILIADO'

            -- 5) No hay nada confiable
            ELSE 'SIN_TELEFONO'
        END AS TelefonoEstado
    FROM Base b
    INNER JOIN AfiliadosPorGuia apg
        ON apg.NUMGUIA = b.NUMGUIA
       AND apg.CLIENTE = b.CLIENTE;

    DROP TABLE #Guias;
END
GO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelefonoGuiaResultado");

            migrationBuilder.DropColumn(
                name: "AfiliadoNombre",
                table: "GuiaDTO");

            migrationBuilder.AlterColumn<long>(
                name: "Afiliado",
                table: "GuiaDTO",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
