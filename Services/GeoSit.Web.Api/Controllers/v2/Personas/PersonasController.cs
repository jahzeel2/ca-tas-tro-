using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.DAL.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Personas
{
    [RoutePrefix("api/v2/personas")]
    public class PersonasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;
        public PersonasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [Route("buscar")]
        public async Task<IHttpActionResult> Search(string texto)
        {
            return Ok(await _unitOfWork.PersonaRepository.SearchByPattern(texto));
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetById(long id)
        {
            return Ok(_unitOfWork.PersonaRepository.GetPersonaById(id));
        }

        [HttpGet]
        [Route("{id}/profesiones")]
        public IHttpActionResult GetProfesionesByPersonaId(long id)
        {
            return Ok(_unitOfWork.PersonaRepository.GetProfesionesByPersonaId(id));
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> SavePersona([FromBody] JObject data)
        {
            var persona = data["persona"].ToObject<Persona>();
            var domicilios = data["domicilios"].ToObject<List<Domicilio>>();
            var profesiones = data["profesiones"].ToObject<List<Profesion>>();

            long usuarioOperacion = long.Parse(data["usuarioOperacion"].ToString());
            string ip = data["ip"].ToString();
            string machineName = data["machineName"].ToString();

            try
            {
                persona = await _unitOfWork.PersonaRepository.Save(persona, domicilios, profesiones, usuarioOperacion, ip, machineName);

                return persona != null
                                ? (IHttpActionResult)Ok(persona)
                                : BadRequest();
            }
            catch (InvalidDataException)
            {
                return BadRequest();
            }
            catch (NotSupportedException)
            {
                return Conflict();
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IHttpActionResult> DeletePersona(long id, [FromBody] JObject data)
        {
            long usuarioOperacion = long.Parse(data["usuarioOperacion"].ToString());
            string ip = data["ip"].ToString();
            string machineName = data["machineName"].ToString();

            bool ok = await _unitOfWork.PersonaRepository.Delete(id, usuarioOperacion, ip, machineName);

            return ok ? (IHttpActionResult)Ok() : InternalServerError();
        }
    }
}
