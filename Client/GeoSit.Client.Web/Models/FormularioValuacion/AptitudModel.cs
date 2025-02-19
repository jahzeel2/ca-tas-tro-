using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct AptitudModel
    {
        public long IdAptitud { get; set; }
        public string Descripcion { get; set; }
        public long Orden { get; set; }

        private AptitudModel(long idAptitud, string descripcion, long orden)
        {
            IdAptitud = idAptitud;
            Descripcion = descripcion;
            Orden = orden;
        }

        public static AptitudModel FromEntity(VALAptitudes entity)
            => new AptitudModel(entity.IdAptitud, entity.Descripcion, entity.Numero.Value);
    }
}