using System.Web.Http;
using GeoSit.Data.DAL.Common;

namespace GeoSit.Web.Api.Controllers
{
    public class PadronMunicipalController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public PadronMunicipalController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IHttpActionResult GetParcelasMunicipioVigentes(long idMunicipio)
        {
            var parcelasMunicipioVigentes = _unitOfWork.PadronMunicipalRepository.GetParcelasMunicipio(idMunicipio, true, true);
            return Ok(parcelasMunicipioVigentes);
        }

        public IHttpActionResult GetParcelasMunicipioHistorico(long idMunicipio)
        {
            var parcelasMunicipioHistoricas = _unitOfWork.PadronMunicipalRepository.GetParcelasMunicipio(idMunicipio, false, true);
            return Ok(parcelasMunicipioHistoricas);
        }

        public IHttpActionResult GetUTsMunicipioVigentes(long idMunicipio)
        {
            var utMunicipioVigentes = _unitOfWork.PadronMunicipalRepository.GetParcelasMunicipio(idMunicipio, true, false);
            return Ok(utMunicipioVigentes);
        }

        public IHttpActionResult GetUTsMunicipioHistorico(long idMunicipio)
        {
            var utMunicipioHistoricas = _unitOfWork.PadronMunicipalRepository.GetParcelasMunicipio(idMunicipio, false, false);
            return Ok(utMunicipioHistoricas);
        }

    }
}
