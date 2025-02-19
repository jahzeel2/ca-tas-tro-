using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Common;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.DeclaracionesJuradas
{
    [RoutePrefix("api/v2/valuaciones")]
    public class ValuacionesController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public ValuacionesController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetValuacion(int id)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValuacion(id));
        }

        [HttpGet]
        [Route("unidadesTributarias/{idUnidadTributaria}/vigente")]
        public IHttpActionResult Get(long idUnidadTributaria)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValuacionVigente(idUnidadTributaria));
        }

        [HttpGet]
        [Route("unidadesTributarias/{idUnidadTributaria}/historicas")]
        public IHttpActionResult GetValuacionesHistoricas(int idUnidadTributaria)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValuacionesHistoricas(idUnidadTributaria));
        }

        [HttpPost]
        [Route("unidadesTributarias/{idUnidadTributaria}/preview/")]
        public async Task<IHttpActionResult> PreviewValuacion(long idUnidadTributaria, VALSuperficie[] superficies)
        {
            try
            {
                var resultado = await _unitOfWork.DeclaracionJuradaRepository
                                                 .CalcularValuacionAsync(idUnidadTributaria, superficies);

                return resultado == null ? (IHttpActionResult)NotFound() : Ok(resultado);
            }
            catch(InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest, ex.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("DeclaracionesJuradas/Preview", ex);
                return InternalServerError();
            }
        }

        [HttpPost]
        [Route("temporales/unidadesTributarias/{idUnidadTributaria}/preview/")]
        public async Task<IHttpActionResult> PreviewValuacionTemporal(long idUnidadTributaria, VALSuperficie[] superficies)
        {
            try
            {
                var resultado = await _unitOfWork.DeclaracionJuradaTemporalRepository
                                                 .CalcularValuacionAsync(idUnidadTributaria, superficies);

                return resultado == null ? (IHttpActionResult)NotFound() : Ok(resultado);
            }
            catch(InvalidOperationException ex)
            {
                return Content(System.Net.HttpStatusCode.BadRequest, ex.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError("DeclaracionesJuradas/Preview", ex);
                return InternalServerError();
            }
        }

        [HttpPost]
        [Route("unidadesTributarias/{idUnidadTributaria}/formularios")]
        public async Task<IHttpActionResult> SaveFormulario(int idUnidadTributaria, DatosFormulario formulario)
        {
            return Ok(await _unitOfWork.DeclaracionJuradaRepository.SaveFormularioAsync(idUnidadTributaria, formulario));
        }

        [HttpGet]
        [Route("temp/corrida")]
        public IHttpActionResult GetValuacionesTmpCorrida()
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValuacionesTmpCorrida());
        }

        [HttpGet]
        [Route("temp/corrida/depto")]
        public IHttpActionResult GetValuacionesTmpDepto(int corrida)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValuacionesTmpDepto(corrida));
        }

        [HttpGet]
        [Route("temp/corrida/eliminar")]
        public IHttpActionResult EliminarCorridaTemporal(int corrida, long usuarioModificacionID)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.EliminarCorridaTemporal(corrida, usuarioModificacionID));
        }

        [HttpPost]
        [Route("temp/corrida")]
        public IHttpActionResult GenerarValuacionTemporal(long usuario)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GenerarValuacionTemporal(usuario));
        }

        [HttpPost]
        [Route("temp/corrida/{corrida}/produccion")]
        public IHttpActionResult PasarValuacionTmpProduccion(int corrida, long usuario)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.PasarValuacionTmpProduccion(corrida, usuario));
        }
    }
}
