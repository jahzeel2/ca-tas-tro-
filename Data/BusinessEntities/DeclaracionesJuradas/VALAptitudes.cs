using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class VALAptitudes
    {
        public long IdAptitud { get; set; }
        public long IdVersion { get; set; }
        public long? Numero { get; set; }
        public string Descripcion { get; set; }
        public int PuntajeDepreciable { get; set; }
      

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public DDJJVersion Version { get; set; }
        public ICollection<DDJJSorGrupoCaracteristica> GruposCaracteristicas { get; set; }
    }
}
