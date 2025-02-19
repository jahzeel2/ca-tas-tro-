using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.Inmuebles.DTO
{
    public class OperacionAlfanumerica
    {
        public long Operacion { get; set; }
        public string NumeroPlano { get; set; }
        public DateTime? FechaOperacion { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public long IdUsuario { get; set; }
        public string Ip { get; set; }
        public string MachineName { get; set; }

        public List<long> ParcelasOrigen { get; set; }

        public List<ParcelaDestinoAlfanumerica> ParcelasDestino { get; set; }
    }
}
