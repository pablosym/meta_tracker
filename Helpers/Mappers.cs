using Tracker.DTOs;
using Tracker.Models;

namespace Tracker.Helpers
{
    public static class Mappers
    {
        public static Parametrico MapTo(ParametricoDTO input)
        {
            Parametrico output = new Parametrico()
            {
                Baja = input.Baja,
                Codigo = input.Codigo,
                Descripcion = input.Descripcion,
                Id = input.Id,
                Orden = input.Orden ?? 0,
                Valor = input.Valor,
                ParametricosHeaderId = input.ParametricosHeaderId,
                Color = input.Color

            };
            return output;
        }

        public static ParametricoDTO MapTo(Parametrico input)
        {
            ParametricoDTO output = new ParametricoDTO()
            {
                Baja = input.Baja,
                Codigo = input.Codigo,
                Descripcion = input.Descripcion,
                Id = input.Id,
                Orden = input.Orden,
                Valor = input.Valor,
                ParametricosHeaderId = input.ParametricosHeaderId,
                Color = input.Color

            };
            return output;
        }

        public static Chofer MapTo(ChoferDTO input)
        {
            Chofer output = new Chofer()
            {
                ApellidoNombre = input.ApellidoNombre,
                Observacion = input.Observacion,
                Dni = input.Dni,
                EstadoId = input.EstadoId,
                Id = input.Id,
                Legajo = input.Legajo,
                Telefono = input.Telefono
            };
            return output;
        }
        public static ChoferDTO MapTo(Chofer input)
        {
            ChoferDTO output = new ChoferDTO()
            {
                ApellidoNombre = input.ApellidoNombre,
                Observacion = input.Observacion,
                Dni = input.Dni,
                EstadoId = input.EstadoId,
                EstadoColor = input.Estado.Color,
                Estado = input.Estado.Descripcion,
                Id = input.Id,
                Legajo = input.Legajo,
                Telefono = input.Telefono
            };
            return output;
        }


        public static vwTransportista MapTo(TransportistaDTO input)
        {
            vwTransportista output = new vwTransportista()
            {
                Direccion= input.Direccion,
                EstadoId= input.EstadoId,
                
            };
            return output;
        }
        
        public static TransportistaDTO MapTo(vwTransportista input)
        {
            TransportistaDTO output = new TransportistaDTO()
            {
                Activo = input.Activo,
                Codigo = input.Codigo,
                Direccion = input.Direccion,
                Idprovlogi = input.Idprovlogi,
                Nombre = input.Nombre,
                Estado = input.Estado?.Valor,
                EstadoColor = input.Estado?.Color,
                EstadoId = input.EstadoId ?? -1,
                Coordenadas = input.Coordenadas
            };
            return output;
        }
        public static vwTransportista MapTo(vwTransportista input, TransportistaDTO transportistaDTO)
        {


            input.Direccion = transportistaDTO.Direccion;
            input.EstadoId = transportistaDTO.EstadoId;

            return input;
        }

        public static Chofer MapTo(Chofer input, ChoferDTO choferDTO)
        {


            input.ApellidoNombre = choferDTO.ApellidoNombre;
            input.Observacion = choferDTO.Observacion;
            input.Dni = choferDTO.Dni;
            input.EstadoId = choferDTO.EstadoId;
            //input.Id = choferDTO.Id;
            input.Legajo = choferDTO.Legajo;
            input.Telefono = choferDTO.Telefono;


            return input;
        }

        public static Vehiculo MapTo(VehiculoDTO input)
        {
            Vehiculo output = new Vehiculo()
            {
                Patente = input.Patente,
                Descripcion = input.Descripcion,
                TipoId = input.TipoId,
                EstadoId = input.EstadoId,
                Id = input.Id,
            };
            return output;
        }

        public static VehiculoDTO MapTo(Vehiculo input)
        {
            VehiculoDTO output = new VehiculoDTO()
            {
                Patente = input.Patente,
                Descripcion = input.Descripcion,
                TipoId = input.TipoId ?? 0,
                EstadoId = input.EstadoId,
                Id = input.Id
            };
            return output;
        }

        public static Vehiculo MapTo(Vehiculo input, VehiculoDTO vehiculoDTO)
        {

            input.Patente = vehiculoDTO.Patente;
            input.Descripcion = vehiculoDTO.Descripcion;
            input.TipoId = vehiculoDTO.TipoId;
            input.EstadoId = vehiculoDTO.EstadoId;

            return input;
        }


        public static EnvioDTO MapTo(Envio input)
        {
            EnvioDTO output = new EnvioDTO()
            {
                FechaInicio = input.FechaInicio,
                FechaTurno = input.FechaTurno,
                Chofer = input.Chofer?.ApellidoNombre ?? "",
                Estado = input.Estado?.Descripcion ?? "",
                EstadoColor = input.Estado?.Color ?? "",
                EstadoId = input.EstadoId,
                Id = input.Id,
                Numero = input.Numero,
                Patente = input.Vehiculo?.Patente,
                // Vehiculo = input.Vehiculo.Observacion,
                // VehiculoTipo = input.Vehiculo.Tipo?.Observacion ?? ""
            };
            return output;
        }


        public static Envio MapTo(EnvioDTO input)
        {
            Envio output = new Envio()
            {
                ChoferId = input.ChoferId ?? 0,
                Observaciones = input.Observaciones,
                TransportistaCodigo = input.TransportistaCodigo ?? -1,
                TransportistaDestinoCodigo = input.TransportistaDestinoCodigo,
                VehiculoId = input.VehiculoId ?? 0,
//                 Vehiculo = new Vehiculo() { Patente = input?.Patente ??"", Descripcion = input.Vehiculo},
                EstadoId = input.EstadoId,
                Id = input.EnvioId ?? 0,
                Numero = input.Numero,
                FechaInicio = input.FechaInicio,
                FechaTurno = input.FechaTurno,
                CodigoViaje = input.CodigoViaje 
            };
            return output;
        }


        public static GuiaDTO MapTo(EnvioGuia input)
        {
            GuiaDTO output = new GuiaDTO()
            {

                Estado = input.Estado?.Descripcion ?? "",
                EstadoColor = input.Estado?.Color ?? "",
                EstadoId = input.EstadoId,
                Id = input.Id,
                Fecha = input.Fecha,
                EnvioId = input.EnvioId,
                Numero = input.Numero,
            };
            return output;
        }

        public static EnvioGuia MapTo(GuiaDTO input)
        {
            EnvioGuia output = new EnvioGuia()
            {
                EstadoId = input.EstadoId,
                Id = int.Parse( input.Id.ToString()),
                Fecha = input.Fecha,
                EnvioId = input.EnvioId ??0,
                Numero = input.Numero,
            };
            return output;
        }
    }
}