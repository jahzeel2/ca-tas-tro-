using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.Seguridad
{
    public class AnalisisTecnicos : IEntity
    {
        public long Id_Analisis { get; set; }
        public string Nombre { get; set; }
        public long Informe_Sar { get; set; }

        public ICollection<CargasTecnicas> CargasTecnicas { get; set; }
    }
}


