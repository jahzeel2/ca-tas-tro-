using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.DAL.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Catastro
{
    [RoutePrefix("api/v2/mensuras")]
    public class MensurasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;
        public MensurasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> SaveMensura([FromBody] JObject data)
        {
            var mensura = data["mensura"].ToObject<Mensura>();
            var parcelas = data["parcelas"].ToObject<List<long>>();
            var documentos = data["documentos"].ToObject<List<long>>();
            var mensurasOrigen = data["mensurasOrigen"].ToObject<List<long>>();
            var mensurasDestino = data["mensurasDestino"].ToObject<List<long>>();

            long usuarioOperacion = long.Parse(data["usuarioOperacion"].ToString());
            string ip = data["ip"].ToString();
            string machineName = data["machineName"].ToString();

            try
            {
                await _unitOfWork.MensurasRepository.Save(mensura, parcelas, mensurasOrigen, mensurasDestino, documentos, usuarioOperacion, ip, machineName);
                return Ok();
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpDelete]
        [Route("")]
        public async Task<IHttpActionResult> Delete(Mensura mensura)
        {
            try
            {
                if (await _unitOfWork.MensurasRepository.Delete(mensura))
                {
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception)
            {
                return Conflict();
            }
        }

        [HttpDelete]
        [Route("")]
        public async Task<IHttpActionResult> DeleteParcelaMensura([FromBody] Mensura mensura, [FromUri] long idParcelaMensura)
        {
            try
            {
                if (await _unitOfWork.MensurasRepository.DeleteRelacionParcelaMensura(mensura, idParcelaMensura))
                {
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception)
            {
                return Conflict();
            }
        }


        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetById(long id)
        {
            return Ok(_unitOfWork.MensurasRepository.GetById(id));
        }

        [HttpGet]
        [Route("GetParcelasMensuras/{idParcela}")]
        public IHttpActionResult GetParcelasMensuras(long idParcela)
        {
            return Ok(_unitOfWork.MensurasRepository.GetParcelasMensuraByParcelaId(idParcela));
        }

        [HttpPost]
        [Route("Search")]
        public IHttpActionResult Search(DataTableParameters parametros)
        {
            return Ok(_unitOfWork.MensurasRepository.SearchByText(parametros));
        }
    
        [HttpGet]
        [Route("Tipos")]
        public IHttpActionResult GetTipos()
        {
            return Ok(_unitOfWork.MensurasRepository.GetTiposMensura());
        }
    
        [HttpGet]
        [Route("Tipos/Saneamiento")]
        public IHttpActionResult GetTiposSaneamiento()
        {
            return Ok(_unitOfWork.MensurasRepository.GetTiposMensuraSaneamiento());
        }
    
        [HttpGet]
        [Route("Tipos/{id}/Parcelas/Estados")]
        public IHttpActionResult GetEstadosParcelaByTipoMensura(long id)
        {
            return Ok(_unitOfWork.EstadosParcelaRepository.GetEstadosParcelaByTipoMensura(id));
        }
    
        [HttpGet]
        [Route("Tipos/{id}/Parcelas/Clase")]
        public IHttpActionResult GetClaseParcelaByTipoMensura(long id)
        {
            return Ok(_unitOfWork.ClaseParcelaRepository.GetClaseParcelaByTipoMensura(id));
        }
    }
}
