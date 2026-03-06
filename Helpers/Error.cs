namespace Tracker.Helpers;

public class Error
{
    public static void WriteLog(Exception ex)
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
            log.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            log.WriteLine($"Tipo: {ex.GetType().FullName}");
            log.WriteLine($"Mensaje: {ex.Message}");
            if (ex.InnerException != null)
            {
                log.WriteLine($"InnerException: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}");
            }
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
}


