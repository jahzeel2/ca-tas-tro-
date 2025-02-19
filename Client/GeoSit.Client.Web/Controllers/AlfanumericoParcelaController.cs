using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using GeoSit.Data.BusinessEntities.Inmuebles;
using OA = GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using System.Threading.Tasks;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Client.Web.ViewModels;

namespace GeoSit.Client.Web.Controllers
{
    public class AlfanumericoParcelaController : Controller
    {
        private readonly HttpClient _cliente = new HttpClient();
        private readonly MantenimientoParcelarioController mantenedorParcelarioCtrl = new MantenimientoParcelarioController();

        private UsuariosModel Usuario
        {
            get { return (UsuariosModel)Session["usuarioPortal"]; }
        }

        public AlfanumericoParcelaController()
        {
            _cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
            _cliente.Timeout = TimeSpan.FromMinutes(60);
        }

        async public Task<ActionResult> Index()
        {
            /*
            var operaciones = GetOperaciones();
            var departamentos = GetDepartamentos();
            var tiposParcela = GetTiposParcela();
            var emptyEstadosParcela = GetEstadosParcelaByOperacion();
            var emptyHijosDepartamento = GetCircunscripcionesDepartamento();

            ViewData["Operaciones"] = (await operaciones).ToSelectListItems();
            ViewData["TiposParcela"] = (await tiposParcela).ToSelectListItems();
            ViewData["Departamentos"] = (await departamentos).ToSelectListItems();

            ViewData["EstadosParcela"] = (await emptyEstadosParcela).ToSelectListItems();
            ViewData["Secciones"] = ViewData["Circunscripciones"] = (await emptyHijosDepartamento).ToSelectListItems();
            */
            ViewData["TiposParcela"] = mantenedorParcelarioCtrl.GetTiposParcelas();
            ViewData["ClasesParcela"] = mantenedorParcelarioCtrl.GetClasesParcelas();
            ViewData["TiposUnidadTributaria"] = mantenedorParcelarioCtrl.GetTiposUnidadTributaria();


            return PartialView();
        }

        async public Task<ActionResult> Save(OperacionAlfanumerica operacion)
        {
            operacion.IdUsuario = Usuario.Id_Usuario;
            operacion.Ip = Request.UserHostAddress;
            operacion.MachineName = Helpers.AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/OperacionParcelaria", operacion))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                }
                string error = await resp.Content.ReadAsAsync<string>();
                return Json(new { error });
            }
        }
        async public Task<ActionResult> GetCircunscripciones(long departamento)
        {
            return Json((await GetCircunscripcionesDepartamento(departamento)).ToSelectListItems(), JsonRequestBehavior.AllowGet);
        }

        async public Task<ActionResult> GetSecciones(long circunscripcion)
        {
            return Json((await GetSeccionesCircunscripcion(circunscripcion)).ToSelectListItems(), JsonRequestBehavior.AllowGet);
        }

        async public Task<ActionResult> GetOperacionInitializationData(long operacion)
        {
            var estados = GetEstadosParcelaByOperacion(operacion);
            var claseParcela = GetClaseParcelaByOperacion(operacion);

            return Json(new { estados = (await estados).ToSelectListItems(), claseParcela = await claseParcela }, JsonRequestBehavior.AllowGet);
        }

        async public Task<ActionResult> ValidateDestino(FormCollection values)
        {
            long.TryParse(values["IdTipoParcela"], out long idTipoParcela);
            long.TryParse(values["IdDepartamento"], out long featidDepartamento);
            long.TryParse(values["IdCircunscripcion"], out long featidCircunscripcion);
            long.TryParse(values["IdSeccion"], out long featidSeccion);
            var validar = new NomenclaturaValidable()
            {
                IdTipoParcela = idTipoParcela,
                FeatIdCircunscripcion = featidCircunscripcion,
                FeatIdDepartamento = featidDepartamento,
                FeatIdSeccion = featidSeccion,
                Chacra = values["Chacra"] ?? "",
                Fraccion = values["Fraccion"] ?? "",
                Quinta = values["Quinta"] ?? "",
                Manzana = values["Manzana"] ?? "",
                Parcela = values["Parcela"] ?? "",
                Partida = values["Partida"] ?? "",
            };
            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/OperacionParcelaria/Destino/Validar", validar))
            {
                if (resp.IsSuccessStatusCode)
                {
                    string nomenclatura = await resp.Content.ReadAsAsync<string>();
                    return Json(new { nomenclatura });
                }
                string error = await resp.Content.ReadAsAsync<string>();

                return Json(new { error });
            }
        }

        async public Task<IEnumerable<EstadoParcela>> GetEstadosParcelaByOperacion(long operacion = 0)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Mensuras/Tipos/{operacion}/Parcelas/Estados"))
            {
                return new[] { new EstadoParcela() { Descripcion = "-- Seleccione --", EstadoParcelaID = 0 } }
                        .Concat((await resp.Content.ReadAsAsync<IEnumerable<EstadoParcela>>()).OrderBy(ep => ep.Descripcion));
            }
        }

        async private Task<ClaseParcela> GetClaseParcelaByOperacion(long operacion)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Mensuras/Tipos/{operacion}/Parcelas/Clase"))
            {
                return await resp.Content.ReadAsAsync<ClaseParcela>();
            }
        }

        async private Task<IEnumerable<OA.Objeto>> GetDepartamentos()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/ObjetosAdministrativos/Departamentos"))
            {
                return new[] { new OA.Objeto() { Nombre = "-- Seleccione --", FeatId = 0 } }
                        .Concat((await resp.Content.ReadAsAsync<IEnumerable<OA.Objeto>>()).OrderBy(o => o.Nombre));
            }
        }

        async private Task<IEnumerable<OA.Objeto>> GetCircunscripcionesDepartamento(long departamento = 0)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/ObjetosAdministrativos/Departamentos/{departamento}/Circunscripciones"))
            {
                return new[] { new OA.Objeto() { Nombre = "-- Seleccione --", FeatId = 0 } }
                        .Concat((await resp.Content.ReadAsAsync<IEnumerable<OA.Objeto>>()).OrderBy(o => o.Nombre));
            }
        }

        async private Task<IEnumerable<OA.Objeto>> GetSeccionesCircunscripcion(long circunscripcion = 0)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/ObjetosAdministrativos/Circunscripciones/{circunscripcion}/Secciones"))
            {
                return new[] { new OA.Objeto() { Nombre = "-- Seleccione --", FeatId = 0 } }
                        .Concat((await resp.Content.ReadAsAsync<IEnumerable<OA.Objeto>>()).OrderBy(o => o.Nombre));
            }
        }

        async private Task<IEnumerable<TipoMensura>> GetOperaciones()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Mensuras/Tipos/Saneamiento"))
            {
                return new[] { new TipoMensura() { Descripcion = "-- Seleccione --", IdTipoMensura = 0 } }
                            .Concat((await resp.Content.ReadAsAsync<IEnumerable<TipoMensura>>()).OrderBy(tpo => tpo.Descripcion));
            }
        }

        async private Task<IEnumerable<TipoParcela>> GetTiposParcela()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/Tipos"))
            {
                return new[] { new TipoParcela() { Descripcion = "-- Seleccione --", TipoParcelaID = 0 } }
                            .Concat((await resp.Content.ReadAsAsync<IEnumerable<TipoParcela>>()).OrderBy(tpo => tpo.Descripcion));
            }
        }

        public ActionResult ValidarNomenclatura(string nomenclatura, long tipoParcelaID, long claseParcelaID, int tipoUTid, int cantidadUF)
        {
            string mensaje = string.Empty;

            var resultadoValidacion = ValidarNomenclaturaApi(nomenclatura);

            if (resultadoValidacion != null)
            {
                mensaje = ProcesarResultadoValidacion(resultadoValidacion);
                if (!string.IsNullOrEmpty(mensaje))
                {
                    return Json(new { mensaje }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                mensaje = "Error al validar la nomenclatura.";
                return Json(new { mensaje }, JsonRequestBehavior.AllowGet);
            }

            var parcela = CrearParcela(tipoParcelaID, claseParcelaID, nomenclatura);
            var unidadTributaria = CrearUnidadTributaria(tipoUTid);

            CargarViewData(parcela, cantidadUF);

            ParcelaUTViewModel parcelaUTViewModel = new ParcelaUTViewModel
            {
                Parcela = parcela,
                UnidadTributaria = unidadTributaria
            };

            return PartialView("~/Views/AlfanumericoParcela/AltaAlfanumerica.cshtml", parcelaUTViewModel);
        }

        private dynamic ValidarNomenclaturaApi(string nomenclatura)
        {
            using (var resp = _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/Nomenclatura/Validar?nomenclatura={nomenclatura.ToUpper()}").Result)
            {
                if (resp.IsSuccessStatusCode)
                {
                    return resp.Content.ReadAsAsync<dynamic>().Result;
                }
            }
            return null;
        }

        private string ProcesarResultadoValidacion(dynamic resultado)
        {
            if (resultado.ExisteEnAmbas.Value)
            {
                return "La nomenclatura existe tanto en la parcela alfanumérica como en la gráfica.";
            }
            else if (resultado.ExisteEnParcelaAlfanumerica.Value)
            {
                return "La nomenclatura existe en la parcela alfanumérica.";
            }
            else if (resultado.ExisteEnParcelaGrafica.Value)
            {
                return "La nomenclatura existe en la parcela gráfica.";
            }

            return string.Empty;
        }

        private Data.BusinessEntities.Inmuebles.Parcela CrearParcela(long tipoParcelaID, long claseParcelaID, string nomenclatura)
        {
            var parcela = new Data.BusinessEntities.Inmuebles.Parcela
            {
                ParcelaID = 0,
                Superficie = 0,
                TipoParcelaID = tipoParcelaID,
                ClaseParcelaID = claseParcelaID,
                Nomenclaturas = new List<Nomenclatura>
                {
                    new Nomenclatura
                    {
                        Nombre = nomenclatura,
                        ParcelaID = 0,
                        TipoNomenclaturaID = 0,
                        Tipo = new TipoNomenclatura
                        {
                            TipoNomenclaturaID = 0,
                            Descripcion = "NO ESPECIFICADO",
                            ExpresionRegular = "(?<Dep>[0-9]{2})(?<Circ>[0-9]{3})(?<Sec>[0-9a-zA-Z]{2})(?<Cha>[0-9a-zA-Z]{4})(?<Qui>[0-9a-zA-Z]{4})(?<Fra>[0-9a-zA-Z]{4})(?<Man>[0-9a-zA-Z]{4})(?<Par>[0-9a-zA-Z]{5})",
                            Observaciones = ""
                        }
                    }
                }
            };
            return parcela;
        }

        private UnidadTributaria CrearUnidadTributaria(int tipoUTid)
        {
            return new UnidadTributaria
            {
                UnidadTributariaId = 0,
                CodigoProvincial = "0",
                UnidadFuncional = "",
                TipoUnidadTributariaID = tipoUTid,
                Piso = "0",
                Observaciones = "",
                PorcentajeCopropiedad = 100
            };
        }

        private void CargarViewData(Data.BusinessEntities.Inmuebles.Parcela parcela, int cantidadUF)
        {
            ViewData["Parcela"] = parcela.Nomenclaturas.OrderByDescending(n => n.FechaAlta.GetValueOrDefault()).First().GetNomenclaturas()["Par"].ToUpper();
            ViewData["TiposParcela"] = mantenedorParcelarioCtrl.GetTiposParcelas();
            ViewData["ClasesParcela"] = mantenedorParcelarioCtrl.GetClasesParcelas();
            ViewData["Nomenclatura"] = parcela.Nomenclaturas.OrderByDescending(n => n.FechaAlta.GetValueOrDefault()).First().GetNomenclaturas();
            ViewData["Observaciones"] = "";
            ViewData["TiposUnidadTributaria"] = mantenedorParcelarioCtrl.GetTiposUnidadTributaria();
            ViewData["CantidadUF"] = cantidadUF;
        }

        /*
        public JsonResult FormatNomenclatura(long idParcela, string nomenclatura)
        {
            var result = _cliente.GetAsync("api/parcela/getparcelabyid/" + idParcela).Result;
            result.EnsureSuccessStatusCode();
            var parcela = result.Content.ReadAsAsync<Parcela>().Result;

            var nomenclaturas = parcela.Nomenclaturas.First(x => x.Nombre == nomenclatura).GetNomenclaturas();
            //var str = nomenclaturas.Select(x => x.Key + ": " + x.Value).Aggregate((s1, s2) => s1 + " " + s2);

            return new JsonResult
            {
                Data = new
                {
                    nomenclatura = nomenclaturas.Select(x => x.Key + ": " + x.Value).Aggregate((s1, s2) => s1 + " " + s2),
                    idParcela
                }
            };
        }
                public ActionResult ValidarPartida(string numero)
                {
                    string partida = $"{Session["CODIGO_JURISDICCION"]}{numero}{Session["TIPO_PARCELA"]}".ToUpper();

                    if (UnidadAlfanumericoParcela.OperacionesParcelasDestinos.Any(x => x.Item.UnidadesTributarias.Any(u => u.CodigoProvincial == partida)))
                    {
                        return new HttpStatusCodeResult(System.Net.HttpStatusCode.Conflict);
                    }
                    return Content(partida);
                }

                public ActionResult SaveParcelaOrigen(long operacion, long idParcela)
                {
                    if (operacion != Convert.ToInt64(TiposParcelaOperacion.Creacion))
                    {
                        using (var result = _cliente.GetAsync($"api/parcela/{idParcela}/simple").Result)
                        {
                            result.EnsureSuccessStatusCode();
                            var parcela = result.Content.ReadAsAsync<Parcela>().Result;
                            UnidadAlfanumericoParcela.OperacionesParcelasOrigenes.Add(new OperationItem<Parcela>
                            {
                                Operation = Operation.Add,
                                Item = parcela
                            });
                            return Json(new { id = idParcela, idTipo = parcela.TipoParcelaID, idClase = parcela.ClaseParcelaID });
                        }
                    }
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                }

                public ActionResult DeleteParcelaOrigen(long idParcela)
                {
                    int idx = 0;
                    if ((idx = UnidadAlfanumericoParcela.OperacionesParcelasOrigenes.FindIndex(x => x.Item.ParcelaID == idParcela)) != -1)
                    {
                        UnidadAlfanumericoParcela.OperacionesParcelasOrigenes.RemoveAt(idx);
                        return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                    }
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                }

                public ActionResult SaveParcelaDestino(string partida, long idTipoOperacion, long idParcela, long idTipoParcela,
                    long idClaseParcela, long idEstadoParcela, double? superficie, string expedienteCreacion, string fechaCreacion)
                {
                    using (_cliente)
                    using (var resp = _cliente.GetAsync($"api/UnidadTributaria/GetPartidaDisponible?idUnidadTributaria={-1}&partida={partida}").Result)
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            return new HttpStatusCodeResult(System.Net.HttpStatusCode.Conflict);
                        }

                        using (var result = _cliente.GetAsync("api/parametro/getparametro/32").Result)
                        {
                            var origen = result.Content.ReadAsAsync<ParametrosGenerales>().Result;
                            int tipoUtId = (idClaseParcela == 5 && idTipoOperacion == 4) ? 2 : 1;
                            var parcela = new Parcela
                            {
                                ParcelaID = idParcela,
                                Superficie = idTipoOperacion == 4 ? Convert.ToDecimal(superficie) : 0,
                                TipoParcelaID = idTipoParcela,
                                ClaseParcelaID = idClaseParcela,
                                EstadoParcelaID = idEstadoParcela,
                                OrigenParcelaID = origen.Valor.AsInt(),
                                ExpedienteAlta = expedienteCreacion,
                                FechaAltaExpediente = !fechaCreacion.IsEmpty() ? DateTime.Parse(fechaCreacion) : (DateTime?)null,
                                UnidadesTributarias = new List<UnidadTributaria>
                                                        {
                                                            new UnidadTributaria { CodigoProvincial = partida,
                                                                                   TipoUnidadTributariaID = tipoUtId,
                                                                                   Superficie = idTipoOperacion == 4 ? superficie : 0 }
                                                        },
                                UsuarioModificacionID = Usuario.Id_Usuario

                            };

                            bool AfectaPH = false;
                            AfectaPH = (idClaseParcela == 5 && idTipoOperacion == 4) ? true : false;
                            parcela.AtributosCrear(string.Empty, AfectaPH);

                            UnidadAlfanumericoParcela.OperacionesParcelasDestinos.Add(new OperationItem<Parcela>
                            {
                                Operation = Operation.Add,
                                Item = parcela
                            });
                        }
                        return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                    }
                }

                public ActionResult DeleteParcelaDestino(long idParcela)
                {
                    int idx = 0;
                    if ((idx = UnidadAlfanumericoParcela.OperacionesParcelasDestinos.FindIndex(x => x.Item.ParcelaID == idParcela)) != -1)
                    {
                        UnidadAlfanumericoParcela.OperacionesParcelasDestinos.RemoveAt(idx);
                        return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                    }
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                }

                public ActionResult Save(int operacion, string expediente, string fecha, string vigencia)
                {
                    UnidadAlfanumericoParcela.Operacion = operacion;
                    UnidadAlfanumericoParcela.Expediente = expediente;
                    UnidadAlfanumericoParcela.Fecha = !fecha.IsEmpty() ? DateTime.Parse(fecha) : (DateTime?)null;
                    UnidadAlfanumericoParcela.IdJurisdiccion = Convert.ToInt64(Session["ID_JURISDICCION"]);
                    UnidadAlfanumericoParcela.Vigencia = !vigencia.IsEmpty() ? DateTime.Parse(vigencia) : (DateTime?)null;
                    UnidadAlfanumericoParcela.IdUsuario = Usuario.Id_Usuario;

                    var modelo = new
                    {
                        unidadAlfanumericoParcela = UnidadAlfanumericoParcela,
                        machineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress),
                        ip = Request.UserHostAddress
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(modelo), Encoding.UTF8, "application/json");
                    var result = _cliente.PostAsync("api/alfanumericoparcela/post", content).Result;
                    string data = "IDX_SOLR";
                    try
                    {
                        result.EnsureSuccessStatusCode();
                    }
                    catch
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            return new HttpStatusCodeResult(System.Net.HttpStatusCode.Conflict);
                        }
                        else if (result.StatusCode != System.Net.HttpStatusCode.ExpectationFailed)
                        {
                            return new HttpStatusCodeResult(System.Net.HttpStatusCode.InternalServerError);
                        }
                        data = "MVW_PARCELA";
                    }

                    UnidadAlfanumericoParcela.Clear();
                    return new JsonResult { Data = data };
                }

                public JsonResult UnidadAlfanumericoParcelaClear()
                {
                    UnidadAlfanumericoParcela.Clear();
                    return new JsonResult { Data = "Ok" };
                }

                public string GetValidarNumeroExpediente()
                {
                    var result = _cliente.GetAsync("api/parametro/getvalor/" + @Recursos.ValidarNumeroExpediente).Result;
                    result.EnsureSuccessStatusCode();
                    return result.Content.ReadAsStringAsync().Result;
                }

                public List<ValuacionMejoraModel> GetMejorasByParcelaId(long ParcelaID)
                {
                    HttpResponseMessage resp = _cliente.GetAsync("api/ValuacionService/GetMejorasByParcelaId/" + ParcelaID).Result;
                    resp.EnsureSuccessStatusCode();
                    return (List<ValuacionMejoraModel>)resp.Content.ReadAsAsync<List<ValuacionMejoraModel>>().Result;
                }

                public string GetZonaTributaria()
                {
                    HttpResponseMessage resp = _cliente.GetAsync("api/AlfanumericoParcela/GetZonaTributaria/").Result;
                    resp.EnsureSuccessStatusCode();
                    return resp.Content.ReadAsAsync<string>().Result;
                }

                public ActionResult SetJurisdiccion(long id)
                {
                    using (var resp = _cliente.GetAsync($"api/ObjetoAdministrativoService/GetObjetoById/{id}").Result)
                    {
                        resp.EnsureSuccessStatusCode();
                        Session["ID_JURISDICCION"] = id;
                        Session["CODIGO_JURISDICCION"] = resp.Content.ReadAsAsync<OA.Objeto>().Result.Codigo;
                        return Content(Session["CODIGO_JURISDICCION"].ToString());
                    }
                }

                [HttpGet]
                public ActionResult GetJurisdiccionesByDepartamentoParcela(long id)
                {
                    try
                    {
                        using (var resp = _cliente.GetAsync($"api/Parcela/GetJurisdiccionesByDepartamentoParcela/{id}").Result)
                        {
                            resp.EnsureSuccessStatusCode();
                            var data = resp.Content.ReadAsAsync<Dictionary<long, List<OA.Objeto>>>().Result;
                            var kvp = data.Single();
                            return Json(new { selectedValue = kvp.Key, values = kvp.Value.OrderBy(j => j.Nombre) }, JsonRequestBehavior.AllowGet);
                        }
                    }
                    catch (Exception ex)
                    {
                        MvcApplication.GetLogger().LogError($"AlfanumericoParcelaController/GetJurisdiccionesByDepartamentoParcela/{id}", ex);
                        return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                    }
                }

                [HttpPost]
                public ActionResult GetJurisdiccionesByDepartamentoParcelaUnificacion(long[] ids)
                {
                    Dictionary<long, List<OA.Objeto>> jurisdicciones = new Dictionary<long, List<OA.Objeto>>();

                    foreach (var idP in (ids ?? new long[0]))
                    {
                        using (var resp = _cliente.GetAsync($"api/Parcela/GetJurisdiccionesByDepartamentoParcela/{idP}").Result)
                        {
                            resp.EnsureSuccessStatusCode();
                            var data = resp.Content.ReadAsAsync<Dictionary<long, List<OA.Objeto>>>().Result;
                            var kvp = data.Single();
                            if (!jurisdicciones.ContainsKey(kvp.Key))
                            {
                                jurisdicciones.Add(kvp.Key, kvp.Value);
                            }
                            //jurisdicciones.Add(kvp);
                        }
                    }
                    return Json(jurisdicciones.Values);
                }

                //[HttpGet]
                //public ActionResult GetJurisdicciones()
                //{
                //    try
                //    {
                //        return Json(new { selectedValue = GetJurisdiccionByConfig().FeatId, values = GetAllJurisdicciones().OrderBy(j => j.Nombre) }, JsonRequestBehavior.AllowGet);
                //    }
                //    catch (Exception ex)
                //    {
                //        MvcApplication.GetLogger().LogError($"AlfanumericoParcelaController/GetJurisdicciones", ex);
                //        return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                //    }
                //}
                public ActionResult SetTipoParcelas(long id)
                {
                    Session["TIPO_PARCELA"] = id.ToString();
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                }

                public string GetZonaValuatoria()
                {
                    using (var resp = _cliente.GetAsync("api/AlfanumericoParcela/GetZonaValuatoria/").Result)
                    {
                        resp.EnsureSuccessStatusCode();
                        return resp.Content.ReadAsAsync<string>().Result;
                    }
                }

                public List<OA.Domicilio> GetDatosDomiciliosByParcela(long parcelaId)
                {
                    using (var resp = _cliente.GetAsync("api/AlfanumericoParcela/GetDatosDomiciliosByParcela/parcelaId=" + parcelaId).Result)
                    {
                        resp.EnsureSuccessStatusCode();
                        return resp.Content.ReadAsAsync<List<OA.Domicilio>>().Result;
                    }
                }

                private TipoNomenclatura GetNomenclaturaById(long tipoNomenclaturaID)
                {
                    using (var resp = _cliente.GetAsync("api/TipoNomenclatura/GetById?Id=" + tipoNomenclaturaID).Result)
                    {
                        resp.EnsureSuccessStatusCode();
                        return resp.Content.ReadAsAsync<TipoNomenclatura>().Result;
                    }
                }

                public ActionResult GetExpedienteRegularExpression()
                {
                    var param = GetParametrosGenerales().FirstOrDefault(x => x.Clave == "EXP_CREACION");
                    var obj = new object();

                    if (param != null)
                    {
                        using (var resp = _cliente.GetAsync($"api/GenericoService/RegexRandomGenerator?regex={Convert.ToBase64String(Encoding.UTF8.GetBytes(param.Valor))}").Result)
                        {
                            resp.EnsureSuccessStatusCode();
                            obj = new
                            {
                                regex = param.Valor,
                                ejemplo = resp.Content.ReadAsAsync<string>().Result
                            };
                        }
                    }
                    return Json(obj, JsonRequestBehavior.AllowGet);
                }

                private List<ParametrosGeneralesModel> GetParametrosGenerales()
                {
                    using (var resp = _cliente.GetAsync("api/SeguridadService/GetParametrosGenerales").Result)
                    {
                        resp.EnsureSuccessStatusCode();
                        return resp.Content.ReadAsAsync<List<ParametrosGeneralesModel>>().Result;
                    }
                }
        */
    }

    static class XTMethods
    {
        internal static List<SelectListItem> ToSelectListItems(this IEnumerable<OA.Objeto> objetos)
        {
            return objetos
                      .Select(o => new SelectListItem() { Text = o.Nombre, Value = o.FeatId.ToString() })
                      .ToList();
        }

        internal static List<SelectListItem> ToSelectListItems(this IEnumerable<EstadoParcela> estadosParcela)
        {
            return estadosParcela
                        .Select(o => new SelectListItem() { Text = o.Descripcion, Value = o.EstadoParcelaID.ToString() })
                        .ToList();
        }

        internal static List<SelectListItem> ToSelectListItems(this IEnumerable<TipoMensura> operaciones)
        {
            return operaciones
                        .Select(o => new SelectListItem() { Text = o.Descripcion, Value = o.IdTipoMensura.ToString() })
                        .ToList();
        }

        internal static List<SelectListItem> ToSelectListItems(this IEnumerable<TipoParcela> tiposParcela)
        {
            return tiposParcela
                        .Select(o => new SelectListItem() { Text = o.Descripcion, Value = o.TipoParcelaID.ToString() })
                        .ToList();
        }
    }
}