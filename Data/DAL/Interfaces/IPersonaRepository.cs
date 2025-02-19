using System.Collections.Generic;
using System.Threading.Tasks;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.ObrasParticulares;
using GeoSit.Data.BusinessEntities.Personas;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IPersonaRepository
    {
        IEnumerable<PersonaExpedienteRolDomicilio> SearchPersona(string nombre);
        PersonaExpedienteRolDomicilio GetPersona(long idPersona);
        Persona GetPersonaDatos(long idPersona);
        IEnumerable<Persona> GetPersonasByTramite(int tramite);
        IEnumerable<Persona> GetPersonasCompletas(long[] personas);
        Task<Persona> Save(Persona persona, IEnumerable<Domicilio> domicilios, IEnumerable<Profesion> profesiones, long usuarioOperacion, string ip, string machineName);
        Task<List<Persona>> SearchByPattern(string pattern, int limit = 100);
        Persona GetPersonaById(long id);
        List<Profesion> GetProfesionesByPersonaId(long id);
        Task<bool> Delete(long id, long usuarioOperacion, string ip, string machineName);
    }
}
