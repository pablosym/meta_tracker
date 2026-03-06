
using Tracker.Interfaces;

namespace Tracker.DTOs
{
    public class FiltroEnvioDTO : IPaginador
    {
        public int Id { get; set; }

        public string? Desde { get; set; }
        public string? Hasta { get; set; }

        public int? EstadoId { get; set; }

        public Int64? GuiaNumero { get; set; }
        public Int64? Numero { get; set; }

        public int RecordsTotal { get; set; }

        public string? SearchValue { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        private int _pageSize = 10;
        public int PageSize 
        {
            get { return _pageSize;  }
            set { _pageSize = value; }
        }

        public int Skip { get; set; }

        public long? TransportistaDestinoCodigo { get; set; }
    }
}