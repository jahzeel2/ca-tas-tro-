using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJSorGrupoCaracteristica
    {
        public short IdGrupoCaracteristica { get; set; }
        public string Descripcion { get; set; }
        public bool Maestra { get; set; }

        public ICollection<DDJJSorTipoCaracteristica> Tipos { get; set; }
        public ICollection<VALAptitudes> Aptitudes { get; set; }
    }
}
