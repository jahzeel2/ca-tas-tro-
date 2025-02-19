using GeoSit.Client.Web.Helpers;
using GeoSit.Client.Web.Helpers.ExtensionMethods.FormularioValuacion;
using GeoSit.Client.Web.Models;
using GeoSit.Client.Web.Models.FormularioValuacion;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.Temporal;
using Newtonsoft.Json;
using Resources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace GeoSit.Client.Web.Controllers
{
    public class ValuacionesController : Controller
    {
        private readonly static HttpClientHandler handler = new HttpClientHandler();
        private readonly HttpClient httpClient;
     
        private UsuariosModel UsuarioConectado { get { return Session["usuarioPortal"] as UsuariosModel; } }
        private ArchivoDescarga ArchivoDescarga
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }

        private long IdDDJJUTVigente
        {
            get { return Convert.ToInt64(Session["IdDDJJUTVigente"]); }
            set { Session["IdDDJJUTVigente"] = value; }
        }

        public ValuacionesController()
        {
            httpClient = new HttpClient(handler, false)
            {
                Timeout = TimeSpan.FromMinutes(5),
                BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"])
            };
        }

        // GET: Valuaciones
        public ActionResult Administrador(long id, bool editable = false)
        {
            var ut = GetUnidadTributaria(id);
            var puedeValuar = !IsDentroEjido(ut) && int.Parse(ut.CodigoProvincial) != 0;
            ViewData["PartidaInmobiliaria"] = ut?.CodigoProvincial;
            ViewData["IdUnidadTributaria"] = id;
            ViewData["Editable"] = puedeValuar && SeguridadController.ExisteFuncion(Seguridad.AdministracionDDJJ);
            return PartialView();
        }

        public ActionResult ResumenVigente(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/unidadestributarias/{id}/vigente").Result)
            {
                resp.EnsureSuccessStatusCode();
                var valuacion = resp.Content.ReadAsAsync<VALValuacion>().Result;
                if (valuacion == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }
                return Json(valuacion.ToValuacionModel(), JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFormularioVigente(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/unidadestributarias/{id}/vigentes").Result)
            {
                resp.EnsureSuccessStatusCode();
                var ddjjs = resp.Content.ReadAsAsync<List<DDJJ>>().Result;

                return Json(new { data = ddjjs.Select(x => x.ToDDJJVigenteModel()).Where(x => !x.IsEmpty()).ToList() }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFormulariosHistoricos(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/unidadestributarias/{id}/historicas").Result)
            {
                resp.EnsureSuccessStatusCode();
                var ddjjs = resp.Content.ReadAsAsync<List<DDJJ>>().Result;
                return Json(new { data = ddjjs.Select(x => x.ToDDJJHistoricaModel()).Where(x => !x.IsEmpty()).ToList() }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetValuacionesHistoricas(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/unidadestributarias/{id}/historicas").Result)
            {
                resp.EnsureSuccessStatusCode();
                var valuaciones = resp.Content.ReadAsAsync<List<VALValuacion>>().Result;
                return Json(new { data = valuaciones.Select(x => x.ToValuacionModel()) }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> GetSuperficies(long id)
        {
            bool nueva = id <= 0;

            var aptitudes = GetAptitudes();
            var superficies = GetValSuperficies(nueva ? IdDDJJUTVigente : id).Select(s => s.ToSuperficieModel(nueva));

            return Json(new { superficies = superficies.ToArray(), aptitudes = await aptitudes }, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetCurrentSuperficiesTemporales()
        {
            var valuacion = (FormularioValuacionModel)Session["ValuacionTemporal"];
            var superficiesActuales = (SuperficieModel[])Session["ValuacionTemporalSuperficies"];
            var aptitudes = GetAptitudes();
            var superficies = superficiesActuales ?? GetValSuperficiesTemporales(valuacion.IdDDJJ).Select(s => s.ToSuperficieModel());

            return Json(new { superficies = superficies.ToArray(), aptitudes = await aptitudes }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCaracteristicas(long idAptitud, bool editable, bool editaMaestras)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/aptitudes/{idAptitud}/sor/caracteristicas/tipos").Result)
            {
                resp.EnsureSuccessStatusCode();
                var tiposCaracteristicas = resp.Content.ReadAsAsync<IEnumerable<DDJJSorTipoCaracteristica>>().Result;

                ViewData["Editable"] = editable;
                ViewData["EditaMaestras"] = editaMaestras;
                return PartialView("DetalleAptitud", tiposCaracteristicas.ToListOfGruposCaracteristicasModel());
            }
        }

        public ActionResult GetFormattedSuperficies(decimal superficieParcela, decimal superficieValuada)
        {
            return PartialView("ResumenSuperficiesRurales",
                                FormularioValuacionModel.CreateResumenSuperficies(superficieParcela, superficieValuada));
        }

        public ActionResult VerFormulario(long id)
        {
            return PartialView("Formulario", FormularioValuacionModel.FromEntity(GetValuacion(id)));
        }

        public ActionResult NuevoFormulario(long id)
        {
            var valuacion = GetValuacionVigenteByIdUT(id);
            IdDDJJUTVigente = valuacion.DeclaracionJurada.IdDeclaracionJurada;
            return PartialView("Formulario", FormularioValuacionModel.Create(valuacion));
        }

        public ActionResult EvalCaracteristicasByAptitud(long aptitud, long[] caracteristicas)
        {
            using (var resp = httpClient.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/aptitudes/{aptitud}/sor/caracteristicas/completa", caracteristicas).Result)
            {
                if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.BadRequest)
                {
                    return Json(new { valid = resp.IsSuccessStatusCode });
                }
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ActionResult> Preview(long idUnidadTributaria, SuperficieModel[] superficies)
        {
            var superficiesValuadas = superficies.Select(s => new VALSuperficie()
            {
                IdAptitud = s.Aptitud.IdAptitud,
                Caracteristicas = s.Caracteristicas
                                   .Select(c => new DDJJSorCar()
                                   {
                                       IdSorCar = c
                                   }).ToArray(),
                Superficie = Convert.ToDouble(s.SuperficieHa),
                TrazaDepreciable = s.TrazaDepreciable
            });
            var content = new ObjectContent<VALSuperficie[]>(superficiesValuadas.ToArray(), new JsonMediaTypeFormatter());
            using (var respPreview = await httpClient.PostAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/unidadesTributarias/{idUnidadTributaria}/preview", content))
            {
                if (respPreview.IsSuccessStatusCode)
                {
                    var result = await respPreview.Content.ReadAsAsync<DatosComputo>();
                    return PartialView("DetalleValuacion", result);
                }
                var error = await respPreview.Content.ReadAsAsync<string[]>();
                return Json(new { error });
            }
        }

        public async Task<ActionResult> PreviewTemporal(long idUnidadTributaria, SuperficieModel[] superficies)
        {
            var superficiesValuadas = superficies.Select(s => new VALSuperficie()
            {
                IdAptitud = s.Aptitud.IdAptitud,
                Caracteristicas = s.Caracteristicas
                                   .Select(c => new DDJJSorCar()
                                   {
                                       IdSorCar = c
                                   }).ToArray(),
                Superficie = Convert.ToDouble(s.SuperficieHa),
                TrazaDepreciable = s.TrazaDepreciable
            });
            var content = new ObjectContent<VALSuperficie[]>(superficiesValuadas.ToArray(), new JsonMediaTypeFormatter());
            using (var respPreview = await httpClient.PostAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/temporales/unidadesTributarias/{idUnidadTributaria}/preview", content))
            {
                if (respPreview.IsSuccessStatusCode)
                {
                    var result = await respPreview.Content.ReadAsAsync<DatosComputo>();
                    return PartialView("DetalleValuacion", result);
                }
                var error = await respPreview.Content.ReadAsAsync<string[]>();
                return Json(new { error });
            }
        }
        public async Task<ActionResult> Save(long idUnidadTributaria, SuperficieModel[] superficies)
        {
            var superficiesValuadas = superficies.Select(s => new VALSuperficie()
            {
                IdAptitud = s.Aptitud.IdAptitud,
                Caracteristicas = s.Caracteristicas
                                   .Select(c => new DDJJSorCar()
                                   {
                                       IdSorCar = c
                                   }).ToArray(),
                Superficie = Convert.ToDouble(s.SuperficieHa),
                TrazaDepreciable = s.TrazaDepreciable
            });
            var formulario = new DatosFormulario(superficiesValuadas.ToArray(), UsuarioConectado.Id_Usuario, Request.UserHostAddress, AuditoriaHelper.ReverseLookup(Request.UserHostAddress));
            var content = new ObjectContent<DatosFormulario>(formulario, new JsonMediaTypeFormatter());
            using (var resp = await httpClient.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/unidadesTributarias/{idUnidadTributaria}/formularios", formulario))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                var error = await resp.Content.ReadAsAsync<string[]>();
                return Json(new { error });
            }
        }

        public async Task<ActionResult> CalcularPuntaje(long aptitud, double superficie, long[] caracteristicas, short? trazaDepreciable)
        {
            var linea = new VALSuperficie()
            {
                Superficie = superficie,
                Caracteristicas = caracteristicas.Select(c => new DDJJSorCar() { IdSorCar = c }).ToList(),
                TrazaDepreciable = trazaDepreciable,
            };
            using (var resp = await httpClient.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/aptitudes/{aptitud}/sor/caracteristicas/puntaje", linea))
            {
                resp.EnsureSuccessStatusCode();
                var ret = await resp.Content.ReadAsAsync<VALSuperficie>();
                return Json(new { puntaje = ret.Puntaje, puntajeSuperficie = ret.PuntajeSuperficie });
            }
        }

        private async Task<AptitudModel[]> GetAptitudes()
        {
            using (var respAptitudes = await httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/aptitudes"))
            {
                respAptitudes.EnsureSuccessStatusCode();

                return (await respAptitudes.Content.ReadAsAsync<IEnumerable<VALAptitudes>>())
                                    .Select(a => a.ToAptitudModel())
                                    .OrderBy(d => d.Orden)
                                    .ToArray();
            }

        }
        private bool IsDentroEjido(UnidadTributaria ut)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/parcelas/{ut?.ParcelaID ?? 0}/ejido").Result)
            {
                return resp.IsSuccessStatusCode && resp.Content.ReadAsAsync<ObjetoAdministrativoModel>().Result != null;
            }
        }

        private VALValuacion GetValuacion(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/{id}").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<VALValuacion>().Result;
            }
        }

        private VALValuacion GetValuacionVigenteByIdUT(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/valuaciones/unidadesTributarias/{id}/vigente").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<VALValuacion>().Result;
            }
        }

        private UnidadTributaria GetUnidadTributaria(long id)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/unidadestributarias/{id}").Result)
            {
                resp.EnsureSuccessStatusCode();

                return resp.Content.ReadAsAsync<UnidadTributaria>().Result;
            }
        }

        public FileResult AbrirReporte()
        {
            Response.AppendHeader("Content-Disposition", new ContentDisposition { FileName = ArchivoDescarga.NombreArchivo, Inline = true }.ToString());
            return File(ArchivoDescarga.Contenido, ArchivoDescarga.MimeType);
        }

        public ActionResult CertificadoValuatorio(long id)
        {
            if (!UnidadTributariaTieneFormularioValido(id, out HttpStatusCode status))
            {
                return new HttpStatusCodeResult(status);
            }
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/CertificadoValuatorio/Get?idUnidadTributaria={id}&usuario={usuario}&idTramite=").Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(resp.StatusCode);
                }

                AuditoriaHelper.Register(((UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarCertificadoValuatorio);

                string bytesBase64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytesBase64), $"CertificadoDeValuaciónFiscal_{DateTime.Now:yyyyMMdd_HHmmss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public ActionResult InformeValuacionParcela(long? idParcela = null, long? idTramite = null)
        {

            var parcela = Session["Parcela"] as Parcela;
            if (idParcela == null && parcela == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No se ha seleccionado ninguna parcela.");
            }

            if (idParcela != null)
            {
                var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/{idParcela}").Result;
                resp.EnsureSuccessStatusCode();
                parcela = resp.Content.ReadAsAsync<Parcela>().Result;
            }

            if (!parcela.UnidadesTributarias.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No se puede generar el informe de valuación para esta parcela.");
            }

            string tipo = "Rural";
            if (parcela.UnidadesTributarias.Any(ut => ut.TipoUnidadTributariaID == 2)) // es ph (puede ser urbana o no)
            {
                tipo = "Ph";
            }
            else if (parcela.TipoParcelaID != 2 && parcela.TipoParcelaID != 3)
            {
                tipo = "Urbana";
            }
            long idUt = parcela.UnidadesTributarias
                                    .Where(x => x.TipoUnidadTributariaID == 1 || x.TipoUnidadTributariaID == 2 || x.TipoUnidadTributariaID == 4)
                                    .OrderBy(x => x.TipoUnidadTributariaID)
                                    .First().UnidadTributariaId;

            if (!UnidadTributariaTieneFormularioValido(idUt, out _))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Conflict);
            }

            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/InformeValuacion{tipo}/Get?idUnidadTributaria={idUt}&idTramite={idTramite}&usuario={usuario}").Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytes64), $"{parcela.UnidadesTributarias.First().CodigoProvincial}_InformeValuacion{tipo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        private bool UnidadTributariaTieneFormularioValido(long id, out HttpStatusCode status)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/unidadestributarias/{id}/certificadovaluatorio/valido").Result)
            {
                status = resp.StatusCode;
                return resp.IsSuccessStatusCode;
            }
        }

        public DDJJ GetDDJJVigenteByUT(long idUt)
        {
            var url = $"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/unidadesTributarias/{idUt}/vigentes";
            try
            {
                using (var resp = httpClient.GetAsync(url).Result)
                {
                    resp.EnsureSuccessStatusCode();
                    return JsonConvert.DeserializeObject<List<DDJJ>>(resp.Content.ReadAsStringAsync().Result).FirstOrDefault();
                }
            }
            catch (HttpRequestException ex)
            {
                throw new ApplicationException("Error en la solicitud HTTP", ex);
            }
        }

        public List<VALSuperficie> GetValSuperficies(long idDDJJ)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/declaracionesjuradas/{idDDJJ}/superficies").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<List<VALSuperficie>>().Result;
            }
        }

        private List<VALSuperficieTemporal> GetValSuperficiesTemporales(long idDDJJ)
        {
            using (var resp = httpClient.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/unidadestributarias/valuacion/{idDDJJ}/superficies").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<List<VALSuperficieTemporal>>().Result;
            }
        }

        #region Valuacion Masiva View
        public ActionResult ValuacionMasiva() // Crea la vista en ValuacionMasiva.cshtml
        {
            return PartialView("ValuacionMasiva");
        }

        public JsonResult GetValTmpCorridas()
        {
            string url = $"{MvcApplication.V2_API_PREFIX}/valuaciones/temp/corrida";
            using (var result = httpClient.GetAsync(url).Result)
            {
                result.EnsureSuccessStatusCode();
                var data = result.Content.ReadAsAsync<IEnumerable<VALValuacionTempCorrida>>().Result;
                var cl = new CultureInfo("es-CL");
                var dataFormateada = data.Select(item =>
                {
                    string fechastr = null;
                    if (DateTime.TryParse(item.FechaProc, out DateTime fecha))
                        fechastr = fecha.ToString("dd-MM-yyyy HH:mm");

                    return new
                    {
                        item.Corrida,
                        FechaProc = fechastr,
                        CantidadParcProc = item.CantidadParcProc.ToString("#,0", cl),
                        SupValuada = item.SupValuada.ToString("#,0", cl),
                        ValTotal = item.ValTotal.ToString("#,0", cl),
                        ValMax = item.ValMax.ToString("#,0", cl),
                        PromedioValParc = item.PromedioValParc.ToString("#,0", cl),
                        ValMin = item.ValMin.ToString("#,0.00", cl).Replace('.', ',')
                    };
                }).ToList();
                return Json(dataFormateada, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetInformeDetalleCorrida(int corrida)
        {
            string usuario = $"{UsuarioConectado.Nombre} {UsuarioConectado.Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/CertificadoValuatorio/GetInformeDetalleCorrida?corrida={corrida}&usuario={usuario}").Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(resp.StatusCode);
                }
                AuditoriaHelper.Register(UsuarioConectado.Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.ValuacionTemporal);
                string bytesBase64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytesBase64), $"CertificadoDetalleCorrida_{DateTime.Now:yyyyMMdd_HHmmss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        [HttpGet]
        public ActionResult EliminarCorridaTemporal(int corrida)
        {
            string url = $"{MvcApplication.V2_API_PREFIX}/valuaciones/temp/corrida/eliminar?corrida={corrida}&usuarioModificacionID={UsuarioConectado.Id_Usuario}";
            using (var result = httpClient.GetAsync(url).Result)
            {
                result.EnsureSuccessStatusCode();
                if (result.Content.ReadAsAsync<bool>().Result)
                {
                    return Json(new { success = true, message = $"La corrida temporal N°{corrida} ha sido eliminada correctamente." }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false, message = "Ha ocurrido un error al eliminar la corrida temporal." }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GenerarValuacionTemporal()
        {
            string url = $"{MvcApplication.V2_API_PREFIX}/valuaciones/temp/corrida?usuario={UsuarioConectado.Id_Usuario}";
            using (var result = httpClient.PostAsync(url, null).Result)
            {
                result.EnsureSuccessStatusCode();
                if (result.Content.ReadAsAsync<bool>().Result)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public ActionResult PasarValuacionTmpProduccion(int corrida)
        {
            string url = $"{MvcApplication.V2_API_PREFIX}/valuaciones/temp/corrida/{corrida}/produccion?usuario={UsuarioConectado.Id_Usuario}";
            using (var result = httpClient.PostAsync(url, null).Result)
            {
                result.EnsureSuccessStatusCode();
                if (result.Content.ReadAsAsync<bool>().Result)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion
    }
}