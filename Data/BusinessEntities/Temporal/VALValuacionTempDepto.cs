using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.BusinessEntities.Temporal
{
    public class VALValuacionTempDepto
    {
        public int Corrida { get; set; }
        public string Depto { get; set; }
        public string FechaProc { get; set; }
        public decimal CantidadParcProc { get; set; }
        public decimal SupValuada { get; set; }
        public decimal ValTotal { get; set; }
        public decimal ValMax { get; set; }
        public decimal ValMin { get; set; }
        public decimal PromedioValParc { get; set; }
    }
}
