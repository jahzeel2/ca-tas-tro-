using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.BusinessEntities.Temporal
{
    public class VALValuacionTmp
    {
        public long IdValuacion { get; set; }
        public long IdUnidadTributaria { get; set; }
        public int IdDDJJ { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public decimal? ValorTierra { get; set; }
        public decimal? ValorMejoras { get; set; }
        public decimal? ValorMejorasPropio { get; set; }
        public decimal? ValorTotal { get; set; }
        public double? Superficie { get; set; }
        public double? CoefProrrateo { get; set; }
        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }
        public int Corrida { get; set; }
    }
}
