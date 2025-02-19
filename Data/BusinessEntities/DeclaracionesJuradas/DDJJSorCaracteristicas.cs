using System;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJSorCaracteristicas
    {
        public long IdSorCaracteristica { get; set; }
        public long IdSorTipoCaracteristica { get; set; }
        public int Puntaje { get; set; }
        public int PuntajeDepreciable { get; set; }
        public string Descripcion { get; set; }
        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public DDJJSorTipoCaracteristica TipoCaracteristica { get; set; }
    }
}
