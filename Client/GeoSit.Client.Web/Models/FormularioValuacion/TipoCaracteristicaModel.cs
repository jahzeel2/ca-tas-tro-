using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct TipoCaracteristicaModel
    {
        public string Nombre { get; set; }
        public List<CaracteristicaModel> Caracteristicas { get; set; }

        private TipoCaracteristicaModel(string nombre, IEnumerable<CaracteristicaModel> caracteristicas)
        {
            Nombre = nombre;
            Caracteristicas = caracteristicas.ToList();
        }

        public static TipoCaracteristicaModel FromEntity(DDJJSorTipoCaracteristica tipo)
            => new TipoCaracteristicaModel(tipo.Descripcion, tipo.Caracteristicas
                                                                 .OrderBy(c => c.Puntaje)
                                                                 .Select(c => CaracteristicaModel.FromEntity(c)));
    }
}