using GeoSit.Data.BusinessEntities.ModuloPloteo;
using System.Collections.Generic;

namespace GeoSit.Client.Web.ViewModels
{
    public class CategoriaModel
    {
        public int IdTipo { get; set; }
        public string Nombre { get; set; }
        public List<Plantilla> Plantillas { get; set; }
        public string Action { get; set; }
    }
}