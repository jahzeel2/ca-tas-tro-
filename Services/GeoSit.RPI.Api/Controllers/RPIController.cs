using System.Web.Http;

namespace GeoSit.RPI.Api.Controllers
{
    [RoutePrefix("api/rpi")]
    public class RPIController : ApiController
    { 
        [Route("IncripcionDominio")]
        public IHttpActionResult GetInscripcionDominio()
        {
            return Ok();
        }
    }
}
