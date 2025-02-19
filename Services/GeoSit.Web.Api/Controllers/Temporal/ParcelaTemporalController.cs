using GeoSit.Data.DAL.Common;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers
{
    public class ParcelaTemporalController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public ParcelaTemporalController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IHttpActionResult GetParcelaById(long id, int tramite)
        {
            return Ok(_unitOfWork.ParcelaTemporalRepository.GetParcelaById(id,tramite));
        }

        public IHttpActionResult Get(long id, int tramite)
        {
            return Ok(_unitOfWork.ParcelaTemporalRepository.GetParcelaById(id, tramite));
        }

        [HttpGet]
        [Route("api/ParcelaTemporal/Tramite/{tramite}/Entradas")]
        public IHttpActionResult GetEntradasByIdTramite(int tramite)
        {
            return Ok(_unitOfWork.ParcelaTemporalRepository.GetEntradasByIdTramite(tramite));
        }
    }
}
