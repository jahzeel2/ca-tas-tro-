using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CaptchaMvc.HtmlHelpers;
using GeoSit.Client.Web.Helpers;
using GeoSit.Client.Web.Models;
using GeoSit.Data.BusinessEntities.Seguridad;
using Resources;
using GeoSit.Data.BusinessEntities.GlobalResources;

namespace GeoSit.Client.Web.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
    public class AccountController : Controller
    {
        private bool BlockedSession
        {
            get { return (bool)Session["blocked"]; }
            set { Session["blocked"] = value; }
        }
        private HttpClient cliente = new HttpClient();

        public AccountController()
        {
            cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
        }
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login()
        {
            //Se cambia login de windows por login por LDAP, o sea, se pasa
            //mediante form normal, usuario y password de sistema operativo.
            //por cuestiones de seguridad, debería ser imperativo que el sitio
            //se publique mediante HTTPS para evitar que el usuario y password
            //del S.O. viajen sin seguridad

            var pgm = new SeguridadController().GetParametrosGenerales();
            bool esAD = pgm.Where(x => x.Descripcion == "Active_Directory").Select(x => x.Valor).FirstOrDefault() == "1";
            ViewData["EsAD"] = Session["EsAD"] = esAD;
            return View();
        }

        [AllowAnonymous]
        public ActionResult ExpiredSession()
        {
            BlockedSession = false;
            return View();
        }
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(Usuario model)
        {
            bool esAD = (bool)Session["EsAD"];
            BlockedSession = false;

            string error;
            UsuariosModel usuario = null;
            string view = "Login";
            try
            {
                usuario = GetLogin(model.Login, model.Password, out error);
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
            }
            if (usuario == null)
            {
                ModelState.AddModelError("", $"Acceso restringido. {error}");
                return View(view);
            }

            void registrarLoginInvalido(long idUsuario, string errorMsg)
            {
                ModelState.Clear();
                Session["usuarioPortal"] = null;
                MvcApplication.GetLogger().LogInfo(errorMsg);
                AuditoriaHelper.Register(idUsuario, errorMsg, Request, TiposOperacion.Login, Autorizado.No, Eventos.Login);
            }
            var sc = new SeguridadController();
            var pgl = sc.GetParametrosGenerales();

            #region Validacion de Funciones y Horarios Habilitados
            var funcionesHabilitadas = new List<PerfilFuncion>();
            DateTime ahora = DateTime.Now;
            string dia = string.Empty;
            if (sc.GetFeriadoByFecha(ahora.Ticks) != null) //es feriado
            {
                dia = "FE";
            }
            else
            {
                switch (ahora.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        dia = "LU";
                        break;
                    case DayOfWeek.Tuesday:
                        dia = "MA";
                        break;
                    case DayOfWeek.Wednesday:
                        dia = "MI";
                        break;
                    case DayOfWeek.Thursday:
                        dia = "JU";
                        break;
                    case DayOfWeek.Friday:
                        dia = "VI";
                        break;
                    case DayOfWeek.Saturday:
                        dia = "SA";
                        break;
                    case DayOfWeek.Sunday:
                        dia = "DO";
                        break;
                    default:
                        break;
                }
            }
            funcionesHabilitadas = sc.GetUsuariosPerfiles(usuario.Id_Usuario)
                                     .Where(p => p.Horarios
                                                  .HorariosDetalle
                                                  .Any(j => j.Hora_Inicio.TimeOfDay < ahora.TimeOfDay &&
                                                            j.Hora_Fin.TimeOfDay > ahora.TimeOfDay &&
                                                            j.Dia == dia))
                                     .SelectMany(p => sc.GetFuncionesByPerfil(p.Id_Perfil))
                                     .OrderBy(x => x.Id_Funcion)
                                     .ToList();

            if (!funcionesHabilitadas.Any())
            {
                error = $"El usuario {model.Login} intenta ingresar fuera de horario.";
                registrarLoginInvalido(usuario.Id_Usuario, error);
                ModelState.AddModelError("", "Acceso restringido. Ingreso fuera de horario.");
                return View(view, model);
            }
            Session["FuncionesHabilitadas"] = funcionesHabilitadas;
            #endregion

            Session["usuarioPortal"] = usuario;

            if (!esAD && usuario.Cambio_pass)
            {
                return View("ChangePassword");
            }
            else if (!esAD)
            {
                if (int.TryParse(pgl.Where(x => x.Descripcion == "Vigencia de clave en dias").FirstOrDefault()?.Valor, out int vigenciaClave)
                    && vigenciaClave > 0 && usuario.Fecha_Operacion != null)
                {
                    DateTime? fechaVencClave = usuario.Fecha_Operacion.Value.AddDays(vigenciaClave);
                    if (fechaVencClave < DateTime.Now)
                    {
                        error = "Contraseña actual vencida. Ingrese una nueva contraseña.";
                        registrarLoginInvalido(usuario.Id_Usuario, error);
                        ModelState.AddModelError("Login", error);
                        return View("ChangePassword");
                    }
                    int diasVenc = (fechaVencClave.Value - DateTime.Now).Days;
                    if (int.TryParse(pgl.Where(x => x.Descripcion == "Aviso de vencimiento en días").FirstOrDefault()?.Valor, out int diasAnticipoAviso)
                        && diasAnticipoAviso > 0 && diasVenc <= diasAnticipoAviso)
                    {
                        ViewBag.Title = Recursos.TituloMensajesAviso;
                        ViewBag.Description = $"Su contraseña expira en {diasVenc} días.";
                        ViewBag.ReturnUrl = Url.Action("Index", "Home");
                        return View("InformationMessageView");
                    }
                }
            }

            usuario.Ip = Request.UserHostAddress;
            usuario.Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            usuario.Token = SetUsuariosActivos(usuario.Id_Usuario).Token;

            FormsAuthentication.SetAuthCookie(usuario.Login, false);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult LogOff()
        {
            Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, "") { Expires = DateTime.Now.AddYears(-1) });
            Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", "") { Expires = DateTime.Now.AddYears(-1) });

            FormsAuthentication.SignOut();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }
        //

        private UsuariosModel GetLogin(string login, string pass, out string error)
        {
            error = string.Empty;
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string,string>("user", login),
                new KeyValuePair<string,string>("pass", pass),
                new KeyValuePair<string,string>("ip", Request.UserHostAddress),
                new KeyValuePair<string,string>("machineName", AuditoriaHelper.ReverseLookup(Request.UserHostAddress)),

            });

            using (var resp = cliente.PostAsync($"api/SeguridadService/LoginUsuario", content).Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    error = resp.Content.ReadAsAsync<string>().Result;
                    MvcApplication.GetLogger().LogError("GetLogin", new Exception($"{error}"));
                    return null;
                }
                return resp.Content.ReadAsAsync<UsuariosModel>().Result;
            }
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View(new ValidarUsuarioModel() { Mensaje = "" });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ValidatePass(ValidarUsuarioModel model)
        {
            if (!this.IsCaptchaValid("El Captcha es incorrecto."))
            {
                model.Mensaje = "El código no es válido.";
                return View("ForgotPassword", model);
            }
            if (!TryResetPassword(model, out string[] datos))
            {
                model.Mensaje = Recursos.DNIInexistente;
                if (datos?.Any() ?? false)
                {
                    model.Mensaje = string.Join("<br /> ", datos);
                }
                return View("ForgotPassword", model);
            }
            ViewBag.Title = Recursos.TituloMensajesAviso;
            ViewBag.Description = Recursos.MailRecuperoPassword;
            return View("InformationMessageNotLogonView");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ValidateAccount(ValidarUsuarioModel model)
        {
            ViewData["account"] = 1;
            if (!this.IsCaptchaValid("El Captcha es incorrecto."))
            {
                model.Mensaje = "El código no es válido.";
                return View("ForgotPassword", model);
            }
            if(!TryRecoverLogin(model, out string[] datos)) 
            {
                model.Mensaje = Recursos.DNIInexistente;
                if (datos?.Any() ?? false)
                {
                    model.Mensaje = string.Join("<br /> ", datos);
                }
                return View("ForgotPassword", model);
            }
            ViewBag.Title = Recursos.TituloMensajesAviso;
            ViewBag.Description = Recursos.MailRecuperoUsuario;
            return View("InformationMessageNotLogonView");
        }

        private bool TryRecoverLogin(ValidarUsuarioModel model, out string[] datos)
        {
            datos = null;
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("dni", model.Nro_doc) });
            using (var resp = cliente.PostAsync($"api/SeguridadService/Usuarios/AccountLogin/Recover", content).Result)
            {
                if (resp.IsSuccessStatusCode)
                {
                    datos = resp.Content.ReadAsAsync<string[]>().Result;
                }
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    datos = new[] { resp.Content.ReadAsAsync<string>().Result };
                }
                return resp.IsSuccessStatusCode;
            }
        }

        private bool TryResetPassword(ValidarUsuarioModel model, out string[] datos)
        {
            datos = null;
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("dni", model.Nro_doc) });
            using (var resp = cliente.PostAsync($"api/SeguridadService/Usuarios/Password/Reset", content).Result)
            {
                if (resp.IsSuccessStatusCode)
                {
                    datos = resp.Content.ReadAsAsync<string[]>().Result;
                }
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    datos = new[] { resp.Content.ReadAsAsync<string>().Result };
                }
                return resp.IsSuccessStatusCode;
            }
        }
        public UsuariosActivos SetUsuariosActivos(long userId)
        {
            using (var resp = cliente.GetAsync("api/GenericoService/SetUsuariosActivos/" + userId).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<UsuariosActivos>().Result;
            }
        }
        private UsuariosModel GetUsuarioByNroDoc(string Nro_Doc)
        {
            using (var resp = cliente.GetAsync("api/SeguridadService/GetUsuarioByNroDoc/" + Nro_Doc).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<UsuariosModel>().Result;
            }
        }

        private HttpResponseMessage GetUpdatePasswordResponse(Usuario model)
        {
            long idUsuario = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario;
            var cambio = new CambioPassword()
            {
                Vigente = model.Password,
                Nueva = model.NewPassword,
                Confirmacion = model.ConfirmNewPassword
            };
            return cliente.PutAsJsonAsync($"api/SeguridadService/Usuarios/{idUsuario}/Password", cambio).Result;
        }

        [HttpGet]
        public ActionResult UsuarioCambiarPassword()
        {
            return PartialView("UserChangePassword");
        }

        [HttpPost]
        public ActionResult UsuarioCambiarPassword(Usuario model)
        {
            using (var resp = GetUpdatePasswordResponse(model))
            {
                string result = string.Empty;
                if (!resp.IsSuccessStatusCode)
                {
                    result = resp.StatusCode == HttpStatusCode.InternalServerError
                                    ? "Ha ocurrido un error al cambiar la contraseña."
                                    : resp.Content.ReadAsAsync<IEnumerable<string>>().Result.FirstOrDefault();
                }

                return Json(result);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CambiarPassword(Usuario model)
        {
            if (ModelState.IsValid)
            {
                using (var resp = GetUpdatePasswordResponse(model))
                {
                    ViewResult view;
                    if (resp.IsSuccessStatusCode)
                    {
                        ViewBag.Title = Recursos.TituloMensajesAviso;
                        ViewBag.Description = "Su contraseña se actualizó correctamente.";
                        ViewBag.ReturnUrl = Url.Action("Index", "Home");

                        view = View("InformationMessageView");
                    }
                    else
                    {
                        var errores = resp.Content.ReadAsAsync<IEnumerable<string>>().Result;
                        string state = resp.StatusCode == HttpStatusCode.BadRequest ? "NewPassword" : "Password";

                        foreach (string error in errores)
                        {
                            ModelState.AddModelError(state, error);
                        }

                        view = View("ChangePassword", model);
                    }
                    return view;
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }
    }
}