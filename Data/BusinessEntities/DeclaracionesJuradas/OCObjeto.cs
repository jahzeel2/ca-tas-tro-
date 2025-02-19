using System;
using System.Data.Spatial;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class OCObjeto
    {
        public long FeatId { get; set; }
        public long IdSubtipoObjeto { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Atributos { get; set; }
        public DbGeometry Geometry { get; set; }
        public string GeomTxt { get; set; }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

    }
}
