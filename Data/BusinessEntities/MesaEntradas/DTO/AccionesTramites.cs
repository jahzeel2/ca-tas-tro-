using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.MesaEntradas.DTO
{
    public class AccionesTramites
    {
        public List<Accion> Generales { get; set; }
        public List<Accion> Seleccion { get; set; }

        public AccionesTramites()
        {
            Generales = new List<Accion>();
            Seleccion = new List<Accion>();
        }
    }
}
