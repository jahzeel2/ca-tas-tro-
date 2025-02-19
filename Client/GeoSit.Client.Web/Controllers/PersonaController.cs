using System;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mime;
using System.Linq;
using GeoSit.Client.Web.Helpers;
using System.Text;
using System.Web.Http;
using GeoSit.Client.Web.Helpers.ExtensionMethods.Personas;
using System.Threading.Tasks;

namespace GeoSit.Client.Web.Controllers
{
    public class PersonaController : Controller
    {
        private HttpClient cliente = new HttpClient();

        public PersonaController()
        {
            cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
        }

        private ArchivoDescarga ArchivoDescarga
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }

        public ActionResult Index()
        {
            return PartialView();
        }

        public ActionResult BuscadorPersona()
        {
            return RedirectToAction("DatosPersona", new { altaBuscador = true });
        }

        [ValidateInput(false)]
        public ActionResult DatosPersona(bool altaBuscador = false, PersonaModel persona = null)
        {
            ViewData["TiposPersona"] = new SelectList(GetTipoPersonas(), "Value", "Text");
            ViewData["TiposDNI"] = new SelectList(GetTipoDocumentos(), "Value", "Text");
            ViewData["Sexos"] = new SelectList(new SeguridadController().GetTipoSexo(), "Value", "Text");
            ViewData["EstadosCiviles"] = new SelectList(GetTipoEstadoCivil(), "Value", "Text");
            ViewData["Nacionalidades"] = new SelectList(GetTipoNacionalidad(), "Value", "Text");
            ViewData["esAltaBuscador"] = altaBuscador;

            return PartialView("AdministradorPersonas", persona ?? new PersonaModel());
        }

        public ActionResult LoadDatosPersona(long id)
        {
            var persona = new PersonaModel { PersonaId = id };
            return DatosPersona(false, persona);
        }
        
        //public List<SelectListItem> GetTiposDomicilio()
        //{
        //    List<SelectListItem> itemsTipos = new List<SelectListItem>();
        //    foreach (var tipo in GetTiposDomicilios())
        //    {
        //        itemsTipos.Add(new SelectListItem { Text = tipo.Descripcion, Value = Convert.ToString(tipo.TipoDomicilioId) });
        //    }
        //    return itemsTipos;
        //}
        //public List<TiposDomicilioModel> GetTiposDomicilios()
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/TipoDomicilioService/GetTiposDomicilio").Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<TiposDomicilioModel>)resp.Content.ReadAsAsync<IEnumerable<TiposDomicilioModel>>().Result;
        //}


        //public List<PersonaModel> GetDatosPersona()
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/PersonaService/GetPersonas").Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<PersonaModel>)resp.Content.ReadAsAsync<IEnumerable<PersonaModel>>().Result;
        //}

        //public JsonResult GetPersonaByDocumentoJson(string id)
        //{
        //    return Json(GetDatosPersonaByDocumento(id));
        //}

        [System.Web.Mvc.HttpPost]
        public JsonResult SearchPersonas([FromBody] string pattern)
        {
            return Json(GetDatosPersonaByAll(pattern));
        }

        //public List<PersonaModel> GetDatosPersonaByNombre(string nombre_completo)
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/PersonaService/GetDatosPersonaByNombre/" + nombre_completo).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<PersonaModel>)resp.Content.ReadAsAsync<IEnumerable<PersonaModel>>().Result;
        //}

        //public List<PersonaModel> GetDatosPersonaByDocumento(string documento)
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/PersonaService/GetDatosPersonaByDocumento/" + documento).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<PersonaModel>)resp.Content.ReadAsAsync<IEnumerable<PersonaModel>>().Result;
        //}

        public PersonaModel[] GetDatosPersonaByAll(string pattern)
        {
            using (var resp = cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/personas/buscar?texto={pattern}", null).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<IEnumerable<Data.BusinessEntities.Personas.Persona>>()
                            .Result
                            .Select(p => p.ToPersonaModel())
                            .ToArray();
            }
        }

        public async Task<ActionResult> GetDatosPersonaJson(long id)
        {
            var personaLoader = GetDatosPersonaById(id);
            var profesionesLoader = GetDatosProfesionByPersona(id);

            await Task.WhenAll(personaLoader, profesionesLoader);

            var persona = personaLoader.Result;
            var profesiones = profesionesLoader.Result;

            return Json(new
            {
                detalle = persona.ToPersonaModel(),
                domicilios = persona.PersonaDomicilios.Where(d => d.Domicilio != null).Select(d => d.ToDomicilioModel()),
                profesiones
            }, JsonRequestBehavior.AllowGet);
        }

        private async Task<Data.BusinessEntities.Personas.Persona> GetDatosPersonaById(long id)
        {
            using (var resp = await cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/personas/{id}"))
            {
                resp.EnsureSuccessStatusCode();
                var persona = await resp.Content.ReadAsAsync<Data.BusinessEntities.Personas.Persona>();
                return persona;
            }
        }

        private async Task<List<ProfesionModel>> GetDatosProfesionByPersona(long id)
        {
            using (var resp = await cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/personas/{id}/profesiones"))
            {
                resp.EnsureSuccessStatusCode();
                var profesiones = resp.Content.ReadAsAsync<IEnumerable<ProfesionModel>>().Result;
                return profesiones.ToList();
            }
        }

        //public JsonResult GetDatosDomicilioByPersonaJson(long id)
        //{
        //    var listB = new List<PersonaDomicilioModel>();
        //    listB = GetDatosDomicilioByPersona(id);
        //    var domicilios = new List<DomicilioModel>();
        //    foreach (PersonaDomicilioModel element in listB)
        //    {
        //        var dom = GetDatosDomicilioById(element.DomicilioId);
        //        if (dom != null)
        //            domicilios.Add(dom);
        //    };
        //    return Json(domicilios);
        //}

        //public DomicilioModel GetDatosDomicilioById(long id)
        //{
        //    try
        //    {
        //        HttpResponseMessage resp = cliente.GetAsync("api/DomicilioService/GetDomicilioById/" + id).Result;
        //        resp.EnsureSuccessStatusCode();
        //        return resp.Content.ReadAsAsync<DomicilioModel>().Result;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        //public List<PersonaDomicilioModel> GetDatosDomicilioByPersona(long id)
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/PersonaDomicilioService/GetPersonaDomiciliosByPersona/" + id).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<PersonaDomicilioModel>)resp.Content.ReadAsAsync<IEnumerable<PersonaDomicilioModel>>().Result;
        //}

        [System.Web.Mvc.HttpPost]
        public ActionResult Save_DatosPersona(PersonaModel persona, List<DomicilioModel> domicilios, List<ProfesionModel> profesiones)
        {
            var usuario = (UsuariosModel)Session["usuarioPortal"];
            long usuarioOperacion = usuario.Id_Usuario;
            string ip = Request.UserHostAddress,
                machineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            var model = new
            {
                persona,
                domicilios,
                profesiones,
                usuarioOperacion,
                ip,
                machineName,
            };
            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

            using (var resp = cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/personas", content).Result)
            {
                try
                {
                    resp.EnsureSuccessStatusCode();
                    var guardado = resp.Content.ReadAsAsync<PersonaModel>().Result;
                    return Json(guardado);
                }
                catch (HttpRequestException ex)
                {
                    MvcApplication.GetLogger().LogError("PersonaController/Save_DatosPersona", ex);
                    return new HttpStatusCodeResult(resp.StatusCode);
                }
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult DeletePersona(PersonaModel persona)
        {
            var auditoria = new
            {
                usuarioOperacion = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario,
                ip = Request.UserHostAddress,
                machineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress),
            };

            var content = new StringContent(JsonConvert.SerializeObject(auditoria), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{MvcApplication.V2_API_PREFIX}/personas/{persona.PersonaId}")
            {
                Content = content
            };
            using (var resp = cliente.SendAsync(request).Result)
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        //public JsonResult QuitarPersonaViewJson(string nombre)
        //{
        //    ViewBag.DatosPersona = GetDatosPersonaByNombre(nombre).Concat(GetDatosPersonaByDocumento(nombre));
        //    return Json("Ok");
        //}

        //public JsonResult DeletePersonaJson(long id)
        //{
        //    return Json(DeletePersonaById(id));
        //}

        //public JsonResult DeleteDomicilioJson(long id)
        //{
        //    return Json(DeleteDomicilioById(id));
        //}

        //public JsonResult DeletePersonaDomicilioJson(long id, long persona, long tipodomicilio)
        //{
        //    return Json(DeletePersonaDomicilioById(id, persona, tipodomicilio));
        //}

        //public string DeletePersonaById(long id)
        //{
        //    string machineName;
        //    try
        //    {
        //        machineName = Dns.GetHostEntry(Request.UserHostAddress)?.HostName ?? Request.UserHostName;
        //    }
        //    catch (Exception)
        //    {
        //        // Error al recuperar el nombre de la maquina
        //        machineName = Request.UserHostName;
        //    }
        //    var per = new Data.BusinessEntities.Personas.Persona()
        //    {
        //        PersonaId = id,
        //        _Id_Usuario = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario,
        //        _Ip = Request.UserHostAddress,
        //        _Machine_Name = machineName
        //    };
        //    return cliente.PostAsJsonAsync("api/PersonaService/DeletePersona_Save", per).Result.EnsureSuccessStatusCode().StatusCode.ToString();
        //}

        //public string DeleteDomicilioById(long id)
        //{
        //    string machineName;
        //    try
        //    {
        //        machineName = Dns.GetHostEntry(Request.UserHostAddress)?.HostName ?? Request.UserHostName;
        //    }
        //    catch (Exception)
        //    {
        //        // Error al recuperar el nombre de la maquina
        //        machineName = Request.UserHostName;
        //    }
        //    var dom = new Data.BusinessEntities.ObjetosAdministrativos.Domicilio()
        //    {
        //        DomicilioId = id,
        //        _Id_Usuario = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario,
        //        _Ip = Request.UserHostAddress,
        //        _Machine_Name = machineName
        //    };
        //    var resp = cliente.PostAsJsonAsync("api/DomicilioService/DeleteDomicilio_Save", dom).Result.EnsureSuccessStatusCode();
        //    return resp.StatusCode.ToString();
        //}

        //public string DeletePersonaDomicilioById(long id, long persona, long tipodomicilio)
        //{
        //    string machineName;
        //    try
        //    {
        //        machineName = Dns.GetHostEntry(Request.UserHostAddress)?.HostName ?? Request.UserHostName;
        //    }
        //    catch (Exception)
        //    {
        //        // Error al recuperar el nombre de la maquina
        //        machineName = Request.UserHostName;
        //    }
        //    var httpMSG = new HttpRequestMessage(HttpMethod.Delete, "api/PersonaDomicilioService/DeletePersonaDomicilio_Save")
        //    {
        //        Content = new ObjectContent<Data.BusinessEntities.Personas.PersonaDomicilio>(new Data.BusinessEntities.Personas.PersonaDomicilio()
        //        {
        //            DomicilioId = id,
        //            PersonaId = persona,
        //            TipoDomicilioId = tipodomicilio,
        //            _Id_Usuario = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario,
        //            _Ip = Request.UserHostAddress,
        //            _Machine_Name = machineName
        //        }, new JsonMediaTypeFormatter())
        //    };
        //    return cliente.SendAsync(httpMSG).Result.EnsureSuccessStatusCode().StatusCode.ToString();
        //}

        //public List<TiposProfesionesModel> GetDatosTiposProfesiones()
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/TipoProfesionService/GetTiposProfesion").Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<TiposProfesionesModel>)resp.Content.ReadAsAsync<IEnumerable<TiposProfesionesModel>>().Result;
        //}

        //public JsonResult GetProfesionesJson(long id)
        //{
        //    return Json(GetDatosProfesionById(id));
        //}
        //public List<ProfesionModel> GetDatosProfesionById(long id)
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/ProfesionService/GetProfesionByPersona/" + id).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<ProfesionModel>)resp.Content.ReadAsAsync<IEnumerable<ProfesionModel>>().Result;
        //}

        //Tipos
        public List<SelectListItem> GetTipoPersonas()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "Física", Value = "1", Selected = true },
                new SelectListItem { Text = "Jurídica", Value = "2" }
            };
        }
        public List<SelectListItem> GetTipoDocumentos()
        {
            using (var ctrl = new SeguridadController())
            {
                var tipos = ctrl.GetTipoDoc().Select(x => new SelectListItem { Text = x.Descripcion, Value = x.Id_Tipo_Doc_Ident.ToString() });
                return tipos.Prepend(new SelectListItem { Text = "", Value = "", Selected = true }).ToList();
            }
        }
        public List<SelectListItem> GetTipoEstadoCivil()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "Casado/a", Value = "1" },
                new SelectListItem { Text = "Separado/a", Value = "2" },
                new SelectListItem { Text = "Divorciado/a", Value = "3" },
                new SelectListItem { Text = "Viudo/a", Value = "4" },
                new SelectListItem { Text = "Soltero/a", Value = "5" },
                new SelectListItem { Text = "Sin Identificar", Value = "6", Selected = true }
            };
        }
        public List<SelectListItem> GetTipoNacionalidad()
        {
            using (var resp = cliente.GetAsync("api/NacionalidadService/GetNacionalidades").Result)
            {
                resp.EnsureSuccessStatusCode();
                var nacionalidades = resp.Content.ReadAsAsync<IEnumerable<NacionalidadModel>>().Result;
                return nacionalidades.Select(x => new SelectListItem { Text = x.Descripcion, Value = x.NacionalidadId.ToString(), Selected = x.NacionalidadId == 1 }).ToList();
            }
        }
        //public List<SelectListItem> GetTipoProfesiones()
        //{
        //    List<SelectListItem> itemsTiposProf = new List<SelectListItem>();
        //    foreach (var tipo in GetDatosTiposProfesiones())
        //    {
        //        if (tipo.TipoProfesionId == 1)
        //        {
        //            itemsTiposProf.Add(new SelectListItem { Text = tipo.Descripcion, Value = Convert.ToString(tipo.TipoProfesionId), Selected = true });
        //        }
        //        else
        //        {
        //            itemsTiposProf.Add(new SelectListItem { Text = tipo.Descripcion, Value = Convert.ToString(tipo.TipoProfesionId) });
        //        }
        //    }
        //    return itemsTiposProf;
        //}

        public ActionResult GenerarReportePersona(long id)
        {
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            {
                HttpResponseMessage resp = apiReportes.GetAsync($"api/InformePersona/Get?idPersona={id}&usuario={usuario}").Result;
                if (!resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;

                var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytes64), $"InformePersona_{fecha}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
                //return AbrirReporte();
            }
        }
        public ActionResult GenerarReporteBienesRegistrados(long id)
        {
            long? idTramite = null;
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            {
                HttpResponseMessage resp = apiReportes.GetAsync($"api/InformeBienesRegistrados/GetInformeBienesRegistrados?idPersona={id}&usuario={usuario}&idTramite={idTramite}").Result;
                if (!resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;

                var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                ArchivoDescarga = new ArchivoDescarga(Convert.FromBase64String(bytes64), $"InformeBienesRegistrados{fecha}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
                //return AbrirReporte();
            }
        }
        public FileResult AbrirReporte()
        {
            Response.AppendHeader("Content-Disposition", new ContentDisposition { FileName = ArchivoDescarga.NombreArchivo, Inline = true }.ToString());
            return File(ArchivoDescarga.Contenido, ArchivoDescarga.MimeType);
        }
    }
}
