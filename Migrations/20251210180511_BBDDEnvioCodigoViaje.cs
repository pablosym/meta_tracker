using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDEnvioCodigoViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CodigoViaje",
                table: "Envios",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CodigoViaje",
                table: "EnviosAudit",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[EnviosGet]
@desde      date    = NULL,
@hasta      date    = NULL,
@numero     bigint  = 0,
@guiaNumero bigint  = 0,
@estado     int     = NULL,
@pageSize   int     = 10,
@skip       int     = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @desdeDt datetime2(0) = CASE WHEN @desde IS NULL THEN CONVERT(datetime2(0),'1900-01-01')
                                         ELSE DATEFROMPARTS(YEAR(@desde), MONTH(@desde), DAY(@desde)) END;
    DECLARE @hastaDt datetime2(0) = CASE WHEN @hasta IS NULL THEN CONVERT(datetime2(0),'9999-12-31')
                                         ELSE DATEADD(DAY, 1, DATEFROMPARTS(YEAR(@hasta), MONTH(@hasta), DAY(@hasta))) END;

    ;WITH
    
    Base AS (
        SELECT 
            e.NUMENVIO                 AS Numero,
            de.Id                      AS EnvioId,
            de.Observaciones,
            de.FechaInicio,
            de.FechaTurno,
            de.FechaUltimoMov,
            de.TransportistaDestinoCodigo,
            v.Id                       AS VehiculoId,
            v.Patente,
            v.Descripcion              AS Vehiculo,
            tv.Id                      AS VehiculoTipoId,
            tv.Descripcion             AS VehiculoTipo,
            est.Id                     AS EstadoId,
            est.Descripcion            AS Estado,
            est.Color                  AS EstadoColor,
            ch.Id                      AS ChoferId,
            ch.ApellidoNombre          AS Chofer,
			de.CodigoViaje
        FROM dbo.vwEnvios e
			LEFT JOIN dbo.Envios           de  ON de.Numero   = e.NUMENVIO
			LEFT JOIN dbo.Vehiculos        v   ON v.Id        = de.VehiculoId
			LEFT JOIN dbo.Parametricos     tv  ON tv.Id       = v.TipoId
			LEFT JOIN dbo.Parametricos     est ON est.Id      = de.EstadoId
			LEFT JOIN dbo.Choferes         ch  ON ch.Id       = de.ChoferId
        WHERE
            e.FECINGRE >= @desdeDt AND e.FECINGRE < @hastaDt
            AND (@numero = 0 OR e.NUMENVIO = @numero)
            AND (@estado IS NULL OR @estado = 0 OR de.EstadoId = @estado)
            AND (
                @guiaNumero = 0 OR EXISTS (
                    SELECT 1
                    FROM Presea_Mas_Migracion.dbo.GUIA gx
                    WHERE gx.NUMENVIO = e.NUMENVIO
                      AND gx.NUMGUIA  = @guiaNumero
                )
            )
    ),
    -- Agregado directo desde GUIA: cantidad y PROVLOGI por envío
    GuiasPorEnvio AS (
        SELECT
            g.NUMENVIO,
            CantidadGuias = COUNT(DISTINCT g.NUMGUIA),
            PROVLOGI      = MIN(g.PROVLOGI)  -- criterio simple/determinista; cambiable
        FROM Presea_Mas_Migracion.dbo.GUIA g
        GROUP BY g.NUMENVIO
    )
    SELECT
        b.Numero,
        t.Codigo                                   AS TransportistaCodigo,
        t.Nombre                                   AS Transportista,
        b.Chofer,
        b.ChoferId,
        b.VehiculoId,
        b.Patente,
        b.Vehiculo,
        b.VehiculoTipoId,
        b.VehiculoTipo,
        b.Estado,
        b.EstadoColor,
        b.EstadoId,
        b.EnvioId,
        b.Observaciones,
        b.FechaInicio,
        b.FechaTurno,
        b.FechaUltimoMov,
        ISNULL(guiasEnvio.CantidadGuias, 0)        AS CantidadGuias,
        b.TransportistaDestinoCodigo,
        tDestino.Nombre                            AS TransportistaDestino,
		b.CodigoViaje,
        COUNT(*) OVER()                            AS RecordsTotal,
        ROW_NUMBER() OVER (ORDER BY b.Numero ASC)  AS Id
    FROM Base b
		LEFT JOIN GuiasPorEnvio guiasEnvio                     ON guiasEnvio.NUMENVIO = b.Numero
		LEFT JOIN dbo.vwTransportistas t         ON t.Codigo    = guiasEnvio.PROVLOGI          
		LEFT JOIN dbo.vwTransportistas tDestino  ON tDestino.Codigo = b.TransportistaDestinoCodigo
    ORDER BY b.Numero DESC, t.Nombre
    OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY;
END
");
                
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoViaje",
                table: "Envios");
        }
    }
}
