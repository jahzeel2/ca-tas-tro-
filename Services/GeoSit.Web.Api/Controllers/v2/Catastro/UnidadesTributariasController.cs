using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Common.CustomErrors.UnidadesTributarias;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Catastro
{
    [RoutePrefix("api/v2/unidadestributarias")]
    public class UnidadesTributariasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public UnidadesTributariasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Get(long id, bool incluirDominios = false)
        {
            return Ok(_unitOfWork.UnidadTributariaRepository.GetUnidadTributariaById(id, incluirDominios));
        }

        [HttpGet]
        [Route("{id}/Domicilios")]
        public IHttpActionResult GetDomicilios(long id)
        {
            return Ok(_unitOfWork.DomicilioRepository.GetDomiciliosByUnidadTributariaId(id));
        }

        [HttpGet]
        [Route("{id}/ResponsablesFiscales")]
        public IHttpActionResult GetResponsablesFiscales(long id)
        {
            return Ok(_unitOfWork.UnidadTributariaPersonaRepository.GetUnidadTributariaResponsablesFiscales(id));
        }

        [HttpGet]
        [Route("{id}/dominios")]
        public IHttpActionResult GetDominios(long id)
        {
            return Ok(_unitOfWork.DominioRepository.GetDominios(id));
        }

        [HttpGet]
        [Route("tipos")]
        public IHttpActionResult GetTipos()
        {
            return Ok(_unitOfWork.UnidadTributariaRepository.GetTiposUnidadTributaria());
        }

        [HttpGet]
        [Route("{id}/CertificadoValuatorio/Valido")]
        public IHttpActionResult TieneCertificadoValuatorioValido(long id)
        {
            var error = _unitOfWork.UnidadTributariaRepository.ValidarCertificadoValuatorio(id);
            IHttpActionResult result = InternalServerError();
            if (error == null)
            {
                result = Ok();
            }
            else if (error.GetType() == typeof(SinDominiosSinSuperficie))
            {
                result = Conflict();
            }
            else if (error.GetType() == typeof(SinDDJJVigente))
            {
                result = BadRequest();
            }

            return result;
        }

    }
}
