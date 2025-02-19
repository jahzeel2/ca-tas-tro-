using System;
using System.Web.Http;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;
using GeoSit.Data.DAL.Common;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Web.Api.Ploteo;
using GeoSit.Web.Api.Solr;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Web.Api.Common;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using Newtonsoft.Json;
using System.Diagnostics;

namespace GeoSit.Web.Api.Controllers
{
    public class ParcelaController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public ParcelaController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IHttpActionResult GetParcelaById(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaById(id));
        }

        public IHttpActionResult Get(int id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaById(id));
        }

        public IHttpActionResult GetParcelaMantenedor(int id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaMantenedorById(id));
        }

        public IHttpActionResult GetZonificacion(long idParcela, bool esHistorico = false)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetZonificacion(idParcela, esHistorico));
        }

        public IHttpActionResult GetAtributosZonificacion(long idParcela)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetAtributosZonificacion(idParcela));
        }

        public IHttpActionResult GetParcelasOrigen(long idParcelaDestino)
        {
            return Ok(_unitOfWork.ParcelaOperacionRepository.GetParcelasOrigenOperacion(idParcelaDestino));
        }

        public IHttpActionResult GetParcelaOperacionesOrigen(long idParcelaOperacion)
        {
            return Ok(_unitOfWork.ParcelaOperacionRepository.GetParcelaOperacionesOrigen(idParcelaOperacion));
        }

        public IHttpActionResult GetParcelaOperacionesDestino(long idParcelaOperacion)
        {
            return Ok(_unitOfWork.ParcelaOperacionRepository.GetParcelaOperacionesDestino(idParcelaOperacion));
        }

        public IHttpActionResult GetParcelaDatos(long idParcelaOperacion)
        {
            return Ok(_unitOfWork.ParcelaOperacionRepository.GetParcelaDatos(idParcelaOperacion));
        }

        public IHttpActionResult GetParcelaValuacionZonas()
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaValuacionZonas());
        }

        /*
        public IHttpActionResult GetParcelaValuacionZona(long idAtributoZona)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaValuacionZona(idAtributoZona));
        }
        */
        public IHttpActionResult GetParcelaByUt(long idUnidadTributaria)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaByUt(idUnidadTributaria));
        }

        [Route("api/parcela/{id}/esVigente")]
        [HttpGet]
        public IHttpActionResult ParcelaVigente(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.EsVigente(id));
        }

        public IHttpActionResult Post(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            try
            {
                unidadMantenimientoParcelario.OperacionesUnidadTributaria.AnalyzeOperations("UnidadTributariaId");
                unidadMantenimientoParcelario.OperacionesParcelaOrigen.AnalyzeOperations("ParcelaOperacionID");
                unidadMantenimientoParcelario.OperacionesParcelaDocumento.AnalyzeOperations("ParcelaDocumentoID");
                unidadMantenimientoParcelario.OperacionesUnidadTributariaDocumento.AnalyzeOperations("UnidadTributariaDocID");
                unidadMantenimientoParcelario.OperacionesNomenclatura.AnalyzeOperations("NomenclaturaID");
                unidadMantenimientoParcelario.OperacionesUnidadTributariaPersona.AnalyzeOperations("PersonaID");
                unidadMantenimientoParcelario.OperacionesDominio.AnalyzeOperations("DominioID");
                unidadMantenimientoParcelario.OperacionesDominioTitular.AnalyzeOperations("PersonaID");
                unidadMantenimientoParcelario.OperacionesDesignaciones.AnalyzeOperations("IdDesignacion");
                unidadMantenimientoParcelario.OperacionesVIR.AnalyzeOperations("InmuebleId");

                Parcela parcela = null;

                var auditoriasParcela = new List<Auditoria>();
                var auditoriasUT = new Dictionary<UnidadTributaria, List<Auditoria>>();

                long getOperacion(Operation operacion)
                {
                    long idTipoOperacion = Convert.ToInt64(TiposOperacion.Alta);
                    switch (operacion)
                    {
                        case Operation.Update:
                            idTipoOperacion = Convert.ToInt64(TiposOperacion.Modificacion);
                            break;
                        case Operation.Remove:
                            idTipoOperacion = Convert.ToInt64(TiposOperacion.Baja);
                            break;
                    }
                    return idTipoOperacion;
                }
                Auditoria auditarParcela(string idEvento, Operation operacion, string datosAdicionales)
                {
                    var auditoria = new Auditoria()
                    {
                        Id_Objeto = parcela.ParcelaID,
                        Id_Evento = long.Parse(idEvento),
                        Id_Tipo_Operacion = getOperacion(operacion),
                        Datos_Adicionales = datosAdicionales,
                        Cantidad = 1,
                        Objeto = "Parcela",
                        Autorizado = Autorizado.Si,
                        Machine_Name = parcela._Machine_Name,
                        Ip = parcela._Ip,
                        Id_Usuario = parcela.UsuarioModificacionID,
                    };
                    auditoriasParcela.Add(auditoria);
                    return auditoria;
                }
                void auditarUnidadTributaria(UnidadTributaria ut, string idEvento, Operation operacion, string datosAdicionales)
                {
                    if (!auditoriasUT.Any(kvp => kvp.Key.UnidadTributariaId == ut.UnidadTributariaId))
                    {
                        auditoriasUT.Add(ut, new List<Auditoria>());
                    }

                    auditoriasUT[auditoriasUT.Single(kvp => kvp.Key.UnidadTributariaId == ut.UnidadTributariaId).Key].Add(new Auditoria()
                    {
                        Id_Evento = long.Parse(idEvento),
                        Id_Tipo_Operacion = getOperacion(operacion),
                        Datos_Adicionales = datosAdicionales,
                        Cantidad = 1,
                        Objeto = "UnidadTributaria",
                        Autorizado = Autorizado.Si,
                        Machine_Name = parcela._Machine_Name,
                        Ip = parcela._Ip,
                        Id_Usuario = parcela.UsuarioModificacionID,
                    });
                }

                if (unidadMantenimientoParcelario.OperacionesParcela.Any())
                {
                    parcela = unidadMantenimientoParcelario.OperacionesParcela.First().Item;
                    //unidadesTributariasModificadas = parcela.UnidadesTributarias ?? _unitOfWork.UnidadTributariaRepository.GetUnidadesTributarias(parcela.ParcelaID);
                    //if (unidadesTributariasModificadas != null)
                    //{
                    //    var idCollection = new HashSet<long>(unidadMantenimientoParcelario.OperacionesUnidadTributaria.Select(x => x.Item.UnidadTributariaId));
                    //    unidadesTributariasModificadas = unidadesTributariasModificadas.Where(x => !x.FechaBaja.HasValue && !idCollection.Contains(x.UnidadTributariaId)).ToList();
                    //}
                }
                foreach (var operacion in unidadMantenimientoParcelario.OperacionesParcela)
                {
                    if (operacion.Operation == Operation.Update)
                    {
                        _unitOfWork.ParcelaRepository.UpdateParcela(operacion.Item);
                        auditarParcela(Eventos.ModificarMantenedorParcelario, Operation.Update, "Se han guardado los datos de la parcela");
                    }
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesParcelaOrigen)
                {
                    string partida = operacion.Item.ParcelaOrigen.UnidadesTributarias.Single().CodigoProvincial;
                    string evento = Eventos.AltaParcelaOrigen;
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    operacion.Item.ParcelaOrigen = operacion.Item.ParcelaDestino = null;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.ParcelaOperacionRepository.InsertParcelaOperacion(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificacionParcelaOrigen;
                            _unitOfWork.ParcelaOperacionRepository.EditParcelaOperacion(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaParcelaOrigen;
                            _unitOfWork.ParcelaOperacionRepository.DeleteParcelaOperacion(operacion.Item);
                            break;
                    }
                    auditarParcela(evento, operacion.Operation, $"Partida: {partida}");
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesUnidadTributaria)
                {
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    operacion.Item.TipoUnidadTributaria = null;
                    operacion.Item.CodigoProvincial = operacion.Item.CodigoProvincial.ToUpper();

                    string evento = Eventos.AltaMantenedorUnidadTributaria;

                    Operation finalOp = parcela.FechaBaja.HasValue && operacion.Operation == Operation.Add
                                                ? Operation.None
                                                : operacion.Operation;

                    switch (finalOp)
                    {
                        case Operation.Add:
                            operacion.Item.ParcelaID = parcela.ParcelaID;
                            _unitOfWork.UnidadTributariaRepository.InsertUnidadTributaria(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarMantenedorUnidadTributaria;
                            _unitOfWork.UnidadTributariaRepository.EditUnidadTributaria(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaMantenedorUnidadTributaria;
                            _unitOfWork.UnidadTributariaRepository.DeleteUnidadTributaria(operacion.Item);
                            break;
                    }
                    auditarParcela(evento, operacion.Operation, operacion.Item.CodigoProvincial);
                    auditarUnidadTributaria(operacion.Item, evento, operacion.Operation, null);
                }

                if (parcela.FechaBaja.HasValue)
                {
                    foreach (var ut in _unitOfWork.UnidadTributariaRepository.GetUnidadesTributariasByParcela(parcela.ParcelaID))
                    {
                        ut.UsuarioBajaID = ut.UsuarioModificacionID = parcela.UsuarioBajaID;
                        ut.FechaBaja = ut.FechaModificacion = parcela.FechaBaja;
                        //ut.TipoUnidadTributaria = null;
                        string evento = Eventos.BajaMantenedorUnidadTributaria;
                        Operation operacion = Operation.Remove;
                        auditarParcela(evento, operacion, ut.CodigoProvincial);
                        auditarUnidadTributaria(ut, evento, operacion, null);
                    }
                }


                foreach (var operacion in unidadMantenimientoParcelario.OperacionesParcelaDocumento)
                {
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    string evento = Eventos.AltaDocumentosParcela;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.ParcelaDocumentoRepository.InsertParcelaDocumento(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaDocumentosParcela;
                            _unitOfWork.ParcelaDocumentoRepository.DeleteParcelaDocumento(operacion.Item);
                            break;
                    }

                    var doc = _unitOfWork.DocumentoRepository.GetDocumentoById(operacion.Item.DocumentoID);
                    auditarParcela(evento, operacion.Operation, doc.nombre_archivo);
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesUnidadTributariaDocumento)
                {
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    string evento = Eventos.AltaDocumentosUnidadTributaria;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.UnidadTributariaDocumentoRepository.InsertUnidadTributariaDocumento(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaDocumentosUnidadTributaria;
                            _unitOfWork.UnidadTributariaDocumentoRepository.RemoveUnidadTributariaDocumento(operacion.Item);
                            break;
                    }

                    var doc = _unitOfWork.DocumentoRepository.GetDocumentoById(operacion.Item.DocumentoID);
                    auditarUnidadTributaria(new UnidadTributaria { UnidadTributariaId = operacion.Item.UnidadTributariaID }, evento, operacion.Operation, doc.nombre_archivo);
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesNomenclatura)
                {
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    string evento = Eventos.AltaNomenclatura;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.NomenclaturaRepository.InsertNomenclatura(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarNomenclatura;
                            _unitOfWork.NomenclaturaRepository.UpdateNomenclatura(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaNomenclatura;
                            _unitOfWork.NomenclaturaRepository.DeleteNomenclatura(operacion.Item);
                            break;
                    }

                    auditarParcela(evento, operacion.Operation, operacion.Item.Nombre);
                }

                double ms = 0d;
                foreach (var operacion in unidadMantenimientoParcelario.OperacionesDominio)
                {
                    operacion.Item.IdUsuarioModif = parcela.UsuarioModificacionID;

                    //me doy asco, deberían matarme y tirarme a la hoguera, pero a esta altura.... es lo que hay
                    //ante cualquier duda, ajo y agua. Ernesto.-
                    operacion.Item.FechaModif = DateTime.Now.AddMilliseconds(ms++); 
                    string evento = Eventos.AltaDominio;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.DominioRepository.InsertDominio(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarDominio;
                            _unitOfWork.DominioRepository.UpdateDominio(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaDominio;
                            _unitOfWork.DominioRepository.DeleteDominio(operacion.Item);
                            break;
                    }
                    auditarUnidadTributaria(new UnidadTributaria { UnidadTributariaId = operacion.Item.UnidadTributariaID }, evento, operacion.Operation, operacion.Item.Inscripcion);
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesDominioTitular)
                {
                    operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                    string evento = Eventos.AltaTitular;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.DominioTitularRepository.InsertDominioTitular(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarTitular;
                            _unitOfWork.DominioTitularRepository.UpdateDominioTitular(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaTitular;
                            _unitOfWork.DominioTitularRepository.DeleteDominioTitular(operacion.Item);
                            break;
                    }
                    var titular = _unitOfWork.PersonaRepository.GetPersonaDatos(operacion.Item.PersonaID);
                    var dom = unidadMantenimientoParcelario.OperacionesDominio.FirstOrDefault(d => d.Item.DominioID == operacion.Item.DominioID)?.Item
                              ?? _unitOfWork.DominioRepository.GetDominioById(operacion.Item.DominioID);

                    auditarUnidadTributaria(new UnidadTributaria { UnidadTributariaId = dom.UnidadTributariaID }, evento, operacion.Operation, $"{titular.NombreCompleto}");
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesDesignaciones)
                {
                    operacion.Item.IdUsuarioModif = parcela.UsuarioModificacionID;
                    string evento = Eventos.AltaDesignacion;
                    string tipo = operacion.Item.TipoDesignador?.Nombre;
                    operacion.Item.TipoDesignador = null;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.DesignacionRepository.InsertDesignacion(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarDesignacion;
                            _unitOfWork.DesignacionRepository.UpdateDesignacion(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaDesignacion;
                            _unitOfWork.DesignacionRepository.DeleteDesignacion(operacion.Item);
                            break;
                    }
                    auditarParcela(evento, operacion.Operation, tipo);
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesVIR)
                {
                    string evento = Eventos.ModificacionVIR;
                    VIRInmueble inmueble;
                    switch (operacion.Operation)
                    {
                        case Operation.Update:
                            inmueble = _unitOfWork.VIRInmuebleRepository.SaveVIRInmueble(operacion.Item);
                            break;
                        default:
                            throw new InvalidOperationException("Operación no valida para datos VIR.");
                    }
                    var auditoria = auditarParcela(evento, operacion.Operation, $"Se modifica el inmueble VIR {operacion.Item.Partida}");

                    var entityEntry = _unitOfWork.GetDbEntityEntry(inmueble);
                    auditoria.Objeto_Origen = JsonConvert.SerializeObject(entityEntry.OriginalValues.ToObject());
                    auditoria.Objeto_Modif = JsonConvert.SerializeObject(entityEntry.CurrentValues.ToObject());
                }

                _unitOfWork.Save();

                DateTime ahora = DateTime.Now;
                foreach (var auditoria in auditoriasParcela)
                {
                    auditoria.Fecha = ahora;
                    _unitOfWork.AuditoriaRepository.InsertAuditoria(auditoria);
                }
                foreach (var kvp in auditoriasUT)
                {
                    foreach (var auditoria in kvp.Value)
                    {
                        auditoria.Id_Objeto = kvp.Key.UnidadTributariaId;
                        auditoria.Fecha = ahora;
                        _unitOfWork.AuditoriaRepository.InsertAuditoria(auditoria);
                    }
                }
                _unitOfWork.Save();

                return Ok(parcela.FechaBaja == null);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Parcela/Post", ex);
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult GetPlanosMensuraByIdParcela(long id)
        {
            return Ok(_unitOfWork.ParcelaDocumentoRepository.GetPlanoMensura(id));
        }

        [Route("api/parcela/{id}/superficies/")]
        [HttpGet]
        public IHttpActionResult GetSuperficiesByIdParcela(long id, bool esHistorico = false)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetSuperficiesByIdParcela(id, esHistorico));
        }

        [Route("api/parcela/{id}/superficiesRural/")]
        [HttpGet]
        public IHttpActionResult GetSuperficiesRuralesByIdParcela(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetSuperficiesRuralesByIdParcela(id));
        }

        [Route("api/parcela/{id}/superficieGrafica/")]
        [HttpGet]
        public IHttpActionResult GetSuperficieGrafica(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetSuperficieGrafica(id));
        }

        [Route("api/parcela/{id}/estampilla")]
        [HttpGet]
        public IHttpActionResult GetParcelaEstampilla(long id)
        {
            ModPlot modPlot = new ModPlot(_unitOfWork.PlantillaRepository, _unitOfWork.LayerGrafRepository, _unitOfWork.PlantillaFondoRepository, _unitOfWork.HojaRepository, _unitOfWork.NorteRepository, _unitOfWork.ParcelaPlotRepository, _unitOfWork.CuadraPlotRepository, _unitOfWork.ManzanaPlotRepository, _unitOfWork.CallePlotRepository, _unitOfWork.ParametroRepository, _unitOfWork.ImagenSatelitalRepository, _unitOfWork.ExpansionPlotRepository, _unitOfWork.TipoPlanoRepository, _unitOfWork.PartidoRepository, _unitOfWork.CensoRepository,
                _unitOfWork.PloteoFrecuenteRepository, _unitOfWork.PloteoFrecuenteEspecialRepository, _unitOfWork.PlantillaViewportRepository, _unitOfWork.TipoViewportRepository, _unitOfWork.LayerViewportReposirory, _unitOfWork.AtributoRepository, _unitOfWork.ComponenteRepository);
            return Ok(modPlot.GetEstampilla(id));
        }

        [Route("api/parcela/{id}/simple")]
        [HttpGet]
        public IHttpActionResult GetParcelaSimple(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaById(id, false));
        }

        [Route("api/parcela/{id}/utshistoricas")]
        [HttpGet]
        public IHttpActionResult GetParcelaUTSHistoricas(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetParcelaById(id, utsHistoricas: true));
        }

        public IHttpActionResult GetJurisdiccionesByDepartamentoParcela(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetJurisdiccionesByDepartamentoParcela(id));
        }

        // ------------------------------------------------------------------------------------

        private long GetTipoOperacion(Operation operacion)
        {
            long idTipoOperacion = Convert.ToInt64(TiposOperacion.Alta);
            switch (operacion)
            {
                case Operation.Update:
                    idTipoOperacion = Convert.ToInt64(TiposOperacion.Modificacion);
                    break;
                case Operation.Remove:
                    idTipoOperacion = Convert.ToInt64(TiposOperacion.Baja);
                    break;
                default:
                    break;
            }
            return idTipoOperacion;
        }

        private Auditoria AuditarParcela(string idEvento, Operation operacion, string datosAdicionales, Parcela parcela, List<Auditoria> auditoriasParcela)
        {
            var auditoria = new Auditoria()
            {
                Id_Objeto = parcela.ParcelaID,
                Id_Evento = long.Parse(idEvento),
                Id_Tipo_Operacion = GetTipoOperacion(operacion),
                Datos_Adicionales = datosAdicionales,
                Cantidad = 1,
                Objeto = "Parcela",
                Autorizado = Autorizado.Si,
                Machine_Name = parcela._Machine_Name,
                Ip = parcela._Ip,
                Id_Usuario = parcela.UsuarioModificacionID,
            };
            auditoriasParcela.Add(auditoria);
            return auditoria;
        }

        private void AuditarUnidadTributaria(UnidadTributaria ut, string idEvento, Operation operacion, string datosAdicionales, Dictionary<UnidadTributaria, List<Auditoria>> auditoriasUT)
        {
            if (!auditoriasUT.TryGetValue(ut, out var auditorias))
            {
                auditorias = new List<Auditoria>();
                auditoriasUT[ut] = auditorias;
            }
            auditorias.Add(new Auditoria()
            {
                Id_Evento = long.Parse(idEvento),
                Id_Tipo_Operacion = GetTipoOperacion(operacion),
                Datos_Adicionales = datosAdicionales,
                Cantidad = 1,
                Objeto = "UnidadTributaria",
                Autorizado = Autorizado.Si,
                Machine_Name = ut._Machine_Name,
                Ip = ut._Ip,
                Id_Usuario = (long)ut.UsuarioModificacionID,
            });
        }

        private Parcela GetOperacionParcela(UnidadMantenimientoParcelario unidadMantenimientoParcelario, Parcela parcela, List<Auditoria> auditoriasParcela)
        {
            foreach (var operacion in unidadMantenimientoParcelario.OperacionesParcela)
            {
                switch (operacion.Operation)
                {
                    case Operation.Add:
                        parcela = _unitOfWork.ParcelaRepository.InsertParcela(operacion.Item);
                        AuditarParcela(Eventos.AltaParcela, Operation.Add, "Se ha dado de alta una nueva parcela", parcela, auditoriasParcela);
                        break;
                    case Operation.Update:
                        _unitOfWork.ParcelaRepository.UpdateParcela(operacion.Item);
                        AuditarParcela(Eventos.ModificarMantenedorParcelario, Operation.Update, "Se ha actualizado la parcela", parcela, auditoriasParcela);
                        break;
                    case Operation.Remove:
                        operacion.Item.FechaBaja = parcela.FechaBaja;
                        _unitOfWork.ParcelaRepository.DeleteParcela(operacion.Item);
                        AuditarParcela(Eventos.EliminarParcelaOrigen, Operation.Remove, "Se ha eliminado la parcela", parcela, auditoriasParcela);
                        break;
                }
            }
            return parcela;
        }

        private void GetOperacionNomenclatura(UnidadMantenimientoParcelario unidadMantenimientoParcelario, Parcela parcela, List<Auditoria> auditoriasParcela)
        {
            foreach (var operacion in unidadMantenimientoParcelario.OperacionesNomenclatura)
            {
                operacion.Item.UsuarioModificacionID = parcela.UsuarioModificacionID;
                string evento = Eventos.AltaNomenclatura;
                switch (operacion.Operation)
                {
                    case Operation.Add:
                        _unitOfWork.NomenclaturaRepository.InsertNomenclatura(operacion.Item);
                        break;
                    case Operation.Update:
                        evento = Eventos.ModificarNomenclatura;
                        _unitOfWork.NomenclaturaRepository.UpdateNombreNomenclatura(operacion.Item);
                        break;
                    case Operation.Remove:
                        evento = Eventos.BajaNomenclatura;
                        _unitOfWork.NomenclaturaRepository.DeleteNomenclatura(operacion.Item);
                        break;
                }
                AuditarParcela(evento, operacion.Operation, operacion.Item.Nombre, parcela, auditoriasParcela);
            }
        }

        private void RegistrarAuditoriasParcela(List<Auditoria> auditoriasParcela)
        {
            foreach (var auditoria in auditoriasParcela)
            {
                auditoria.Fecha = DateTime.Now;

                _unitOfWork.AuditoriaRepository.InsertAuditoria(auditoria);

            }
            _unitOfWork.Save();
        }

        private Parcela GetPrimerParcelaOperacion(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            if (unidadMantenimientoParcelario.OperacionesParcela.Any())
            {
                return unidadMantenimientoParcelario.OperacionesParcela.First().Item;
            }
            return null;
        }

        public IHttpActionResult UpdateParcela(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            try
            {
                unidadMantenimientoParcelario.OperacionesNomenclatura.AnalyzeOperations("NomenclaturaID");
                Parcela parcela = null;
                var auditoriasParcela = new List<Auditoria>();
                parcela = GetPrimerParcelaOperacion(unidadMantenimientoParcelario);
                GetOperacionParcela(unidadMantenimientoParcelario, parcela, auditoriasParcela);
                GetOperacionNomenclatura(unidadMantenimientoParcelario, parcela, auditoriasParcela);
                _unitOfWork.Save();
                RegistrarAuditoriasParcela(auditoriasParcela);
                if (unidadMantenimientoParcelario.OperacionesUnidadTributaria.Any()) PostUnidadTributaria(unidadMantenimientoParcelario);
                return Ok(parcela.FechaBaja == null);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Parcela/UpdateParcela", ex);
                return InternalServerError(ex);
            }
        }

        public IHttpActionResult PostUnidadTributaria(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            try
            {
                unidadMantenimientoParcelario.OperacionesUnidadTributaria.AnalyzeOperations("UnidadTributariaId");
                unidadMantenimientoParcelario.OperacionesDominio.AnalyzeOperations("DominioID");
                unidadMantenimientoParcelario.OperacionesDominioTitular.AnalyzeOperations("PersonaID");

                UnidadTributaria unidadTributaria = null;
                var auditoriasUT = new Dictionary<UnidadTributaria, List<Auditoria>>();

                if (unidadMantenimientoParcelario.OperacionesUnidadTributaria.Any())
                {
                    unidadTributaria = unidadMantenimientoParcelario.OperacionesUnidadTributaria.First().Item;
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesUnidadTributaria)
                {
                    operacion.Item.UsuarioModificacionID = unidadTributaria.UsuarioModificacionID;
                    operacion.Item.TipoUnidadTributaria = null;
                    operacion.Item.CodigoProvincial = operacion.Item.CodigoProvincial.ToUpper();

                    string evento = Eventos.AltaMantenedorUnidadTributaria;

                    Operation finalOp = unidadTributaria.FechaBaja.HasValue && operacion.Operation == Operation.Add
                                                ? Operation.None
                                                : operacion.Operation;
                    switch (finalOp)
                    {
                        case Operation.Add:
                            operacion.Item.ParcelaID = unidadTributaria.ParcelaID;
                            _unitOfWork.UnidadTributariaRepository.InsertUnidadTributaria(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarMantenedorUnidadTributaria;
                            _unitOfWork.UnidadTributariaRepository.EditUnidadTributaria(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaMantenedorUnidadTributaria;
                            _unitOfWork.UnidadTributariaRepository.DeleteUnidadTributaria(operacion.Item);
                            break;
                    }

                    AuditarUnidadTributaria(operacion.Item, evento, operacion.Operation, null, auditoriasUT);
                }

                if (unidadTributaria.FechaBaja.HasValue)
                {
                    string evento = Eventos.BajaMantenedorUnidadTributaria;
                    AuditarUnidadTributaria(unidadTributaria, evento, Operation.Remove, null, auditoriasUT);
                }

                double ms = 0d;
                foreach (var operacion in unidadMantenimientoParcelario.OperacionesDominio)
                {
                    operacion.Item.IdUsuarioModif = (long)unidadTributaria.UsuarioModificacionID;
                    //me doy asco, deberían matarme y tirarme a la hoguera, pero a esta altura.... es lo que hay
                    //ante cualquier duda, ajo y agua. Ernesto.-
                    operacion.Item.FechaModif = DateTime.Now.AddMilliseconds(ms++);
                    string evento = Eventos.AltaDominio;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.DominioRepository.InsertDominio(operacion.Item);
                            break;

                        case Operation.Update:
                            evento = Eventos.ModificarDominio;
                            _unitOfWork.DominioRepository.UpdateDominio(operacion.Item);
                            break;

                        case Operation.Remove:
                            evento = Eventos.BajaDominio;
                            _unitOfWork.DominioRepository.DeleteDominio(operacion.Item);
                            break;
                    }
                    //auditarUnidadTributaria(new UnidadTributaria { UnidadTributariaId = operacion.Item.UnidadTributariaID }, evento, operacion.Operation, operacion.Item.Inscripcion);
                    AuditarUnidadTributaria(unidadTributaria, evento, operacion.Operation, operacion.Item.Inscripcion, auditoriasUT);
                }

                foreach (var operacion in unidadMantenimientoParcelario.OperacionesDominioTitular)
                {
                    operacion.Item.UsuarioModificacionID = (long)unidadTributaria.UsuarioModificacionID;
                    string evento = Eventos.AltaTitular;
                    switch (operacion.Operation)
                    {
                        case Operation.Add:
                            _unitOfWork.DominioTitularRepository.InsertDominioTitular(operacion.Item);
                            break;
                        case Operation.Update:
                            evento = Eventos.ModificarTitular;
                            _unitOfWork.DominioTitularRepository.UpdateDominioTitular(operacion.Item);
                            break;
                        case Operation.Remove:
                            evento = Eventos.BajaTitular;
                            _unitOfWork.DominioTitularRepository.DeleteDominioTitular(operacion.Item);
                            break;
                    }

                    var titular = _unitOfWork.PersonaRepository.GetPersonaDatos(operacion.Item.PersonaID);
                    var dom = unidadMantenimientoParcelario.OperacionesDominio
                                    .FirstOrDefault(d => d.Item.DominioID == operacion.Item.DominioID)?.Item
                                ?? _unitOfWork.DominioRepository.GetDominioById(operacion.Item.DominioID);

                    AuditarUnidadTributaria(unidadTributaria, evento, operacion.Operation, $"{titular.NombreCompleto}", auditoriasUT);
                    //auditarUnidadTributaria(new UnidadTributaria { UnidadTributariaId = dom.UnidadTributariaID }, evento, operacion.Operation, $"{titular.NombreCompleto}");
                }

                _unitOfWork.Save();
                DateTime ahora = DateTime.Now;
                foreach (var kvp in auditoriasUT)
                {
                    foreach (var auditoria in kvp.Value)
                    {
                        auditoria.Id_Objeto = kvp.Key.UnidadTributariaId;
                        auditoria.Fecha = ahora;
                        _unitOfWork.AuditoriaRepository.InsertAuditoria(auditoria);
                    }
                }
                _unitOfWork.Save();
                return Ok(unidadTributaria.FechaBaja == null);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Error genérico en PostUnidadTributaria", ex);
                return InternalServerError(ex);
            }

        }

        public Parcela AddNuevaParcela(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            try
            {
                Parcela parcela = null;
                var auditoriasParcela = new List<Auditoria>();
                parcela = GetPrimerParcelaOperacion(unidadMantenimientoParcelario);
                parcela = GetOperacionParcela(unidadMantenimientoParcelario, parcela, auditoriasParcela);
                RegistrarAuditoriasParcela(auditoriasParcela);
                return parcela;
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Parcela/AltaParcela", ex);
                return null;
            }
        }

        public IHttpActionResult AddNuevaNomenclatura(UnidadMantenimientoParcelario unidadMantenimientoParcelario)
        {
            try
            {
                unidadMantenimientoParcelario.OperacionesNomenclatura.AnalyzeOperations("NomenclaturaID");
                Parcela parcela = null;
                var auditoriasParcela = new List<Auditoria>();
                parcela = GetPrimerParcelaOperacion(unidadMantenimientoParcelario);
                GetOperacionNomenclatura(unidadMantenimientoParcelario, parcela, auditoriasParcela);
                _unitOfWork.Save();
                RegistrarAuditoriasParcela(auditoriasParcela);
                return Ok();
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Parcela/AddNuevaNomenclatura", ex);
                return InternalServerError(ex);
            }
        }
    }
}
