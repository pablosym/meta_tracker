using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDTelefonoGuias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelefonosGuiasLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumGuia = table.Column<long>(type: "bigint", nullable: false),
                    Cliente = table.Column<int>(type: "int", nullable: false),
                    Afiliado = table.Column<long>(type: "bigint", nullable: false),
                    Listapre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelefonoEstado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelefonoDomicili = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TelefonoAfiliado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioRegistra = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelefonosGuiasLog", x => x.Id);
                });

            migrationBuilder.Sql(@"CREATE OR ALTER VIEW [dbo].[vwTelefonosGuias] AS
SELECT 
    cta.NUMGUIA,                            -- Número de guía (clave principal de la entrega)
    cta.CLIENTE,                            -- Cliente asociado
    cta.AFILIADO,                           -- Afiliado relacionado a la guía
    cta.LISTAPRE,                           -- Lista de prestaciones

    dom.TEL_CEL AS TelefonoDomicili,        -- Teléfono celular desde la tabla DOMICILI
    telAfiliado.CEL AS TelefonoAfiliado,    -- Teléfono del afiliado (si hay uno único)

    -- Determina el estado del teléfono disponible con prioridad:
    -- 1) DOMICILI  → si hay teléfono en domicilio
    -- 2) AFILIADO  → si hay un solo teléfono en AFILIADO
    -- 3) AFILIADO_MULTIPLES → si hay más de un teléfono (no se puede elegir)
    -- 4) SIN_TELEFONO → si no hay ningún teléfono válido
    CASE 
        WHEN dom.TEL_CEL IS NOT NULL THEN 'DOMICILI'
        WHEN telAfiliado.CEL IS NOT NULL THEN 'AFILIADO'
        WHEN ex.MultipleTelefonos = 1 THEN 'AFILIADO_MULTIPLES'
        ELSE 'SIN_TELEFONO'
    END AS TelefonoEstado

FROM Presea_Mas_Migracion.dbo.CTACTE cta

-- Se asegura que la lista de prestaciones esté habilitada para notificación (ParametricosHeaderId = 7)
INNER JOIN Parametricos p ON p.Codigo = cta.LISTAPRE AND p.ParametricosHeaderId = 7
    and p.baja = 0  

-- Intenta obtener el teléfono desde la tabla DOMICILI si está completo
LEFT JOIN Presea_Mas_Migracion.dbo.DOMICILI dom 
    ON dom.CODIGO = cta.DOMI_ENTRE 
    AND dom.CLIENTE = cta.CLIENTE
    AND dom.TEL_CEL IS NOT NULL AND dom.TEL_CEL <> ''

-- Usa OUTER APPLY para traer el celular del afiliado solo si hay uno único
OUTER APPLY (
    SELECT TOP 1 a.CEL
    FROM Presea_Mas_Migracion.dbo.AFILIADO a
    WHERE  a.CODIGO = cta.AFILIADO
           AND a.CLIENTE = cta.CLIENTE
           AND a.CEL IS NOT NULL AND a.CEL <> ''
    GROUP BY a.CEL
    HAVING COUNT(*) = 1
) telAfiliado

-- Detecta si hay más de un teléfono en AFILIADO → no puede decidirse automáticamente
OUTER APPLY (
    SELECT 1 AS MultipleTelefonos
    FROM Presea_Mas_Migracion.dbo.AFILIADO a
    WHERE a.CLIENTE = cta.CLIENTE 
          AND a.CODIGO = cta.AFILIADO
          AND a.CEL IS NOT NULL AND a.CEL <> ''
    GROUP BY a.CEL
    HAVING COUNT(*) > 1
) ex

-- Solo considera guías válidas con afiliados definidos
WHERE cta.NUMGUIA > 0 AND cta.AFILIADO > 0
GO");


            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GuiasGet] 
    @numeroEnvio BIGINT = 0, 
    @numeroGuia BIGINT = 0, 
    @pageSize INT = 10,
    @skip INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        g.NumeroGuia AS Numero, 
        g.FECHAHORA AS Fecha, 	
        cliente.Codigo AS ClienteCodigo,  
        CASE ISNULL(cliente.N_Fantasia,'') 
            WHEN '' THEN cliente.Nombre 
            ELSE cliente.N_Fantasia 
        END AS ClienteNombre, 
        cliente.DIRECCION AS ClienteDireccion, 
		null AS ClienteTelefono,
        --cliente.TELEFONO AS ClienteTelefono,
        datosAdicionalCliente.DOMI_LAT AS DestinoLatitud, 
        datosAdicionalCliente.DOMI_LON AS DestinoLongitud, 
        guiaEstado.Descripcion AS Estado, 
        guiaEstado.Color AS EstadoColor, 
        guiaEstado.Id AS EstadoId, 
        eg.EnvioId,
        COUNT(ctacte.NUMGUIA) AS CantidadComprobantes,
		RecordsTotal = COUNT(*) OVER(), --para el paginador
        ROW_NUMBER() OVER(ORDER BY g.NumeroGuia ASC) AS Id
    FROM 
        dbo.vwGuias g
        LEFT JOIN Presea_Mas_Migracion.[dbo].[CLIENTES] AS cliente ON g.ClienteCodigo = cliente.CODIGO 
        LEFT JOIN Presea_Mas_Migracion.[dbo].[CLIENADI] AS datosAdicionalCliente ON cliente.CODIGO = datosAdicionalCliente.CODIGO
        LEFT JOIN EnviosGuias eg ON eg.Numero = g.NumeroGuia
        LEFT JOIN Parametricos guiaEstado ON guiaEstado.Id = eg.EstadoId
        LEFT JOIN [Presea_Mas_Migracion].[dbo].[CTACTE] AS ctacte ON ctacte.NUMGUIA = g.NumeroGuia
    WHERE 
        (@numeroEnvio = 0 OR g.NumeroEnvio = @numeroEnvio)
        AND (@numeroGuia = 0 OR g.NumeroGuia = @numeroGuia)
    GROUP BY 
        g.NumeroGuia, g.FECHAHORA, 
        cliente.Codigo, cliente.Nombre, cliente.N_Fantasia, cliente.DIRECCION, cliente.TELEFONO, 
        datosAdicionalCliente.DOMI_LAT, datosAdicionalCliente.DOMI_LON, guiaEstado.Id, guiaEstado.Descripcion, guiaEstado.color, eg.envioId
    ORDER BY  
        g.FECHAHORA, g.NumeroGuia 
    OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY;
END
GO
");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelefonosGuiasLog");
        }
    }
}
