using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Xml;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Common.ExtensionMethods.Data;
using GeoSit.Data.DAL.Common.ExtensionMethods.DecimalDegreesToDMS;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Repositories
{
    public class PadronMunicipalRepository : IPadronMunicipalRepository
    {
        private readonly GeoSITMContext _context;

        public PadronMunicipalRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public class ResultadoConsulta
        {
            public string Nomenclatura { get; set; }
            public string DescripcionTipoParcela { get; set; }
            public string CodigoProvincial { get; set; }
            public string uf { get; set; }
            public string Ubicacion { get; set; }
            public string coordenadas { get; set; }
            public string AfectacionPh { get; set; }
            public string Inscripcion { get; set; }
            public string FechaDesde { get; set; }
            public string ValorTierra { get; set; }
            public string ValorMejoras { get; set; }
            public string ValorTotal { get; set; }
            public string SuperficieRegistrada { get; set; }
            public string SuperficieGrafica { get; set; }
            public string unidadSup { get; set; }
        }

        public List<ResultadoConsulta> GetParcelasMunicipio(long idMunicipio, bool vigentes, bool tipoInformeParcelario)
        {
            using (var ctx = GeoSITMContext.CreateContext())

            using (var qb = ctx.CreateSQLQueryBuilder())

            using (var qbDesignacion = ctx.CreateSQLQueryBuilder())

            using (var qbValuacion = ctx.CreateSQLQueryBuilder())

            {

                string esquema = ConfigurationManager.AppSettings["DATABASE"];

                string fechaBaja = "fecha_baja";

                long tipoMunicipio = 5;


                var campoIdParcela = new Data.BusinessEntities.MapasTematicos.Atributo() { Campo = "id_parcela" };

                var campoGeometry = new Data.BusinessEntities.MapasTematicos.Atributo { Campo = "geometry" };

                var campoIdTipoPacela = new Data.BusinessEntities.MapasTematicos.Atributo() { Campo = "id_tipo_parcela" };

                var campoIdUt = new Data.BusinessEntities.MapasTematicos.Atributo() { Campo = "id_unidad_tributaria" };

                var geomCentroide = qb.CreateGeometryFieldBuilder(campoGeometry, "pgraf").Centroid().ChangeToSRID(Common.Enums.SRID.LL84).ToWKT();

                var geometryArea = qb.CreateGeometryFieldBuilder(campoGeometry, "pgraf").AreaSqrMeters();

                var geometryAreaRural = qb.CreateGeometryFieldBuilder(campoGeometry, "pgraf").AreaRural();

                var FiltroDeInforme = vigentes ? Data.DAL.Common.Enums.SQLOperators.IsNull : Data.DAL.Common.Enums.SQLOperators.IsNotNull;


                var designacion = qbDesignacion.AddTable(esquema, "inm_designacion", "id")
                    .AddFilter("fecha_baja", null, Data.DAL.Common.Enums.SQLOperators.IsNull)
                    .AddFields("id_parcela", "calle", "numero", "seccion", "chacra", "quinta", "fraccion", "manzana", "lote");

                var valuacion = qbValuacion.AddTable(esquema, "val_valuacion", "val")
                                .AddFilter("fecha_baja", null, Data.DAL.Common.Enums.SQLOperators.IsNull)
                                .AddFilter("fecha_hasta", null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                                .AddFields("id_unidad_tributaria", "fecha_desde", "valor_tierra", "valor_mejoras", "valor_total");


                qb.AddTable(esquema, "inm_unidad_tributaria", "iut")
                .AddFields("id_unidad_tributaria", "codigo_provincial", "id_tipo_ut", "unidad_funcional");

                if (tipoInformeParcelario && vigentes)
                {
                    qb.AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull)
                      .AddFilter("id_tipo_ut", "1, 2, 4", Data.DAL.Common.Enums.SQLOperators.In, Data.DAL.Common.Enums.SQLConnectors.And);

                }
                else if (!tipoInformeParcelario && vigentes)
                {
                    qb.AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull);

                }
                else if (tipoInformeParcelario && !vigentes)
                {
                    qb.AddFilter("id_tipo_ut", "1, 2, 4", Data.DAL.Common.Enums.SQLOperators.In)
                    .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNotNull, Data.DAL.Common.Enums.SQLConnectors.And);

                }else if(!tipoInformeParcelario && !vigentes)
                {
                    qb.AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNotNull);
                }

                qb.AddTable(esquema, "inm_parcela_grafica", "pgraf")
                  .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                  .AddFields("id_parcela")
                  .AddTable(esquema, "inm_parcela", "palfa")
                  .AddFields("atributos", "superficie");

                if (vigentes)
                {
                    qb.AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And);
                }
                else
                {
                    qb.AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNotNull, Data.DAL.Common.Enums.SQLConnectors.And);
                }

                if (tipoInformeParcelario)
                {
                    qb.AddFilter("id_clase_parcela", "1,3,4", Data.DAL.Common.Enums.SQLOperators.In, Data.DAL.Common.Enums.SQLConnectors.And);
                }

                qb.AddTable(esquema, "inm_tipo_parcela", "tipopar")
                .AddFields("descripcion", "id_tipo_parcela")
                .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddTable(esquema, "oa_objeto", "muni")
                .AddFields("nombre", "codigo")
                .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddFilter("featid", idMunicipio, Data.DAL.Common.Enums.SQLOperators.EqualsTo, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddFilter("id_tipo_objeto", tipoMunicipio, Data.DAL.Common.Enums.SQLOperators.EqualsTo, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddTable(esquema, "inm_dominio", "dom")
                .AddFields("inscripcion")
                .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddTable(esquema, "inm_nomenclatura", "nomenc")
                .AddFields("nomenclatura")
                .AddFilter(fechaBaja, null, Data.DAL.Common.Enums.SQLOperators.IsNull, Data.DAL.Common.Enums.SQLConnectors.And)
                .AddJoin(designacion.ToString(), "designacion", "iut", new[] { campoIdParcela }, Data.DAL.Common.Enums.SQLJoin.Left)
                .AddFormattedField("calle")
                .AddFormattedField("numero")
                .AddFormattedField("seccion")
                .AddFormattedField("chacra")
                .AddFormattedField("quinta")
                .AddFormattedField("fraccion")
                .AddFormattedField("manzana")
                .AddFormattedField("lote")
                .AddJoin(valuacion.ToString(), "valuacion", "iut", new[] { campoIdUt }, Data.DAL.Common.Enums.SQLJoin.Left)
                .AddFormattedField("fecha_desde")
                .AddFormattedField("valor_tierra")
                .AddFormattedField("valor_mejoras")
                .AddFormattedField("valor_total")
                .AddJoinFilter("iut", campoIdParcela, "pgraf", campoIdParcela)
                .AddJoinFilter("pgraf", campoIdParcela, "palfa", campoIdParcela)
                .AddJoinFilter("palfa", campoIdParcela, "nomenc", campoIdParcela)
                .AddJoinFilter("iut", campoIdUt, "dom", campoIdUt)
                .AddJoinFilter("palfa", campoIdTipoPacela, "tipopar", campoIdTipoPacela)
                .AddTableFilter(qb.CreateGeometryFieldBuilder(campoGeometry, "pgraf").Centroid(), qb.CreateGeometryFieldBuilder(campoGeometry, "muni"), Data.DAL.Common.Enums.SQLSpatialRelationships.AnyInteract)
                .AddGeometryField(geomCentroide, "centroide")
                .AddGeometryField(geometryArea, "area")
                .AddGeometryField(geometryAreaRural, "arearural")
                .OrderBy(Common.Enums.SQLSort.Desc, "nomenclatura");

                var listado = qb.ExecuteQuery(datareader =>
                {
                    var geometryLL84 = datareader.GetGeometryFromField(datareader.GetOrdinal("centroide"), Common.Enums.SRID.LL84);

                    string punto = $"{geometryLL84.YCoordinate.GetValueOrDefault().ConvertToDMS()},{geometryLL84.XCoordinate.GetValueOrDefault().ConvertToDMS()}";
                    string area = "";
                    string unidad = "";
                    string superRegis = "";

                    if (datareader.GetInt32(datareader.GetOrdinal("id_tipo_parcela")) == 2)
                    {
                        area = datareader.GetNullableDecimal(datareader.GetOrdinal("arearural")).Value.ToString("N4");
                        superRegis = ((datareader.GetNullableDecimal(datareader.GetOrdinal("superficie")).Value) / 10000).ToString("N4");
                        unidad = "ha";
                    }
                    else
                    {
                        area = datareader.GetNullableDecimal(datareader.GetOrdinal("area")).Value.ToString("N2");
                        superRegis = datareader.GetNullableDecimal(datareader.GetOrdinal("superficie")).Value.ToString("N2");
                        unidad = "m2";
                    }

                    var c = datareader.GetStringOrEmpty(datareader.GetOrdinal("calle")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("calle")) : "S/D";
                    var n = datareader.GetStringOrEmpty(datareader.GetOrdinal("numero")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("numero")) : "S/D";
                    var s = datareader.GetStringOrEmpty(datareader.GetOrdinal("seccion")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("seccion")) : "S/D";
                    var ch = datareader.GetStringOrEmpty(datareader.GetOrdinal("chacra")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("chacra")) : "S/D";
                    var q = datareader.GetStringOrEmpty(datareader.GetOrdinal("quinta")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("quinta")) : "S/D";
                    var f = datareader.GetStringOrEmpty(datareader.GetOrdinal("fraccion")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("fraccion")) : "S/D";
                    var m = datareader.GetStringOrEmpty(datareader.GetOrdinal("manzana")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("manzana")) : "S/D";
                    var l = datareader.GetStringOrEmpty(datareader.GetOrdinal("lote")) != "" ? datareader.GetStringOrEmpty(datareader.GetOrdinal("lote")) : "S/D";

                    string ubicacion = $"Calle: {c} - Numero: {n} - Sección: {s} - Chacra: {ch} - Quinta: {q} - Fracción: {f} - Manzana: {m} - Lote: {l}";

                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(datareader.GetStringOrEmpty(datareader.GetOrdinal("atributos")));
                    var node = xmlDoc.SelectSingleNode("//datos/AfectaPH/text()");

                    var vm = datareader.GetNullableDecimal(datareader.GetOrdinal("valor_mejoras"));
                    var vt = datareader.GetNullableDecimal(datareader.GetOrdinal("valor_tierra"));
                    var vtt = datareader.GetNullableDecimal(datareader.GetOrdinal("valor_total"));
                    var fd = datareader.GetNullableDateTime(datareader.GetOrdinal("fecha_desde"));

                    var UF = datareader.GetStringOrEmpty(datareader.GetOrdinal("unidad_Funcional"));

                    return new ResultadoConsulta
                    {
                        Nomenclatura = datareader.GetStringOrEmpty(datareader.GetOrdinal("nomenclatura")),
                        DescripcionTipoParcela = datareader.GetStringOrEmpty(datareader.GetOrdinal("descripcion")),
                        CodigoProvincial = datareader.GetStringOrEmpty(datareader.GetOrdinal("codigo_provincial")),
                        Ubicacion = ubicacion,
                        AfectacionPh = node?.Value == "true" ? "SI" : "NO",
                        coordenadas = punto,
                        uf = UF != "" ? UF : "0",
                        Inscripcion = datareader.GetStringOrEmpty(datareader.GetOrdinal("inscripcion")),
                        FechaDesde = fd != null ? fd.Value.ToString("dd/MM/yyyy") : "-",
                        ValorTierra = vt != null ? vt.Value.ToString("N2") : "-",
                        ValorMejoras = vm != null ? vm.Value.ToString("N2") : "-",
                        ValorTotal = vtt != null ? vtt.Value.ToString("N2") : "-",
                        SuperficieRegistrada = superRegis != null ? superRegis : "-",
                        SuperficieGrafica = area != null ? area : "-",
                        unidadSup = unidad,
                    };
                });

                return listado.ToList();
            }
        }
    }
}

