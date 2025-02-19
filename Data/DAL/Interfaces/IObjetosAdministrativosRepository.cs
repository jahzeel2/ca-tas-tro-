using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IObjetosAdministrativosRepository
    {
        IEnumerable<Objeto> GetDepartamentos();
        IEnumerable<Objeto> GetCircunscripcionesByDepartamento(long idDepartamento);
        IEnumerable<Objeto> GetSeccionesByCircunscripcion(long idCircunscripcion);
    }
}
