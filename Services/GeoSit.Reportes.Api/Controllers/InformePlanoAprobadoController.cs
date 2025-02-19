using GeoSit.Reportes.Api.Helpers;
using System;
using System.Configuration;
using System.Net.Http;
using System.Web.Http;
using GeoSit.Data.BusinessEntities.Inmuebles;

namespace GeoSit.Reportes.Api.Controllers
{
    public class InformePlanoAprobadoController : ApiController
    {
        public IHttpActionResult GetInformePlanoAprobado(long idMensura, string usuario)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                var response = client.GetAsync($"api/MensuraService/GetMensuraInformeById?id={idMensura}").Result;
                response.EnsureSuccessStatusCode();
                Mensura mensura = response.Content.ReadAsAsync<Mensura>().Result;


                byte[] bytes = ReporteHelper.GenerarReporte(new Reportes.InformePlanoAprobado(), mensura, usuario);
                return Ok(Convert.ToBase64String(bytes));
            }
        }
    }
}
