using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Temporal;
using System.Linq;

namespace GeoSit.Client.Web.Models.FormularioValuacion
{
    public struct FormularioValuacionModel
    {
        public long IdUnidadTributaria { get; set; }
        public long IdValuacion { get; set; }
        public long IdDDJJ { get; set; }
        public bool Editable { get; set; }

        public string ZonaEcologica { get; set; }
        public SuperficieRuralModel SuperficieParcela { get; set; }
        public SuperficieRuralModel SuperficieValuacion { get; set; }

        private FormularioValuacionModel(long idValuacion, long idDDJJ, long idUnidadTributaria, string zonaEcologica,
                                         SuperficieRuralModel superficieValuacion,
                                         SuperficieRuralModel superficieParcela, bool editable)
        {
            IdUnidadTributaria = idUnidadTributaria;
            IdValuacion = idValuacion;
            IdDDJJ = idDDJJ;
            ZonaEcologica = zonaEcologica;
            SuperficieValuacion = superficieValuacion;
            SuperficieParcela = superficieParcela;
            Editable = editable;
        }

        internal static FormularioValuacionModel FromEntity(VALValuacion valuacion)
            => new FormularioValuacionModel(valuacion.IdValuacion,
                                            valuacion.DeclaracionJurada.IdDeclaracionJurada,
                                            valuacion.IdUnidadTributaria,
                                            valuacion.UnidadTributaria.Parcela.AtributoZonaID.ToString(),
                                            SuperficieRuralModel.Create(valuacion.Superficie),
                                            SuperficieRuralModel.Create(valuacion.UnidadTributaria.Parcela.Superficie),
                                            false);

        internal static FormularioValuacionModel Create(VALValuacion valuacion)
            => new FormularioValuacionModel(0, 0,
                                            valuacion.IdUnidadTributaria,
                                            valuacion.UnidadTributaria.Parcela.AtributoZonaID.ToString(),
                                            SuperficieRuralModel.Create(valuacion.Superficie),
                                            SuperficieRuralModel.Create(valuacion.UnidadTributaria.Parcela.Superficie),
                                            true);

        internal static FormularioValuacionModel CreateResumenSuperficies(decimal parcela, decimal valuada)
            => new FormularioValuacionModel(0, 0, 0, string.Empty,
                                            SuperficieRuralModel.Create(valuada),
                                            SuperficieRuralModel.Create(parcela),
                                            false);

        internal static FormularioValuacionModel FromTemporalEntity(VALValuacionTemporal valuacion, SuperficieModel[] superficies)
        {
            double? superficieValuada = (superficies?.Any() ?? false) 
                                                ? (double?)superficies.Sum(s => s.SuperficieHa * 10_000) 
                                                : valuacion.Superficie;

            return new FormularioValuacionModel(valuacion.IdValuacion,
                                                valuacion.DeclaracionJurada?.IdDeclaracionJurada ?? 0,
                                                valuacion.UnidadTributaria.UnidadTributariaId,
                                                valuacion.UnidadTributaria.Parcela.AtributoZonaID.ToString(),
                                                SuperficieRuralModel.Create(superficieValuada),
                                                SuperficieRuralModel.Create(valuacion.UnidadTributaria.Parcela.Superficie),
                                                true);
        }
    }
}