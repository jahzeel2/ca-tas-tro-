using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Temporal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IDeclaracionJuradaTemporalRepository
    {
        DDJJTemporal GetById(long id, int idTramite);
        IEnumerable<Tuple<long, DDJJTemporal>> GetDDJJByTramite(int tramite);
        Task<DatosComputo> CalcularValuacionAsync(long idUnidadTributaria, VALSuperficie[] superficies);
    }
}
