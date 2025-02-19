using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Reportes.Api.Helpers;
using System;
using System.Configuration;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Documentos;
using GeoSit.Reportes.Api.Reportes;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Designaciones;
using GeoSit.Data.BusinessEntities.Via;

namespace GeoSit.Reportes.Api.Controllers
{
    public class InformeValuacionRuralController : ApiController
    {
        public IHttpActionResult Get(long idUnidadTributaria, long? idTramite, string usuario)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                try
                {

                    var result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/UnidadesTributarias/{idUnidadTributaria}").Result;
                    result.EnsureSuccessStatusCode();
                    var unidadTributaria = result.Content.ReadAsAsync<UnidadTributaria>().Result;

                    /*
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{unidadTributaria.ParcelaID}/parcelasorigen").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.ParcelaOrigenes = result.Content.ReadAsAsync<IEnumerable<ParcelaOrigen>>().Result;
                    */

                    //valuaciones
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/valuaciones/unidadesTributarias/{idUnidadTributaria}/vigente").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.UTValuaciones = result.Content.ReadAsAsync<VALValuacion>().Result;

                    //DDJJs
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/declaracionesjuradas/unidadesTributarias/{idUnidadTributaria}/vigentes").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.DeclaracionJ = result.Content.ReadAsAsync<List<DDJJ>>().Result.SingleOrDefault();

                    //parte tramite para que se muestre el numero y no el id en el reporte
                    string tramiteNumero = null;
                    if (idTramite != null && idTramite > 0)
                    { 
                        result = client.GetAsync($"api/MesaEntradas/Tramites/{idTramite}").Result;
                        result.EnsureSuccessStatusCode();
                        tramiteNumero = result.Content.ReadAsAsync<METramite>().Result?.Numero;
                    }

                    //Designacion
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{unidadTributaria.ParcelaID}/designaciones").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Designacion = result.Content.ReadAsAsync<List<Designacion>>().Result.FirstOrDefault();

                    
                    byte[] bytes = ReporteHelper.GenerarReporte(new InformeValuacionRural(), unidadTributaria, usuario, tramiteNumero);
                    return Ok(Convert.ToBase64String(bytes));
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                    return NotFound();
                }
            }
        }
    }
}
