using GeoSit.Data.BusinessEntities.Inmuebles;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IPartidaSecuenciaRepository
    {
        void UpdatePartidaSecuencia(PartidaSecuencia partida);
        void InsertPartidaSecuencia(PartidaSecuencia partida);

    }
}