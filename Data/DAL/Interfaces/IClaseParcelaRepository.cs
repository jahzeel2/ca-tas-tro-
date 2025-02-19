using GeoSit.Data.BusinessEntities.Inmuebles;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IClaseParcelaRepository
    {
        ICollection<ClaseParcela> GetClasesParcelas();
        ClaseParcela GetClaseParcelaByTipoMensura(long tipoMensura);
        ClaseParcela GetClaseParcela(long id);
    }
}
