using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Logictracker.Migrations
{
    /// <inheritdoc />
    public partial class BBDDwvGuiasConAfiliado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"CREATE OR ALTER FUNCTION [dbo].[SplitString]
(    
@Input VARCHAR(MAX),
@Delimiter CHAR(1)
)
RETURNS @Output TABLE (
    Item VARCHAR(MAX)
)
AS
BEGIN
    DECLARE @Start INT, @End INT
    
    -- Maneja el caso inicial donde la cadena no tiene delimitador
    SELECT @Start = 1, @End = CHARINDEX(@Delimiter, @Input)

    -- Bucle para encontrar y extraer cada elemento
    WHILE @Start < LEN(@Input) + 1 BEGIN
        IF @End = 0 
            SELECT @End = LEN(@Input) + 1

        INSERT INTO @Output (Item) 
        VALUES (SUBSTRING(@Input, @Start, @End - @Start))

        SELECT @Start = @End + 1
        SELECT @End = CHARINDEX(@Delimiter, @Input, @Start)
    END

    RETURN
END
GO");

            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE [dbo].[GuiasGet]
@numeroEnvio BIGINT = 0, 
@numeroGuia  BIGINT = 0, 
@pageSize    INT    = 10,
@skip        INT    = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) Prefiltro: sólo las guías solicitadas
    SELECT 
        g.NUMGUIA    AS NumeroGuia,
        g.NUMENVIO   AS NumeroEnvio,
        g.FECHAHORA  AS Fecha,        -- DateTime (no null)
        g.FECHENVIO  AS FechaEnvio,
        g.PROVLOGI,
        g.OPERADOR,
        g.OPERENVIO,
        g.CLIENTE    AS ClienteCodigo
    INTO #GuiasClave
    FROM Presea_Mas_Migracion.dbo.GUIA g
    WHERE (@numeroEnvio = 0 OR g.NUMENVIO = @numeroEnvio)
      AND (@numeroGuia  = 0 OR g.NUMGUIA  = @numeroGuia);

    IF NOT EXISTS (SELECT 1 FROM #GuiasClave)
    BEGIN
        SELECT TOP 0
            CAST(NULL AS BIGINT)              AS Afiliado,               -- long?
            CAST(0     AS BIGINT)             AS Numero,                 -- long
            CAST(GETDATE() AS datetime)       AS Fecha,                  -- DateTime
            CAST(0     AS int)                AS ClienteCodigo,          -- int
            CAST(''    AS nvarchar(250))      AS ClienteNombre,          -- string
            CAST(NULL  AS nvarchar(250))      AS ClienteDireccion,       -- string?
            CAST(NULL  AS nvarchar(50))       AS ClienteTelefono,        -- string?
            CAST(NULL  AS decimal(18,8))      AS DestinoLatitud,         -- decimal?
            CAST(NULL  AS decimal(18,8))      AS DestinoLongitud,        -- decimal?
            CAST(0     AS int)                AS CantidadComprobantes,   -- int
            CAST(0     AS int)                AS RecordsTotal,           -- int
            CAST(NULL  AS int)                AS EnvioId,                -- int?
            CAST(1     AS bigint)             AS Id,                     -- long
            CAST(NULL AS nvarchar(250)) AS AfiliadoNombre,
			CAST(NULL AS nvarchar(250)) AS Estado,
            CAST(NULL AS nvarchar(50))  AS EstadoColor,
            CAST(NULL AS INT)           AS EstadoId;
        RETURN;
    END

    -- 2) Agregado en CTACTE SOLO para esas guías
    SELECT 
        c.NUMGUIA,
        ClienteCodigo        = MIN(c.CLIENTE),
        Afiliado             = MIN(NULLIF(c.AFILIADO, 0)),   -- NULL si 0/NULL
        CantidadComprobantes = COUNT(*)                      -- int
    INTO #GuiasAgg
    FROM Presea_Mas_Migracion.dbo.CTACTE c
    JOIN #GuiasClave gc ON gc.NumeroGuia = c.NUMGUIA
    GROUP BY c.NUMGUIA;

    -- 3) SELECT final (sin GROUP BY global), alineado al DTO
    SELECT 
        CAST(a.Afiliado AS BIGINT)                             AS Afiliado,   -- long?
        CAST(gc.NumeroGuia AS BIGINT)                          AS Numero,     -- long
        gc.Fecha                                               AS Fecha,      -- DateTime

        ISNULL(CONVERT(int, COALESCE(gc.ClienteCodigo, a.ClienteCodigo)), 0) AS ClienteCodigo,

        ISNULL(CASE ISNULL(cli.N_FANTASIA,'')
                   WHEN '' THEN cli.NOMBRE ELSE cli.N_FANTASIA END, '')       AS ClienteNombre,

        cli.DIRECCION                                           AS ClienteDireccion,
        null                                            AS ClienteTelefono,

        adi.DOMI_LAT                                            AS DestinoLatitud,
        adi.DOMI_LON                                            AS DestinoLongitud,

        est.Descripcion                                         AS Estado,
        est.Color                                               AS EstadoColor,
        est.Id                                                  AS EstadoId,

        eg.EnvioId,

        ISNULL(a.CantidadComprobantes, 0)                       AS CantidadComprobantes,
        afi.NOMBRE                                              AS AfiliadoNombre,

        CAST(COUNT(*) OVER() AS int)                            AS RecordsTotal,  
        ROW_NUMBER() OVER (ORDER BY gc.NumeroGuia ASC)          AS Id             
    FROM #GuiasClave gc
    LEFT JOIN #GuiasAgg a
           ON a.NUMGUIA = gc.NumeroGuia
    LEFT JOIN Presea_Mas_Migracion.dbo.CLIENTES  cli
           ON cli.CODIGO = COALESCE(gc.ClienteCodigo, a.ClienteCodigo)
    LEFT JOIN Presea_Mas_Migracion.dbo.CLIENADI  adi
           ON adi.CODIGO = cli.CODIGO
    LEFT JOIN dbo.EnviosGuias eg
           ON eg.Numero = gc.NumeroGuia
    LEFT JOIN dbo.Parametricos est
           ON est.Id = eg.EstadoId
    LEFT JOIN Presea_Mas_Migracion.dbo.AFILIADO afi
           ON afi.CODIGO  = a.Afiliado
          AND afi.CLIENTE = COALESCE(gc.ClienteCodigo, a.ClienteCodigo)
    ORDER BY gc.Fecha, gc.NumeroGuia
    OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY
END
");
            
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
            ch.ApellidoNombre          AS Chofer
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

        COUNT(*) OVER()                            AS RecordsTotal,
        ROW_NUMBER() OVER (ORDER BY b.Numero ASC)  AS Id
    FROM Base b
		LEFT JOIN GuiasPorEnvio guiasEnvio                     ON guiasEnvio.NUMENVIO = b.Numero
		LEFT JOIN dbo.vwTransportistas t         ON t.Codigo    = guiasEnvio.PROVLOGI          
		LEFT JOIN dbo.vwTransportistas tDestino  ON tDestino.Codigo = b.TransportistaDestinoCodigo
    ORDER BY b.Numero DESC, t.Nombre
    OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY;
END
GO");

            migrationBuilder.Sql(@"CREATE OR ALTER  PROCEDURE [dbo].[GetTelefonosGuias]
@NumGuiasCSV varchar(MAX)
AS
BEGIN
    
    -- 1. Crear la tabla temporal con los IDs de las guías
    CREATE TABLE #Guias (NumGuia bigint PRIMARY KEY);
    
    INSERT INTO #Guias (NumGuia)
    SELECT CAST(Item AS bigint) 
    FROM dbo.SplitString(@NumGuiasCSV, ',');

    --------------------------------------------------------------------------------
    -- LÓGICA DE RECUPERACIÓN DE DATOS
    --------------------------------------------------------------------------------
    
    SELECT DISTINCT
        cta.NUMGUIA,
        cta.CLIENTE,
        cta.AFILIADO,
        cta.LISTAPRE,
        dom.TEL_CEL AS TelefonoDomicili,
        tuni.CEL AS TelefonoAfiliado, 
        -- afiliado.NOMBRE AS NombreAfiliado,
        
        -- Determina la fuente y validez del teléfono
        CASE 
            -- Prioridad 1: Domicilio, si la fila se unió y el teléfono es válido
            WHEN dom.TEL_CEL IS NOT NULL AND LTRIM(RTRIM(dom.TEL_CEL)) <> '' THEN 'DOMICILI' 
            -- Prioridad 2: Afiliado, si tiene un teléfono único
            WHEN tuni.Cuenta = 1 THEN 'AFILIADO' 
            -- Prioridad 3: Afiliado, si tiene múltiples teléfonos únicos
            WHEN tuni.Cuenta > 1 THEN 'AFILIADO_MULTIPLES'
            -- Prioridad 4: No se encontró ningún teléfono válido
            ELSE 'SIN_TELEFONO'
        END AS TelefonoEstado

    -- La consulta comienza filtrada por la tabla temporal
    FROM #Guias g
    
    -- INNER JOIN: Filtra solo las guías existentes en CTACTE
    INNER JOIN Presea_Mas_Migracion.dbo.CTACTE cta 
        ON cta.NUMGUIA = g.NumGuia 

    -- INNER JOIN: Filtra solo las listas de precios habilitadas
    INNER JOIN Parametricos p 
        ON p.Codigo = cta.LISTAPRE 
       AND p.ParametricosHeaderId = 7
       AND p.Baja = 0
       
    -- ⬅️ LEFT JOIN COMENTADO: Se obvia para no recuperar el NombreAfiliado
    /*
    LEFT JOIN Presea_Mas_Migracion.dbo.AFILIADO afiliado 
        ON afiliado.CODIGO = cta.AFILIADO
    */
    
    -- LEFT JOIN DOMICILI (Usando CAST para garantizar la coincidencia de claves)
    LEFT JOIN Presea_Mas_Migracion.dbo.DOMICILI dom 
        -- Normaliza las claves de domicilio
        ON CAST(dom.CODIGO AS VARCHAR(50)) = CAST(cta.DOMI_ENTRE AS VARCHAR(50))
        -- Normaliza las claves de cliente
       AND CAST(dom.CLIENTE AS VARCHAR(50)) = CAST(cta.CLIENTE AS VARCHAR(50))
       
    -- OUTER APPLY: Calcula el teléfono de Afiliado y el conteo de teléfonos únicos
    OUTER APPLY (
        SELECT TOP 1 a.CEL, 
               COUNT(a.CEL) OVER() AS Cuenta -- Función de ventana para el conteo de filas
        FROM Presea_Mas_Migracion.dbo.AFILIADO a
        WHERE a.CODIGO = cta.AFILIADO
          AND a.CLIENTE = cta.CLIENTE
          AND a.CEL IS NOT NULL AND a.CEL <> ''
        GROUP BY a.CEL -- Agrupa para contar solo los CELs únicos
    ) tuni
    
    WHERE cta.NUMGUIA > 0 AND cta.AFILIADO > 0
    
END
GO");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
