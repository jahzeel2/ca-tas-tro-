using System.Collections.Generic;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using static GeoSit.Data.DAL.Repositories.PadronMunicipalRepository;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IPadronMunicipalRepository
    {
        List<ResultadoConsulta> GetParcelasMunicipio(long idMunicipio, bool vigentes, bool tipoInformeParcelario);

    }
}
