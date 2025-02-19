using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;

using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Computations
{
    internal class PuntajeSuperficieComputation
    {
        private readonly GeoSITMContext _context;
        public PuntajeSuperficieComputation(GeoSITMContext ctx)
        {
            _context = ctx;
        }
        public int Compute(VALSuperficie superficie)
        {
            bool depreciable = (superficie.TrazaDepreciable ?? 0) > 0;
            long idAptitudSuperficie = superficie.IdAptitud;
            var caracteristicasSeleccionadas = superficie.Caracteristicas.Select(c => c.IdSorCar);

            var queryCaracteristicas = from caracteristica in _context.DDJJSorCaracteristicas
                                       where caracteristicasSeleccionadas.Contains(caracteristica.IdSorCaracteristica)
                                       select caracteristica;

            int puntajeDepreciableAptitud = 0;
            if (depreciable)
            {
                puntajeDepreciableAptitud = (superficie.Aptitud ?? _context.VALAptitudes.Find(idAptitudSuperficie)).PuntajeDepreciable;
            }

            return queryCaracteristicas.Sum(c => c.Puntaje - (depreciable ? c.PuntajeDepreciable : 0)) - puntajeDepreciableAptitud;
        }
    }
}
