using System;
using System.Collections.Generic;
using DATA = System.Data;
using System.Data.Entity;
using System.Linq;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using GeoSit.Data.DAL.Common.ExtensionMethods.Data;
using GeoSit.Data.DAL.Common.ExtensionMethods.Atributos;
using System.Xml;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using GeoSit.Data.DAL.Common;
using System.Linq.Expressions;
using System.Configuration;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.DAL.Common.CustomErrors.Parcelas;
using GeoSit.Data.DAL.Common.CustomErrors;
using Z.EntityFramework.Plus;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.DAL.Common.CustomErrors.OperacionesParcelarias;
using System.Text.RegularExpressions;

namespace GeoSit.Data.DAL.Repositories
{
    public class ParcelaRepository : IParcelaRepository
    {
        private readonly GeoSITMContext _context;
        private const int IdZonaTributaria = 42;

        public ParcelaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public string GetNextPartida(long idTipo, long idJurisdiccion)
        {
            var juri = Convert.ToInt64(_context.ParametrosGenerales.Single(x => x.Clave == "ID_TIPO_OBJETO_JURISDICCION").Valor);
            var query = from ut in _context.UnidadesTributarias
                        join par in _context.Parcelas on ut.ParcelaID equals par.ParcelaID
                        join jur in _context.Objetos on ut.JurisdiccionID equals jur.FeatId
                        where jur.TipoObjetoId == juri && par.TipoParcelaID == idTipo && ut.JurisdiccionID == idJurisdiccion
                        select ut.CodigoProvincial.Substring(2, 6);

            int valor = Convert.ToInt32(query.Max()) + 1;
            return valor.ToString().PadLeft(6, '0');
        }

        public Parcela GetParcelaById(long idParcela, bool completa = true, bool utsHistoricas = false) => GetParcela(x => x.ParcelaID == idParcela, completa, utsHistoricas);
        public Parcela GetParcelaMantenedorById(long idParcela) => GetParcelaMantenedor(x => x.ParcelaID == idParcela);
        public Parcela GetParcelaByFeatIdDGC(long featId) => GetParcela(x => x.FeatIdDGC.HasValue && x.FeatIdDGC.Value == featId, true, false);

        private Parcela GetParcela(Expression<Func<Parcela, bool>> predicado, bool completa, bool utsHistoricas)
        {
            var parcela = _context.Parcelas.SingleOrDefault(predicado);
            if (parcela != null && completa)
            {
                load(parcela, utsHistoricas);
            }
            return parcela;
        }

        private Parcela GetParcelaMantenedor(Expression<Func<Parcela, bool>> predicado)
        {
            var parcela = _context.Parcelas.SingleOrDefault(predicado);
            loadParcelaMantenedor(parcela);
            return parcela;
        }

        public Zonificacion GetZonificacion(long idParcela, bool esHistorico = false)
        {
            try
            {
                using (var qbuilder = _context.CreateSQLQueryBuilder())
                {
                    long idComponenteParcela = long.Parse(_context.ParametrosGenerales.Single(pg => pg.Clave == "ID_COMPONENTE_PARCELA").Valor);
                    var cmpParcela = _context.Componente.Include(c => c.Atributos).Single(c => c.ComponenteId == idComponenteParcela);
                    cmpParcela.Tabla = cmpParcela.TablaGrafica ?? cmpParcela.Tabla;
                    var cmpObjeto = new Componente() { ComponenteId = -1, Tabla = "oa_objeto", Esquema = ConfigurationManager.AppSettings["DATABASE"] };
                    var foreignKeyIdTipo = new Atributo() { ComponenteId = cmpObjeto.ComponenteId, Campo = "id_tipo_objeto" };
                    var cmpTipo = new Componente() { ComponenteId = -2, Tabla = "oa_tipo_objeto", Esquema = ConfigurationManager.AppSettings["DATABASE"] };
                    var campoGeomParcela = qbuilder.CreateGeometryFieldBuilder(cmpParcela.Atributos.GetAtributoGeometry(), "par");

                    long? featidZPLN = null;
                    Zonificacion zonificacion = null;
                    qbuilder.AddTable(cmpObjeto, "obj")
                            .AddJoin(cmpTipo, "tipo", new Atributo() { ComponenteId = cmpTipo.ComponenteId, Campo = "id_tipo_objeto" }, foreignKeyIdTipo)
                            .AddTable(cmpParcela, "par")
                            .AddFilter(foreignKeyIdTipo, 15, Common.Enums.SQLOperators.EqualsTo)
                            .AddFilter(campoGeomParcela, qbuilder.CreateGeometryFieldBuilder(new Atributo() { ComponenteId = cmpObjeto.ComponenteId, Campo = "geometry" }, "obj"), Common.Enums.SQLSpatialRelationships.Inside | Common.Enums.SQLSpatialRelationships.CoveredBy, Common.Enums.SQLConnectors.And)
                            .AddFilter(cmpParcela.Atributos.GetAtributoClave(), idParcela, Common.Enums.SQLOperators.EqualsTo, Common.Enums.SQLConnectors.And)
                            .AddFields(new Atributo() { ComponenteId = cmpObjeto.ComponenteId, Campo = "featid" },
                                       new Atributo() { ComponenteId = cmpObjeto.ComponenteId, Campo = "codigo" },
                                       new Atributo() { ComponenteId = cmpObjeto.ComponenteId, Campo = "nombre" })
                            .ExecuteQuery((DATA.IDataReader reader, ReaderStatus status) =>
                            {
                                featidZPLN = reader.GetInt64(reader.GetOrdinal("featid"));
                                zonificacion = new Zonificacion()
                                {
                                    CodigoZona = reader.GetStringOrEmpty(reader.GetOrdinal("codigo")),
                                    NombreZona = reader.GetStringOrEmpty(reader.GetOrdinal("nombre")),
                                };
                                status.Break();
                            });
                    if (featidZPLN.HasValue)
                    {
                        zonificacion.AtributosZonificacion = _context.ZonaAtributo
                                                                     .Include(a => a.Atributo)
                                                                     .Where(a => a.FeatId_Objeto == featidZPLN && (esHistorico || !a.Fecha_Baja.HasValue))
                                                                     .ToList()
                                                                     .Select(a => new AtributosZonificacion() { Descripcion = a.Atributo.Descripcion, UnidadMedida = a.U_Medida, Valor = a.Valor })
                                                                     .OrderBy(a => a.Descripcion)
                                                                     .ToList();
                    }
                    return zonificacion;
                }
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"ParcelaRepository.GetZonificacion({idParcela})", ex);
                return null;
            }
        }

        public List<AtributosZonificacion> GetAtributosZonificacion(long idParcela)
        {
            throw new NotSupportedException("Ya no se recupera datos usando este método.");
        }

        private void load(Parcela parcela, bool utsHistoricas)
        {
            this._context.Entry(parcela).Reference(p => p.Clase).Load();
            this._context.Entry(parcela).Reference(p => p.Estado).Load();
            this._context.Entry(parcela).Reference(p => p.Origen).Load();
            this._context.Entry(parcela).Reference(p => p.Tipo).Load();

            this._context.Entry(parcela).Collection(c => c.UnidadesTributarias).Query().Where(ut => utsHistoricas || ut.FechaBaja == null).Load();

            this._context.Entry(parcela).Collection(c => c.ParcelaMensuras).Query().Where(m => m.FechaBaja == null).Load();
            if (parcela.ParcelaMensuras != null)
            {
                foreach (var parcelamensura in parcela.ParcelaMensuras)
                {
                    this._context.Entry(parcelamensura).Reference(pm => pm.Mensura).Load();
                    this._context.Entry(parcelamensura.Mensura).Reference(pm => pm.TipoMensura).Load();
                    this._context.Entry(parcelamensura.Mensura).Reference(pm => pm.EstadoMensura).Load();
                }
            }
            foreach (var ut in parcela.UnidadesTributarias ?? new List<UnidadTributaria>())
            {
                this._context.Entry(ut).Collection(u => u.UTDomicilios).Query().Where(utd => utd.FechaBaja == null).Load();
                this._context.Entry(ut).Reference(u => u.TipoUnidadTributaria).Load();
                if (ut.UTDomicilios != null)
                {
                    foreach (var utDom in ut.UTDomicilios)
                    {
                        this._context.Entry(utDom).Reference(utd => utd.Domicilio).Load();
                        this._context.Entry(utDom.Domicilio).Reference(d => d.TipoDomicilio).Load();
                    }
                }
                this._context.Entry(ut).Collection(u => u.UTDocumentos).Query().Where(utd => utd.FechaBaja == null).Load();
                if (ut.UTDocumentos != null)
                {
                    foreach (var utDoc in ut.UTDocumentos)
                    {
                        this._context.Entry(utDoc).Reference(d => d.Documento).Load();
                        utDoc.Documento.contenido = null;
                        this._context.Entry(utDoc.Documento).Reference(d => d.Tipo).Load();
                    }
                }
            }
            this._context.Entry(parcela).Collection(c => c.Nomenclaturas).Load();
            foreach (var nomenc in parcela.Nomenclaturas)
            {
                this._context.Entry(nomenc).Reference(n => n.Tipo).Load();
            }

            this._context.Entry(parcela).Collection(p => p.ParcelaDocumentos).Query().Where(pd => pd.FechaBaja == null).Load();
            if (parcela.ParcelaDocumentos?.Any() ?? false)
            {
                var documentosMensuraByParcela = (from parcelaMensura in this._context.ParcelaMensura
                                                  join mensura in this._context.Mensura on parcelaMensura.IdMensura equals mensura.IdMensura
                                                  join mensuraDocumento in this._context.MensuraDocumento on mensura.IdMensura equals mensuraDocumento.IdMensura
                                                  where mensura.IdEstadoMensura == 3 && parcelaMensura.FechaBaja == null
                                                        && mensura.FechaBaja == null && mensuraDocumento.FechaBaja == null
                                                        && parcelaMensura.IdParcela == parcela.ParcelaID
                                                  select mensuraDocumento.IdDocumento).ToList();

                foreach (var pd in parcela.ParcelaDocumentos.Where(x => !documentosMensuraByParcela.Contains(x.DocumentoID)))
                {
                    this._context.Entry(pd).Reference(d => d.Documento).Load();
                    pd.Documento.contenido = null;
                    this._context.Entry(pd.Documento).Reference(d => d.Tipo).Load();
                }

                parcela.ParcelaDocumentos = parcela.ParcelaDocumentos.Where(x => x.Documento != null).ToList();
            }

            try
            {
                using (var qbuilder = _context.CreateSQLQueryBuilder())
                {
                    if (!long.TryParse(_context.ParametrosGenerales.SingleOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA")?.Valor, out long idCompParcela))
                    {
                        throw new Exception("No se encuentra el parámetro ID_COMPONENTE_PARCELA");
                    }
                    var cmp = _context.Componente.Include(c => c.Atributos).SingleOrDefault(c => c.ComponenteId == idCompParcela);
                    Atributo attrCampoClave;
                    Atributo attrCampoGeometry;
                    try
                    {
                        attrCampoClave = cmp.Atributos.GetAtributoClave();
                        attrCampoGeometry = cmp.Atributos.GetAtributoGeometry();
                    }
                    catch (ApplicationException appEx)
                    {
                        _context.GetLogger().LogError("Componente (id: " + cmp.ComponenteId + ") mal configurado.", appEx);
                        throw;
                    }

                    var geometryArea = qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par");

                    if (parcela.TipoParcelaID == (long)TipoParcelaEnum.Rural || parcela.TipoParcelaID == (long)TipoParcelaEnum.Suburbana)
                    {
                        geometryArea.AreaRural();
                    }
                    else
                    {
                        geometryArea.AreaSqrMeters();
                    }

                    qbuilder
                        .AddTable(cmp.TablaGrafica ?? cmp.Tabla, "par")
                        .AddFilter(attrCampoClave.Campo, attrCampoClave.GetFormattedValue(parcela.ParcelaID), Common.Enums.SQLOperators.EqualsTo)
                        .AddGeometryField(qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par").Centroid().ToWKT(), "geom")
                        .AddGeometryField(qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par").Centroid().ChangeToSRID(Common.Enums.SRID.LL84).ToWKT(), "geomll84")
                        .AddGeometryField(geometryArea, "area")
                        .ExecuteQuery((reader, status) =>
                        {
                            var geometryNativa = reader.GetGeometryFromField(reader.GetOrdinal("geom"), Common.Enums.SRID.DB);
                            var geometryLL84 = reader.GetGeometryFromField(reader.GetOrdinal("geomll84"), Common.Enums.SRID.LL84);
                            var area = reader.GetDecimal(reader.GetOrdinal("area"));
                            if (geometryLL84 != null)
                            {
                                parcela.Coordenadas = $"POSGAR 2007 Faja 5: {Math.Round(geometryNativa.YCoordinate.GetValueOrDefault(), 4)}, {Math.Round(geometryNativa.XCoordinate.GetValueOrDefault(), 4)}";
                                parcela.CoordenadasLL84 = $"WGS84: {Math.Round(geometryLL84.YCoordinate.GetValueOrDefault(), 5)}, {Math.Round(geometryLL84.XCoordinate.GetValueOrDefault(), 5)}";
                                parcela.SuperficieGrafica = area;
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("ParcelaRepository - load ubicación", ex);
            }
        }
        
        private void loadParcelaMantenedor(Parcela parcela)
        {
            this._context.Entry(parcela).Reference(p => p.Clase).Load();
            this._context.Entry(parcela).Reference(p => p.Estado).Load();
            this._context.Entry(parcela).Reference(p => p.Origen).Load();
            this._context.Entry(parcela).Reference(p => p.Tipo).Load();
            this._context.Entry(parcela).Collection(c => c.ParcelaMensuras).Query().Where(m => m.FechaBaja == null).Load();
            if (parcela.ParcelaMensuras != null)
            {
                foreach (var parcelamensura in parcela.ParcelaMensuras)
                {
                    this._context.Entry(parcelamensura).Reference(pm => pm.Mensura).Load();
                    this._context.Entry(parcelamensura.Mensura).Reference(pm => pm.TipoMensura).Load();
                    this._context.Entry(parcelamensura.Mensura).Reference(pm => pm.EstadoMensura).Load();
                }
            }
            this._context.Entry(parcela).Collection(c => c.Nomenclaturas).Load();
            foreach (var nomenc in parcela.Nomenclaturas)
            {
                this._context.Entry(nomenc).Reference(n => n.Tipo).Load();
            }
            try
            {
                using (var qbuilder = _context.CreateSQLQueryBuilder())
                {
                    if (!long.TryParse(_context.ParametrosGenerales.SingleOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA")?.Valor, out long idCompParcela))
                    {
                        throw new Exception("No se encuentra el parámetro ID_COMPONENTE_PARCELA");
                    }
                    var cmp = _context.Componente.Include(c => c.Atributos).SingleOrDefault(c => c.ComponenteId == idCompParcela);
                    Atributo attrCampoClave;
                    Atributo attrCampoGeometry;
                    try
                    {
                        attrCampoClave = cmp.Atributos.GetAtributoClave();
                        attrCampoGeometry = cmp.Atributos.GetAtributoGeometry();
                    }
                    catch (ApplicationException appEx)
                    {
                        _context.GetLogger().LogError("Componente (id: " + cmp.ComponenteId + ") mal configurado.", appEx);
                        throw;
                    }

                    var geometryArea = qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par");

                    if (parcela.TipoParcelaID == (long)TipoParcelaEnum.Rural || parcela.TipoParcelaID == (long)TipoParcelaEnum.Suburbana)
                    {
                        geometryArea.AreaRural();
                    }
                    else
                    {
                        geometryArea.AreaSqrMeters();
                    }

                    qbuilder
                        .AddTable(cmp.TablaGrafica ?? cmp.Tabla, "par")
                        .AddFilter(attrCampoClave.Campo, attrCampoClave.GetFormattedValue(parcela.ParcelaID), Common.Enums.SQLOperators.EqualsTo)
                        .AddGeometryField(qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par").Centroid().ToWKT(), "geom")
                        .AddGeometryField(qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par").Centroid().ChangeToSRID(Common.Enums.SRID.LL84).ToWKT(), "geomll84")
                        .AddGeometryField(geometryArea, "area")
                        .ExecuteQuery((reader, status) =>
                        {
                            var geometryNativa = reader.GetGeometryFromField(reader.GetOrdinal("geom"), Common.Enums.SRID.DB);
                            var geometryLL84 = reader.GetGeometryFromField(reader.GetOrdinal("geomll84"), Common.Enums.SRID.LL84);
                            var area = reader.GetDecimal(reader.GetOrdinal("area"));
                            if (geometryLL84 != null)
                            {
                                parcela.Coordenadas = $"POSGAR 2007 Faja 5: {Math.Round(geometryNativa.YCoordinate.GetValueOrDefault(), 4)}, {Math.Round(geometryNativa.XCoordinate.GetValueOrDefault(), 4)}";
                                parcela.CoordenadasLL84 = $"WGS84: {Math.Round(geometryLL84.YCoordinate.GetValueOrDefault(), 5)}, {Math.Round(geometryLL84.XCoordinate.GetValueOrDefault(), 5)}";
                                parcela.SuperficieGrafica = area;
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("ParcelaRepository - load ubicación", ex);
            }
        }
        
        public VALValuacion GetValuacionParcela(long idParcela, bool esHistorico)
        {
            try
            {
                return new DeclaracionJuradaRepository(_context).GetValuacionVigenteConsolidada(idParcela, esHistorico);
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"GetValuacionParcela(IdParcela: {idParcela})", ex);
                throw;
            }
        }

        public void UpdateParcela(Parcela parcela)
        {
            parcela.FechaModificacion = DateTime.Now;
            if (parcela.FechaBajaExpediente.HasValue)
            {
                parcela.FechaBaja = parcela.FechaModificacion;
                parcela.UsuarioBajaID = parcela.UsuarioModificacionID;
            }
            _context.Entry(parcela).State = EntityState.Modified;
            _context.Entry(parcela).Property(x => x.UsuarioAltaID).IsModified = false;
            _context.Entry(parcela).Property(x => x.FechaAlta).IsModified = false;
        }

        public void DeleteParcela(Parcela parcela)
        {
            if (parcela == null) return;
            long idUsuario = parcela.UsuarioModificacionID;
            DateTime? fechaBaja = parcela.FechaBaja;
            parcela = _context.Parcelas.Find(parcela.ParcelaID);
            parcela.UsuarioModificacionID = idUsuario;
            parcela.UsuarioBajaID = parcela.UsuarioModificacionID;
            parcela.FechaBaja = fechaBaja;
            UpdateParcela(parcela);
            
        }

        public IEnumerable<Objeto> GetParcelaValuacionZonas()
        {
            var idTipoObjeto = Int32.Parse(_context.ParametrosGenerales.Find(IdZonaTributaria).Valor);
            var zonas = _context.Objetos.Where(o => o.TipoObjetoId == idTipoObjeto && !o.FechaBaja.HasValue).Take(200);
            return zonas.ToList();
        }

        public Zonificacion GetZonaValuacionByIdParcela(long id)
        {
            long idTipoZonaValuacion = long.Parse(_context.ParametrosGenerales.SingleOrDefault(p=>p.Clave == "ID_TIPO_ZONA_VALUACION").Valor);

            var zonaValuacion = (from parcela in _context.Parcelas
                                 join zona in _context.Objetos on parcela.AtributoZonaID.ToString() equals zona.Codigo
                                 where parcela.ParcelaID == id && zona.TipoObjetoId == idTipoZonaValuacion
                                 select zona).SingleOrDefault();

            return new Zonificacion()
            {
                CodigoZona = zonaValuacion?.Codigo,
                NombreZona = zonaValuacion?.Nombre
            };
        }

        //private TipoValorBasicoTierra GetTipoValorBasicoTierra(long idParcela)
        //{
        //    TipoValorBasicoTierra tvbt = null;
        //    var valPadronDetalle = _context.ValuacionPadronDetalle.FirstOrDefault(pd => pd.IdParcela == idParcela && pd.Fecha_Baja == null);
        //    if (valPadronDetalle != null)
        //    {
        //        _context.Entry(valPadronDetalle).Reference(pd => pd.Cabecera).Load();
        //        _context.Entry(valPadronDetalle.Cabecera).Reference(pdc => pdc.TipoValorBasicoTierra).Load();
        //        tvbt = valPadronDetalle.Cabecera.TipoValorBasicoTierra;
        //    }
        //    return tvbt;
        //}

        public Parcela InsertParcela(Parcela parcela)
        {
            _context.Parcelas.Add(parcela);
            _context.SaveChanges();
            return parcela;
        }

        public List<string> GetPartidabyId(long idParcela)
        {
            return (from t1 in _context.UnidadesTributarias
                    where t1.ParcelaID == idParcela
                    select t1.CodigoMunicipal).ToList();
        }

        public ParcelaSuperficies GetSuperficiesByIdParcela(long id, bool esHistorico = false)
        {
            try
            {
                var superficies = new ParcelaSuperficies();

                var uTributaria = _context.UnidadesTributarias
                                          .Include(p => p.Parcela)
                                          .Where(ut => ut.ParcelaID == id && (esHistorico || ut.FechaBaja == null) &&
                                                        ((TipoUnidadTributariaEnum)ut.TipoUnidadTributariaID == TipoUnidadTributariaEnum.Comun ||
                                                         (TipoUnidadTributariaEnum)ut.TipoUnidadTributariaID == TipoUnidadTributariaEnum.PropiedaHorizontal))
                                          .OrderBy(ut => ut.TipoUnidadTributariaID)
                                          .Single();

                #region Superficies Parcelas
                try
                {

                    if (!string.IsNullOrEmpty(uTributaria.Parcela.Atributos))
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(uTributaria.Parcela.Atributos);
                        decimal readXmlAttribute(string campo)
                        {
                            var node = doc.SelectSingleNode($"//datos/{campo}/text()");
                            decimal.TryParse(node?.Value, out decimal valor);
                            return valor;
                        }
                        superficies.AtributosParcela.Catastro = readXmlAttribute("SuperficieCatastro");
                        superficies.AtributosParcela.Titulo = readXmlAttribute("SuperficieTitulo");
                        superficies.AtributosParcela.Mensura = readXmlAttribute("SuperficieMensura");
                        superficies.AtributosParcela.Estimada = readXmlAttribute("SuperficieEstimada");
                    }
                }
                catch (Exception ex)
                {
                    _context.GetLogger().LogError($"GetSuperficiesByIdParcela({id}) - Superficies Parcelas", ex);
                    return null;
                }
                #endregion

                #region Superficies Relevamiento
                using (var builder = _context.CreateSQLQueryBuilder())
                {
                    try
                    {

                        string[] fields =
                            {
                                "superficie_tierra_graf", "superficie_cubierta", "superficie_galpon",
                                "superficie_semicubierta", "superficie_piscina", "superficie_deportiva",
                                "superficie_en_const", "superficie_precaria"
                            };
                        builder.AddTable("res_parcela_grafica", "t1")
                               .AddFilter("partida", $"'{uTributaria.CodigoProvincial}'", Common.Enums.SQLOperators.EqualsTo)
                               .AddFields(fields)
                               .ExecuteQuery((DATA.IDataReader reader, ReaderStatus readerStatus) =>
                               {
                                   superficies.RelevamientoParcela.Relevada = reader.GetNullableDecimal(reader.GetOrdinal("superficie_tierra_graf")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasConstrucciones.Cubierta = reader.GetNullableDecimal(reader.GetOrdinal("superficie_cubierta")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasConstrucciones.Galpon = reader.GetNullableDecimal(reader.GetOrdinal("superficie_galpon")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasConstrucciones.Semicubierta = reader.GetNullableDecimal(reader.GetOrdinal("superficie_semicubierta")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasOtras.Piscina = reader.GetNullableDecimal(reader.GetOrdinal("superficie_piscina")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasOtras.Deportiva = reader.GetNullableDecimal(reader.GetOrdinal("superficie_deportiva")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasOtras.Construccion = reader.GetNullableDecimal(reader.GetOrdinal("superficie_en_const")).GetValueOrDefault();
                                   superficies.RelevamientoMejorasOtras.Precaria = reader.GetNullableDecimal(reader.GetOrdinal("superficie_precaria")).GetValueOrDefault();
                                   readerStatus.Break();
                               });
                    }
                    catch (Exception ex)
                    {
                        _context.GetLogger().LogError($"GetSuperficiesByIdParcela({id}) - Superficies Relevamiento", ex);
                        return null;
                    }
                }
                #endregion
                return superficies;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"GetSuperficiesByIdParcela({id}) - General", ex);
                return null;
            }
        }

        public Dictionary<string, double> GetSuperficiesRuralesByIdParcela(long id)
        {
            try
            {
                Dictionary<string, double> supRural = new Dictionary<string, double>();


                var idUnidadTributaria = _context.UnidadesTributarias
                                         .Where(ut => ut.ParcelaID == id)
                                         .FirstOrDefault().UnidadTributariaId;


                var ddjjRepo = new DeclaracionJuradaRepository(_context);

                var DDJJVigente = ddjjRepo.GetDeclaracionesJuradas(idUnidadTributaria).FirstOrDefault();

                if (DDJJVigente == null)
                {
                    return supRural;
                }

                var Superficies = ddjjRepo.GetValSuperficies(DDJJVigente.IdDeclaracionJurada);

                return Superficies.GroupBy(s => s.Aptitud)
                                  .ToDictionary(g => g.Key.Descripcion,
                                                g => g.Sum(s => s.Superficie ?? 0) / 10_000);

            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"GetSuperficiesRuralesByIdParcela({id}) - Superficies Parcelas", ex);
                return null;
            }
        }

        public void RefreshVistaMaterializadaParcela()
        {
            //_context.CreateSQLQueryBuilder().RefreshMaterializedView("VW_PARCELAS_GRAF_ALFA");
        }

        public Dictionary<long, List<Objeto>> GetJurisdiccionesByDepartamentoParcela(long id)
        {
            long objetoJurisdiccionTipo = -666; // long.Parse(TiposObjetoAdministrativo.JURISDICCION);
            long objetoDepartamentoTipo = long.Parse(TiposObjetoAdministrativo.DEPARTAMENTO);
            var jurisdicciones = (from jurByParcela in _context.Objetos
                                  join deptoParcela in _context.Objetos on jurByParcela.ObjetoPadreId equals deptoParcela.FeatId
                                  join jurByDeptoParcela in _context.Objetos on deptoParcela.FeatId equals jurByDeptoParcela.ObjetoPadreId
                                  join ut in _context.UnidadesTributarias on jurByParcela.FeatId equals ut.JurisdiccionID
                                  where ut.ParcelaID == id && ut.FechaBaja == null &&
                                        jurByDeptoParcela.TipoObjetoId == objetoJurisdiccionTipo && jurByDeptoParcela.FechaBaja == null &&
                                        deptoParcela.TipoObjetoId == objetoDepartamentoTipo
                                  group jurByDeptoParcela by jurByParcela into gp
                                  select gp);

            return jurisdicciones.ToList().ToDictionary(j => j.Key.FeatId, j => j.Distinct().ToList());
        }

        public bool EsVigente(long id) => GetParcela(x => x.ParcelaID == id && x.FechaBaja == null, false, false) != null;

        public Parcela GetParcelaByUt(long idUnidadTributaria)
        {
            var query = (from ut in _context.UnidadesTributarias
                         join par in _context.Parcelas on ut.ParcelaID equals par.ParcelaID
                         where ut.UnidadTributariaId == idUnidadTributaria
                         select par).FirstOrDefault();

            return query;
        }

        public ICustomError DeleteGrafico(long idParcela, ParcelaGrafica grafico)
        {
            grafico.ParcelaID = null;
            return processGrafico(idParcela, grafico);
        }
        public ICustomError AddGrafico(long idParcela, ParcelaGrafica grafico)
        {
            grafico.ParcelaID = idParcela;
            return processGrafico(idParcela, grafico);
        }
        public Tuple<string, ICustomError> ValidateDestino(NomenclaturaValidable nomenclatura)
        {
            if (!long.TryParse(nomenclatura.Partida, out long partida))
            {
                return Tuple.Create<string, ICustomError>(null, new PartidaInvalida());
            }

            if (nomenclatura.IdTipoParcela == (long)TipoParcelaEnum.Urbana && partida != 0 ||
                nomenclatura.IdTipoParcela != (long)TipoParcelaEnum.Urbana && partida == 0)
            {
                return Tuple.Create<string, ICustomError>(null, new PartidaInvalidaParaParcela());
            }
            var nomenclaturaRepo = new NomenclaturaRepository(_context);
            return nomenclaturaRepo.ValidarDisponibilidad(nomenclatura);
        }
        public Objeto GetEjido(long id)
        {
            using (var qbuilder = _context.CreateSQLQueryBuilder())
            {
                long idEjido = qbuilder
                                    .AddFunctionTable($"obtener_ejido_by_id_parcela({id})", null)
                                    .AddFields("featid")
                                    .ExecuteQuery((reader, status) =>
                                    {
                                        status.Break();
                                        return reader.GetNullableInt64(0);
                                    })
                                    .SingleOrDefault() ?? 0;

                return _context.Objetos.Find(id);
            }
        }
        public ICustomError SaveOperacionAlfanumerica(OperacionAlfanumerica datos)
        {
            var regex = new Regex("^[0-9]{2}-[0-9]{3}-[1-9][0-9]{3}$");
            if(!string.IsNullOrEmpty(datos.NumeroPlano) && !regex.IsMatch(datos.NumeroPlano))
            {
                return new ErrorFormatoNumeroPlano();
            }
            void auditarParcela(Parcela parcela, string evento, string operacion, string datosAdicionales = null)
            {
                _context.Auditoria.Add(new Auditoria()
                {
                    Id_Objeto = parcela.ParcelaID,
                    Id_Evento = long.Parse(evento),
                    Id_Tipo_Operacion = long.Parse(operacion),
                    Fecha = parcela.FechaModificacion,
                    Datos_Adicionales = datosAdicionales,
                    Cantidad = 1,
                    Objeto = "Parcela",
                    Autorizado = "S",
                    Machine_Name = datos.MachineName,
                    Ip = datos.Ip,
                    Id_Usuario = parcela.UsuarioModificacionID

                });
            }
            void auditarUnidadTributaria(UnidadTributaria ut, string evento, string operacion)
            {
                _context.Auditoria.Add(new Auditoria()
                {
                    Id_Objeto = ut.UnidadTributariaId,
                    Id_Evento = long.Parse(evento),
                    Id_Tipo_Operacion = long.Parse(operacion),
                    Fecha = ut.FechaModificacion.Value,
                    Cantidad = 1,
                    Objeto = "UnidadTributaria",
                    Autorizado = "S",
                    Machine_Name = datos.MachineName,
                    Ip = datos.Ip,
                    Id_Usuario = ut.UsuarioModificacionID.Value
                });
            }

            DateTime now = DateTime.Now;
            var dominios = new List<Dominio>();
            var parcelasOrigen = new List<Parcela>();

            using (var trans = _context.Database.BeginTransaction())
            {
                try
                {
                    var query = from parcela in _context.Parcelas
                                where datos.ParcelasOrigen.Contains(parcela.ParcelaID)
                                select parcela;

                    if (datos.Operacion != long.Parse(TiposMensuras.Saneamiento) && datos.Operacion != long.Parse(TiposMensuras.PrescripcionAdquisitiva))
                    {
                        query = query
                                    .IncludeFilter(p => p.UnidadesTributarias.Where(ut => ut.FechaBaja == null))
                                    .IncludeFilter(p => p.UnidadesTributarias.Where(ut => ut.FechaBaja == null).Select(ut => ut.Dominios.Where(d => d.FechaBaja == null)))
                                    .IncludeFilter(p => p.UnidadesTributarias
                                                         .Where(ut => ut.FechaBaja == null)
                                                         .Select(ut => ut.Dominios
                                                                         .Where(d => d.FechaBaja == null)
                                                                         .Select(d => d.Titulares.Where(t => t.FechaBaja == null))));

                       
                        foreach (var origen in query.ToList())
                        {
                            parcelasOrigen.Add(origen);
                            dominios.AddRange(origen.UnidadesTributarias.SelectMany(ut => ut.Dominios));

                            origen.FechaBaja = origen.FechaModificacion = now;
                            origen.UsuarioBajaID = origen.UsuarioModificacionID = datos.IdUsuario;

                            origen.ExpedienteBaja = datos.NumeroPlano;
                            origen.FechaBajaExpediente = datos.FechaOperacion;

                            auditarParcela(origen, Eventos.BajaParcela, TiposOperacion.Baja);

                            try
                            {
                                var graficos = _context
                                                   .ParcelaGrafica
                                                   .Where(pg => pg.ParcelaID == origen.ParcelaID && pg.FechaBaja == null);

                                bool eliminaGraficos = false;
                                foreach (var grafico in graficos.ToList())
                                {
                                    eliminaGraficos = true;
                                    grafico.UsuarioBajaID = grafico.UsuarioModificacionID = origen.UsuarioBajaID;
                                    grafico.FechaBaja = grafico.FechaModificacion = origen.FechaBaja;
                                }
                                if (eliminaGraficos)
                                {
                                    auditarParcela(origen, Eventos.BajaParcelagrafica, TiposOperacion.Baja);
                                }
                            }
                            catch
                            {
                                trans.Rollback();
                                return new ErrorBajaGrafico();
                            }

                            foreach (var ut in origen.UnidadesTributarias)
                            {
                                ut.FechaBaja = ut.FechaModificacion = origen.FechaModificacion;
                                ut.UsuarioBajaID = ut.UsuarioModificacionID = origen.UsuarioModificacionID;
                                auditarUnidadTributaria(ut, Eventos.BajaUnidadesTributarias, TiposOperacion.Baja);
                                auditarParcela(origen, Eventos.BajaUnidadesTributarias, TiposOperacion.Baja, ut.CodigoProvincial);
                            }
                        }
                    }
                    else if (datos.Operacion == long.Parse(TiposMensuras.PrescripcionAdquisitiva))
                    {
                        parcelasOrigen = query.ToList();
                    }

                    var parcelasNuevas = new List<Parcela>();
                    for (int idx = 0; idx < datos.ParcelasDestino.Count; idx++)
                    {
                        parcelasNuevas.Add(_context.Parcelas.Add(generarParcela(datos.ParcelasDestino[idx], datos, dominios, now, idx)));
                    }
                    _context.SaveChanges();

                    if (datos.Operacion != long.Parse(TiposMensuras.Saneamiento))
                    {
                        var componentesNumeroPlano = datos.NumeroPlano.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                        long TEMP_ID_MENSURA = 0;
                        _context.Mensura.Add(new Mensura()
                        {
                            IdMensura = TEMP_ID_MENSURA,
                            Departamento = componentesNumeroPlano[0],
                            Numero = componentesNumeroPlano[1],
                            Anio = componentesNumeroPlano[2],
                            Descripcion = datos.NumeroPlano,
                            IdTipoMensura = datos.Operacion,
                            IdEstadoMensura = 1, //REGISTRADA
                            ParcelasMensuras = parcelasNuevas
                                                    .Select((n, idx) => new ParcelaMensura()
                                                    {
                                                        IdParcelaMensura = TEMP_ID_MENSURA,
                                                        FechaAlta = n.FechaAlta,
                                                        FechaModif = n.FechaModificacion,
                                                        IdUsuarioAlta = n.UsuarioAltaID,
                                                        IdUsuarioModif = n.UsuarioModificacionID,
                                                        Parcela = n,
                                                    }).ToList(),
                            FechaAlta = now,
                            FechaModif = now,
                            IdUsuarioAlta = datos.IdUsuario,
                            IdUsuarioModif = datos.IdUsuario
                        });
                    }

                    var operaciones = parcelasOrigen
                                        .SelectMany(po => parcelasNuevas
                                                                .Select(pn => new ParcelaOperacion()
                                                                {
                                                                    ParcelaDestinoID = pn.ParcelaID,
                                                                    ParcelaOrigenID = po.ParcelaID,
                                                                    TipoOperacionID = datos.Operacion,
                                                                    FechaOperacion = datos.FechaOperacion,
                                                                    FechaAlta = now,
                                                                    FechaModificacion = now,
                                                                    UsuarioAltaID = datos.IdUsuario,
                                                                    UsuarioModificacionID = datos.IdUsuario
                                                                }));

                    _context.ParcelaOperacion.AddRange(operaciones);

                    _context.SaveChanges();

                    foreach (var parcela in parcelasNuevas)
                    {
                        var ut = parcela.UnidadesTributarias.Single();
                        auditarParcela(parcela, Eventos.AltaParcela, TiposOperacion.Alta);
                        auditarParcela(parcela, Eventos.AltaUnidadesTributarias, TiposOperacion.Alta, ut.CodigoProvincial);
                        auditarUnidadTributaria(ut, Eventos.AltaUnidadesTributarias, TiposOperacion.Alta);
                    }

                    _context.SaveChanges();

                    trans.Commit();
                    return null;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    _context.GetLogger().LogError("ParcelaRepository-SaveOperacionAlfanumerica", ex);
                    return new ErrorOperacionParcelaria();
                }
            }
        }
        private Parcela generarParcela(ParcelaDestinoAlfanumerica parcelaDestino, OperacionAlfanumerica datos, IEnumerable<Dominio> dominios, DateTime fechaProcesamiento, int idx)
        {
            long idGrupo = (idx + 1) * -1;
            long nextIdDominio(int i) => (i + 1) * -1;
            long ID_ORIGEN_PARCELA = 2;
            return new Parcela
            {
                UnidadesTributarias = new[]
                {
                    new UnidadTributaria()
                    {
                        UnidadTributariaId = idGrupo,
                        CodigoProvincial = parcelaDestino.Partida,
                        TipoUnidadTributariaID = (int)TipoUnidadTributariaEnum.Comun,
                        FechaVigenciaDesde = datos.FechaVigencia,
                        FechaAlta = fechaProcesamiento,
                        UsuarioAltaID = datos.IdUsuario,
                        FechaModificacion = fechaProcesamiento,
                        UsuarioModificacionID = datos.IdUsuario,
                        Vigencia = datos.FechaVigencia,
                        Dominios = dominios.Where(d=>d.FechaBaja == null).Select((d,i)=>new Dominio
                        {
                            UnidadTributariaID = idGrupo,
                            DominioID = nextIdDominio(i),
                            Fecha = d.Fecha,
                            Inscripcion = d.Inscripcion,
                            TipoInscripcionID = d.TipoInscripcionID,
                            Titulares = (d.Titulares ?? new DominioTitular[0]).Where(t=>t.FechaBaja == null).Select(t=>new DominioTitular()
                            {
                                DominioID = nextIdDominio(i),
                                PersonaID = t.PersonaID,
                                TipoTitularidadID = t.TipoTitularidadID,
                                TipoPersonaID = t.TipoPersonaID,
                                PorcientoCopropiedad = t.PorcientoCopropiedad,
                                UsuarioAltaID = datos.IdUsuario,
                                FechaAlta = fechaProcesamiento,
                                UsuarioModificacionID = datos.IdUsuario,
                                FechaModificacion = fechaProcesamiento
                            }).ToList(),
                            IdUsuarioAlta = datos.IdUsuario,
                            FechaAlta = fechaProcesamiento,
                            IdUsuarioModif = datos.IdUsuario,
                            FechaModif = fechaProcesamiento
                        }).ToList()
                    }
                },
                Nomenclaturas = new[]
                {
                    new Nomenclatura
                    {
                        Nombre = parcelaDestino.Nomenclatura,
                        TipoNomenclaturaID = 0,
                        UsuarioAltaID = datos.IdUsuario,
                        FechaAlta = fechaProcesamiento,
                        UsuarioModificacionID = datos.IdUsuario,
                        FechaModificacion = fechaProcesamiento
                   }
                },
                ParcelaID = idGrupo,
                TipoParcelaID = parcelaDestino.IdTipoParcela,
                ClaseParcelaID = new ClaseParcelaRepository(_context).GetClaseParcelaByTipoMensura(datos.Operacion).ClaseParcelaID,
                EstadoParcelaID = parcelaDestino.IdEstadoParcela,
                OrigenParcelaID = ID_ORIGEN_PARCELA,
                ExpedienteAlta = datos.NumeroPlano,
                FechaAltaExpediente = datos.FechaOperacion,
                UsuarioAltaID = datos.IdUsuario,
                FechaAlta = fechaProcesamiento,
                UsuarioModificacionID = datos.IdUsuario,
                FechaModificacion = fechaProcesamiento
            };
        }
        private ICustomError processGrafico(long idParcela, ParcelaGrafica grafico)
        {
            var pgraf = _context.ParcelaGrafica
                                .SingleOrDefault(pg => pg.FechaBaja == null &&
                                                       pg.FeatID == grafico.FeatID);

            var palfa = GetParcela(p => p.FechaBaja == null && p.ParcelaID == idParcela, false, false);

            if (pgraf == null || palfa == null)
            { // se intenta trabajar con datos dados de baja
                return new DatosNoVigentes();
            }
            if (pgraf.ParcelaID != null && pgraf.ParcelaID != idParcela ||
                pgraf.ParcelaID == null && grafico.ParcelaID == null)
            { // se intenta trabajar con un gráfico asociado a otra parcela
                return new GraficoNoValidoParaAlfa();
            }

            pgraf.UsuarioModificacionID = palfa.UsuarioModificacionID = grafico._Id_Usuario;
            pgraf.FechaModificacion = palfa.FechaModificacion = DateTime.Now;
            pgraf.ParcelaID = grafico.ParcelaID;

            try
            {
                string evento = Eventos.ModificacionParcelaGrafica;
                string tipoOperacion = TiposOperacion.Modificacion;
                string msg = "Se modificó la Parcela Gráfica con exitosamente";

                _context.SaveChanges(new Auditoria(grafico._Id_Usuario, evento, msg,
                                                   grafico._Machine_Name, grafico._Ip,
                                                   "S", _context.Entry(pgraf).OriginalValues?.ToObject(), pgraf,
                                                   "ParcelaGrafica", 1, tipoOperacion));
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"ParcelaRepository-ToggleGrafico({idParcela},{grafico.FeatID})", ex);
                throw;
            }
            try
            {
                RefreshVistaMaterializadaParcela();
                return null;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"ParcelaRepository-RefreshVistaMaterializadaParcela", ex);
                return new ErrorActualizacionVistaMaterializada();
            }
        }

        public int GetSuperficieGrafica(long parcelaId)
        {
            try
            {
                using (var qbuilder = _context.CreateSQLQueryBuilder())
                {
                    if (!long.TryParse(_context.ParametrosGenerales.SingleOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA")?.Valor, out long idCompParcela))
                    {
                        throw new Exception("No se encuentra el parámetro ID_COMPONENTE_PARCELA");
                    }
                    var cmp = _context.Componente.Include(c => c.Atributos).SingleOrDefault(c => c.ComponenteId == idCompParcela);
                    if (cmp == null)
                    {
                        throw new Exception("No se encontró el componente para la parcela.");
                    }
                    Atributo attrCampoClave;
                    Atributo attrCampoGeometry;
                    try
                    {
                        attrCampoClave = cmp.Atributos.GetAtributoClave();
                        attrCampoGeometry = cmp.Atributos.GetAtributoGeometry();
                    }
                    catch (ApplicationException appEx)
                    {
                        _context.GetLogger().LogError("Componente (id: " + cmp.ComponenteId + ") mal configurado.", appEx);
                        throw;
                    }
                    var geometryArea = qbuilder.CreateGeometryFieldBuilder(attrCampoGeometry, "par");
                    geometryArea.AreaSqrMeters();
                    int superficieGrafica = 0;
                    qbuilder
                        .AddTable(cmp.TablaGrafica ?? cmp.Tabla, "par")
                        .AddFilter(attrCampoClave.Campo, attrCampoClave.GetFormattedValue(parcelaId), Common.Enums.SQLOperators.EqualsTo)
                        .AddGeometryField(geometryArea, "area")
                        .ExecuteQuery((reader, status) =>
                        {
                            superficieGrafica = (int)reader.GetDecimal(reader.GetOrdinal("area"));
                        });
                    return superficieGrafica;
                }
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("ParcelaRepository - GetSuperficieGrafica", ex);
                throw;
            }
        }
    }
}
