# Revisión técnica preventiva – Servicio .NET / EF Core

## Alcance analizado
- `Services/EnvioService.cs`
- `Services/BackgroundTaskQueue.cs`
- `Services/BackgroundQueueService.cs`
- `Controllers/EnviosController.cs` (puntos de entrada)

---

## Hallazgos

### 1) Llamada async sin `await` (fire-and-forget) en notificación inicial
**Ubicación:** `SincronizarAsync`.

```csharp
_notificationHubContext.Clients.Group("Notificacion").SendAsync("ReceiveNotificacion", notificacion);
```

#### Riesgo
- Si `SendAsync` falla, la excepción puede quedar no observada o perderse fuera del flujo principal.
- Puede provocar comportamientos no deterministas en background y errores de tipo `System.Threading.Tasks` (tareas faulted no observadas).

#### Recomendación
- `await` explícito antes de encolar, o capturar con `ContinueWith` centralizado de logging.
- Mejor opción: hacer `SincronizarAsync` completamente `async` y devolver `await` real.

#### Ejemplo sugerido
```csharp
await _notificationHubContext.Clients
    .Group("Notificacion")
    .SendAsync("ReceiveNotificacion", notificacion);
```

---

### 2) Uso de parámetro nullable sin guard clause fuerte (`usuario`)
**Ubicación:** múltiples accesos `usuario.Nombre` y `usuario.Id` en ejecución en cola/background.

#### Riesgo
- Si llega un `usuario` incompleto o nulo en propiedades, aparecen `System.NullReferenceException` o datos inconsistentes.
- En background es más difícil correlacionar el origen de la falla.

#### Recomendación
- Validar al inicio de `SincronizarAsync` y `EnviarALogictrackerAsync`:
  - `usuario != null`
  - `!string.IsNullOrWhiteSpace(usuario.Nombre)`

#### Ejemplo sugerido
```csharp
if (usuario is null)
    return MessageDTO.Error("Usuario requerido.");
if (string.IsNullOrWhiteSpace(usuario.Nombre))
    return MessageDTO.Error("Nombre de usuario requerido.");
```

---

### 3) `SingleOrDefault()` sobre colección de teléfonos (puede lanzar excepción)
**Ubicación:** consolidación de teléfono por guía.

```csharp
var telefono = articulos
    .Select(a => a.Telefono)
    .Where(t => !string.IsNullOrWhiteSpace(t) && t != "ERROR")
    .Distinct()
    .SingleOrDefault();
```

#### Riesgo
- Si hay más de un teléfono distinto, `SingleOrDefault()` lanza `System.InvalidOperationException`.
- Es un riesgo real en escenarios de datos sucios o múltiples afiliados por guía.

#### Recomendación
- Cambiar a `Take(2).ToList()` y resolver explícitamente cardinalidad.

#### Ejemplo sugerido
```csharp
var phones = articulos
    .Select(a => a.Telefono)
    .Where(t => !string.IsNullOrWhiteSpace(t) && t != "ERROR")
    .Distinct()
    .Take(2)
    .ToList();

string? telefono = phones.Count switch
{
    0 => null,
    1 => phones[0],
    _ => null // o política de negocio: log + warning
};
```

---

### 4) Posible `NullReferenceException` por supresión forzada (`envio!`)
**Ubicación:** `EnviarALogictrackerAsync`.

```csharp
envio!.Estado = null;
```

#### Riesgo
- Aunque arriba existe un guard clause, futuras refactorizaciones pueden romper ese contrato.
- El operador `!` silencia el compilador pero no evita `NullReferenceException` en runtime.

#### Recomendación
- Evitar `!` y trabajar con variable local no-null tras validación.

#### Ejemplo sugerido
```csharp
if (envio is null)
    return MessageDTO.Error("El envío es obligatorio.");

var envioSafe = envio;
envioSafe.Estado = null;
```

---

### 5) Riesgo de duplicación/concurrencia de sincronización del mismo envío
**Ubicación:** flujo `PrepararEnvioASincronizarAsync` + `SincronizarAsync` (background queue).

#### Riesgo
- Dos requests casi simultáneos pueden encolar el mismo envío y procesarlo dos veces.
- Posibles efectos:
  - estados sobrescritos,
  - auditoría duplicada,
  - invocaciones SOAP duplicadas.

#### Recomendación
- Agregar lock lógico por `envio.Id`/`envio.Numero` (en memoria distribuida o DB con marca “en proceso”).
- Implementar idempotencia por `CodigoViaje` + guía.

#### Ejemplo sugerido
```csharp
// pseudo-código
if (!await _syncLock.TryAcquireAsync(envio.Numero))
    return MessageDTO.Warning("El envío ya está en sincronización.");
try
{
    // sincronizar
}
finally
{
    await _syncLock.ReleaseAsync(envio.Numero);
}
```

---

### 6) N+1 queries en sincronización masiva
**Ubicación:** bucle por `listEnvios` en `SincronizarAsync`.

#### Riesgo
- Por cada envío se hacen varias consultas separadas (transportista origen/destino, vehículo+tipo, chofer).
- Incrementa latencia, carga DB y probabilidad de timeout (`System.TimeoutException` / `SqlException`).

#### Recomendación
- Pre-cargar en batch por IDs/códigos, mapear con diccionarios y reutilizar.
- Mantener una sola unidad de trabajo por bloque.

---

### 7) Conversión explícita de `decimal` a `int` en sumatoria
**Ubicación:** construcción de `InsumoCompletoWs`.

```csharp
Cantidad = gInsumo.Sum(x => (int)x.CantidadSolicitada)
```

#### Riesgo
- Pérdida de precisión (truncamiento).
- Si el valor excede `int.MaxValue`, puede lanzar `System.OverflowException`.

#### Recomendación
- Mantener tipo `decimal` hasta el contrato final o usar validación/rango antes de convertir.

#### Ejemplo sugerido
```csharp
var cantidadTotal = gInsumo.Sum(x => x.CantidadSolicitada);
if (cantidadTotal > int.MaxValue)
    return MessageDTO.Error("Cantidad excede rango permitido.");
Cantidad = decimal.ToInt32(decimal.Truncate(cantidadTotal));
```

---

### 8) Excepciones absorbidas sin logging estructurado en background worker
**Ubicación:** `BackgroundQueueService` y `SincronizarAsync` catch general.

#### Riesgo
- Se captura `Exception` y se continúa sin trazabilidad completa.
- Fallas de tipo `System.*` quedan invisibles (diagnóstico difícil en producción).

#### Recomendación
- Inyectar `ILogger` y registrar `LogError(ex, ...)` con contexto de envío/usuario.
- Evitar `catch (Exception)` silencioso salvo que se registre y se trace correlación.

---

## Prioridad sugerida
1. **Alta:** punto 1 (await faltante), 3 (`SingleOrDefault`), 5 (duplicación concurrente).
2. **Media:** punto 6 (N+1), 8 (logging de excepciones), 7 (casts).
3. **Media/Baja:** punto 4 (`!`) por robustez futura.

---

## Resumen ejecutivo
El código tiene una base correcta para uso de `DbContext` en background (scope dedicado), pero presenta riesgos reales en manejo async, cardinalidad de LINQ y concurrencia de sincronización que sí pueden derivar en `System.InvalidOperationException`, `System.NullReferenceException`, `System.Threading.Tasks` (tareas faulted/no observadas) y excepciones por rendimiento/carga (timeouts). Recomiendo priorizar correcciones de await, control de concurrencia e idempotencia.
