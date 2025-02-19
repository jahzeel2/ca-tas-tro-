using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;

namespace GeoSit.Data.DAL.OperacionesParcelarias.Generators
{
    public class OperacionCreacion : IOperacionParcelaria
    {
        private OperacionCreacion() { }
        internal static OperacionCreacion Create(UnidadAlfanumericoParcela uap)
        {
            if(uap.Operacion == int.Parse(TiposParcelaOperacion.Creacion))
            {
                return new OperacionCreacion();
            }
            return null;
        }
    }
}
