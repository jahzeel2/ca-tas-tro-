using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Reportes.Api.Helpers;
using System;
using System.Configuration;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Reportes.Api.Reportes;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Designaciones;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.BusinessEntities.Temporal;

namespace GeoSit.Reportes.Api.Controllers
{
    public class CertificadoValuatorioController : ApiController
    {
        public IHttpActionResult Get(long idUnidadTributaria, string usuario)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                try
                {
                    var result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/unidadestributarias/{idUnidadTributaria}?incluirDominios=true").Result;
                    result.EnsureSuccessStatusCode();
                    var unidadTributaria = result.Content.ReadAsAsync<UnidadTributaria>().Result;

                    // domicilios
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/unidadestributarias/{idUnidadTributaria}/domicilios").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.Domicilios = result.Content.ReadAsAsync<IEnumerable<Domicilio>>().Result;

                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{unidadTributaria.ParcelaID}/nomenclaturas").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.Nomenclaturas = result.Content.ReadAsAsync<IEnumerable<Nomenclatura>>().Result.ToList();

                    string depto = unidadTributaria.Parcela.Nomenclaturas.LastOrDefault()?.Nombre.Substring(0, 2);
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{3}/objeto/{depto.TrimStart('0')}").Result;
                    result.EnsureSuccessStatusCode();
                    Objeto objeto = result.Content.ReadAsAsync<Objeto>().Result; // Nombre Departamento Completo

                    long auxIdUT = idUnidadTributaria;

                    var reporte = new CertificadoValuatorio();
                    //var lblCi = reporte.FindControl("lblCi", true);

                    // responsable fiscal
                    /*if (!unidadTributaria.Dominios.Any() && unidadTributaria.Parcela.ClaseParcelaID == 4)
                    {
                        auxIdUT = unidadTributaria.Parcela.UnidadesTributarias
                                                  .Single(x => x.TipoUnidadTributariaID == 4)
                                                  .UnidadTributariaId;

                        lblCi.Text = "PH Especial sin inscripción registrada, se informa Dominio y Titular de inmueble Origen";
                    }
                    else
                    {
                        lblCi.Visible = false;
                    }*/
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/unidadestributarias/{auxIdUT}/dominios").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.Dominios = result.Content.ReadAsAsync<IEnumerable<DominioUT>>().Result;


                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/unidadestributarias/{auxIdUT}/responsablesfiscales").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.ResponsablesFiscales = result.Content.ReadAsAsync<IEnumerable<ResponsableFiscal>>().Result;

                    //------
                    /*foreach (var nomenc in unidadTributaria.Parcela.Nomenclaturas)
                    {
                        nomenc.FechaAlta = unidadTributaria.Parcela.FechaAlta;
                        nomenc.FechaModificacion = unidadTributaria.Parcela.FechaModificacion;
                    }*/

                    if (!string.IsNullOrEmpty(unidadTributaria.Parcela.Atributos))
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(unidadTributaria.Parcela.Atributos);
                        var node = xmlDoc.SelectSingleNode("//datos/AfectaPH/text()");
                        unidadTributaria.Parcela.AfectaPh = node != null ? (node.Value == "true" ? "SI" : "NO") : string.Empty;
                        node = xmlDoc.SelectSingleNode("//datos/SuperfecieTitulo/text()");
                        unidadTributaria.Parcela.SuperfecieTitulo = node != null ? node.Value : string.Empty;
                        node = xmlDoc.SelectSingleNode("//datos/SuperfecieMensura/text()");
                        unidadTributaria.Parcela.SuperfecieMensura = node != null ? node.Value : string.Empty;
                    }

                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{unidadTributaria.ParcelaID}/parcelasorigen").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.ParcelaOrigenes = result.Content.ReadAsAsync<IEnumerable<ParcelaOrigen>>().Result;

                    //valuaciones
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/valuaciones/unidadestributarias/{unidadTributaria.UnidadTributariaId}/vigente").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.UTValuaciones = result.Content.ReadAsAsync<VALValuacion>().Result;

                    //parte tramite para que se muestre el numero y no el id en el reporte
                    string numeroTramite = string.Empty;
                    //if (idTramite != null && idTramite > 0)
                    //{
                    //    result = client.GetAsync($"api/MesaEntradas/Tramites/{idTramite}").Result;
                    //    result.EnsureSuccessStatusCode();
                    //    numeroTramite = result.Content.ReadAsAsync<METramite>().Result.Numero;
                    //}

                    //Designacion
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/parcelas/{unidadTributaria.ParcelaID}/designaciones").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Designacion = result.Content.ReadAsAsync<List<Designacion>>().Result.FirstOrDefault(d => d.IdTipoDesignador == 0);

                    //Superficies
                    //result = client.GetAsync($"api/parcela/{unidadTributaria.ParcelaID}/superficies/").Result; 
                    //result.EnsureSuccessStatusCode();
                    //var parcelaSuperficies = result.Content.ReadAsAsync<ParcelaSuperficies>().Result;

                    //SuperficiesRural
                    result = client.GetAsync($"api/parcela/{unidadTributaria.ParcelaID}/superficiesRural/").Result;
                    result.EnsureSuccessStatusCode();
                    var parcelaRuralSuperficies = result.Content.ReadAsAsync<Dictionary<string, double>>().Result;

                    //Superficie Grafica
                    result = client.GetAsync($"api/parcela/{unidadTributaria.ParcelaID}/superficieGrafica/").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.SuperficieGrafica = result.Content.ReadAsAsync<int>().Result;

                    //Parcelas Mensuras
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/mensuras/GetParcelasMensuras/{unidadTributaria.ParcelaID}").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.ParcelaMensuras = result.Content.ReadAsAsync<List<ParcelaMensura>>().Result;

                    //Mensura
                    result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/mensuras/{unidadTributaria.Parcela.ParcelaMensuras.FirstOrDefault().IdMensura}").Result;
                    result.EnsureSuccessStatusCode();
                    unidadTributaria.Parcela.ParcelaMensuras.FirstOrDefault().Mensura = result.Content.ReadAsAsync<Mensura>().Result;

                    byte[] bytes = ReporteHelper.GenerarCertificadoValuatorio(reporte, unidadTributaria, depto + " - " + objeto.Nombre, parcelaRuralSuperficies, usuario);
                    return Ok(Convert.ToBase64String(bytes));
                }
                catch (Exception ex)
                {
                    WebApiApplication.GetLogger().LogError("CertificadoValuatorioController", ex);
                    return NotFound();
                }
            }
        }

        [HttpGet]
        public IHttpActionResult GetInformeDetalleCorrida(int corrida, string usuario)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                var result = client.GetAsync($"{WebApiApplication.V2_API_PREFIX}/valuaciones/temp/corrida/depto?corrida={corrida}").Result;
                result.EnsureSuccessStatusCode();
                var list = result.Content.ReadAsAsync<List<VALValuacionTempDepto>>().Result;
                byte[] bytes = ReporteHelper.GenerarInformeDetalleCorrida(list, usuario);
                return Ok(Convert.ToBase64String(bytes));
            }
        }
    }
}
