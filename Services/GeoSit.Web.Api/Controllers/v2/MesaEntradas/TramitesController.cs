using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Data.BusinessEntities.ValidacionesDB;
using GeoSit.Data.BusinessEntities.ValidacionesDB.Enums;
using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Contexts;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using static GeoSit.Data.DAL.Repositories.MesaEntradasRepository;

namespace GeoSit.Web.Api.Controllers.v2.MesaEntradas
{
    [RoutePrefix("api/v2/mesaentradas")]
    public class TramitesController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;
        public TramitesController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("tramites/{id}")]
        public IHttpActionResult GetById(int id, bool incluirEntradas = false)
        {
            var tramite = _unitOfWork.MesaEntradasRepository.GetTramite(id, incluirEntradas);
            return Ok(tramite);
        }

        [HttpGet]
        [Route("tramites/{id}/operador")]
        public IHttpActionResult GetOperadorByTramite(int id)
        {
            var operador = _unitOfWork.MesaEntradasRepository.GetOperadorTramite(id);
            return Ok(operador);
        }

        [HttpPost]
        [Route("tramites/propios")]
        public IHttpActionResult RecuperarTramitesPropios(long idUsuario, DataTableParameters parametros)
        {
            return recuperarTramites(Grilla.Propios, parametros, idUsuario);
        }

        [HttpGet]
        [Route("tramites/{idTramite}/accionesdisponibles")]
        public IHttpActionResult GetAvailableActionsTramite(long idUsuario, int idTramite, long idTipoTramite, bool soloLectura)
        {
            var acciones = _unitOfWork.MesaEntradasRepository.RecuperarAccionesTramite(idTramite, idTipoTramite, idUsuario, soloLectura);
            return Ok(acciones);
        }
        
        [HttpPost]
        [Route("tramites/propios/accionesdisponibles")]
        public IHttpActionResult GetAvailableActionsTramitesPropios(long idUsuario, int[] tramites)
        {
            return recuperarAcciones(Grilla.Propios, tramites, idUsuario);
        }

        [HttpPost]
        [Route("tramites/propios/acciones/{accion}/disponible")]
        public IHttpActionResult GetValidActionTramitesPropios(long accion, int[] tramites, long idUsuario)
        {
            return esAccionValida(Grilla.Propios, accion, tramites, idUsuario);
        }

        [HttpPost]
        [Route("tramites/sector")]
        public IHttpActionResult RecuperarTramitesSector(long idUsuario, DataTableParameters parametros)
        {
            return recuperarTramites(Grilla.Sector, parametros, idUsuario);
        }

        [HttpPost]
        [Route("tramites/sector/accionesdisponibles")]
        public IHttpActionResult GetAvailableActionsTramitesSector(long idUsuario, int[] tramites)
        {
            return recuperarAcciones(Grilla.Sector, tramites, idUsuario);
        }

        [HttpPost]
        [Route("tramites/sector/acciones/{accion}/disponible")]
        public IHttpActionResult GetValidActionTramitesSector(long accion, int[] tramites, long idUsuario)
        {
            return esAccionValida(Grilla.Sector, accion, tramites, idUsuario);
        }

        [HttpPost]
        [Route("tramites/catastro")]
        public IHttpActionResult RecuperarTramitesCatastro(long idUsuario, DataTableParameters parametros)
        {
            return recuperarTramites(Grilla.Catastro, parametros, idUsuario);
        }

        [HttpPost]
        [Route("tramites/catastro/accionesdisponibles")]
        public IHttpActionResult GetAvailableActionsTramitesCatastro(long idUsuario, int[] tramites)
        {
            return recuperarAcciones(Grilla.Catastro, tramites, idUsuario);
        }

        [HttpPost]
        [Route("tramites/catastro/acciones/{accion}/disponible")]
        public IHttpActionResult GetValidActionTramitesCatastro(long accion, int[] tramites, long idUsuario)
        {
            return esAccionValida(Grilla.Catastro, accion, tramites, idUsuario);
        }

        [HttpPut]
        [Route("tramites/observacion")]
        public IHttpActionResult Observar(Observacion observacion)
        {
            _unitOfWork.MesaEntradasRepository.ObservarTramite(observacion);
            return Ok();
        }

        [HttpPut]
        [Route("tramites/usuario")]
        public IHttpActionResult AsignarUsuario(Asignacion asignacion)
        {
            _unitOfWork.MesaEntradasRepository.AsignarTramites(asignacion);
            return Ok();
        }

        [HttpPut]
        [Route("tramites/sector")]
        public IHttpActionResult Derivar(Derivacion derivacion)
        {
            try
            {
                _unitOfWork.MesaEntradasRepository.DerivarTramites(derivacion);
                return Ok();
            }
            catch (ValidacionException ex)
            {
                return Content(HttpStatusCode.ExpectationFailed, ex);
            }
        }

        [HttpGet]
        [Route("tramites/asuntos")]
        public IHttpActionResult GetTiposTramite()
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetTiposTramite());
        }
        
        [HttpGet]
        [Route("tramites/asuntos/{idAsunto}/causas")]
        public IHttpActionResult GetObjetosTramite(long idAsunto)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetObjetosTramiteByTipo(idAsunto));
        }

        [HttpGet]
        [Route("tramites/estados")]
        public IHttpActionResult GetEstadosTramite()
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetEstadosTramite());
        }

        [HttpGet]
        [Route("tramites/prioridades")]
        public IHttpActionResult GetPrioridadesTramite()
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetPrioridadesTramite());
        }

        [HttpPost]
        [Route("tramites/datosespecificos/origen/objetos/tipos/{tipo}")]
        public IHttpActionResult GenerarDatoEspecificoOrigen(short tipo, long[] ids)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GenerarDatoEspecificoOrigen(tipo, ids));
        }

        [HttpGet]
        [Route("tramites/{id}/datosespecificos/destino")]
        public IHttpActionResult GetDatosOrigenTramite(int id)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetDatosDestinoTramite(id));
        }

        [HttpGet]
        [Route("tramites/{id}/datosespecificos/origen")]
        public IHttpActionResult GetDatosDestinoTramite(int id)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GetDatosOrigenTramite(id));
        }

        [Route("tramites/parametrosgenerales")]
        public IHttpActionResult GetParametrosGenerales()
        {
            try
            {
                using (var db = GeoSITMContext.CreateContext())
                {
                    return Ok(db.ParametrosGenerales.ToList());
                }
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Recuperar Parametros Generales", ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("tramites/{id}/antecedentes")]
        public IHttpActionResult TieneAntecedentesGenerados(int id)
        {
            try
            {
                
                return Ok(_unitOfWork.MesaEntradasRepository.TieneAntecedentesGenerados(id));
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"TieneAntecedentesGenerados({id})", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites/antecedentes")]
        public IHttpActionResult GenerarAntecedentes(METramiteParameters parametros)
        {
            try
            {
                int idTramite = _unitOfWork.MesaEntradasRepository.GenerarAntecedentes(parametros);
                return Ok(idTramite);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (ValidacionTramiteException ex)
            {
                var statusCode = HttpStatusCode.ExpectationFailed;
                switch (ex.ErrorValidacion)
                {
                    case ResultadoValidacion.Advertencia:
                        statusCode = HttpStatusCode.Conflict;
                        break;
                    case ResultadoValidacion.Bloqueo:
                        statusCode = HttpStatusCode.PreconditionFailed;
                        break;
                    default: //error
                        break;
                }
                return Content(statusCode, ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Generación Antecedentes ({parametros?.Tramite?.IdTramite ?? 0})", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites/reservas")]
        public IHttpActionResult ConfirmarReservas(METramiteParameters parametros)
        {
            try
            {
                int idTramite = _unitOfWork.MesaEntradasRepository.ConfirmarReservas(parametros);
                return Ok(idTramite);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (ValidacionTramiteException ex)
            {
                var statusCode = HttpStatusCode.ExpectationFailed;
                switch (ex.ErrorValidacion)
                {
                    case ResultadoValidacion.Advertencia:
                        statusCode = HttpStatusCode.Conflict;
                        break;
                    case ResultadoValidacion.Bloqueo:
                        statusCode = HttpStatusCode.PreconditionFailed;
                        break;
                    default: //error
                        break;
                }
                return Content(statusCode, ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Confirmar Reservas ({parametros.Tramite.IdTramite})", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites/reservas/solicitud")]
        public IHttpActionResult SolicitarReservas(METramiteParameters parametros)
        {
            try
            {
                int idTramite = _unitOfWork.MesaEntradasRepository.SolicitarReservas(parametros);
                return Ok(idTramite);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (ValidacionTramiteException ex)
            {
                var statusCode = HttpStatusCode.ExpectationFailed;
                switch (ex.ErrorValidacion)
                {
                    case ResultadoValidacion.Advertencia:
                        statusCode = HttpStatusCode.Conflict;
                        break;
                    case ResultadoValidacion.Bloqueo:
                        statusCode = HttpStatusCode.PreconditionFailed;
                        break;
                    default: //error
                        break;
                }
                return Content(statusCode, ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Solicitar Reservas ({parametros.Tramite.IdTramite})", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites")]
        public IHttpActionResult Save(METramiteParameters parametros)
        {
            try
            {
                int id = _unitOfWork.MesaEntradasRepository.TramiteSave(parametros);
                return Ok(id);
            }
            catch (ValidacionTramiteException ex)
            {
                var statusCode = HttpStatusCode.ExpectationFailed;
                switch (ex.ErrorValidacion)
                {
                    case ResultadoValidacion.Advertencia:
                        statusCode = HttpStatusCode.Conflict;
                        break;
                    case ResultadoValidacion.Bloqueo:
                        statusCode = HttpStatusCode.PreconditionFailed;
                        break;
                    case ResultadoValidacion.ApiExterna:
                        statusCode = HttpStatusCode.ServiceUnavailable;
                        break;
                    default: //error
                        break;
                }
                return Content(statusCode, ex);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Tramite Save", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites/informe")]
        public IHttpActionResult SaveInforme(METramiteParameters parametros)
        {
            try
            {
                int id = _unitOfWork.MesaEntradasRepository.TramiteSaveInforme(parametros);
                return Ok(id);
            }
            catch (ValidacionTramiteException ex)
            {
                return Content(HttpStatusCode.ExpectationFailed, ex);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Tramite Save Informe", ex);
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("tramites/informe/version")]
        public IHttpActionResult SaveInformeFirmado(bool final, METramiteParameters parametros)
        {
            try
            {
                int id = _unitOfWork.MesaEntradasRepository.TramiteSaveVersionInforme(final, parametros);
                return Ok(id);
            }
            catch (ValidacionTramiteException ex)
            {
                return Content(HttpStatusCode.ExpectationFailed, ex);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("Tramite Save Informe", ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("tramites/{tramite}/unidadestributarias/{ut}/valuacion")]
        public IHttpActionResult ObtenerValuacion(int tramite, long ut)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.ObtenerValuacion(tramite, ut));
        }

        [HttpGet]
        [Route("tramites/unidadestributarias/valuacion/{idValuacion}/superficies")]
        public IHttpActionResult ObtenerSuperficiesValuacion(long idValuacion)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.ObtenerSuperficiesValuacion(idValuacion));
        }

        [HttpPost]
        [Route("tramites/parcelas/generador")]
        public IHttpActionResult GenerarParcelasDestino(GeneradorParcelas generador)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GenerarDatosEspecificosDestino(generador));
        }

        [HttpPost]
        [Route("tramites/partidas/generador")]
        public IHttpActionResult GenerarPartidasDestino(GeneradorPartidas generador)
        {
            return Ok(_unitOfWork.MesaEntradasRepository.GenerarDatosEspecificosDestino(generador));
        }

        [HttpPost]
        [Route("tramites/actions/{accion}")]
        public IHttpActionResult ExecuteAction(METramite[] tramites, long accion)
        {
            try
            {
                _unitOfWork.MesaEntradasRepository.ExecuteAction(tramites, accion);
                return Ok();
            }
            catch (ValidacionTramiteException ex)
            {
                return Content(HttpStatusCode.Conflict, ex);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"ExecuteAction {accion}", ex);
                return InternalServerError();
            }
        }

        private IHttpActionResult recuperarTramites(Grilla grilla, DataTableParameters parametros, long idUsuario)
        {
            try
            {
                var tramites = _unitOfWork.MesaEntradasRepository.RecuperarTramites(grilla, parametros, idUsuario);
                return Ok(tramites);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Recuperar Trámites Bandeja {grilla}", ex);
                return InternalServerError(ex);
            }
        }

        private IHttpActionResult recuperarAcciones(Grilla grilla, int[] tramites, long idUsuario)
        {
            try
            {
                var acciones = _unitOfWork.MesaEntradasRepository.RecuperarAccionesDisponibles(grilla, tramites, idUsuario);
                return Ok(acciones);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Recuperar Acciones Disponibles Bandeja {grilla} según selección", ex);
                return InternalServerError(ex);
            }
        }

        private IHttpActionResult esAccionValida(Grilla grilla, long accion, int[] tramites, long idUsuario)
        {
            try
            {
                bool valida = _unitOfWork.MesaEntradasRepository.ValidarDisponibilidadAccion(grilla, accion, tramites, idUsuario);
                return Ok(valida);
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError($"Validar Accion Disponible Bandeja {grilla} según selección", ex);
                return InternalServerError(ex);
            }
        }
    }
}
