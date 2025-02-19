using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;
using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Common.CustomErrors.Parcelas;
using GeoSit.Web.Api.ExtensionMethods;
using Newtonsoft.Json.Linq;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Catastro
{
    [RoutePrefix("api/v2/parcelas")]
    public class ParcelasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public ParcelasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("{id}/ParcelasOrigen")]
        public IHttpActionResult GetParcelasOrigen(long id)
        {
            return Ok(_unitOfWork.ParcelaOperacionRepository.GetParcelasOrigenOperacion(id));
        }

        [HttpGet]
        [Route("{id}/Nomenclaturas")]
        public IHttpActionResult GetNomenclaturas(long id)
        {
            return Ok(_unitOfWork.NomenclaturaRepository.GetByIdParcela(id));
        }

        [HttpGet]
        [Route("{id}/Objeto/{depto}")]
        public IHttpActionResult GetObjeto(long id, string depto)
        {
            return Ok(_unitOfWork.NomenclaturaRepository.GetObjetoByTipo(id, depto));
        }

        [HttpGet]
        [Route("{id}/ZonaEcologica")]
        public IHttpActionResult GetZonaEcologica(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetZonaValuacionByIdParcela(id));
        }

        [HttpGet]
        [Route("{id}/Ejido")]
        public IHttpActionResult GetEjido(long id)
        {
            return Ok(_unitOfWork.ParcelaRepository.GetEjido(id));
        }

        [HttpGet]
        [Route("{id}/Designaciones")]
        public IHttpActionResult GetDesignaciones(long id)
        {
            return Ok(_unitOfWork.DesignacionRepository.GetDesignacionesByParcela(id));
        }

        [HttpGet]
        [Route("{id}/Estampilla")]
        public IHttpActionResult GetEstampilla(long id)
        {
            return Ok(_unitOfWork.PlotearEstampilla(id));
        }

        [HttpPost]
        [Route("{id}/Grafico")]
        public IHttpActionResult AddGrafico(long id, ParcelaGrafica grafico)
        {
            try
            {
                var result = _unitOfWork.ParcelaRepository.AddGrafico(id, grafico);
                if (result == null) return Ok();

                if (result.GetType() == typeof(GraficoNoValidoParaAlfa)) return Conflict();
                if (result.GetType() == typeof(DatosNoVigentes)) return BadRequest();
                return StatusCode(System.Net.HttpStatusCode.ExpectationFailed);
            }
            catch
            {
                return InternalServerError();
            }
        }

        [HttpDelete]
        [Route("{id}/Grafico")]
        public IHttpActionResult DeleteGrafico(long id, ParcelaGrafica grafico)
        {
            try
            {
                var result = _unitOfWork.ParcelaRepository.DeleteGrafico(id, grafico);
                if (result == null) return Ok();

                if (result.GetType() == typeof(GraficoNoValidoParaAlfa)) return Conflict();
                if (result.GetType() == typeof(DatosNoVigentes)) return BadRequest();
                return StatusCode(System.Net.HttpStatusCode.ExpectationFailed);
            }
            catch
            {
                return InternalServerError();
            }
        }

        [HttpGet]
        [Route("Operaciones/Tipos/{tipoMensura}/Parcelas/Clase")]
        public IHttpActionResult GetClaseParcelaByOperacionParcelaria(long tipoMensura)
        {
            return Ok(_unitOfWork.ClaseParcelaRepository.GetClaseParcelaByTipoMensura(tipoMensura));
        }


        [HttpGet]
        [Route("Operaciones/Tipos/{tipoMensura}/Parcelas/Estados")]
        public IHttpActionResult GetEstadosParcelaByOperacionParcelaria(long tipoMensura)
        {
            return Ok(_unitOfWork.EstadosParcelaRepository.GetEstadosParcelaByTipoMensura(tipoMensura));
        }


        [HttpGet]
        [Route("Operaciones/Tipos")]
        public IHttpActionResult GetTiposOperacionesParcelarias()
        {
            return Ok(_unitOfWork.TipoParcelaOperacionRepository.GetTipoParcelaOperaciones());
        }


        [HttpGet]
        [Route("Tipos")]
        public IHttpActionResult GetTiposParcela()
        {
            return Ok(_unitOfWork.TipoParcelaRepository.GetTipoParcelas());
        }

        [HttpGet]
        [Route("Clases")]
        public IHttpActionResult GetClasesParcela()
        {
            return Ok(_unitOfWork.ClaseParcelaRepository.GetClasesParcelas());
        }

        [HttpGet]
        [Route("Estados")]
        public IHttpActionResult GetEstadosParcela()
        {
            return Ok(_unitOfWork.EstadosParcelaRepository.GetEstadosParcela());
        }

        [HttpPost]
        [Route("OperacionParcelaria")]
        public IHttpActionResult OperacionParcelaria(OperacionAlfanumerica operacion)
        {
            var error = _unitOfWork.ParcelaRepository.SaveOperacionAlfanumerica(operacion);
            if(error != null)
            {
                return Content(System.Net.HttpStatusCode.BadRequest, error.Error);
            }
            return Ok();
        }

        [HttpPost]
        [Route("OperacionParcelaria/Destino/Validar")]
        public IHttpActionResult Validate(NomenclaturaValidable nomenclatura)
        {
            var ret = _unitOfWork.ParcelaRepository.ValidateDestino(nomenclatura);
            if(ret.Item2 == null)
            {
                return Ok(ret.Item1);
            }
            return Content(System.Net.HttpStatusCode.BadRequest, ret.Item2.Error);
        }

        [HttpGet]
        [Route("Nomenclatura/Validar")]
        public IHttpActionResult ValidarNomenclaturaExistente(string nomenclatura)
        {
            bool resultParcelaAlfanumerica = _unitOfWork.NomenclaturaRepository.ValidarExistenciaNomenclatura(nomenclatura);
            bool resultParcelaGrafica = _unitOfWork.ParcelaGraficaRepository.ValidarExistenciaNomenclatura(nomenclatura);

            var response = new
            {
                ExisteEnParcelaAlfanumerica = resultParcelaAlfanumerica,
                ExisteEnParcelaGrafica = resultParcelaGrafica,
                ExisteEnAmbas = resultParcelaAlfanumerica && resultParcelaGrafica
            };

            return Ok(response);
        }
    }
}
