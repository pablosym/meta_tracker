using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tracker.Helpers;

public class Error
{
    public static void WriteLog(
        Exception ex,
        string? contexto = null,
        [CallerMemberName] string origenMetodo = "",
        [CallerFilePath] string origenArchivo = "",
        [CallerLineNumber] int origenLinea = 0)
    {
        try
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ERROR_PATH);
            string logFileName = $"Log-{DateTime.Today:yyyy-MM-dd}.txt";
            string fullPath = Path.Combine(logFilePath, logFileName);

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var log = new StreamWriter(fullPath, append: true, Encoding.UTF8);
            log.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");

            if (!string.IsNullOrWhiteSpace(contexto))
            {
                log.WriteLine($"Contexto: {contexto}");
            }

            log.WriteLine($"Origen invocación: {origenMetodo} ({Path.GetFileName(origenArchivo)}:{origenLinea})");
            log.WriteLine($"Método excepción: {GetMetodoExcepcion(ex)}");
            log.WriteLine($"Tipo: {ex.GetType().FullName}");
            log.WriteLine($"Mensaje: {ex.Message}");

            EscribirInnerExceptions(log, ex);

            log.WriteLine("StackTrace:");
            log.WriteLine(ex.StackTrace ?? "Sin stack trace");
            log.WriteLine("----- FIN -----");
            log.WriteLine();
        }
        catch
        {
            // Nada, ni avisamos. Logging de backup no debería romper.
        }
    }


    public static void WriteLog(string mensaje)
    {
        try
        {
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ERROR_PATH);
            string logFileName = $"Log-{DateTime.Today:yyyy-MM-dd}.txt";
            string fullPath = Path.Combine(logFilePath, logFileName);

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var log = new StreamWriter(fullPath, append: true);
            log.WriteLine(mensaje);
            log.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            log.WriteLine("----- FIN -----");
        }
        catch
        {

        }
    }

    private static void EscribirInnerExceptions(StreamWriter log, Exception ex)
    {
        var depth = 0;
        var actual = ex.InnerException;

        while (actual != null)
        {
            depth++;
            log.WriteLine($"InnerException[{depth}]: {actual.GetType().FullName} - {actual.Message}");
            log.WriteLine($"InnerException[{depth}] método: {GetMetodoExcepcion(actual)}");
            actual = actual.InnerException;
        }
    }

    private static string GetMetodoExcepcion(Exception ex)
    {
        var targetSite = ex.TargetSite;
        if (targetSite != null)
        {
            var clase = targetSite.DeclaringType?.FullName ?? "TipoDesconocido";
            return $"{clase}.{targetSite.Name}";
        }

        if (string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            return "No disponible";
        }

        try
        {
            var trace = new StackTrace(ex, true);
            var frames = trace.GetFrames();
            StackFrame? frame = null;

            if (frames != null)
            {
                foreach (var currentFrame in frames)
                {
                    if (currentFrame.GetMethod() != null)
                    {
                        frame = currentFrame;
                        break;
                    }
                }
            }

            if (frame?.GetMethod() != null)
            {
                var metodo = frame.GetMethod()!;
                var clase = metodo.DeclaringType?.FullName ?? "TipoDesconocido";
                var archivo = Path.GetFileName(frame.GetFileName() ?? "SinArchivo");
                var linea = frame.GetFileLineNumber();
                return $"{clase}.{metodo.Name} ({archivo}:{linea})";
            }
        }
        catch
        {
            // fallback seguro
        }

        return "No disponible";
    }
}
