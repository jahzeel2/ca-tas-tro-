using GeoSit.Data.DAL.Common;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.DeclaracionesJuradas
{
    [RoutePrefix("api/v2/declaracionesjuradas")]
    public class DeclaracionesJuradasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public DeclaracionesJuradasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("unidadesTributarias/{idUnidadTributaria}/vigentes")]
        public IHttpActionResult GetDeclaracionesJuradas(long idUnidadTributaria)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetDeclaracionesJuradas(idUnidadTributaria));
        }

        [HttpGet]
        [Route("unidadesTributarias/{idUnidadTributaria}/historicas")]
        public IHttpActionResult GetDeclaracionesJuradasHistoricas(int idUnidadTributaria)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetDeclaracionesJuradasNoVigentes(idUnidadTributaria));
        }

        [HttpGet]
        [Route("{id}/superficies")]
        public IHttpActionResult GetValSuperficies(long id)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetValSuperficies(id));
        }
    }
}
