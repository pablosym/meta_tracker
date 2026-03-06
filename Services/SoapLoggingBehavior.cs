namespace Tracker.Services;

using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.Extensions.Logging;

public sealed class SoapLoggingBehavior : IEndpointBehavior
{
    private readonly ILogger _logger;
    private readonly bool _toFile;
    private readonly string? _path;

    public SoapLoggingBehavior(ILogger logger, bool toFile = false, string? path = null)
    {
        _logger = logger;
        _toFile = toFile;
        _path = path;
    }

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        => clientRuntime.ClientMessageInspectors.Add(new SoapLoggingInspector(_logger, _toFile, _path));
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
    public void Validate(ServiceEndpoint endpoint) { }

    private sealed class SoapLoggingInspector : IClientMessageInspector
    {
        private readonly ILogger _logger;
        private readonly bool _toFile;
        private readonly string? _path;

        // contador para diferenciar múltiples requests en el mismo milisegundo
        private static long _seq = 0;

        public SoapLoggingInspector(ILogger logger, bool toFile, string? path)
        {
            _logger = logger;
            _toFile = toFile;
            _path = path;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            try
            {
                var xml = MessageToXml(ref request);

                // baseId compartido para REQUEST y RESPONSE
                var now = DateTime.Now;
                var seq = Interlocked.Increment(ref _seq);
                var baseId = $"{now:yyyyMMdd_HHmmss_fff}_{seq:D4}";

                var guia = SoapLogContext.GuiaNumero;                 
                Write("SOAP_REQUEST", xml, baseId, guia);

                // devolvemos info para usarla en AfterReceiveReply
                return new Correlation(baseId, guia);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo loguear el SOAP Request");
                return null!;
            }
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            try
            {
                var xml = MessageToXml(ref reply);
                var corr = correlationState as Correlation;
                Write("SOAP_RESPONSE", xml, corr?.BaseId, corr?.Guia);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo loguear el SOAP Response");
            }
        }

        private static string MessageToXml(ref Message message)
        {
            var buffer = message.CreateBufferedCopy(int.MaxValue);
            var copyForLog = buffer.CreateMessage();
            message = buffer.CreateMessage(); // reponer el original

            var sb = new StringBuilder();
            using var sw = new StringWriter(sb);
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };
            using var xw = XmlWriter.Create(sw, settings);
            copyForLog.WriteMessage(xw);
            xw.Flush();
            return sb.ToString();
        }

        private void Write(string title, string xml, string? baseId = null, string? guia = null)
        {
            if (_toFile && !string.IsNullOrWhiteSpace(_path))
            {
                Directory.CreateDirectory(_path!);

                // si no vino baseId, generamos uno
                var id = baseId ?? $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Interlocked.Increment(ref _seq):D4}";

                var guiaPart = string.IsNullOrWhiteSpace(guia) ? "" : $"_G{Sanitize(guia)}";
                var file = Path.Combine(_path!, $"{id}{guiaPart}_{Sanitize(title)}.xml");

                File.WriteAllText(file, xml, Encoding.UTF8);
                _logger.LogInformation("{Title} guardado en {File}", title, file);
            }
            else
            {
                _logger.LogInformation("{Title}:\n{Xml}", title, xml);
            }
        }

        private static string Sanitize(string s) => s.Replace(" ", "_");

        private sealed record Correlation(string BaseId, string? Guia);
    }
}

// Contexto ambient para pasar el número de guía al logger
public static class SoapLogContext
{
    private static readonly System.Threading.AsyncLocal<string?> _guia = new();
    public static string? GuiaNumero
    {
        get => _guia.Value;
        set => _guia.Value = value;
    }

    // helper opcional para usar con using(...)
    public static IDisposable UseGuia(string? numero)
    {
        var prev = _guia.Value;
        _guia.Value = numero;
        return new Restore(() => _guia.Value = prev);
    }
    private sealed class Restore : IDisposable
    {
        private readonly Action _restore;
        public Restore(Action restore) => _restore = restore;
        public void Dispose() => _restore();
    }
}
