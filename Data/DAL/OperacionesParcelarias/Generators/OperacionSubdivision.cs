using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;

namespace GeoSit.Data.DAL.OperacionesParcelarias.Generators
{
    public class OperacionSubdivision : IOperacionParcelaria
    {
        private OperacionSubdivision() { }
        internal static OperacionSubdivision Create(UnidadAlfanumericoParcela uap)
        {
            if(uap.Operacion == int.Parse(TiposParcelaOperacion.Subdivision))
            {
                return new OperacionSubdivision();
            }
            return null;
        }
    }
}
