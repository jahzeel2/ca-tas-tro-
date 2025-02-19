using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Reportes.Api.Helpers;
using System;
using System.Configuration;
using System.Net.Http;
using System.Web.Http;

namespace GeoSit.Reportes.Api.Controllers
{
    public class InformeTramiteResumenController : ApiController
    {
        [HttpPost]
        public IHttpActionResult Post(ResumenTramite resumen)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                try
                {
                    byte[] bytes = ReporteHelper.GenerarInformeTramiteResumen(resumen);
                    return Ok(Convert.ToBase64String(bytes));
                }
                catch (Exception ex)
                {
                    WebApiApplication.GetLogger().LogError("InformeTramiteResumenController", ex);
                    return NotFound();
                }
            }
        }
    }
}
