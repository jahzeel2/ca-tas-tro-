using System;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Configuration;
using GeoSit.Data.BusinessEntities.Inmuebles;
using OA = GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using System.Net;
using System.Linq;
using GeoSit.Client.Web.Solr;
using Newtonsoft.Json;
using GeoSit.Client.Web.Helpers;
using System.Net.Http.Formatting;

namespace GeoSit.Client.Web.Controllers
{
    public class ParcelaGraficaController : Controller
    {
        private readonly static HttpClientHandler handler = new HttpClientHandler();
        private readonly HttpClient cliente;

        public ParcelaGraficaController()
        {
            cliente = new HttpClient(handler, false)
            {
                Timeout = TimeSpan.FromMinutes(5),
                BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"])
            };
        }

        // GET: ParcelaGrafica
        public ActionResult Index()
        {
            var items = GetDepartamentos()
                            .OrderBy(d => int.Parse(d.Codigo))
                            .Select(d => new SelectListItem()
                            {
                                Value = d.Codigo,
                                Text = d.Codigo.PadLeft(2, '0')
                            });
            ViewData["Departamentos"] = items;
            return PartialView("AdministradorParcelaGrafico");
        }

        public ActionResult Search(FormCollection values)
        {
            string valuePadding(string value, int length)
            {
                return (value ?? string.Empty).PadLeft(length, '0');
            }

            string nomenclatura = string.Join("", valuePadding(values["departamento"], 2),
                                                  valuePadding(values["circunscripcion"], 3),
                                                  valuePadding(values["seccion"], 2),
                                                  valuePadding(values["chacra"], 4),
                                                  valuePadding(values["quinta"], 4),
                                                  valuePadding(values["fraccion"], 4),
                                                  valuePadding(values["manzana"], 4),
                                                  valuePadding(values["parcela"], 5));

            var solrServer = new SolrServer()
            {
                UseDefaultBaseParams = true
            };

            solrServer.AddParam(new SolrParam("fq", "tipo:parcelas"));
            solrServer.AddParam(new SolrParam("fl", "id,featids,idpadre,uid,capa,nombre"));
            var result = JsonConvert.DeserializeObject(solrServer.Search(nomenclatura));

            var docs = (result as Newtonsoft.Json.Linq.JToken)["response"]["docs"] as Newtonsoft.Json.Linq.JArray;

            var list = new List<dynamic>();

            foreach (var doc in docs)
            {
                list.Add(new
                {
                    id = doc.Value<long>("id"),
                    nomenclatura = doc["nombre"].ToString(),
                    uid = doc["uid"].ToString(),
                    capa = doc["capa"]?.ToString(),
                    idpadre = doc.Value<long?>("idpadre"),
                    featids = (doc["featids"] as Newtonsoft.Json.Linq.JArray)?.Select(d => long.Parse(d.ToString())).ToArray()
                });
            }

            return Json(list);
        }

        // GET: /ParcelaGrafica/DatosParcelaGrafica
        public ActionResult DatosParcelaGrafica()
        {
            return PartialView();
        }

        public List<ParcelaGrafica> GetParcelasGrafica()
        {
            HttpResponseMessage resp = cliente.GetAsync("api/ParcelaGraficaService/GetParcelasGrafica").Result;
            resp.EnsureSuccessStatusCode();
            return (List<ParcelaGrafica>)resp.Content.ReadAsAsync<IEnumerable<ParcelaGrafica>>().Result;
        }

        public List<SelectListItem> GetTiposDivision()
        {
            List<SelectListItem> itemsTipos = new List<SelectListItem>();
            List<OA.TipoDivision> Tipos = GetAllTiposDivision();
            foreach (var item in Tipos)
            {
                itemsTipos.Add(new SelectListItem { Text = item.Nombre, Value = Convert.ToString(item.TipoObjetoId) });
            }
            return itemsTipos;
        }

        public List<OA.TipoDivision> GetAllTiposDivision()
        {
            HttpResponseMessage resp = cliente.GetAsync("api/TiposDivisionesAdministrativas/GetTiposDivision").Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<IEnumerable<OA.TipoDivision>>().Result.ToList();
        }


        public JsonResult GetParcelaGrafByParcelaJson(long parcelaId)
        {
            return Json(GetParcelaGrafByParcela(parcelaId));
        }
        public ParcelaGraficaModel GetParcelaGrafByParcela(long parcelaId)
        {
            try
            {
                HttpResponseMessage resp = cliente.GetAsync("api/ParcelaGraficaService/GetParcelaGraficaByParcelaId/" + parcelaId).Result;
                resp.EnsureSuccessStatusCode();
                return (ParcelaGraficaModel)resp.Content.ReadAsAsync<ParcelaGraficaModel>().Result;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                ParcelaGraficaModel ParcelaVacia = new ParcelaGraficaModel();
                ParcelaVacia.FeatID = 0;
                return ParcelaVacia;
            }

        }

        public JsonResult GetParcelaGrafByFeatidJson(long id)
        {
            return Json(GetParcelaGrafByFeatid(id));
        }
        public ParcelaGraficaModel GetParcelaGrafByFeatid(long id)
        {
            try
            {
                HttpResponseMessage resp = cliente.GetAsync("api/ParcelaGraficaService/GetParcelaGraficaById/" + id).Result;
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<ParcelaGraficaModel>().Result;
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("GetParcelaGrafByFeatid", ex);
                return new ParcelaGraficaModel() { FeatID = 0 };
            }

        }

        //[HttpPost]
        //public JsonResult GetManzanaCoordsByNomenclatura(string nomenclatura)
        //{
        //    var coords = string.Empty;
        //    DbGeometry geometryManzana = GetGeometryDivisionByNomenclatura(nomenclatura);
        //    if (geometryManzana != null)
        //    {
        //        bool isPoint = !geometryManzana.PointCount.HasValue;
        //        //var pointFormat = "{0},{1}";
        //        var pointFormat = "{0},{1},{2}";
        //        if (isPoint)
        //        {
        //            coords = string.Format(pointFormat, geometryManzana.XCoordinate, geometryManzana.YCoordinate, 0);
        //        }
        //        else
        //        {
        //            var lista = new List<string>();
        //            for (int i = 1; i <= geometryManzana.PointCount; i++)
        //            {
        //                var punto = geometryManzana.PointAt(i);
        //                lista.Add(string.Format(pointFormat, punto.XCoordinate, punto.YCoordinate));
        //            }
        //            coords = string.Join(",", lista.ToArray());
        //        }
        //    }
        //    //JsonResult jsonResult = new JsonResult
        //    //{
        //    //    Data = (string)resp.Content.ReadAsAsync<string>().Result
        //    //};
        //    JsonResult jsonResult = new JsonResult
        //    {
        //        Data = coords
        //    };
        //    return jsonResult;
        //}

        //[HttpPost]
        //public JsonResult GetManzanaCentroidCoordsByNomenclatura(string nomenclatura)
        //{
        //    var coords = string.Empty;
        //    DbGeometry geometryManzana = GetGeometryDivisionByNomenclatura(nomenclatura);
        //    if (geometryManzana != null)
        //    {
        //        DbGeometry geometryManzanaCentroid = geometryManzana.Centroid;
        //        var pointFormat = "{0},{1},{2}";
        //        coords = string.Format(pointFormat, geometryManzanaCentroid.XCoordinate, geometryManzanaCentroid.YCoordinate, 0);
        //    }
        //    //JsonResult jsonResult = new JsonResult
        //    //{
        //    //    Data = (string)resp.Content.ReadAsAsync<string>().Result
        //    //};
        //    JsonResult jsonResult = new JsonResult
        //    {
        //        Data = coords
        //    };
        //    return jsonResult;
        //}

        //public DbGeometry GetGeometryDivisionByNomenclatura(string nomenclatura)
        //{
        //    DbGeometry geometry = null;
        //    string parametros = string.Format("nomenclatura={0}", nomenclatura);

        //    HttpResponseMessage resp = cliente.GetAsync("api/DivisionService/GetGeometryWKTByNomenclatura?" + parametros).Result;
        //    try
        //    {
        //        resp.EnsureSuccessStatusCode();
        //        var wkt = resp.Content.ReadAsAsync<string>().Result;
        //        geometry = DbGeometry.FromText(wkt);
        //    }
        //    catch (Exception)
        //    {
        //        string msgErr = resp.ReasonPhrase;
        //    }
        //    return geometry;
        //}


        private OA.Objeto[] GetDepartamentos()
        {
            using (var resp = cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/ObjetosAdministrativos/Departamentos").Result)
            {
                var departamentos = new OA.Objeto[0];
                if (resp.IsSuccessStatusCode)
                {
                    departamentos = resp.Content.ReadAsAsync<IEnumerable<OA.Objeto>>().Result.ToArray();
                }

                return departamentos;
            }
        }


        public JsonResult GetNomenclaturasJson(string nomenclatura, long tipo_division)
        {
            return Json(GetGetNomenclaturasByDivision(nomenclatura, tipo_division));
        }
        public List<NomenclaturaModel> GetGetNomenclaturasByDivision(string nombre_completo, long tipo_division)
        {
            HttpResponseMessage resp = cliente.GetAsync("api/NomenclaturaService/GetNomenclaturaByNombre/" + nombre_completo).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<NomenclaturaModel>>().Result;
        }

        // Recupera las divisiones de acuerdo al tipo y nomenclatura.
        public JsonResult GetDivisionByNomenclaturaJson(string nomenclatura)
        {
            return Json(GetDivisionByNomenclatura(nomenclatura));
        }
        public List<DivisionModel> GetDivisionByNomenclatura(string nomenclatura)
        {
            HttpResponseMessage resp = cliente.GetAsync(string.Format("api/DivisionService/GetNomenclaturaByNomenclatura/{0}", nomenclatura)).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<DivisionModel>>().Result;
        }

        [HttpPost]
        public ActionResult Save(long featid, long idparcela, string operation)
        {
            var parcela = new ParcelaGrafica()
            {
                FeatID = featid,
                _Id_Usuario = ((UsuariosModel)Session["usuarioPortal"]).Id_Usuario,
                _Ip = Request.UserHostAddress,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress)
            };

            var request = new HttpRequestMessage()
            {
                Method = operation == "A" ? HttpMethod.Post : HttpMethod.Delete,
                RequestUri = new Uri($"{cliente.BaseAddress}{MvcApplication.V2_API_PREFIX}/Parcelas/{idparcela}/Grafico"),
                Content = new ObjectContent<ParcelaGrafica>(parcela, new JsonMediaTypeFormatter())
            };
            
            using (var resp = cliente.SendAsync(request).Result)
            {
                if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.ExpectationFailed)
                {
                    MvcApplication.GetLogger().LogError("ParcelaGraficaController-ParcelaGraficaSave", new Exception("Error al asociar/desasociar parcela"));
                    return new HttpStatusCodeResult(resp.StatusCode);
                }
                return Json(new { ok = resp.IsSuccessStatusCode });
            }
        }

        //public JsonResult ParcelaGraficaDeleteJson(long id, long parcela_id, string fecha, int usuario)
        //{
        //    return Json(ParcelaGraficaDelete(id, parcela_id, fecha, usuario));
        //}

        //public string ParcelaGraficaDelete(long id, long parcela_id, string fecha, int usuario)
        //{
        //    ParcelaGraficaModel parcela = new ParcelaGraficaModel();
        //    parcela.FeatID = id;
        //    parcela.ParcelaID = parcela_id;
        //    //parcela.FechaModificacion = Convert.ToDateTime(fecha);
        //    DateTime fechaActual = DateTime.Now;
        //    //parcela.FechaModificacion = Convert.ToDateTime(fecha);
        //    parcela.FechaModificacion = fechaActual;
        //    parcela.UsuarioModificacionID = usuario;
        //    //parcela.FechaBaja = Convert.ToDateTime(fecha);
        //    parcela.FechaBaja = fechaActual;
        //    parcela.UsuarioBajaID = usuario;
        //    parcela.ClassID = 105;
        //    parcela.RevisionNumber = 0;

        //    using (HttpResponseMessage resp = cliente.PostAsJsonAsync("api/ParcelaGraficaService/ParcelaGrafica_Delete", parcela).Result)
        //    {
        //        if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.ExpectationFailed)
        //        {
        //            return HttpStatusCode.OK.ToString();
        //        }
        //        else
        //        {
        //            throw new Exception(resp.StatusCode.ToString());
        //        }
        //    }
        //}

        public JsonResult GetParcelaVistaJson(long id)
        {
            return Json(GetParcelaVista(id));
        }
        public string GetParcelaVista(long id)
        {
            HttpResponseMessage resp = cliente.GetAsync("api/ParcelaGraficaService/GetNomenclaturaVista/" + id).Result;
            resp.EnsureSuccessStatusCode();
            return (string)resp.Content.ReadAsAsync<string>().Result;
        }

        public JsonResult GetDivisionFeatIdByParcelaFeatid(long featIdParc)
        {
            long featidManz = 0;
            HttpResponseMessage resp = cliente.GetAsync(string.Format("api/ParcelaGraficaService/GetDivisionFeatIdByParcelaFeatid?featIdParc={0}", featIdParc)).Result;
            try
            {
                resp.EnsureSuccessStatusCode();
                featidManz = (long)resp.Content.ReadAsAsync<long>().Result;
            }
            catch (Exception)
            {
                string msgErr = resp.ReasonPhrase;
            }
            JsonResult jsonResult = new JsonResult
            {
                Data = featidManz
            };
            return jsonResult;
        }

        // Determina el centro de la parcela gráfica.
        public JsonResult GetCentroParcelaJson(long id)
        {
            return Json(GetCentroParcela(id));
        }
        public string GetCentroParcela(long id)
        {
            HttpResponseMessage resp = cliente.GetAsync("api/ParcelaGraficaService/GetCentroParcela/" + id).Result;
            resp.EnsureSuccessStatusCode();
            return (string)resp.Content.ReadAsAsync<string>().Result;
        }

        //public List<PersonaModel> GetDatosPersonaByAll(string id)
        //{
        //    HttpResponseMessage resp = cliente.GetAsync("api/PersonaService/GetDatosPersonaByAll/" + id).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return (List<PersonaModel>)resp.Content.ReadAsAsync<IEnumerable<PersonaModel>>().Result;
        //}
    }
}