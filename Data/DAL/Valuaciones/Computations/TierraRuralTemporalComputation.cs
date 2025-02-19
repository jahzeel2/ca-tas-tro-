using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Valuaciones.Computations
{
    public class TierraRuralTemporalComputation : TierraRuralComputation
    {
        public TierraRuralTemporalComputation(GeoSITMContext context)
            : base(context) { }

        protected override ISQLQueryBuilder AddValorBaseTable(ISQLQueryBuilder qb, DDJJ ddjj)
        {
            return qb.AddFunctionTable("temporal", $"obtener_valor_valuacion_zona_ecologica({ddjj.IdUnidadTributaria})", null);
        }
    }
}
