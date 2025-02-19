using GeoSit.Data.DAL.Common;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Catastro
{
    [RoutePrefix("api/v2/inscripciones")]
    public class InscripcionesController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public InscripcionesController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("tipos/activos")]
        public IHttpActionResult GetTiposActivos()
        {
            return Ok(_unitOfWork.TipoInscripcionRepository.GetTipoInscripciones());
        }
    }
}
