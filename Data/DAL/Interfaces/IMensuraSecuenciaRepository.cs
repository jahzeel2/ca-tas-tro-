using GeoSit.Data.BusinessEntities.Inmuebles;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IMensuraSecuenciaRepository
    {
        void UpdateMensuraSecuencia(MensuraSecuencia mensura);
        void InsertMensuraSecuencia(MensuraSecuencia mensura);

    }
}