using System.Collections.Generic;
using System.Linq;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Repositories
{
    public class TipoParcelaOperacionRepository : ITipoParcelaOperacionRepository
    {
        private readonly GeoSITMContext _context;

        public TipoParcelaOperacionRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public ICollection<TipoParcelaOperacion> GetTipoParcelaOperaciones()
        {
            var filtros = new[]
            {
                long.Parse(TiposParcelaOperacion.Subdivision),
                long.Parse(TiposParcelaOperacion.Unificacion),
                long.Parse(TiposParcelaOperacion.Redistribucion),
                long.Parse(TiposParcelaOperacion.Creacion),
                long.Parse(TiposParcelaOperacion.PrescripcionAdquisitiva),
                long.Parse(TiposParcelaOperacion.DerechoRealSuperficie)
            };

            return _context.TipoParcelaOperacion.Where(t => filtros.Contains(t.Id)).OrderBy(x => x.Descripcion).ToList();
        }
    }
}
