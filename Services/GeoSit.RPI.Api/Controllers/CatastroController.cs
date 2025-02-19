using GeoSit.RPI.Api.CorsPolicies;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.RPI.Api.Controllers
{
    [EnableCorsRPI]
    [RoutePrefix("api/dpcyc")]
    public class CatastroController : ApiController
    {
        [HttpPost]
        [Route("certificadocatastral")]
        public async Task<IHttpActionResult> GenerarCertificadoCatastral()
        {
            return Ok();
        }

        [HttpPost]
        [Route("certificadocatastral/copia")]
        public async Task<IHttpActionResult> GetCopiaCertificadoCatastral()
        {
            return Ok();
        }

        [HttpPost]
        [Route("planosmensura")]
        public async Task<IHttpActionResult> ConsultarPlanoMensura()
        {
            return Ok();
        }
    }
}