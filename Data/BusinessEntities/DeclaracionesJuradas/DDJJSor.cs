using GeoSit.Data.BusinessEntities.Inmuebles;
using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJSor
    {
        public long IdSor { get; set; }

        public long? IdMensura { get; set; }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public ICollection<VALSuperficie> Superficies { get; set; }

        public Mensura Mensuras { get; set; }
    }
}
