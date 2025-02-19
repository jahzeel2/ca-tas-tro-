using System;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Configuration;
using System.Net;
using GeoSit.Data.BusinessEntities.Inmuebles;
using System.Linq;
using System.Net.Http.Formatting;
using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Client.Web.Helpers.ExtensionMethods.Mensuras;
using GeoSit.Client.Web.Helpers;
using Newtonsoft.Json;
using System.Text;
using GeoSit.Data.BusinessEntities.GlobalResources;
using System.Net.Mime;

namespace GeoSit.Client.Web.Controllers
{
    public class MensuraController : Controller
    {
        private HttpClient cliente = new HttpClient();

        public MensuraController()
        {
            cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
        }

        private ArchivoDescarga ArchivoDescarga
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }

        // GET: /Mensura/Index
        public ActionResult Index()
        {
            return PartialView();
        }

        public ActionResult BuscadorMensura()
        {
            return RedirectToAction("DatosMensura", new { altaBuscador = true });
        }

        // GET: /Mensura/DatosMensura
        [ValidateInput(false)]
        public ActionResult DatosMensura(bool altaBuscador = false)
        {
            var model = new MensuraModel();
            ViewData["tiposmensuras"] = new SelectList(GetTipoMensuras(), "Value", "Text");
            ViewData["estadosmensuras"] = new SelectList(GetEstadoMensuras(), "Value", "Text");
            ViewData["departamentos"] = new SelectList(GetDepartamentos(), "Value", "Text");
            ViewData["esAltaBuscador"] = altaBuscador;
            return PartialView(model);
        }

        public List<SelectListItem> GetTipoMensuras()
        {
            List<TiposMensurasModel> model = GetTipoMensura();

            List<SelectListItem> itemsTipoMensura = new List<SelectListItem>();
            itemsTipoMensura.Add(new SelectListItem { Text = "", Value = "99" });

            foreach (var a in model)
            {
                itemsTipoMensura.Add(new SelectListItem { Text = a.Descripcion, Value = a.IdTipoMensura.ToString() });
            }
            return itemsTipoMensura;
        }

        public List<SelectListItem> GetDepartamentos()
        {
            try
            {
                using (var resp = cliente.GetAsync($"api/objetoadministrativoservice/GetObjetoByTipo/{(long)TipoObjetoAdministrativoEnum.Departamento}").Result.EnsureSuccessStatusCode())
                {
                    return resp.Content.ReadAsAsync<IEnumerable<ObjetoAdministrativoModel>>().Result
                               .OrderBy(x => x.Nombre)
                               .Select(obj => new SelectListItem() { Text = obj.Nombre, Value = obj.Codigo.PadLeft(2, '0') })
                               .ToList();
                }
            }
            catch (Exception)
            {
                return new List<SelectListItem>();
            }
        }

        public List<TiposMensurasModel> GetTipoMensura()
        {
            HttpResponseMessage resp = cliente.GetAsync("api/TipoMensuraService/GetTiposMensura").Result;
            resp.EnsureSuccessStatusCode();
            return (List<TiposMensurasModel>)resp.Content.ReadAsAsync<IEnumerable<TiposMensurasModel>>().Result;
        }

        public List<SelectListItem> GetEstadoMensuras()
        {
            List<EstadosMensurasModel> model = GetEstadoMensura();

            List<SelectListItem> itemsEstadoMensura = new List<SelectListItem>();
            itemsEstadoMensura.Add(new SelectListItem { Text = "", Value = "" });
            foreach (var a in model)
            {
                itemsEstadoMensura.Add(new SelectListItem { Text = a.Descripcion, Value = a.IdEstadoMensura.ToString() });
            }
            return itemsEstadoMensura;
        }

        public List<EstadosMensurasModel> GetEstadoMensura()
        {
            HttpResponseMessage resp = cliente.GetAsync("api/EstadoMensuraService/GetEstadosMensura").Result;
            resp.EnsureSuccessStatusCode();
            return (List<EstadosMensurasModel>)resp.Content.ReadAsAsync<IEnumerable<EstadosMensurasModel>>().Result;
        }

        [HttpPost]
        public JsonResult GetMensurasJson(DataTableParameters parametros)
        {
            return Json(GetDatosMensuraByAll(parametros));
        }
        private DataTableResult<GrillaMensura> GetDatosMensuraByAll(DataTableParameters parametros)
        {
            using(var resp = cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/Mensuras/Search",parametros,new JsonMediaTypeFormatter()).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<DataTableResult<GrillaMensura>>().Result;
            }
        }

        public JsonResult GetDatosMensuraJson(long id)
        {
            var mensura = GetDatosMensuraById(id);
            var model = new
            {
                detalle = mensura.ToMensuraModel(),
                parcelas = mensura.ParcelasMensuras.Select(p => p.ToParcelaMensuraModel()).ToList(),
                mensurasRelacionadas = mensura.MensurasRelacionadasDestino
                                              .Where(p => p.MensuraDestino != null)
                                              .Select(p => p.ToMensuraDestinoRelacionadaModel())
                                              .Concat(mensura.MensurasRelacionadasOrigen
                                                             .Where(p => p.MensuraOrigen != null)
                                                             .Select(p => p.ToMensuraOrigenRelacionadaModel()))
                                              .ToList(),
                documentos = mensura.MensurasDocumentos.Select(p => p.ToMensuraDocumentoModel()).ToList()
            };
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public Mensura GetDatosMensuraById(long id)
        {
            using (var resp = cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Mensuras/{id}").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<Mensura>().Result;
            }
        }

        public List<ParcelaMensura> GetParcelasMensuras(long idParcela)
        {
            using (var resp = cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mensuras/GetParcelasMensuras/{idParcela}").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<List<ParcelaMensura>>().Result;
            }
        }

        [HttpPost]
        public JsonResult GetMensurasDetalleByIds(long[] ids)
        {
            using (var resp = cliente.PostAsJsonAsync("api/MensuraService/GetMensurasDetalleByIds", ids).Result)
            {
                resp.EnsureSuccessStatusCode();
                var mensuras = resp.Content.ReadAsAsync<IEnumerable<Mensura>>()
                                  .Result.Select(mensura => new { mensura.IdMensura, mensura.Descripcion, Tipo = mensura.TipoMensura.Descripcion, Estado = mensura.EstadoMensura.Descripcion });
                return Json(mensuras.ToList(), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetDatosUnidadesByParcelasJson(string idsParcelas)
        {
            return Json(GetDatosUnidadesByParcelas(idsParcelas));
        }

        public List<UnidadTributaria> GetDatosUnidadesByParcelas(string idsParcelas)
        {
            try
            {
                using (var resp = cliente.PostAsync($"api/UnidadTributaria/GetUnidadesTributariasByParcelas", new ObjectContent<long[]>(idsParcelas.Split(',').Select(x => long.Parse(x)).ToArray(), new JsonMediaTypeFormatter())).Result.EnsureSuccessStatusCode())
                {
                    return resp.Content.ReadAsAsync<List<UnidadTributaria>>().Result;
                }
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError($"MensuraController-GetDatosUnidadesByParcelas({idsParcelas})", ex);
                throw;
            }
        }

        [HttpPost]
        public ActionResult Save_DatosMensura(MensuraModel mensura, List<long> parcelas, List<long> documentos, List<long> mensurasOrigen, List<long> mensurasDestino)
        {
            var usuario = (UsuariosModel)Session["usuarioPortal"];
            long usuarioOperacion = usuario.Id_Usuario;
            string ip = Request.UserHostAddress,
                machineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            var model = new
            {
                mensura,
                parcelas,
                mensurasOrigen,
                mensurasDestino,
                documentos,
                usuarioOperacion,
                ip,
                machineName,
            };

            if (string.IsNullOrEmpty(model.mensura.Anio))
            {
                model.mensura.Anio = model.mensura.Descripcion.Split('/').LastOrDefault();
            }
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            using (var resp = cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/mensuras", content).Result)
            {
                try
                {
                    resp.EnsureSuccessStatusCode();
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                catch (HttpRequestException ex)
                {
                    MvcApplication.GetLogger().LogError("MensuraController/Save_DatosMensura", ex);
                    return new HttpStatusCodeResult(resp.StatusCode);
                }
            }
        }

        public ActionResult ValidarDisponible(string numero, string letra, long id)
        {
            using (cliente)

            using (var resp = cliente.GetAsync($"api/mensuraservice/{id}/{numero}/{letra}/disponible").Result)
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    return Json(new { error = true, mensaje = resp.Content.ReadAsAsync<string>().Result }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public JsonResult GetUnidadesTributarias(long[] ids)
        {
            IEnumerable<UnidadTributaria> getAll()
            {
                foreach (long id in ids)
                {
                    using (var result = cliente.GetAsync($"api/unidadtributaria/get/{id}").Result.EnsureSuccessStatusCode())
                    {
                        var ut = result.Content.ReadAsAsync<UnidadTributaria>().Result;
                        ut.Parcela = null;
                        yield return ut;
                    }
                }
            }
            return Json(getAll().ToList());
        }
        public JsonResult DeleteMensuraJson(long id)
        {
            return Json(DeleteMensuraById(id));
        }
        public string DeleteMensuraById(long id)
        {
            var usuario = (UsuariosModel)Session["usuarioPortal"];
            var mensura = new Mensura()
            {
                IdMensura = id,
                IdUsuarioBaja = usuario.Id_Usuario,
                _Ip = Request.UserHostAddress,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress)
            };

            var content = new StringContent(JsonConvert.SerializeObject(mensura), Encoding.UTF8, "application/json");
            
            using (var request = new HttpRequestMessage(HttpMethod.Delete, $"{MvcApplication.V2_API_PREFIX}/mensuras") { Content = content })
            using (var resp = cliente.SendAsync(request).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.StatusCode.ToString();
            }
        }

        public string DeleteRelacionParcelaMensura(long idMensura, long idParcelaMensura)
        {
            var usuario = (UsuariosModel)Session["usuarioPortal"];
            var mensura = new Mensura()
            {
                IdMensura = idMensura,
                IdUsuarioBaja = usuario.Id_Usuario,
                _Ip = Request.UserHostAddress,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress)
            };
            var url = $"{MvcApplication.V2_API_PREFIX}/mensuras?idParcelaMensura={idParcelaMensura}";
            var content = new StringContent(JsonConvert.SerializeObject(mensura), Encoding.UTF8, "application/json");
            using (var request = new HttpRequestMessage(HttpMethod.Delete, url) { Content = content })
            using (var resp = cliente.SendAsync(request).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.StatusCode.ToString();
            }
        }
        
        public ActionResult GenerarNumero(long departamento)
        {
            try
            {
                using (var resp = cliente.GetAsync($"api/mensura/departamento/{departamento}/numero").Result.EnsureSuccessStatusCode())
                {
                    return Json(resp.Content.ReadAsAsync<string[]>().Result);
                }
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public FileResult Abrir()
        {
            Response.AppendHeader("Content-Disposition", new ContentDisposition { FileName = ArchivoDescarga.NombreArchivo, Inline = true }.ToString());
            return File(ArchivoDescarga.Contenido, ArchivoDescarga.MimeType);
        }

        public ActionResult GetInformePlanoAprobado(long id)
        {
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/InformePlanoAprobado/GetInformePlanoAprobado?idMensura={id}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeSituacion);
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificadoPlanoAprobado {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
    }
}