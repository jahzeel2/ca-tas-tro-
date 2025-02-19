using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Repositories
{
    internal class ObjetosAdministrativosRepository : IObjetosAdministrativosRepository
    {
        private readonly GeoSITMContext _context;

        public ObjetosAdministrativosRepository(GeoSITMContext context)
        {
            _context = context;
        }
        public IEnumerable<Objeto> GetCircunscripcionesByDepartamento(long idDepartamento)
        {
            long tipoCircunscripcion = long.Parse(TiposObjetoAdministrativo.CIRCUNSCRIPCION);
            var query = _context.Objetos.Where(oa => oa.TipoObjetoId == tipoCircunscripcion
                                                        && oa.ObjetoPadreId == idDepartamento
                                                        && oa.FechaBaja == null);
            return query.ToList();

        }

        public IEnumerable<Objeto> GetDepartamentos()
        {
            long tipoDepartamento = long.Parse(TiposObjetoAdministrativo.DEPARTAMENTO);
            var query = _context.Objetos.Where(oa => oa.TipoObjetoId == tipoDepartamento 
                                                        && oa.FechaBaja == null);
            return query.ToList();
        }

        public IEnumerable<Objeto> GetSeccionesByCircunscripcion(long idCircunscripcion)
        {
            long tipoSeccion = long.Parse(TiposObjetoAdministrativo.SECCION);
            var query = _context.Objetos.Where(oa => oa.TipoObjetoId == tipoSeccion
                                                        && oa.ObjetoPadreId == idCircunscripcion
                                                        && oa.FechaBaja == null);
            return query.ToList();
        }
    }
}
