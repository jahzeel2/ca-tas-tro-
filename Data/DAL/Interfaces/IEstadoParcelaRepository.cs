using GeoSit.Data.BusinessEntities.Inmuebles;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IEstadoParcelaRepository
    {
        ICollection<EstadoParcela> GetEstadosParcela();
        ICollection<EstadoParcela> GetEstadosParcelaByTipoMensura(long tipoMensura);
        EstadoParcela GetEstadoParcela(long id);
    }
}
