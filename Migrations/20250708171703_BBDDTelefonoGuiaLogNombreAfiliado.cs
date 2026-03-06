using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDTelefonoGuiaLogNombreAfiliado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelefonoAfiliado",
                table: "TelefonosGuiasLog");

            migrationBuilder.RenameColumn(
                name: "TelefonoDomicili",
                table: "TelefonosGuiasLog",
                newName: "NombreAfiliado");
            

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
    END AS TelefonoEstado, 
	afiliado.NOMBRE as NombreAfiliado

FROM Presea_Mas_Migracion.dbo.CTACTE cta

-- Se asegura que la lista de prestaciones esté habilitada para notificación (ParametricosHeaderId = 7)
INNER JOIN Parametricos p ON p.Codigo = cta.LISTAPRE AND p.ParametricosHeaderId = 7

LEFT JOIN Presea_Mas_Migracion.dbo.AFILIADO afiliado on 
	afiliado.CODIGO = cta.AFILIADO
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
GO
");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NombreAfiliado",
                table: "TelefonosGuiasLog",
                newName: "TelefonoDomicili");

            migrationBuilder.AddColumn<string>(
                name: "TelefonoAfiliado",
                table: "TelefonosGuiasLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TransportistaDestino",
                table: "EnvioDTO",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
