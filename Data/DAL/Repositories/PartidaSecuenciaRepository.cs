using System.Data.Entity;
using GeoSit.Data.BusinessEntities.Inmuebles;

using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Repositories
{
    public class PartidaSecuenciaRepository : IPartidaSecuenciaRepository
    {
        private readonly GeoSITMContext _context;

        public PartidaSecuenciaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public void UpdatePartidaSecuencia(PartidaSecuencia partida)
        {
            _context.Entry(partida).State = EntityState.Modified;
        }

        public void InsertPartidaSecuencia(PartidaSecuencia partida)
        {
            _context.PartidaSecuencias.Add(partida);
        }
    }
}
