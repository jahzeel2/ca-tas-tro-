using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;

namespace GeoSit.Data.DAL.OperacionesParcelarias.Generators
{
    public class OperacionUnificacion : IOperacionParcelaria
    {
        private OperacionUnificacion() { }
        internal static OperacionUnificacion Create(UnidadAlfanumericoParcela uap)
        {
            if(uap.Operacion == int.Parse(TiposParcelaOperacion.Unificacion))
            {
                return new OperacionUnificacion();
            }
            return null;
        }
    }
}
