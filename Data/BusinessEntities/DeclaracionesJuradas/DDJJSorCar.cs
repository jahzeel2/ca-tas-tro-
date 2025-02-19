using System;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJSorCar
    {
        public long IdSuperficie { get; set; }
        public long IdSorCar { get; set; }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public DDJJSorCaracteristicas Caracteristica { get; set; }
    }
}
