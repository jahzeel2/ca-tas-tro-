using GeoSit.SGT.Api.Helpers;
using SGTEntities;
using Swashbuckle.Swagger.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.SGT.Api.Controllers
{
    /// <summary>
    /// Controller con los endpoints que permiten interactuar con la api de SGT
    /// </summary>
    [RoutePrefix("api/sgt")]
    public class SGTController : ApiController
    {
        /// <summary>
        /// Valida si el usuario y password proporcionado es válido 
        /// en el contexto de seguridad externo
        /// </summary>
        /// <param name="credentials">Nombre de Usuario y Contraseña del usuario</param>
        /// <returns></returns>
        [Route("usuarios/valido")]
        [HttpPost]
        [SwaggerOperation("Validación de Usuario")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK)]
        [SwaggerResponse(System.Net.HttpStatusCode.Unauthorized, "El usuario informado no existe o la contraseña no es válida")]
        public async Task<IHttpActionResult> ValidarUsuario(Credenciales credentials)
        {
            if (credentials.Login == "invalido")
            {
                return Unauthorized();
            }
            return Ok(new Usuario() { Login = credentials.Login, Organismo = "Chaco", Rol = "Normal" });
        }

        /// <summary>
        /// Informa los datos de un trámite de nuevo ingreso para 
        /// la obtención del correspondiente Nro de Expediente
        /// </summary>
        /// <param name="tramite">Datos del Tramite Ingresado</param>
        /// <returns></returns>
        [Route("expedientes/ingreso")]
        [HttpPost]
        [SwaggerOperation("Obtiene el Número de Expediente a partir de los datos del trámite")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK)]
        [SwaggerResponse(System.Net.HttpStatusCode.BadRequest, "La api de SGT devolvió un error detallado")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, "Se produjo un error al recuperar el número del expediente")]
        public async Task<IHttpActionResult> GenerarNumeroExpediente(Tramite tramite)
        {
            var ret = await SGTAPI.Instance.InsertTramite(tramite);
            if(ret.IsOkey) return Ok(ret);
            return BadRequest(ret.mensaje);
        }

        /// <summary>
        /// Informa pases internos para los trámites especificados
        /// </summary>
        /// <param name="pases">Listado de Pases Internos a Informar</param>
        /// <returns></returns>
        [Route("expedientes/pases/internos")]
        [HttpPost]
        [SwaggerOperation("Informar Pases Interno")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK)]
        [SwaggerResponse(System.Net.HttpStatusCode.BadRequest, "La api de SGT devolvió un error detallado")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, "Se produjo un error al informar el pase del trámite")]
        public async Task<IHttpActionResult> InformarPases(List<PaseInternoTramite> pases)
        {
            var ret = await SGTAPI.Instance.InformarPases(pases);
            if(ret.IsOkey) return Ok();
            return BadRequest(ret.mensaje);
        }

        /// <summary>
        /// Informa una novedad el trámite especificado
        /// </summary>
        /// <param name="novedad">Novedad a informar</param>
        /// <returns></returns>
        [Route("expedientes/novedades")]
        [HttpPost]
        [SwaggerOperation("Informar Novedades de Trámites")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK)]
        [SwaggerResponse(System.Net.HttpStatusCode.BadRequest, "La api de SGT devolvió un error detallado")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, "Se produjo un error al informar la novedad del trámite")]
        public async Task<IHttpActionResult> InformarNovedad(NovedadTramite novedad)
        {
            var ret = await SGTAPI.Instance.InformarNovedad(novedad);
            if(ret.IsOkey) return Ok();
            return BadRequest(ret.mensaje);
        }

        /// <summary>
        /// Se archiva el trámite especificado
        /// </summary>
        /// <param name="motivo">Novedad a informar</param>
        /// <returns></returns>
        [Route("expedientes/archivos")]
        [HttpPost]
        [SwaggerOperation("Informar Archivo/Finaliación de Trámites")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK)]
        [SwaggerResponse(System.Net.HttpStatusCode.BadRequest, "La api de SGT devolvió un error detallado")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, "Se produjo un error al informar el archivo del trámite")]
        public async Task<IHttpActionResult> ArchivarTramite(FinalizacionTramite motivo)
        {
            var ret = await SGTAPI.Instance.ArchivarTramite(motivo);
            if(ret.IsOkey) return Ok();
            return BadRequest(ret.mensaje);
        }
    }
}
