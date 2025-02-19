using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Valuaciones.Computations
{
    public class TierraRuralComputation : IComputation
    {
        private readonly GeoSITMContext _context;
        public TierraRuralComputation(GeoSITMContext context)
        {
            _context = context;
        }
        public Task<DatosComputo> ComputeAsync(DDJJ ddjj)
        {
            foreach(var superficie in ddjj.Sor.Superficies)
            {
                superficie.Superficie *= 10_000;
            }
            return Task.FromResult(GetDatosComputo(ComputeSuperficieValuada(ddjj), ComputePuntajeTotal(ddjj), ComputeValorBaseAplicado(ddjj), ComputeDepreciacion(ddjj)));
        }
        protected virtual ISQLQueryBuilder AddValorBaseTable(ISQLQueryBuilder qb, DDJJ ddjj)
        {
            return qb.AddFunctionTable($"obtener_valor_valuacion_zona_ecologica({ddjj.IdUnidadTributaria})", null);
        }

        private decimal ComputeDepreciacion(DDJJ ddjj)
        {
            return ddjj.Sor.Superficies
                           .Where(s => s.TrazaDepreciable.GetValueOrDefault(0) > 0)
                           .Aggregate(0m, (accum, superficie) =>
                           {
                               switch (superficie.TrazaDepreciable.Value)
                               {
                                   case 1:
                                   case 2:
                                   case 5:
                                       accum += 2m;
                                       break;
                                   case 3:
                                       accum += 5m;
                                       break;
                                   case 4:
                                       accum += 4m;
                                       break;
                                   case 6:
                                       accum += 3m;
                                       break;

                               }
                               return accum;
                           });
        }
        private decimal ComputePuntajeTotal(DDJJ ddjj)
        {
            var computation = new PuntajeSuperficieComputation(_context);
            return ddjj.Sor.Superficies.Aggregate(0, (decimal accum, VALSuperficie superficie) =>
            {
                superficie.Puntaje = computation.Compute(superficie);
                return accum + superficie.PuntajeSuperficie;
            });
        }
        private decimal ComputeSuperficieValuada(DDJJ ddjj)
        {
            return new SuperficieTotalComputation().SumHa(ddjj.Sor.Superficies);
        }
        private ValorBaseZonaEcologica ComputeValorBaseAplicado(DDJJ ddjj)
        {
            using (var qb = _context.CreateSQLQueryBuilder())
            {
                var valorAplicado = AddValorBaseTable(qb, ddjj)
                                        .AddFields("codigo_zona_ecologica", "anio_base", "valor_aplicado")
                                        .ExecuteQuery((reader, status) =>
                                            {
                                                status.Break();
                                                return new ValorBaseZonaEcologica()
                                                {
                                                    CodigoZonaEcologica = reader.GetInt16(reader.GetOrdinal("codigo_zona_ecologica")),
                                                    AnioBase = reader.GetInt32(reader.GetOrdinal("anio_base")),
                                                    Valor = reader.GetDecimal(reader.GetOrdinal("valor_aplicado")),
                                                };
                                            }).SingleOrDefault();

                if (valorAplicado == null)
                {
                    throw new ArgumentOutOfRangeException("Valor Aplicado", "No se ha encontrado un valor a aplicar para la zona ecológica");
                }
                return valorAplicado;
            }
        }
        private decimal ComputeValuacion(decimal puntaje, decimal valorZona, decimal porcentajeDepreciacion)
        {
            return Math.Round(puntaje * valorZona * (100 - porcentajeDepreciacion) / 100, 2);
        }
        private DatosComputo GetDatosComputo(decimal superficieHaValuada, decimal puntajeTotal, ValorBaseZonaEcologica valorZonaEcologica, decimal porcentajeDepreciacion)
        {
            return new DatosComputo()
            {
                AnioBase = valorZonaEcologica.AnioBase,
                CodigoZonaEcologica = valorZonaEcologica.CodigoZonaEcologica,
                PuntajeValuado = puntajeTotal,
                PorcentajeDepreciacion = porcentajeDepreciacion,
                SuperficieValuadaHA = superficieHaValuada,
                ValorOptimo = valorZonaEcologica.Valor,
                Valuacion = ComputeValuacion(puntajeTotal, valorZonaEcologica.Valor, porcentajeDepreciacion)
            };
        }
    }
}
