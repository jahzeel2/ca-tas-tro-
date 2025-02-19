using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Valuaciones.Computations
{
    public interface IComputation
    {
        Task<DatosComputo> ComputeAsync(DDJJ ddjj);
    }
}
