using System.Data.Entity;
using GeoSit.Data.BusinessEntities.Inmuebles;

using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Repositories
{
    public class MensuraSecuenciaRepository : IMensuraSecuenciaRepository
    {
        private readonly GeoSITMContext _context;

        public MensuraSecuenciaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public void UpdateMensuraSecuencia(MensuraSecuencia mensura)
        {
            _context.Entry(mensura).State = EntityState.Modified;
        }

        public void InsertMensuraSecuencia(MensuraSecuencia mensura)
        {
            _context.MensuraSecuencias.Add(mensura);
        }

    }
}
