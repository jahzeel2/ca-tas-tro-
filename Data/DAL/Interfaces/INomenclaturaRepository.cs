using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Common.CustomErrors;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface INomenclaturaRepository
    {
        Nomenclatura GetNomenclatura(string nomenclatura);
        Nomenclatura GetNomenclaturaById(long id);
        void InsertNomenclatura(Nomenclatura nomenclatura);
        void UpdateNomenclatura(Nomenclatura nomenclatura);
        void UpdateNombreNomenclatura(Nomenclatura nomenclatura);
        void DeleteNomenclatura(Nomenclatura nomenclatura);
        Nomenclatura GetNomenclatura(long idParcela, long idTipoNomenclatura);
        string Generar(long idParcela, long tipo);
        IEnumerable<Nomenclatura> GetByIdParcela(long idParcela);
        Objeto GetObjetoByTipo(long id, string depto);
        bool ValidarExistenciaNomenclatura(string nomenclatura);
    }
}
