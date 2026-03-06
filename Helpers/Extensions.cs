using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Tracker.Helpers;

public static class EnumHelper
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}

public static class StringExtensions
{

    public static string ToTitleCase(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var cultureInfo = CultureInfo.CurrentCulture; // Siempre tiene valor
        return cultureInfo.TextInfo.ToTitleCase(input.ToLower(cultureInfo));
    }



    public static bool EsCorreoElectronicoValido(this string correoElectronico)
    {

        if (correoElectronico == null)
            return false;

        // Expresión regular para validar el correo electrónico
        string regexEmail = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        // Validamos el correo electrónico con la expresión regular
        return Regex.IsMatch(correoElectronico, regexEmail);
    }

}

public static class IntExtensions
{

    public static bool IsPar(this int input)
    {
        if (input % 2 == 0)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

}



public static class DecimalExtensions
{

    public static decimal Redondeo(this decimal importe)
    {

        var first2DecimalPlaces = (int)(importe % 1 * 100);


        if (first2DecimalPlaces > 50)
            return Math.Ceiling(importe);
        else
            return Math.Floor(importe);
    }

}

//public static class SessionExtensions
//{
//	public static void Set<T>(this ISession session, string key, T value)
//	{
//		session.SetString(key, JsonConvert.SerializeObject(value));
//	}

//	public static T Get<T>(this ISession session, string key)
//	{
//		var value = session.GetString(key);
//		return value == null ? default :
//							  JsonConvert.DeserializeObject<T>(value);
//	}
//}


public static class Conversores
{
    public static string NumeroALetras(this decimal numberAsString)
    {
        string dec;

        var entero = Convert.ToInt64(Math.Truncate(numberAsString));
        var decimales = Convert.ToInt32(Math.Round((numberAsString - entero) * 100, 2));
        if (decimales > 0)
        {
            //dec = " PESOS CON " + decimales.ToString() + "/100";
            dec = $" PESOS {decimales:0,0} /100";
        }
        //Código agregado por mí
        else
        {
            //dec = " PESOS CON " + decimales.ToString() + "/100";
            dec = $" PESOS {decimales:0,0} /100";
        }
        var res = NumeroALetras(Convert.ToDouble(entero)) + dec;
        return res;
    }
    private static string NumeroALetras(double value)
    {
        string num2Text; value = Math.Truncate(value);
        if (value == 0) num2Text = "CERO";
        else if (value == 1) num2Text = "UNO";
        else if (value == 2) num2Text = "DOS";
        else if (value == 3) num2Text = "TRES";
        else if (value == 4) num2Text = "CUATRO";
        else if (value == 5) num2Text = "CINCO";
        else if (value == 6) num2Text = "SEIS";
        else if (value == 7) num2Text = "SIETE";
        else if (value == 8) num2Text = "OCHO";
        else if (value == 9) num2Text = "NUEVE";
        else if (value == 10) num2Text = "DIEZ";
        else if (value == 11) num2Text = "ONCE";
        else if (value == 12) num2Text = "DOCE";
        else if (value == 13) num2Text = "TRECE";
        else if (value == 14) num2Text = "CATORCE";
        else if (value == 15) num2Text = "QUINCE";
        else if (value < 20) num2Text = "DIECI" + NumeroALetras(value - 10);
        else if (value == 20) num2Text = "VEINTE";
        else if (value < 30) num2Text = "VEINTI" + NumeroALetras(value - 20);
        else if (value == 30) num2Text = "TREINTA";
        else if (value == 40) num2Text = "CUARENTA";
        else if (value == 50) num2Text = "CINCUENTA";
        else if (value == 60) num2Text = "SESENTA";
        else if (value == 70) num2Text = "SETENTA";
        else if (value == 80) num2Text = "OCHENTA";
        else if (value == 90) num2Text = "NOVENTA";
        else if (value < 100) num2Text = NumeroALetras(Math.Truncate(value / 10) * 10) + " Y " + NumeroALetras(value % 10);
        else if (value == 100) num2Text = "CIEN";
        else if (value < 200) num2Text = "CIENTO " + NumeroALetras(value - 100);
        else if ((value == 200) || (value == 300) || (value == 400) || (value == 600) || (value == 800)) num2Text = NumeroALetras(Math.Truncate(value / 100)) + "CIENTOS";
        else if (value == 500) num2Text = "QUINIENTOS";
        else if (value == 700) num2Text = "SETECIENTOS";
        else if (value == 900) num2Text = "NOVECIENTOS";
        else if (value < 1000) num2Text = NumeroALetras(Math.Truncate(value / 100) * 100) + " " + NumeroALetras(value % 100);
        else if (value == 1000) num2Text = "MIL";
        else if (value < 2000) num2Text = "MIL " + NumeroALetras(value % 1000);
        else if (value < 1000000)
        {
            num2Text = NumeroALetras(Math.Truncate(value / 1000)) + " MIL";
            if ((value % 1000) > 0)
            {
                num2Text = num2Text + " " + NumeroALetras(value % 1000);
            }
        }
        else if (value == 1000000)
        {
            num2Text = "UN MILLON";
        }
        else if (value < 2000000)
        {
            num2Text = "UN MILLON " + NumeroALetras(value % 1000000);
        }
        else if (value < 1000000000000)
        {
            num2Text = NumeroALetras(Math.Truncate(value / 1000000)) + " MILLONES ";
            if ((value - Math.Truncate(value / 1000000) * 1000000) > 0)
            {
                num2Text = num2Text + " " + NumeroALetras(value - Math.Truncate(value / 1000000) * 1000000);
            }
        }
        else if (value == 1000000000000) num2Text = "UN BILLON";
        else if (value < 2000000000000) num2Text = "UN BILLON " + NumeroALetras(value - Math.Truncate(value / 1000000000000) * 1000000000000);
        else
        {
            num2Text = NumeroALetras(Math.Truncate(value / 1000000000000)) + " BILLONES";
            if ((value - Math.Truncate(value / 1000000000000) * 1000000000000) > 0)
            {
                num2Text = num2Text + " " + NumeroALetras(value - Math.Truncate(value / 1000000000000) * 1000000000000);
            }
        }
        return num2Text;
    }
}
