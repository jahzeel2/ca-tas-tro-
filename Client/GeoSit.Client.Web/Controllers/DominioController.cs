﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using GeoSit.Client.Web.Helpers.ExtensionMethods;
using GeoSit.Client.Web.Models.Dominio;
using GeoSit.Data.BusinessEntities.Inmuebles;
using Newtonsoft.Json;

namespace GeoSit.Client.Web.Controllers
{
    public class DominioController : Controller
    {
        private readonly HttpClient _client = new HttpClient();

        public DominioController()
        {
            _client.BaseAddress = new Uri(ConfigurationManager.AppSettings["WebApiUrl"]);
        }

        public ActionResult FormContent(DominioViewModel dominio, long? idClaseParcela)
        {
            using (var result = _client.GetAsync($"{MvcApplication.V2_API_PREFIX}/inscripciones/tipos/activos").Result)
            {
                result.EnsureSuccessStatusCode();
                var tipos = result.Content.ReadAsAsync<IEnumerable<TipoInscripcion>>().Result;
                bool esParcelaPrescripcion = new Parcela { ClaseParcelaID = idClaseParcela ?? (Session["Parcela"] as Parcela).ClaseParcelaID }.IsPrescripcion();
                var tiposFiltrados = tipos.FilterTiposIncripcion(esParcelaPrescripcion);

                if (dominio.Operacion == Data.BusinessEntities.LogicalTransactionUnits.Operation.Add)
                {
                    dominio.TipoInscripcionID = esParcelaPrescripcion ? tiposFiltrados.Single().TipoInscripcionID : 0;
                    dominio.Fecha = DateTime.Today;
                    dominio.FechaHora = dominio.Fecha.ToShortDateString();
                }
                else
                {
                    if (tiposFiltrados.All(t => t.TipoInscripcionID != dominio.TipoInscripcionID))
                    {
                        tiposFiltrados = tiposFiltrados.Concat(tipos.Where(t => t.TipoInscripcionID == dominio.TipoInscripcionID));
                    }
                    dominio.Fecha = DateTime.Parse(dominio.FechaHora);
                }

                ViewData["EsPrescripcion"] = esParcelaPrescripcion;
                ViewData["TipoInscripciones"] = tiposFiltrados;
                return PartialView("Dominio", dominio);
            }
        }

        public ActionResult GetInscripcionRegexExample(string regex)
        {
            using (var result = _client.GetAsync($"api/genericoservice/RegexRandomGenerator?regex={regex}").Result)
            {
                result.EnsureSuccessStatusCode();
                string example = result.Content.ReadAsAsync<string>().Result;
                string mensajeRegex = $"El formato del campo Inscripción debe ser: {example}";
                return Json(new { ejemplo = JsonConvert.DeserializeObject(result.Content.ReadAsStringAsync().Result), message = mensajeRegex }, JsonRequestBehavior.AllowGet);
            }
        }

        public long GetUltimoDominioID()
        {
            var response = _client.GetAsync("api/Dominio/GetUltimoDominioID").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
            long ultimoDominioID;
            if (!long.TryParse(content, out ultimoDominioID))
            {
                throw new InvalidOperationException("No se pudo convertir el contenido de la respuesta a long.");
            }
            return ultimoDominioID;
        }
    }
}