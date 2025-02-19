using GeoSit.Data.DAL.Common;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.ObjetosGraficos
{
    [RoutePrefix("api/v2/objetosadministrativos")]
    public class ObjetosAdministrativosController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public ObjetosAdministrativosController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("Departamentos")]
        public IHttpActionResult GetDepartamentos()
        {
            return Ok(_unitOfWork.ObjetosAdministrativosRepository.GetDepartamentos());
        }

        [HttpGet]
        [Route("Departamentos/{id}/Circunscripciones")]
        public IHttpActionResult GetCircunscripcionesByDepartamento(long id)
        {
            return Ok(_unitOfWork.ObjetosAdministrativosRepository.GetCircunscripcionesByDepartamento(id));
        }

        [HttpGet]
        [Route("Circunscripciones/{id}/Secciones")]
        public IHttpActionResult GetSeccionesByCircunscripcion(long id)
        {
            return Ok(_unitOfWork.ObjetosAdministrativosRepository.GetSeccionesByCircunscripcion(id));
        }
    }
}