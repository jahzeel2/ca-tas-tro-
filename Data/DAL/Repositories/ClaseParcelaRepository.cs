using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Repositories
{
    public class ClaseParcelaRepository : IClaseParcelaRepository
    {
        private readonly GeoSITMContext _context;

        public ClaseParcelaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public ICollection<ClaseParcela> GetClasesParcelas()
        {
            return _context.ClasesParcela.ToList();
        }

        public ClaseParcela GetClaseParcelaByTipoMensura(long tipoMensura)
        {
            if (tipoMensura == 0) return new ClaseParcela() { Descripcion = "" };

            long idClasesParcelaValida = long.Parse(ClasesParcelas.ParcelaComun);
            if(tipoMensura == long.Parse(TiposMensuras.DerechoSuperficie))
            {
                idClasesParcelaValida = long.Parse(ClasesParcelas.DerechoRealSuperficie);
            }
            else if (tipoMensura == long.Parse(TiposMensuras.PrescripcionAdquisitiva))
            {
                idClasesParcelaValida = long.Parse(ClasesParcelas.Prescripcion);
            }

            return _context.ClasesParcela.Find(idClasesParcelaValida);
        }

        public ClaseParcela GetClaseParcela(long id)
        {
            return _context.ClasesParcela.Find(id);
        }
    }
}
