using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Repositories
{
    public class EstadoParcelaRepository : IEstadoParcelaRepository
    {
        private readonly GeoSITMContext _context;

        public EstadoParcelaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public EstadoParcela GetEstadoParcela(long id)
        {
            return _context.EstadosParcela.Find(id);
        }

        public ICollection<EstadoParcela> GetEstadosParcela()
        {
            return _context.EstadosParcela.OrderBy(ep => ep.EstadoParcelaID).Take(4).ToList();
        }

        public ICollection<EstadoParcela> GetEstadosParcelaByTipoMensura(long tipoMensura)
        {
            if (tipoMensura == 0)
            {
                return new EstadoParcela[0];
            }

            var query = _context.EstadosParcela.Where(e => e.FechaBaja == null);
            if (tipoMensura == long.Parse(TiposMensuras.DerechoSuperficie))
            {
                long SIN_DATOS = long.Parse(EstadosParcelas.SinDatos);
                query = query.Where(e => e.EstadoParcelaID == SIN_DATOS);
            }
            return query.ToList();
        }
    }
}
