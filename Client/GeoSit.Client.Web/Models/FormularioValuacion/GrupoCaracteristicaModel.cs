using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct GrupoCaracteristicaModel
    {
        public List<TipoCaracteristicaModel> TiposCaracteristicas { get; set; }
        public string Nombre { get; set; }
        public string ClassName { get; set; }
        public bool Maestra { get; set; }

        private GrupoCaracteristicaModel(string nombre, bool maestra, IEnumerable<TipoCaracteristicaModel> tipos)
        {
            TiposCaracteristicas = tipos.ToList();
            Nombre = nombre;
            ClassName = nombre.Split(' ').First().ToLower();
            Maestra = maestra;
        }

        public static GrupoCaracteristicaModel Create(DDJJSorGrupoCaracteristica grupo, IEnumerable<DDJJSorTipoCaracteristica> tipos)
            => new GrupoCaracteristicaModel(grupo.Descripcion, grupo.Maestra, tipos.OrderBy(t => t.IdSorTipoCaracteristica)
                                                                                   .Select(t => TipoCaracteristicaModel.FromEntity(t)));
    }
}