using System;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Configuration;
using System.Linq;

namespace GeoSit.Client.Web.Controllers
{
    public class ProfesionesController : Controller
    {
        private readonly HttpClient cliente = new HttpClient();

        public ProfesionesController()
        {
            cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
        }

        // GET: /Profesiones/Index
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Profesiones/DatosProfesion
        public ActionResult DatosProfesion(ProfesionModel profesion)
        {
            ViewData["TiposProfesion"] = new SelectList(GetDatosTiposProfesion().OrderBy(x => x.Descripcion), "TipoProfesionId", "Descripcion", profesion?.TipoProfesionId);
            return PartialView(profesion);
        }

        public IEnumerable<TipoProfesioneModel> GetDatosTiposProfesion()
        {
            using (var resp = cliente.GetAsync("api/TipoProfesionService/GetTiposProfesion").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<IEnumerable<TipoProfesioneModel>>().Result;
            }
        }
    }
}
