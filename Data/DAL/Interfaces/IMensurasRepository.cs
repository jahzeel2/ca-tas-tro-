using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Interfaces
{
    public interface IMensurasRepository
    {
        List<Mensura> GetMensuras();
        Mensura GetById(long id);
        List<ParcelaMensura> GetParcelasMensuraByParcelaId(long idParcela);
        DataTableResult<GrillaMensura> SearchByText(DataTableParameters parametros);
        Task Save(Mensura mensura, List<long> parcelas, List<long> mensurasOrigen, List<long> mensurasDestino, List<long> documentos, long usuarioOperacion, string ip, string machineName);
        Task<bool> Delete(Mensura mensura);
        Task<bool> DeleteRelacionParcelaMensura(Mensura mensura, long idParcelaMensura);
        List<TipoMensura> GetTiposMensura();
        List<TipoMensura> GetTiposMensuraSaneamiento();
    }
}
