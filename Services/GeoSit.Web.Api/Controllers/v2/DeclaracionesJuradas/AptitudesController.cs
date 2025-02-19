using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Common;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.DeclaracionesJuradas
{
    [RoutePrefix("api/v2/declaracionesjuradas/aptitudes")]

    public class AptitudesController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public AptitudesController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAptitudes()
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetAptitudes());
        }

        [HttpGet]
        [Route("{idAptitud}/sor/caracteristicas/tipos")]
        public IHttpActionResult GetCaracteristicasSoRByAptitud(long idAptitud)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.GetSorTipoCaracteristicas(idAptitud));
        }

        [HttpPost]
        [Route("{idAptitud}/sor/caracteristicas/completa")]
        public IHttpActionResult GetCaracteristicasSoRByAptitud(long idAptitud, long[] caracteristicas)
        {
            try
            {
                if (_unitOfWork.DeclaracionJuradaRepository.AptitudCompleta(idAptitud, caracteristicas))
                {
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return InternalServerError();
            }
        }

        [HttpPost]
        [Route("{idAptitud}/sor/caracteristicas/puntaje")]
        public IHttpActionResult GetPuntajeAptitudByCaracteristicas(long idAptitud, VALSuperficie superficie)
        {
            return Ok(_unitOfWork.DeclaracionJuradaRepository.AptitudPuntaje(idAptitud, superficie));
        }
    }
}
