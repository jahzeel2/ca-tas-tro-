using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;

namespace GeoSit.Data.DAL.OperacionesParcelarias.Generators
{
    public class OperacionRedistribucion : IOperacionParcelaria
    {
        private OperacionRedistribucion() { }
        internal static OperacionRedistribucion Create(UnidadAlfanumericoParcela uap)
        {
            if(uap.Operacion == int.Parse(TiposParcelaOperacion.Redistribucion))
            {
                return new OperacionRedistribucion();
            }
            return null;
        }
    }
}
