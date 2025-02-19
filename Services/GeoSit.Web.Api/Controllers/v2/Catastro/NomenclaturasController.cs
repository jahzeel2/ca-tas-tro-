using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Common.CustomErrors.Nomenclaturas;
using System.Web.Http;

namespace GeoSit.Web.Api.Controllers.v2.Catastro
{
    [RoutePrefix("api/v2/nomenclaturas")]
    public class NomenclaturasController : ApiController
    {
        private readonly UnitOfWork _unitOfWork;

        public NomenclaturasController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
    
    }
}
