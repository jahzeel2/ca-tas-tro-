using GeoSit.Data.BusinessEntities.MesaEntradas;
using SGTEntities.Interfaces;

namespace GeoSit.Data.DAL.Models
{
    public class GeneradorNovedadTramite : ITextGenerator<METramite>
    {
        private readonly string novedad;
        internal GeneradorNovedadTramite(string novedad)
        {
            this.novedad = novedad;
        }
        public string Generate(METramite tramite)
        {
            return string.Format($"Acción realizada en el trámite {tramite.Numero}: {novedad}");
        }
    }
}
