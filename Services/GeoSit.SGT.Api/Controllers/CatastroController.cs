using SGTEntities;
using Swashbuckle.Swagger.Annotations;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.SGT.Api.Controllers
{
    [RoutePrefix("api/dpcyc")]
    public class CatastroController : ApiController
    {
        [HttpPut]
        [Route("expedientes/{expediente}/{anio}/pase")]
        [SwaggerOperation("Permite registrar el pase de un trámite <strong>a</strong> Catastro")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "Error al aceptar el pase")]
        public async Task<IHttpActionResult> AceptarPase(PaseEntrante pase)
        {
            if (pase.NumeroExpediente.EndsWith("00"))
            {
                return InternalServerError();
            }
            return Ok(pase.NumeroExpediente);
        }

        [HttpGet]
        [Route("expedientes/{expediente}/{anio}/{formato}/trazabilidad")]
        [SwaggerOperation("Obtiene un archivo de Trazabilidad del Trámite en formato pdf o json")]
        [SwaggerResponse(HttpStatusCode.OK)]
        [SwaggerResponse(HttpStatusCode.NotFound, "Archivo Inexistente")]
        [SwaggerResponse(HttpStatusCode.InternalServerError, "Error al generar el archivo")]
        public IHttpActionResult GenerarInformeTrazabilidad(string expediente, string anio, string formato)
        {
            var validFormats = new[] { "json", "pdf", };
            if (!validFormats.Contains(formato))
            {
                return NotFound();
            }

            var bytes = File.ReadAllBytes(Path.Combine($@"{ConfigurationManager.AppSettings["dummiesPath"]}", $"trazabilidad.{formato}"));
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
            resp.Content.Headers.ContentType = new MediaTypeHeaderValue($"application/{formato}");
            resp.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Trazabilidad Expediente {expediente}_{anio}.{formato}"
            };
            return ResponseMessage(resp);
        }
    }
}
