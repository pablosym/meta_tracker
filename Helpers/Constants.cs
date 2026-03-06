using System.ComponentModel;

namespace Tracker.Helpers;

public class Constants
{
    public const string TEMP_OBJ_PARA_CONFIRMACION = "TEMP_OBJ_CONFIRM";

    public const string ERROR_PATH = "Logs";

    public enum eParametricosHeader
    {
        [Description("Roles")]
        Roles = 1,

        [Description("Envio Estado")]
        EnvioEstado = 2,
        
        [Description("Chofer Estado")]
        ChoferEstado = 3,

        [Description("Vehiculo Estado")]
        VehiculoEstado = 4,

        [Description("Tipo Vehiculo")]
        TipoVehiculo = 5,

        [Description("Transportista Estado")]
        TransportistaEstado = 6,

        [Description("Convenios a Notificar")]
        ConveniosNotificar = 7
    }

    public enum eEnviosEstados
    {
        [Description("Pendiente")]
        Pendiente = 11,

        [Description("Correcto")]
        Correcto = 12,

        [Description("Con Error")]
        ConError = 13,
    }


    public enum eVehiculosEstados
    {
        [Description("Activo")]
        Activo= 3,

        [Description("Deshabilitado")]
        Deshabilitado = 4
    }


    public enum eChoferEstados
    {
        [Description("Activo")]
        Activo = 1,

        [Description("Deshabilitado")]
        Deshabilitado = 2
    }

    public enum eTransportistaEstados
    {
        [Description("Activo")]
        Activo = 1015,

        [Description("Deshabilitado")]
        Deshabilitado = 1014
    }

    public enum eTelefonoTablaOrigen
    {
        [Description("Domicili")]
        DOMICILI,

        [Description("Afiliado")]
        AFILIADO,

        [Description("Afiliado Multiple")]
        AFILIADO_MULTIPLES_TEL,

        [Description("Misma guia+cliente, varios afiliados")]
        GUIA_MULTIPLES_AFILIADOS,

        [Description("Sin Teléfono")]
        SIN_TELEFONO
    }
}