using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Mime;
using System.Web.Mvc;
using GeoSit.Client.Web.Models.Dominio;
using GeoSit.Client.Web.Models.DominioTitular;
using GeoSit.Client.Web.Models.ResponsableFiscal;
using GeoSit.Data.BusinessEntities.Documentos;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.BusinessEntities.ValidationRules.MantenedorParcelario;
using Newtonsoft.Json;
using Resources;
using Model = GeoSit.Client.Web.Models;
using GeoSit.Client.Web.Helpers;
using System.Net;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Designaciones;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Client.Web.Models;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Diagnostics;
using GeoSit.Client.Web.Controllers.ObrasPublicas;

namespace GeoSit.Client.Web.Controllers
{
    public class MantenimientoParcelarioController : Controller
    {
        private readonly HttpClient cliente = new HttpClient();
        private readonly HttpClient clienteInformes = new HttpClient();
        private static readonly string _modelSession = "Parcela";
        private readonly ValuacionesController valCtrl = new ValuacionesController();
        private readonly MensuraController mensuraCtrl = new MensuraController();
        
        private Model.ArchivoDescarga ArchivoInforme
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }
        private UnidadMantenimientoParcelario UnidadMantenimientoParcelario
        {
            get { return Session["UnidadMantenimientoParcelario"] as UnidadMantenimientoParcelario; }
            set { Session["UnidadMantenimientoParcelario"] = value; }
        }
        private UsuariosModel Usuario
        {
            get { return Session["usuarioPortal"] as UsuariosModel; }
        }
        private Dictionary<long, List<TitularViewModel>> TitularesDominio
        {
            get { return Session["titularesDominio"] as Dictionary<long, List<TitularViewModel>>; }
            set { Session["titularesDominio"] = value; }
        }

        public MantenimientoParcelarioController()
        {
            cliente.Timeout = Timeout.InfiniteTimeSpan;
            cliente.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]);
            clienteInformes.Timeout = Timeout.InfiniteTimeSpan;
            clienteInformes.BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesURL"]);
        }

        public JsonResult CancelAll()
        {
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            return new JsonResult { Data = "Ok" };
        }

        public ActionResult Get(long id)
        {
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            TitularesDominio = new Dictionary<long, List<TitularViewModel>>();

            var parametrosMantenedorParcelario = new SeguridadController().GetParametrosGenerales().Where(pg => pg.Agrupador == "MANTENEDOR_PARCELARIO");
            // de momento, desactivo nomenclatura para que no se vean en el mantenedor parcelario.
            // no tiene sentido un abm de esto dado que es el identificador principal de la parcela
            // en la vista, hago la cabecera siempre visible
            bool activaNomenclatura = false; // parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarNomenclaturas && pmt.Valor == "1");
            bool activaPartidas = parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarPartidas && pmt.Valor == "1");
            bool activaZonificacion = parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarZonificacion && pmt.Valor == "1");
            bool activaDesignaciones = parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarDesignaciones && pmt.Valor == "1") && SeguridadController.ExisteFuncion(Seguridad.VisualizarDesignaciones);
            bool activaValuaciones = parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarValuaciones && pmt.Valor == "1") && SeguridadController.ExisteFuncion(Seguridad.VisualizarValuacion);

            var model = GetParcela(id);

            #region Designaciones
            if (activaDesignaciones)
            {
                model.Designaciones = GetParcelaDesignaciones(id);
            }
            #endregion

            #region Zonificacion
            using (var resp = cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/{id}/ZonaEcologica").Result)
            {
                resp.EnsureSuccessStatusCode();
                var zonificacion = resp.Content.ReadAsAsync<Zonificacion>().Result;

                zonificacion.NombreZona = $"{zonificacion.CodigoZona} - {zonificacion.NombreZona}";
                model.Zonificacion = zonificacion;
            }
            #endregion

            #region Superficies
            using (var result = cliente.GetAsync($"api/parcela/{id}/superficies/").Result)
            {
                try
                {
                    result.EnsureSuccessStatusCode();
                    ViewData["Superficies"] = result.Content.ReadAsAsync<ParcelaSuperficies>().Result;
                }
                catch (HttpRequestException ex)
                {
                    MvcApplication.GetLogger().LogError($"MantenimientoParcelario({id}) - Superficies", ex);
                }
            }
            #endregion

            Session[_modelSession] = model;
            /*Cargo Tipo clase y estados de la parcela*/
            ViewData["TiposParcela"] = GetTiposParcelas();
            ViewData["ClasesParcela"] = GetClasesParcelas();
            ViewData["EstadosParcela"] = GetEstadosParcelas();

            ViewData["PuedeModificarDatos"] = SeguridadController.ExisteFuncion(Seguridad.ModificarMantenedorParcelario) && model.FechaBaja == null;
            ViewData["PuedeImprimirInformeParcelario"] = SeguridadController.ExisteFuncion(Seguridad.InformeParcelario);
            //ViewData["PuedeVerVIR"] = SeguridadController.ExisteFuncion(Seguridad.VIR);
            ViewData["VisualizarNomenclaturas"] = activaNomenclatura;
            ViewData["TieneNomenclaturas"] = model.Nomenclaturas?.Any() ?? false;
            ViewData["VisualizarPartidas"] = activaPartidas;
            ViewData["TienePartidas"] = model.UnidadesTributarias?.Any(ut => ut.TipoUnidadTributariaID == 1 || ut.TipoUnidadTributariaID == 2) ?? false;
            ViewData["VisualizarZonificacion"] = activaZonificacion;
            ViewData["VisualizarDesignaciones"] = activaDesignaciones;
            ViewData["VisualizarValuaciones"] = activaValuaciones;

            using (var resp = cliente.GetAsync($"api/Parametro/GetValor?id={Recursos.ActivarInterfaz}").Result)
            {
                resp.EnsureSuccessStatusCode();
                ViewData["ActivaInterfaz"] = resp.Content.ReadAsStringAsync().Result;
            }
            if (activaValuaciones)
            {
                ViewData["ZonasValuaciones"] = GetZonasValuaciones();
            }

            ViewData["ZonasTributarias"] = GetZonasTributarias();

            ViewData["IdInmueble"] = model.UnidadesTributarias.FirstOrDefault().UnidadTributariaId;
            return PartialView("Index", model);
        }

        private Designacion[] GetParcelaDesignaciones(long id)
        {
            using (var result = cliente.GetAsync($"api/designacion/GetDesignacionesParcela?idParcela={id}").Result)
            {
                result.EnsureSuccessStatusCode();
                return result.Content.ReadAsAsync<Designacion[]>().Result;
            }
        }

        [HttpGet]
        public ActionResult Reload()
        {
            return RedirectToAction("Get", new { id = (Session[_modelSession] as Parcela).ParcelaID });
        }
        public ActionResult Reset()
        {
            bool activaDesignaciones = new SeguridadController()
                                                .GetParametrosGenerales()
                                                .Any(pmt => pmt.Clave == Recursos.ActivarDesignaciones && pmt.Valor == "1") 
                                           && SeguridadController.ExisteFuncion(Seguridad.VisualizarDesignaciones);

            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            TitularesDominio.Clear();
            var model = GetParcela((Session[_modelSession] as Parcela).ParcelaID);
            if (activaDesignaciones)
            {
                model.Designaciones = GetParcelaDesignaciones(model.ParcelaID);
            }

            Session[_modelSession] = model;
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public string GetAtributosZonificacion()
        {
            return JsonConvert.SerializeObject(new { data = ((Parcela)Session[_modelSession]).Zonificacion?.AtributosZonificacion ?? new AtributosZonificacion[0] });
        }

        private Parcela GetParcela(long id, bool mantenedor = false)
        {
            var endpoint = mantenedor ? $"api/Parcela/GetParcelaMantenedor/{id}" : $"api/Parcela/Get/{id}";
            using (var resp = cliente.GetAsync(endpoint).Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<Parcela>().Result;
            }
        }

        public string GetUnidadesTributarias()
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                /*
                if (parcela.ClaseParcelaID == Convert.ToInt64(ClasesParcelas.PropiedadHorizontalEspecial))
                {
                    long UT_TIPO_COMUN = 1;
                    long UT_TIPO_PH = 2;
                    foreach (var ut in parcela.UnidadesTributarias.Where(ut => ut.TipoUnidadTributariaID != UT_TIPO_COMUN)) //
                    {
                        if (ut.TipoUnidadTributariaID == UT_TIPO_PH)
                        {
                            ut.TipoUnidadTributaria.Descripcion = "CI";
                        }
                        else
                        {
                            ut.TipoUnidadTributaria.Descripcion = "UP de CI";
                        }
                    }
                }
                */
                return $"{{\"data\":{JsonConvert.SerializeObject(parcela.UnidadesTributarias, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore })}}}";
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Parcela Documentos
        public string GetParcelaDocumentos()
        {
            Parcela model = (Parcela)Session[_modelSession];
            string data = "";
            if (model.ParcelaDocumentos != null)
            {
                List<Documento> documentos = model.ParcelaDocumentos.Select(pd => pd.Documento).Where(d => d != null).ToList();
                data = JsonConvert.SerializeObject(new { data = documentos });
            }
            else
            {
                data = JsonConvert.SerializeObject(new { data = "" });
            }
            return data;
        }

        [HttpPost]
        public JsonResult AddParcelaDocumento(long idDocumento)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                var fechaHora = DateTime.Now;
                var parcelaDocumento = new ParcelaDocumento
                {
                    DocumentoID = idDocumento,
                    ParcelaID = parcela.ParcelaID,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    FechaAlta = fechaHora,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaModificacion = fechaHora
                };
                if (parcela.ParcelaDocumentos == null) parcela.ParcelaDocumentos = new List<ParcelaDocumento>();
                parcela.ParcelaDocumentos.Add(parcelaDocumento);

                UnidadMantenimientoParcelario.OperacionesParcelaDocumento.Add(new OperationItem<ParcelaDocumento>
                {
                    Operation = Operation.Add,
                    Item = parcelaDocumento
                });

                return Json(new { OK = true });
            }
            catch (Exception)
            {
                return Json(new { OK = false });
            }
        }

        [HttpPost]
        public JsonResult EditParcelaDocumento(long idDocumento)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                var fechaHora = DateTime.Now;
                var parcelaDocumento = new ParcelaDocumento
                {
                    DocumentoID = idDocumento,
                    ParcelaID = parcela.ParcelaID,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    FechaAlta = fechaHora,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaModificacion = fechaHora
                };

                UnidadMantenimientoParcelario.OperacionesParcelaDocumento.Add(new OperationItem<ParcelaDocumento>
                {
                    Operation = Operation.Update,
                    Item = parcelaDocumento
                });

                return Json(new { OK = true });
            }
            catch (Exception)
            {
                return Json(new { OK = false });
            }
        }

        [HttpPost]
        public ActionResult DeleteParcelaDocumento(long idDocumento)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                var parcelaDocumento = parcela.ParcelaDocumentos.First(pd => pd.DocumentoID == idDocumento && pd.ParcelaID == parcela.ParcelaID);
                var idTipoDocumento = parcelaDocumento.Documento.id_tipo_documento;
                /*
                if (idTipoDocumento == 7)
                {

                    var idParcelaMensura = parcela.ParcelaMensuras.Where(pm => pm.IdParcela == parcelaDocumento.Parcela.ParcelaID).FirstOrDefault().IdParcelaMensura;

                    var resp = cliente.PostAsync($"api/MensuraService/SetParcelaMensuraMantenedorDelete_Save?idParcelaMensura={idParcelaMensura}&idUsuario={Usuario.Id_Usuario}", new StringContent(string.Empty)).Result;
                    resp.EnsureSuccessStatusCode();

                }
                */
                parcelaDocumento.Parcela = null;

                UnidadMantenimientoParcelario.OperacionesParcelaDocumento.Add(new OperationItem<ParcelaDocumento>
                {
                    Operation = Operation.Remove,
                    Item = parcelaDocumento
                });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/DeleteParcelaDocumento", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion

        #region Unidades Tributarias Documentos
        public string GetUTDocumentos(long idUT)
        {
            var uts = ((Parcela)Session[_modelSession]).UnidadesTributarias?.ToList() ?? new List<UnidadTributaria>();

            var documentosUT = uts.Where(ut => ut.UnidadTributariaId == idUT && ut.UTDocumentos != null)
                                  .SelectMany(u => u.UTDocumentos
                                                    .Where(d => !d.FechaBaja.HasValue)
                                                    .Select(d => d.Documento));

            return JsonConvert.SerializeObject(new { data = documentosUT.ToList() });
        }

        [HttpPost]
        public JsonResult AddUnidadTributariaDocumento(long idUnidadTributaria, long idDocumento, string descripcion,
            long idTipoDocumento, string tipoDocumentoDescripcion, string nombreArchivo)
        {
            try
            {
                var fechaHora = DateTime.Now;
                UnidadTributariaDocumento utDocumento = new UnidadTributariaDocumento
                {
                    DocumentoID = idDocumento,
                    UnidadTributariaID = idUnidadTributaria,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    FechaAlta = fechaHora,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaModificacion = fechaHora
                };

                Parcela parcela = (Parcela)Session[_modelSession];
                UnidadTributaria unidadTributaria = parcela.UnidadesTributarias.FirstOrDefault(ut => ut.UnidadTributariaId == idUnidadTributaria);
                unidadTributaria.UTDocumentos = unidadTributaria.UTDocumentos ?? new List<UnidadTributariaDocumento>();
                var utDoc = CloneUnidadTributariaDocumento(utDocumento);
                utDoc.Documento = new Documento
                {
                    nombre_archivo = nombreArchivo,
                    descripcion = descripcion,
                    id_tipo_documento = idTipoDocumento,
                    Tipo = new TipoDocumento
                    {
                        TipoDocumentoId = idTipoDocumento,
                        Descripcion = tipoDocumentoDescripcion
                    }
                };
                unidadTributaria.UTDocumentos.Add(utDoc);

                UnidadMantenimientoParcelario.OperacionesUnidadTributariaDocumento.Add(new OperationItem<UnidadTributariaDocumento>
                {
                    Item = utDocumento,
                    Operation = Operation.Add
                });

                return Json(new { OK = true });
            }
            catch (Exception)
            {
                return Json(new { OK = false });
            }
        }

        [HttpPost]
        public ActionResult EditUnidadTributariaDocumento(long idUnidadTributaria, long idDocumento, string descripcion,
            long idTipoDocumento, string tipoDocumentoDescripcion)
        {
            try
            {
                var fechaHora = DateTime.Now;
                var utDocumento = new UnidadTributariaDocumento
                {
                    DocumentoID = idDocumento,
                    UnidadTributariaID = idUnidadTributaria,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    FechaAlta = fechaHora,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaModificacion = fechaHora
                };

                UnidadMantenimientoParcelario.OperacionesUnidadTributariaDocumento.Add(new OperationItem<UnidadTributariaDocumento>
                {
                    Item = utDocumento,
                    Operation = Operation.Update
                });

                //Actualiza el documento en sesión
                var model = (Parcela)Session[_modelSession];
                var ut = model.UnidadesTributarias.Single(u => u.UnidadTributariaId == idUnidadTributaria);
                if (ut == null || ut.UTDocumentos == null) return Json(new { OK = true });
                var utDoc = ut.UTDocumentos.Single(d => d.DocumentoID == idDocumento);
                utDoc.Documento.descripcion = descripcion;
                utDoc.Documento.id_tipo_documento = idTipoDocumento;
                utDoc.Documento.Tipo.TipoDocumentoId = idTipoDocumento;
                utDoc.Documento.Tipo.Descripcion = tipoDocumentoDescripcion;
                Session[_modelSession] = model;

                return Json(new { OK = true });
            }
            catch (Exception)
            {
                return Json(new { OK = false });
            }
        }

        [HttpPost]
        public ActionResult DeleteUnidadTributariaDocumento(long idUnidadTributaria, long idDocumento)
        {
            try
            {
                Parcela parcela = (Parcela)Session[_modelSession];
                UnidadTributaria unidadTributaria = parcela.UnidadesTributarias.Single(ut => ut.UnidadTributariaId == idUnidadTributaria);
                UnidadTributariaDocumento unidadTributariaDocumento = unidadTributaria.UTDocumentos.Single(utd => utd.DocumentoID == idDocumento);
                unidadTributariaDocumento.UnidadTributaria = null;

                UnidadMantenimientoParcelario.OperacionesUnidadTributariaDocumento.Add(new OperationItem<UnidadTributariaDocumento>
                {
                    Item = unidadTributariaDocumento,
                    Operation = Operation.Remove,
                });

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/DeleteUnidadTributariaDocumento", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion

        #region Unidades Tributarias Domicilios
        public string GetUTDomicilios(long idUT)
        {
            List<Domicilio> domicilios = new List<Domicilio>();
            try
            {
                var ut = ((Parcela)Session[_modelSession]).UnidadesTributarias.FirstOrDefault(u => u.UnidadTributariaId == idUT);
                if (ut != null && ut.UTDomicilios != null)
                {
                    domicilios = ut.UTDomicilios.Select(utd => utd.Domicilio).ToList();
                }

                return JsonConvert.SerializeObject(new { data = domicilios }, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            }
            catch (Exception)
            {
                return "";
            }
        }

        [HttpPost]
        public ActionResult AddUnidadTributariaDomicilio(Domicilio domicilio, long idUT)
        {
            try
            {
                var utDomicilio = CreateUnidadTributariaDomicilio(domicilio, idUT);
                UnidadMantenimientoParcelario.OperacionesUnidadTributariaDomicilio.Add(new OperationItem<UnidadTributariaDomicilio>
                {
                    Item = utDomicilio,
                    Operation = Operation.Add
                });
                var ut = ((Parcela)Session[_modelSession]).UnidadesTributarias.SingleOrDefault(u => u.UnidadTributariaId == idUT);
                if (ut == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                ut.UTDomicilios = ut.UTDomicilios ?? new List<UnidadTributariaDomicilio>();
                ut.UTDomicilios.Add(utDomicilio);
                return Json(utDomicilio.Domicilio);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/AddUnidadTributariaDomicilio", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        private T Clone<T>(T objeto)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(objeto, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }

        [HttpPost]
        public ActionResult EditUnidadTributariaDomicilio(Domicilio domicilio, long idUT)
        {
            try
            {
                if (domicilio.DomicilioId > 0)
                {
                    UnidadMantenimientoParcelario.OperacionesDomicilio.Add(new OperationItem<Domicilio>
                    {
                        Item = domicilio,
                        Operation = Operation.Update
                    });

                    var ut = ((Parcela)Session[_modelSession]).UnidadesTributarias.SingleOrDefault(u => u.UnidadTributariaId == idUT);
                    if (ut == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                    if (ut.UTDomicilios == null || !ut.UTDomicilios.Any(d => d.DomicilioID == domicilio.DomicilioId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }
                    else
                    {
                        ut.UTDomicilios.Single(d => d.DomicilioID == domicilio.DomicilioId).Domicilio = domicilio;
                    }
                    return Json(domicilio);
                }
                return new HttpStatusCodeResult(HttpStatusCode.Conflict);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/EditUnidadTributariaDomicilio", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        private UnidadTributariaDomicilio CreateUnidadTributariaDomicilio(Domicilio domicilio, long idUt)
        {
            var fechaHora = DateTime.Now;
            var itemDomicilio = new Domicilio
            {
                DomicilioId = domicilio.DomicilioId,
                ViaNombre = domicilio.ViaNombre,
                numero_puerta = domicilio.numero_puerta,
                piso = domicilio.piso,
                unidad = domicilio.unidad,
                barrio = domicilio.barrio,
                localidad = domicilio.localidad,
                municipio = domicilio.municipio,
                provincia = domicilio.provincia,
                pais = domicilio.pais,
                ubicacion = domicilio.ubicacion,
                codigo_postal = domicilio.codigo_postal,
                UsuarioAltaId = Usuario.Id_Usuario,
                FechaAlta = fechaHora,
                UsuarioModifId = Usuario.Id_Usuario,
                FechaModif = fechaHora,
                ViaId = domicilio.ViaId,
                TipoDomicilioId = domicilio.TipoDomicilioId,
                TipoDomicilio = Clone(domicilio.TipoDomicilio)
            };

            var unidadTributariaDomicilio = new UnidadTributariaDomicilio
            {
                DomicilioID = domicilio.DomicilioId,
                Domicilio = itemDomicilio,
                TipoDomicilioID = itemDomicilio.TipoDomicilioId,
                UsuarioAltaID = Usuario.Id_Usuario,
                FechaAlta = fechaHora,
                UsuarioModificacionID = Usuario.Id_Usuario,
                FechaModificacion = fechaHora
            };

            unidadTributariaDomicilio.UnidadTributariaID = idUt;

            return unidadTributariaDomicilio;
        }

        [HttpPost]
        public ActionResult DeleteUnidadTributariaDomicilio(long idUT, long idDomicilio)
        {
            var parcela = (Parcela)Session[_modelSession];
            if (idDomicilio < 0)
            {
                var operacionesUnidadTributariaDomicilio = UnidadMantenimientoParcelario.OperacionesUnidadTributariaDomicilio
                    .Single(x => x.Item.Domicilio.DomicilioId == idDomicilio);
                operacionesUnidadTributariaDomicilio.Operation = Operation.None;
            }
            else
            {
                var unidadTributariaDomicilio = new UnidadTributariaDomicilio
                {
                    DomicilioID = idDomicilio,
                    UnidadTributariaID = idUT,

                };

                UnidadMantenimientoParcelario.OperacionesUnidadTributariaDomicilio.Add(new OperationItem<UnidadTributariaDomicilio>
                {
                    Item = unidadTributariaDomicilio,
                    Operation = Operation.Remove,
                });
            }
            var domicilioModel = parcela.UnidadesTributarias.FirstOrDefault(ut => ut.UnidadTributariaId == idUT).UTDomicilios.FirstOrDefault(d => d.DomicilioID == idDomicilio);
            parcela.UnidadesTributarias.FirstOrDefault(ut => ut.UnidadTributariaId == idUT).UTDomicilios.Remove(domicilioModel);
            return Json(new { OK = true });
        }

        #endregion

        #region Nomenclaturas
        public string GetNomenclaturas()
        {
            var nomenclaturas = ((Parcela)Session[_modelSession]).Nomenclaturas.Where(n => n.FechaBaja == null);
            return JsonConvert.SerializeObject(new { data = nomenclaturas.ToList() }, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }

        public ActionResult AddNomenclatura(Nomenclatura nomenclatura)
        {
            try
            {
                var fechaHora = DateTime.Now;
                var itemNomenclatura = new Nomenclatura
                {
                    Nombre = nomenclatura.Nombre,
                    NomenclaturaID = nomenclatura.NomenclaturaID,
                    ParcelaID = ((Parcela)Session[_modelSession]).ParcelaID,
                    TipoNomenclaturaID = nomenclatura.TipoNomenclaturaID,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    FechaAlta = fechaHora,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaModificacion = fechaHora
                };

                UnidadMantenimientoParcelario.OperacionesNomenclatura.Add(new OperationItem<Nomenclatura>
                {
                    Item = itemNomenclatura,
                    Operation = Operation.Add
                });
                return Json(nomenclatura);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/AddNomenclatura", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditNomenclatura(Nomenclatura nomenclatura)
        {
            try
            {
                var nomenclaturaItem = new Nomenclatura
                {
                    FechaAlta = nomenclatura.FechaAlta,
                    UsuarioAltaID = nomenclatura.UsuarioAltaID,
                    FechaModificacion = DateTime.Now,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    Nombre = nomenclatura.Nombre,
                    NomenclaturaID = nomenclatura.NomenclaturaID,
                    ParcelaID = ((Parcela)Session[_modelSession]).ParcelaID,
                    TipoNomenclaturaID = nomenclatura.TipoNomenclaturaID,
                };
                UnidadMantenimientoParcelario.OperacionesNomenclatura.Add(new OperationItem<Nomenclatura>
                {
                    Item = nomenclaturaItem,
                    Operation = Operation.Update,
                });

                return Json(nomenclatura);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/AddNomenclatura", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteNomenclatura(Nomenclatura nomenclatura)
        {
            try
            {
                var fechaHora = DateTime.Now;
                nomenclatura.UsuarioBajaID = Usuario.Id_Usuario;
                nomenclatura.FechaBaja = fechaHora;
                nomenclatura.UsuarioModificacionID = Usuario.Id_Usuario;
                nomenclatura.FechaModificacion = fechaHora;

                UnidadMantenimientoParcelario.OperacionesNomenclatura.Add(new OperationItem<Nomenclatura>
                {
                    Item = nomenclatura,
                    Operation = Operation.Remove,
                });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/DeleteNomenclatura", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Unidades Tributarias
        public ActionResult AddUnidadTributaria(UnidadTributaria unidadTributaria)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];

                var unidadtributariaItem = new UnidadTributaria
                {
                    UnidadTributariaId = unidadTributaria.UnidadTributariaId,
                    CodigoMunicipal = unidadTributaria.CodigoMunicipal,
                    CodigoProvincial = unidadTributaria.CodigoProvincial,
                    UnidadFuncional = unidadTributaria.UnidadFuncional,
                    Observaciones = unidadTributaria.Observaciones,
                    PorcentajeCopropiedad = unidadTributaria.PorcentajeCopropiedad,
                    Piso = unidadTributaria.Piso,
                    Unidad = unidadTributaria.Unidad,
                    FechaAlta = unidadTributaria.FechaAlta,
                    TipoUnidadTributariaID = unidadTributaria.TipoUnidadTributariaID,
                    TipoUnidadTributaria = unidadTributaria.TipoUnidadTributaria,
                    Vigencia = unidadTributaria.Vigencia,
                    PlanoId = unidadTributaria.PlanoId,
                    Superficie = unidadTributaria.Superficie,
                    FechaVigenciaDesde = unidadTributaria.FechaVigenciaDesde,
                    FechaVigenciaHasta = unidadTributaria.FechaVigenciaHasta,
                    JurisdiccionID = unidadTributaria.JurisdiccionID,
                    ParcelaID = unidadTributaria.ParcelaID,
                    UsuarioAltaID = unidadTributaria.UsuarioAltaID,
                    UsuarioModificacionID = unidadTributaria.UsuarioModificacionID
                };
                parcela.UnidadesTributarias?.Add(unidadtributariaItem);
                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
                {
                    Item = Clone(unidadtributariaItem),
                    Operation = Operation.Add
                });

                return Json(unidadtributariaItem);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/AddUnidadTributaria", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult EditUnidadTributaria(UnidadTributaria unidadTributaria)
        {
            try
            {
                unidadTributaria.ParcelaID = ((Parcela)Session[_modelSession]).ParcelaID;

                var ut = ((Parcela)Session[_modelSession]).UnidadesTributarias.Single(x => x.UnidadTributariaId == unidadTributaria.UnidadTributariaId);

                ut.CodigoMunicipal = unidadTributaria.CodigoMunicipal;
                ut.CodigoProvincial = unidadTributaria.CodigoProvincial;
                ut.UnidadFuncional = unidadTributaria.UnidadFuncional;
                ut.Observaciones = unidadTributaria.Observaciones;
                ut.PorcentajeCopropiedad = unidadTributaria.PorcentajeCopropiedad;
                ut.Piso = unidadTributaria.Piso;
                ut.Unidad = unidadTributaria.Unidad;
                ut.JurisdiccionID = unidadTributaria.JurisdiccionID;
                ut.PlanoId = unidadTributaria.PlanoId;
                ut.Superficie = unidadTributaria.Superficie;
                ut.FechaVigenciaDesde = unidadTributaria.FechaVigenciaDesde;
                ut.FechaVigenciaHasta = unidadTributaria.FechaVigenciaHasta;
                ut.Vigencia = unidadTributaria.Vigencia;

                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
                {
                    Item = unidadTributaria,
                    Operation = Operation.Update
                });

                return Json(unidadTributaria);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/EditUnidadTributaria", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult DeleteUnidadTributaria(UnidadTributaria unidadTributaria)
        {
            try
            {
                //Marco los domicilios agregados como borrados
                var operacionesUTDomicilio = UnidadMantenimientoParcelario
                                                .OperacionesUnidadTributariaDomicilio
                                                .Where(i => i.Item.UnidadTributariaID == unidadTributaria.UnidadTributariaId ||
                                                            i.Item.UnidadTributaria.UnidadTributariaId == unidadTributaria.UnidadTributariaId);

                foreach (var op in operacionesUTDomicilio)
                {
                    op.Operation = Operation.Remove;
                }
                var parcela = (Parcela)Session[_modelSession];
                var ut = parcela.UnidadesTributarias.Single(x => x.UnidadTributariaId == unidadTributaria.UnidadTributariaId);
                ut.FechaBaja = ut.FechaModificacion = DateTime.Now;
                ut.UsuarioBajaID = ut.UsuarioModificacionID = Usuario.Id_Usuario;
                //parcela.UnidadesTributarias.Remove(ut);
                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
                {
                    Item = new UnidadTributaria() { UnidadTributariaId = ut.UnidadTributariaId, CodigoProvincial = ut.CodigoProvincial },
                    Operation = Operation.Remove,
                });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/DeleteUnidadTributaria", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        
        public ActionResult Test(long id, long UnidadTributariaId = 0)
        {
            if (UnidadTributariaId == 0) // Parcela
            {
                using (var resp = cliente.GetAsync($"api/Parcela/{id}/esVigente").Result)
                {
                    resp.EnsureSuccessStatusCode();
                    bool esVigente = resp.Content.ReadAsAsync<bool>().Result;
                    if (esVigente)
                    {
                        //return RedirectToAction("Get", new { id });
                        return RedirectToAction("GetMantenedorParcelarioView", new { id = id, UnidadTributariaId = UnidadTributariaId });
                    }

                    return PartialView("ParcelaNoVigente");
                }
            }
            return RedirectToAction("GetMantenedorParcelarioView", new { id = id, UnidadTributariaId = UnidadTributariaId }); // UnidadTributaria   
        }

        [HttpPost]
        public ActionResult Save(decimal Superficie, long TipoParcelaID, long ClaseParcelaID, long EstadoParcelaID, string PlanoId, string ExpedienteAlta, DateTime FechaAltaExpediente, string ExpedienteBaja,
            DateTime? FechaBajaExpediente, string Observaciones, bool AfectaPH, short? AtributoZonaID)
        {
            //_WEBSERVICE_ INMBAJA VERIFICAR FECHA BAJA IS_CHANGE - LISTO -
            //IF(FECHA_BAJA != NULL) PROCESO_MODIF : (PROCESO_BAJA)
            try
            {
                var parcelaAnterior = (Parcela)Session[_modelSession];
                var parcela = new Parcela
                {
                    AtributoZonaID = AtributoZonaID,
                    ClaseParcelaID = ClaseParcelaID,
                    EstadoParcelaID = EstadoParcelaID,
                    ExpedienteAlta = ExpedienteAlta,
                    ExpedienteBaja = ExpedienteBaja,
                    FechaAltaExpediente = FechaAltaExpediente,
                    FechaBajaExpediente = FechaBajaExpediente,
                    OrigenParcelaID = parcelaAnterior.OrigenParcelaID,
                    ParcelaID = parcelaAnterior.ParcelaID,
                    Superficie = Superficie,
                    PlanoId = PlanoId,
                    TipoParcelaID = TipoParcelaID,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    Atributos = parcelaAnterior.Atributos,
                    _Ip = Request.UserHostAddress,
                    _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
                };
                parcela.AtributosCrear(Observaciones, AfectaPH);

                UnidadMantenimientoParcelario.OperacionesParcela.Clear();
                UnidadMantenimientoParcelario.OperacionesParcela.Add(new OperationItem<Parcela>
                {
                    Operation = Operation.Update,
                    Item = parcela,
                });

                var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
                var response = cliente.PostAsync("api/Parcela/Post", content).Result;
                response.EnsureSuccessStatusCode();
                if (!response.Content.ReadAsAsync<bool>().Result)
                {
                    return Json(new { eliminada = true });
                }
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                Get(parcela.ParcelaID);
                #region ListadoComarcal
                //if (UnidadMantenimientoParcelario.OperacionesUnidadTributaria == null)
                //{
                //    List<webserviceInmModificar> webList = new List<webserviceInmModificar>();//DATOS UNIDAD TRIBUTARIAS


                //}
                #endregion

                //if (parcela.FechaBaja.HasValue)
                //{
                //    //GET PARTIDAS.
                //    var resp = cliente.GetAsync("api/parcela/GetPartidasbyParcela?idParcela=" + parcela.ParcelaID).Result;
                //    resp.EnsureSuccessStatusCode();
                //    var set = string.Join(",", resp.Content.ReadAsAsync<List<string>>().Result.ToArray());
                //    BajaComarcal(parcela.FechaBaja.Value, parcela.ParcelaID, set); // SE DA DE BAJA TODAS LAS UT DE LA PARCELA                    
                //}
                //else
                //{
                //    //IF PARTIDAS_BAJA != NULL
                //}

                //AuditoriaHelper.Register(Usuario.Id_Usuario, "Se grabo la Parcela", Request, TiposOperacion.Modificacion, Autorizado.Si, Eventos.ModificarParcela);
                //TODO: por operacion informar las altas , las bajas y/o modificaciones en unidades tributarias

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenedorParcelario-Save", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        #region Informes
        public ActionResult GetInformeParcelario(long? id)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeParcelario/GetInforme/{id ?? ((Parcela)Session[_modelSession]).ParcelaID}/?padronPartidaId={@Recursos.MostrarPadrónPartida}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeParcelario);
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificaciónCatastral {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public ActionResult GetConstanciaNomenclaturaCatastral(long? id)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeParcelario/GetConstanciaNomenclaturaCatastral/{id ?? ((Parcela)Session[_modelSession]).ParcelaID}/?usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeParcelario);
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"ConstanciaNomenclaturaCatastral {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        //public ActionResult GetInformeParcelarioVIR()
        //{
        //    string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
        //    using (clienteInformes)
        //    using (var resp = clienteInformes.GetAsync($"api/informeParcelarioVIR/GetInforme/{((Parcela)Session[_modelSession]).ParcelaID}/?padronPartidaId={@Recursos.MostrarPadrónPartida}&usuario={usuario}").Result)
        //    {
        //        resp.EnsureSuccessStatusCode();
        //        string bytes64 = resp.Content.ReadAsAsync<string>().Result;
        //        ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"ReporteParcelarioVIR {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
        //        return new HttpStatusCodeResult(HttpStatusCode.OK);
        //    }
        //}

        public ActionResult GetInformeParcelarioBaja(long id, string partida)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeParcelarioBaja/GetInforme/{id}/?padronPartidaId={@Recursos.MostrarPadrónPartida}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"ReporteParcelarioBaja {partida} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public ActionResult GetInformeHistoricoCambiosParcela(long? idTramite)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            var parcela = Session[_modelSession] as Parcela;
            string identificador = parcela.UnidadesTributarias.Single(ut => (ut.TipoUnidadTributariaID == 2) || (ut.TipoUnidadTributariaID == 1)).CodigoProvincial;
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/InformeHistoricoCambios/GetCambiosParcela?id={parcela.ParcelaID}&identificador={identificador}&idTramite={idTramite}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Models.ArchivoDescarga(Convert.FromBase64String(bytes64), $"InformeHistoricoCambiosParcela {identificador} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public ActionResult GetInformeHistoricoCambiosUnidadTributaria(long idUnidadTributaria, long? idTramite)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            var parcela = Session[_modelSession] as Parcela;
            string identificador = parcela.UnidadesTributarias.Single(ut => ut.UnidadTributariaId == idUnidadTributaria).CodigoProvincial;
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/InformeHistoricoCambios/GetCambiosUnidadTributaria?id={idUnidadTributaria}&identificador={identificador}&idTramite={idTramite}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Models.ArchivoDescarga(Convert.FromBase64String(bytes64), $"InformeHistoricoCambiosUnidadTributaria {identificador} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public FileResult Abrir()
        {
            Response.AppendHeader("Content-Disposition", new ContentDisposition { FileName = ArchivoInforme.NombreArchivo, Inline = true }.ToString());
            return File(ArchivoInforme.Contenido, ArchivoInforme.MimeType);
        }
        public ActionResult GetInformeUT(long idUnidadTributaria)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            var parcela = Session[_modelSession] as Parcela;
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeUnidadTributaria/GetInforme?idParcela={parcela.ParcelaID}&idUnidadTributaria={idUnidadTributaria}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeUT);
                string codigoProvincial = parcela.UnidadesTributarias.Single(ut => ut.UnidadTributariaId == idUnidadTributaria).CodigoProvincial;
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificadoCatastral {codigoProvincial} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeCambioTitularidad(long idUnidadTributaria, long idParcela)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeUnidadTributaria/GetInformeCambioTitularidad?idParcela={idParcela}&idUnidadTributaria={idUnidadTributaria}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeUT);
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"ConstanciaCambioDeTitularidad {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeMedidasLinderos(long idParcela)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeUnidadTributaria/GetInformeMedidasLinderos?idParcela={idParcela}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeUT);
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"DescripciónInmueblesMedidasLinderos {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeUTBaja(long idUnidadTributaria, long idParcela, string partida)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeUnidadTributariaBaja/GetInforme?idParcela={idParcela}&idUnidadTributaria={idUnidadTributaria}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"ReporteUnidadTributariaBaja {partida} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeUTFromSearch(long idUnidadTributaria, long idParcela)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/informeUnidadTributaria/GetInforme?idParcela={idParcela}&idUnidadTributaria={idUnidadTributaria}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeUT);
                var parcela = GetParcela(idParcela);
                string codigoProvincial = parcela.UnidadesTributarias.Single(ut => ut.UnidadTributariaId == idUnidadTributaria).CodigoProvincial;
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificadoCatastral {codigoProvincial} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeUTProvincialFromSearch(long idUnidadTributaria, long idParcela, string partida)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var cliente = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesProvincialURL"]) })
            using (var resp = cliente.GetAsync($"api/informeUnidadTributaria/GetInforme?idParcela={idParcela}&idUnidadTributaria={idUnidadTributaria}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Models.ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificadoCatastral {partida} {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
        public ActionResult GetInformeCatastral(int id)
        {
            var resp = clienteInformes.GetAsync("api/informeCatastral/GetInforme?id=" + id).Result;
            resp.EnsureSuccessStatusCode();
            string bytes64 = resp.Content.ReadAsAsync<string>().Result;
            AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeCatastral);
            byte[] bytes = Convert.FromBase64String(bytes64);
            var cd = new ContentDisposition
            {
                FileName = "InformeCatastral.pdf",
                Inline = true,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());
            return File(bytes, "application/pdf");
        }
        public ActionResult VerificarDeuda(long idParcela, string partidas, char tipo)
        {
            //p es verificar todas las partidas por parcela / i verifica la partida individual

            if (tipo == 'p')
            {
                var resp = cliente.GetAsync("api/parcela/GetPartidasbyParcela?idParcela=" + idParcela).Result;
                resp.EnsureSuccessStatusCode();

                var partidasParcela = resp.Content.ReadAsAsync<List<string>>().Result;
                if (partidasParcela.Count() != 0)
                {
                    var set = string.Join(",", partidasParcela.ToArray());
                    string serviceUrl = string.Format("api/parcela/GetVerificarDeuda?parcelaId={0}&partidas={1}", idParcela, set);
                    var resp2 = cliente.GetAsync(serviceUrl).Result;
                    resp2.EnsureSuccessStatusCode();
                    return Json(JsonConvert.SerializeObject(resp2.Content.ReadAsAsync<IEnumerable<EstadoPartida>>().Result), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string json = JsonConvert.SerializeObject(new List<EstadoPartida>()
                        {
                            new EstadoPartida {partidaID = "-13", Estado = "NOTHING"},
                        });

                    return Json(json, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                string serviceUrl = string.Format("api/parcela/GetVerificarDeuda?parcelaId={0}&partidas={1}", idParcela, partidas);
                var resp3 = cliente.GetAsync(serviceUrl).Result;
                resp3.EnsureSuccessStatusCode();
                return Json(JsonConvert.SerializeObject(resp3.Content.ReadAsAsync<IEnumerable<EstadoPartida>>().Result), JsonRequestBehavior.AllowGet);
            }


        }
/*
        public ActionResult GetInformeSituacionPartidaInmobiliaria(long Id)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            var parcela = Session[_modelSession] as Parcela;
            using (clienteInformes)
            using (var resp = clienteInformes.GetAsync($"api/InformeSituacionPartidaInmobiliaria/GetInformeSituacionPartidaInmobiliaria?IdParcelaOperacion={Id}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                AuditoriaHelper.Register(((Model.UsuariosModel)Session["usuarioPortal"]).Id_Usuario, string.Empty, Request, TiposOperacion.Consulta, Autorizado.Si, Eventos.GenerarInformeSituacion);
                ArchivoInforme = new Model.ArchivoDescarga(Convert.FromBase64String(bytes64), $"InformeSituacionPartidaInmobiliaria.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
*/
        #endregion

        #region Parcela Valuaciones
        public ActionResult GetValuacionParcela()
        {
            try
            {
                using (cliente)
                {
                    var parcela = (Parcela)Session[_modelSession];
                    var resp = cliente.GetAsync($"api/valuacionservice/GetValuacionParcela/{parcela.ParcelaID}").Result;
                    resp.EnsureSuccessStatusCode();
                    return Json(new Model.MantenedorParcelarioValuacionModel(resp.Content.ReadAsAsync<VALValuacion>().Result), JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/GetValuacionParcela", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion

        #region Unidad Triburaria Valuaciones
        public ActionResult GetValuacionUnidadTributaria(long id)
        {
            try
            {
                using (cliente)
                {
                    var resp = cliente.GetAsync($"api/valuacionservice/GetValuacionUnidadTributaria/{id}").Result;
                    resp.EnsureSuccessStatusCode();
                    return Json(new Model.MantenedorParcelarioValuacionModel(resp.Content.ReadAsAsync<VALValuacion>().Result), JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/GetValuacionUnidadTributaria", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion

        #region Parcelas Origen
        public string GetParcelasOrigen()
        {
            var parcelaDestino = Session[_modelSession] as Parcela;
            using (var result = cliente.GetAsync($"api/parcela/getparcelasorigen?idparceladestino={parcelaDestino.ParcelaID}").Result)
            {
                result.EnsureSuccessStatusCode();
                var parcelas = result.Content.ReadAsAsync<IEnumerable<ParcelaOrigen>>().Result;

                //Negrada #1
                parcelaDestino.ParcelasOrigen = parcelas.Select(p => new ParcelaOperacion()
                {
                    ParcelaOperacionID = p.IdOperacion,
                    ParcelaOrigenID = p.IdParcela,
                    ParcelaOrigen = new Parcela() { UnidadesTributarias = new[] { new UnidadTributaria() { CodigoProvincial = p.CodigoProvincial } } },
                    TipoOperacionID = p.IdTipoOperacion,
                    FechaOperacion = p.FechaAlta
                }).ToList();
                return $"{{\"data\":{JsonConvert.SerializeObject(parcelas)}}}";
            }
        }
        public ActionResult GetParcelaOrigen(long? id = null)
        {
            ParcelaOperacion parcelaOperacion;
            var origenes = GetTiposParcelaOrigen();
            if (id == null)
            {
                parcelaOperacion = new ParcelaOperacion()
                {
                    ParcelaOrigenID = 0,
                    ParcelaOrigen = new Parcela() { UnidadesTributarias = new List<UnidadTributaria>() },
                    FechaOperacion = DateTime.Today
                };
            }
            else
            {
                parcelaOperacion = (Session[_modelSession] as Parcela).ParcelasOrigen.SingleOrDefault(po => po.ParcelaOperacionID == id);
            }
            ViewData["tiposOperacion"] = origenes;
            //Negrada #2
            return PartialView("ParcelaOrigen", new ParcelaOrigen()
            {
                CodigoProvincial = parcelaOperacion.ParcelaOrigen.UnidadesTributarias.SingleOrDefault()?.CodigoProvincial,
                IdOperacion = parcelaOperacion.ParcelaOperacionID,
                IdTipoOperacion = parcelaOperacion.TipoOperacionID,
                IdParcela = parcelaOperacion.ParcelaOrigenID.Value,
                FechaAlta = parcelaOperacion.FechaOperacion.Value,
            });
        }
        public ActionResult SaveParcelaOrigen(ParcelaOrigen relacionOrigen)
        {
            try
            {
                var tiposParcela = GetTiposParcelas();
                using (var resp = cliente.GetAsync($"api/parcela/{relacionOrigen.IdParcela}/simple").Result.EnsureSuccessStatusCode())
                {
                    var parcelaOrigen = resp.Content.ReadAsAsync<Parcela>().Result;

                    relacionOrigen.TipoParcela = tiposParcela.Single(tp => tp.Value == parcelaOrigen.TipoParcelaID.ToString()).Text;

                    var parcela = Session[_modelSession] as Parcela;
                    var operacion = Operation.Update;
                    var existente = parcela.ParcelasOrigen.SingleOrDefault(po => po.ParcelaOperacionID == relacionOrigen.IdOperacion);
                    if (existente == null)
                    {
                        existente = new ParcelaOperacion()
                        {
                            ParcelaOperacionID = relacionOrigen.IdOperacion,
                            ParcelaDestinoID = parcela.ParcelaID
                        };
                        parcela.ParcelasOrigen.Add(existente);
                        operacion = Operation.Add;
                    }
                    existente.FechaOperacion = relacionOrigen.FechaAlta;
                    existente.ParcelaOrigenID = relacionOrigen.IdParcela;
                    existente.ParcelaOrigen = new Parcela() { UnidadesTributarias = new[] { new UnidadTributaria() { CodigoProvincial = relacionOrigen.CodigoProvincial } } };
                    existente.TipoOperacionID = relacionOrigen.IdTipoOperacion;

                    UnidadMantenimientoParcelario.OperacionesParcelaOrigen.Add(new OperationItem<ParcelaOperacion>
                    {
                        Item = Clone(existente),
                        Operation = operacion
                    });
                    return Json(relacionOrigen);
                }
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController->SaveParcelaOrigen", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public ActionResult DeleteParcelaOrigen(long id)
        {
            var parcela = Session[_modelSession] as Parcela;
            var parcelaOrigen = parcela.ParcelasOrigen.SingleOrDefault(po => po.ParcelaOperacionID == id);
            UnidadMantenimientoParcelario.OperacionesParcelaOrigen.Add(new OperationItem<ParcelaOperacion>
            {
                Item = parcelaOrigen,
                Operation = Operation.Remove
            });
            parcela.ParcelasOrigen.Remove(parcelaOrigen);
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        private List<TipoParcelaOperacion> GetTiposParcelaOrigen()
        {
            try
            {
                using (var resp = cliente.GetAsync($"api/TipoParcelaOperacion/Get").Result.EnsureSuccessStatusCode())
                {
                    return resp.Content.ReadAsAsync<List<TipoParcelaOperacion>>().Result;
                }
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController->GetTiposParcelaOrigen", ex);
                throw;
            }

        }
        #endregion

        public List<SelectListItem> GetTiposParcelas()
        {
            List<SelectListItem> lista = null;
            HttpResponseMessage response = cliente.GetAsync("api/TipoParcela/Get").Result;
            response.EnsureSuccessStatusCode();
            ICollection<TipoParcela> tiposParcelas = response.Content.ReadAsAsync<ICollection<TipoParcela>>().Result;
            lista = tiposParcelas.Select(tp => new SelectListItem { Text = tp.Descripcion, Value = tp.TipoParcelaID.ToString() }).ToList();

            return lista;
        }

        public List<SelectListItem> GetClasesParcelas()
        {
            List<SelectListItem> lista = null;
            HttpResponseMessage response = cliente.GetAsync("api/ClaseParcela/Get").Result;
            response.EnsureSuccessStatusCode();
            ICollection<ClaseParcela> clasesParcela = response.Content.ReadAsAsync<ICollection<ClaseParcela>>().Result;
            lista = clasesParcela.Select(cp => new SelectListItem { Text = cp.Descripcion, Value = cp.ClaseParcelaID.ToString() }).ToList();

            return lista;
        }

        private List<SelectListItem> GetEstadosParcelas()
        {
            List<SelectListItem> lista = null;
            HttpResponseMessage response = cliente.GetAsync("api/EstadoParcela/Get").Result;
            response.EnsureSuccessStatusCode();
            ICollection<EstadoParcela> estadosParcelas = response.Content.ReadAsAsync<ICollection<EstadoParcela>>().Result;
            lista = estadosParcelas.Select(ep => new SelectListItem { Text = ep.Descripcion, Value = ep.EstadoParcelaID.ToString() }).ToList();

            return lista;
        }

        private SelectList GetZonasValuaciones()
        {
            var response = cliente.GetAsync("api/Parcela/GetParcelaValuacionZonas").Result;
            response.EnsureSuccessStatusCode();
            var objetos = response.Content.ReadAsAsync<ICollection<Models.Objeto>>().Result;
            return new SelectList(objetos, "FeatId", "Nombre");
        }

        private SelectList GetZonasTributarias()
        {
            //var response = cliente.GetAsync("api/InterfaseRentas/GetZonasTributarias").Result;
            //response.EnsureSuccessStatusCode();
            //var objetos = response.Content.ReadAsAsync<ICollection<Objeto>>().Result;

            return new SelectList(new Models.Objeto[0], "Codigo", "Nombre");
        }

        //private TipoNomenclatura GetNomenclaturaById(long tipoNomenclaturaID)
        //{
        //    var resp = cliente.GetAsync("api/TipoNomenclatura/GetById?Id=" + tipoNomenclaturaID).Result;
        //    resp.EnsureSuccessStatusCode();
        //    return resp.Content.ReadAsAsync<TipoNomenclatura>().Result;
        //}

        #region Responsables Fiscales

        public string GetUtResponsablesFiscal(long idUnidadTributaria)
        {
            var result = cliente.GetAsync("api/unidadtributariapersona/get?idunidadtributaria=" + idUnidadTributaria).Result;
            result.EnsureSuccessStatusCode();
            var responsablesFiscales = result.Content.ReadAsAsync<IEnumerable<ResponsableFiscal>>().Result;
            if (responsablesFiscales != null)
            {
                foreach (var responsableFiscal in responsablesFiscales)
                {
                    responsableFiscal.UnidadTributariaId = idUnidadTributaria;
                }
            }
            return "{\"data\":" + JsonConvert.SerializeObject(responsablesFiscales) + "}";
        }

        public string GetCambioMunicipio()
        {
            var result = cliente.GetAsync("api/parametro/getvalor/" + @Recursos.CambioMunicipio).Result;
            result.EnsureSuccessStatusCode();
            return result.Content.ReadAsStringAsync().Result;
        }

        public ActionResult SaveResponsableFiscal(ResponsableFiscalViewModel responsableFiscalViewModel)
        {
            var utPersona = new UtPersona
            {
                OperacionesUnidadTributariaPersona = UnidadMantenimientoParcelario.OperacionesUnidadTributariaPersona,
                UnidadTributariaId = responsableFiscalViewModel.UnidadTributariaId,
                PersonaId = responsableFiscalViewModel.PersonaId,
                SavedPersonaId = responsableFiscalViewModel.SavedPersonaId,
                Operacion = responsableFiscalViewModel.Operacion
            };

            using (var result = cliente.PostAsync("api/unidadtributariapersona/validate", new ObjectContent<UtPersona>(utPersona, new JsonMediaTypeFormatter())).Result)
            {
                result.EnsureSuccessStatusCode();
                string response = result.Content.ReadAsStringAsync().Result;
                if (!string.IsNullOrEmpty(response))
                {
                    return Json(new { error = true, mensaje = response });
                }
                responsableFiscalViewModel.SavedPersonaId = utPersona.PersonaId;
            }
            UnidadMantenimientoParcelario.OperacionesUnidadTributariaPersona.Add(new OperationItem<UnidadTributariaPersona>
            {
                Item = new UnidadTributariaPersona
                {
                    UnidadTributariaID = responsableFiscalViewModel.UnidadTributariaId,
                    PersonaID = responsableFiscalViewModel.PersonaId,
                    PersonaSavedId = responsableFiscalViewModel.SavedPersonaId,
                    TipoPersonaID = responsableFiscalViewModel.TipoPersonaId,
                    CodSistemaTributario = responsableFiscalViewModel.CodSistemaTributario,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                },
                Operation = responsableFiscalViewModel.Operacion
            });

            return Json(responsableFiscalViewModel);
        }

        public ActionResult DeleteResponsableFiscal(ResponsableFiscalViewModel responsableFiscalViewModel)
        {
            try
            {
                var unidadTributariaPersona = new UnidadTributariaPersona
                {
                    UnidadTributariaID = responsableFiscalViewModel.UnidadTributariaId,
                    PersonaID = responsableFiscalViewModel.PersonaId,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                };

                UnidadMantenimientoParcelario.OperacionesUnidadTributariaPersona.Add(new OperationItem<UnidadTributariaPersona>
                {
                    Item = unidadTributariaPersona,
                    Operation = Operation.Remove
                });

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenimientoParcelarioController/DeleteResponsableFiscal", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
        #endregion

        #region Dominios

        public string GetUtDominios(long idUnidadTributaria)
        {
            var result = cliente.GetAsync("api/dominio/get?idunidadtributaria=" + idUnidadTributaria).Result;
            result.EnsureSuccessStatusCode();
            var dominios = result.Content.ReadAsAsync<IEnumerable<DominioUT>>().Result.ToList();
            var dominiosOper = UnidadMantenimientoParcelario.OperacionesDominio
                .Where(dominioOper => dominioOper.Item.UnidadTributariaID == idUnidadTributaria).ToList();

            dominios.AddRange(dominiosOper
                .Where(dominioOper => dominioOper.Operation != Operation.Remove)
                .Select(dominioOper => new DominioUT
                {
                    DominioID = dominioOper.Item.DominioID,
                    Fecha = dominioOper.Item.Fecha,
                    Inscripcion = dominioOper.Item.Inscripcion,
                    TipoInscripcion = dominioOper.Item.TipoInscripcionDescripcion,
                    TipoInscripcionID = dominioOper.Item.TipoInscripcionID
                }));

            foreach (var dominioOper in dominiosOper)
            {
                dominios.RemoveAll(x => x.DominioID == dominioOper.Item.DominioID);
            }
            var datos = dominios.Select(d => new DominioViewModel()
            {
                DominioID = d.DominioID,
                UnidadTributariaID = idUnidadTributaria,
                Inscripcion = d.Inscripcion,
                Fecha = d.Fecha,
                FechaHora = d.Fecha.ToShortDateString(),
                TipoInscripcionID = d.TipoInscripcionID,
                TipoInscripcionDescripcion = d.TipoInscripcion
            });
            return "{\"data\":" + JsonConvert.SerializeObject(datos) + "}";
        }

        public ActionResult SaveDominio(DominioViewModel dominioViewModel)
        {
            var utDominio = new UtDominio
            {
                UnidadTributariaId = dominioViewModel.UnidadTributariaID,
                DominioId = dominioViewModel.DominioID,
                Inscripcion = dominioViewModel.Inscripcion,
                OperacionesDominio = UnidadMantenimientoParcelario.OperacionesDominio
            };

            using (var result = cliente.PostAsync("api/dominio/validate", new ObjectContent<UtDominio>(utDominio, new JsonMediaTypeFormatter())).Result)
            {
                result.EnsureSuccessStatusCode();
                string response = result.Content.ReadAsStringAsync().Result;
                if (!string.IsNullOrEmpty(response))
                {
                    return Json(new { error = true, mensaje = response });
                }
            }
            dominioViewModel.Fecha = DateTime.Parse(dominioViewModel.FechaHora, System.Globalization.CultureInfo.CurrentUICulture);

            var dominioExistente = UnidadMantenimientoParcelario.OperacionesDominio
                .FirstOrDefault(op => op.Operation == Operation.Add && op.Item.DominioID == dominioViewModel.DominioID);
            if (dominioExistente != null)
            {
                UnidadMantenimientoParcelario.OperacionesDominio.Remove(dominioExistente);
            }

            UnidadMantenimientoParcelario.OperacionesDominio.Add(new OperationItem<Dominio>
            {
                Item = new Dominio
                {
                    DominioID = dominioViewModel.DominioID,
                    UnidadTributariaID = dominioViewModel.UnidadTributariaID,
                    TipoInscripcionID = dominioViewModel.TipoInscripcionID,
                    TipoInscripcionDescripcion = dominioViewModel.TipoInscripcionDescripcion,
                    Inscripcion = dominioViewModel.Inscripcion,
                    Fecha = dominioViewModel.Fecha,
                    IdUsuarioModif = Usuario.Id_Usuario,
                },
                Operation = dominioViewModel.Operacion
            });

            return Json(dominioViewModel);
        }

        public ActionResult DeleteDominio(DominioViewModel dominio)
        {
            UnidadMantenimientoParcelario.OperacionesDominio.Add(new OperationItem<Dominio>
            {
                Item = new Dominio
                {
                    DominioID = dominio.DominioID
                },
                Operation = Operation.Remove
            });

            string titularesJson = GetUtTitulares(dominio.DominioID, dominio.UnidadTributariaID);
            var titulares = JsonConvert.DeserializeObject<List<TitularViewModel>>(JsonConvert.DeserializeObject<JObject>(titularesJson)["data"].ToString());
            if (titulares != null && titulares.Any())
            {
                foreach (TitularViewModel titular in titulares)
                {
                    DeleteTitular(titular);
                }
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        #endregion

        #region Titulares

        public JsonResult GetPersonaDatos(long id)
        {
            var result = cliente.GetAsync("api/persona/getdatos/" + id).Result;
            result.EnsureSuccessStatusCode();
            var persona = result.Content.ReadAsAsync<Persona>().Result;

            return Json(persona);
        }

        public string GetUtTitulares(long idDominio, long idUnidadTributaria)
        {
            if (!TitularesDominio.ContainsKey(idDominio))
            {
                var result = cliente.GetAsync($"api/dominiotitular/GetTitulares?idDominio={idDominio}&idUnidadTributaria={idUnidadTributaria}").Result;
                result.EnsureSuccessStatusCode();
                TitularesDominio.Add(idDominio, result.Content
                                                      .ReadAsAsync<IEnumerable<Titular>>()
                                                      .Result.Select(x => new TitularViewModel
                                                      {
                                                          PersonaId = x.PersonaId,
                                                          DominioId = idDominio,
                                                          NombreCompleto = x.NombreCompleto,
                                                          PorcientoCopropiedad = x.PorcientoCopropiedad,
                                                          TipoNoDocumento = x.TipoNoDocumento,
                                                          TipoTitularidadId = x.TipoTitularidadId,
                                                          TipoTitularidad = x.TipoTitular,
                                                          FechaEscritura = x.FechaEscritura,
                                                      }).ToList());
            }

            return "{\"data\":" + JsonConvert.SerializeObject(TitularesDominio[idDominio]) + "}";
        }

        
        public ActionResult SaveTitular(TitularViewModel titularViewModel)
        {
            var dominioTitular = new DominioTitular
            {
                DominioID = titularViewModel.DominioId,
                PersonaID = titularViewModel.PersonaId,
                TipoTitularidadID = titularViewModel.TipoTitularidadId,
                PorcientoCopropiedad = titularViewModel.PorcientoCopropiedad,
                FechaEscritura = titularViewModel.FechaEscritura
            };

            if (titularViewModel.Operacion == Operation.Add)
            {
                var domTitular = new DomTitular
                {
                    OperacionesDominioTitular = UnidadMantenimientoParcelario.OperacionesDominioTitular,
                    DominioId = titularViewModel.DominioId,
                    PersonaId = titularViewModel.PersonaId
                };

                using (var result = cliente.PostAsync("api/dominiotitular/validate", new ObjectContent<DomTitular>(domTitular, new JsonMediaTypeFormatter())).Result)
                {
                    result.EnsureSuccessStatusCode();
                    string response = result.Content.ReadAsStringAsync().Result;
                    if (!string.IsNullOrEmpty(response))
                    {
                        return Json(new { error = true, mensaje = response });
                    }
                }
                
                if (!TitularesDominio.ContainsKey(titularViewModel.DominioId))
                {
                    TitularesDominio[titularViewModel.DominioId] = new List<TitularViewModel>();
                }

                TitularesDominio[titularViewModel.DominioId].Add(titularViewModel);
            }
            else
            {
                TitularesDominio[titularViewModel.DominioId][TitularesDominio[titularViewModel.DominioId].FindIndex(tv => tv.PersonaId == titularViewModel.PersonaId)] = titularViewModel;
            }

            UnidadMantenimientoParcelario.OperacionesDominioTitular.Add(new OperationItem<DominioTitular>
            {
                Item = dominioTitular,
                Operation = titularViewModel.Operacion
            });

            return Json(titularViewModel);
        }
        
        public ActionResult DeleteTitular(TitularViewModel titularViewModel)
        {
            UnidadMantenimientoParcelario.OperacionesDominioTitular.Add(new OperationItem<DominioTitular>
            {
                Item = new DominioTitular()
                {
                    DominioID = titularViewModel.DominioId,
                    PersonaID = titularViewModel.PersonaId
                },
                Operation = Operation.Remove
            });
            TitularesDominio[titularViewModel.DominioId].RemoveAll(tv => tv.PersonaId == titularViewModel.PersonaId);
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
        #endregion

        #region Estado de Deudas

        public JsonResult GetYears()
        {
            var result = cliente.GetAsync("api/estadodeuda/getyears").Result;
            result.EnsureSuccessStatusCode();
            var rentas = result.Content.ReadAsAsync<IEnumerable<int>>().Result;

            return Json(rentas);
        }

        public string GetServiciosGenerales(string padron)
        {
            //var result = cliente.GetAsync("api/estadodeuda/getserviciosgenerales?padron=" + padron).Result;
            //result.EnsureSuccessStatusCode();
            //var serviciosGenerales = result.Content.ReadAsAsync<IEnumerable<EstadoDeudaServicioGeneral>>().Result;

            //return "{\"data\":" + JsonConvert.SerializeObject(serviciosGenerales) + "}";
            return "{\"data\":[]}";
        }

        public string GetRentas(int year)
        {
            var result = cliente.GetAsync("api/estadodeuda/getrentas?year=" + year).Result;
            result.EnsureSuccessStatusCode();
            var rentas = result.Content.ReadAsAsync<IEnumerable<EstadoDeudaRenta>>().Result;

            return "{\"data\":" + JsonConvert.SerializeObject(rentas) + "}";
        }

        #endregion

        public T CloneUnidadTributariaDocumento<T>(T unidadTributariaDocumento)
        {
            var json = JsonConvert.SerializeObject(unidadTributariaDocumento);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public ActionResult BajaComarcal(DateTime fechaBaja, long parcelaId, string partidas)
        {
            string url = string.Format("api/Parcela/BajaComarcal?fechaBaja={0}&parcelaId={1}&partidas={2}",
                fechaBaja.ToString("yyyy-MM-dd HH:mm:ss"), parcelaId, partidas);
            var result = cliente.PostAsync(url, new StringContent(string.Empty)).Result;
            result.EnsureSuccessStatusCode();

            return new JsonResult { Data = "Ok" };
        }

        public ActionResult GetParcelaDesignaciones()
        {
            var parcela = Session[_modelSession] as Parcela;
            var designaciones = parcela.Designaciones?.Select(d => new Models.DesignacionParcelaModel(d, parcela)).ToArray();
            parcela = null;
            return Json(new { data = designaciones }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddParcelaDesignacion(Designacion designacion)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                parcela.Designaciones = new List<Designacion>((parcela.Designaciones ?? new Designacion[0]).Concat(new[] { designacion }));

                UnidadMantenimientoParcelario.OperacionesDesignaciones.Add(new OperationItem<Designacion>
                {
                    Operation = Operation.Add,
                    Item = designacion
                });

                return Json(new Models.DesignacionParcelaModel(designacion, parcela));
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("AddParcelaDesignacion", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult EditParcelaDesignacion(Designacion designacion)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                parcela.Designaciones = new List<Designacion>(parcela.Designaciones.Where(d => d.IdDesignacion != designacion.IdDesignacion).Concat(new[] { designacion }));

                UnidadMantenimientoParcelario.OperacionesDesignaciones.Add(new OperationItem<Designacion>
                {
                    Operation = Operation.Update,
                    Item = designacion
                });

                return Json(new Models.DesignacionParcelaModel(designacion, parcela));
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("EditParcelaDesignacion", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        public ActionResult DeleteParcelaDesignacion(long idDesignacion)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                var current = parcela.Designaciones.SingleOrDefault(d => d.IdDesignacion == idDesignacion);
                if (current == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Conflict);
                }
                parcela.Designaciones = new List<Designacion>(parcela.Designaciones.Except(new[] { current }));

                UnidadMantenimientoParcelario.OperacionesDesignaciones.Add(new OperationItem<Designacion>
                {
                    Operation = Operation.Remove,
                    Item = current
                });
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("DeleteParcelaDesignacion", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public FileContentResult DownloadDocumento(long id)
        {
            using (var cliente = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            using (var resp = cliente.GetAsync($"api/DocumentoService/{id}/File").Result)
            {
                var doc = resp.Content.ReadAsAsync<DocumentoArchivo>().Result;
                return File(doc.Contenido, doc.ContentType, doc.NombreArchivo);
            }
        }
        public ActionResult GetInformeParcelarioProvincial(long id)
        {
            string usuario = $"{((Model.UsuariosModel)Session["usuarioPortal"]).Nombre} {((Model.UsuariosModel)Session["usuarioPortal"]).Apellido}";
            using (var cliente = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesProvincialURL"]) })
            using (var resp = cliente.GetAsync($"api/informeParcelario/GetInforme/{id}/?padronPartidaId={@Recursos.MostrarPadrónPartida}&usuario={usuario}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                ArchivoInforme = new Models.ArchivoDescarga(Convert.FromBase64String(bytes64), $"CertificaciónCatastral {DateTime.Now:dd-MM-yyyy HH:mm:ss}.pdf", "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public ActionResult PuedeAgregarDesignacion(long idClaseParcela)
        {
            using (var result = cliente.GetAsync($"api/designacion/GetTiposDesignador").Result)
            {
                var parcela = Session["Parcela"] as Parcela;
                result.EnsureSuccessStatusCode();
                var tipos = result.Content.ReadAsAsync<IEnumerable<TipoDesignador>>().Result;
                var disponibles = tipos.Where(t => parcela.Designaciones.All(d => t.IdTipoDesignador != d.IdTipoDesignador));

                if (!disponibles.Any() || disponibles.Count() == 1 && disponibles.SingleOrDefault().Nombre == "TITULO" && idClaseParcela == 2)
                {
                    return Content(string.Empty);
                }
                else
                {
                    return Content("Ok");
                }

            }
        }

        public static string GetFormatedSuperficie(decimal superficie)
        {
            string[] partes = superficie.ToString().Split('.');
            int decimales = (System.Web.HttpContext.Current.Session[_modelSession] as Parcela).TipoParcelaID != 1 ? 4 : 2;
            return $"{partes[0]}.{partes[1].PadRight(decimales, '0')}";
        }


        #region Partial Views - Diseño Nuevo Mantenedor Parcelario
        private Parcela InicializarParcelaSession(long id)
        {
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            TitularesDominio = new Dictionary<long, List<TitularViewModel>>();
            var parametrosMantenedorParcelario = new SeguridadController().GetParametrosGenerales().Where(pg => pg.Agrupador == "MANTENEDOR_PARCELARIO");
            bool activaZonificacion = parametrosMantenedorParcelario.Any(pmt => pmt.Clave == Recursos.ActivarZonificacion && pmt.Valor == "1");
            var model = GetParcela(id, true);
            #region Zonificacion
            using (var resp = cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/{id}/ZonaEcologica").Result)
            {
                resp.EnsureSuccessStatusCode();
                var zonificacion = resp.Content.ReadAsAsync<Zonificacion>().Result;
                zonificacion.NombreZona = $"{zonificacion.CodigoZona} - {zonificacion.NombreZona}";
                model.Zonificacion = zonificacion;
            }
            #endregion
            Session[_modelSession] = model;
            return model;
        }

        public Parcela CargarDatosParcelaView(long id)
        {
            var model = Session[_modelSession] as Parcela;
            if (model == null)
            {
                model = InicializarParcelaSession(id);
            }
            List<ParcelaMensura> listaParcelaMensuras = mensuraCtrl.GetParcelasMensuras(model.ParcelaID);
            foreach (ParcelaMensura parcelamensura in listaParcelaMensuras)
            {
                parcelamensura.Mensura = mensuraCtrl.GetDatosMensuraById(parcelamensura.IdMensura);
            }
            ViewData["Planos"] = listaParcelaMensuras;
            ViewData["TiposParcela"] = GetTiposParcelas();
            ViewData["ClasesParcela"] = GetClasesParcelas();
            ViewData["EstadosParcela"] = GetEstadosParcelas();
            ViewData["Nomenclatura"] = model.Nomenclaturas.OrderByDescending(n => n.FechaAlta.GetValueOrDefault()).First().GetNomenclaturas();
            ViewData["Observaciones"] = GetNodoAtributo(model.Atributos, "observacion");
            ViewData["Centroide"] = $"{model.Coordenadas}{Environment.NewLine}{model.CoordenadasLL84}";
            
            return model;
        }

        public ActionResult GetMantenedorParcelarioView(long id, long UnidadTributariaId, bool finalizarAltaAlfa = false)
        {
            Session[_modelSession] = null;
            Parcela parcela = CargarDatosParcelaView(id);
            //ViewData["UnidadesTributarias"] = parcela.UnidadesTributarias;
            ViewData["Objeto"] = UnidadTributariaId > 0;
            ViewData["FinalizarAltaAlfanumerica"] = finalizarAltaAlfa;
            ViewData["Parcela"] = parcela.Nomenclaturas.OrderByDescending(n => n.FechaAlta.GetValueOrDefault()).First().GetNomenclaturas()["Par"].ToUpper();
            ViewData["PuedeModificarDatos"] = SeguridadController.ExisteFuncion(Seguridad.ModificarMantenedorParcelario) && parcela.FechaBaja == null;
            ViewData["PuedeImprimirInforme"] = SeguridadController.ExisteFuncion(Seguridad.InformeParcelario) || SeguridadController.ExisteFuncion(Seguridad.InformeTributario);
            if (UnidadTributariaId == 0) // Parcela
            {
                return PartialView("MantenedorParcelario", parcela);
            }
            return PartialView("MantenedorParcelario", CargarDatosUtView(UnidadTributariaId)); // UnidadTributaria
        }

        public UnidadTributaria CargarDatosUtView(long UnidadTributariaId)
        {
            var parcela = (Parcela)Session[_modelSession];
            if (parcela.UnidadesTributarias == null) parcela.UnidadesTributarias = GetUTSbyParcelaId(parcela.ParcelaID);
            UnidadTributaria ut = parcela.UnidadesTributarias.Single(x => x.UnidadTributariaId == UnidadTributariaId);
            ut.Parcela = parcela;
            List<SelectListItem> tiposUt = GetTiposUnidadTributaria();
            if(parcela.ClaseParcelaID == 3 || parcela.ClaseParcelaID == 4) // PH Ó PH ESPECIAL
            {
                tiposUt.RemoveAll(x => x.Value == "1");
            }
            else
            {
                tiposUt = tiposUt.Where(x => x.Value == "1").ToList();
            }
            ViewData["TiposUnidadTributaria"] = tiposUt;
            //ViewData["UTdominios"] = GetUtDominios(UnidadTributariaId);
            var listaDominios = JsonConvert.DeserializeObject<JObject>(GetUtDominios(UnidadTributariaId))["data"].ToObject<List<DominioViewModel>>();
            ViewData["ListaDominios"] = listaDominios;
            ViewData["UTtitulares"] = listaDominios?.FirstOrDefault() != null ? GetUtTitulares(listaDominios.First().DominioID, listaDominios.First().UnidadTributariaID) : null;
            ViewData["UTvaluaciones"] = JsonConvert.SerializeObject((GetValuacionUnidadTributaria(ut.UnidadTributariaId) as JsonResult)?.Data);
            ViewData["ZonaEcologica"] = parcela.Zonificacion.CodigoZona;

            var ddjjVigente = valCtrl.GetDDJJVigenteByUT(ut.UnidadTributariaId);
            ViewData["UTsuperficies"] =  ddjjVigente == null ? null : valCtrl.GetValSuperficies(ddjjVigente.IdDeclaracionJurada);
            return ut;
        }

        public ActionResult GetUnidadTributariaView(long UnidadTributariaId)
        {
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            TitularesDominio = new Dictionary<long, List<TitularViewModel>>();
            return PartialView("~/Views/MantenimientoParcelario/Partial/_UnidadTributaria.cshtml", CargarDatosUtView(UnidadTributariaId));
        }

        public ActionResult GetParcelaView(string idParcela)
        {
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
            TitularesDominio = new Dictionary<long, List<TitularViewModel>>();
            return PartialView("~/Views/MantenimientoParcelario/Partial/_Parcela.cshtml", CargarDatosParcelaView(Convert.ToInt64(idParcela))); 
        }

        //public string FormatNomenclatura(Nomenclatura nomenc)
        //{
        //if (nomenc == null)
        //{
        //throw new ArgumentNullException(nameof(nomenc));
        //}
        //Dictionary<string, string> nombres = nomenc.GetNomenclaturas();
        //string nomenclatura = string.Join("-", nombres.Values);
        //return nomenclatura;
        //}

        public string GetNodoAtributo(string xmlatributo, string descripcion)
        {
            if (string.IsNullOrEmpty(xmlatributo))
            {
                return string.Empty;
            }
            var doc = new XmlDocument();
            doc.LoadXml(xmlatributo);
            var node = doc.SelectSingleNode("//datos/" + descripcion + "/text()");
            return node != null ? node.Value : string.Empty;
        }

        public List<SelectListItem> GetTiposUnidadTributaria()
        {
            using (var response = cliente.GetAsync("api/TipoUnidadTributaria/Get").Result)
            {
                response.EnsureSuccessStatusCode();
                var tiposParcelas = response.Content
                                            .ReadAsAsync<ICollection<TipoUnidadTributaria>>()
                                            .Result.OrderBy(x => x.Descripcion);

                return tiposParcelas.Select(tut => new SelectListItem { Text = tut.Descripcion, Value = tut.TipoUnidadTributariaID.ToString() }).ToList();
            }
        }

        [HttpPost]
        public ActionResult UpdateParcela(long TipoParcelaID, long ClaseParcelaID, long SuperficieRegistrada, DateTime? FechaBaja, string Observaciones, string Nomenclatura, int CantidadUF)
        {
            try
            {
                var parcelaAnterior = (Parcela)Session[_modelSession];
                var parcela = new Parcela
                {
                    AtributoZonaID = parcelaAnterior.AtributoZonaID,
                    ClaseParcelaID = ClaseParcelaID,
                    EstadoParcelaID = parcelaAnterior.EstadoParcelaID,
                    ExpedienteAlta = parcelaAnterior.ExpedienteAlta,
                    ExpedienteBaja = parcelaAnterior.ExpedienteBaja,
                    FechaAltaExpediente = parcelaAnterior.FechaAltaExpediente,
                    FechaBajaExpediente = parcelaAnterior.FechaBajaExpediente,
                    OrigenParcelaID = parcelaAnterior.OrigenParcelaID,
                    ParcelaID = parcelaAnterior.ParcelaID,
                    Superficie = SuperficieRegistrada,
                    PlanoId = parcelaAnterior.PlanoId,
                    TipoParcelaID = TipoParcelaID,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    Atributos = parcelaAnterior.Atributos,
                    FechaBaja = FechaBaja,
                    Nomenclaturas = parcelaAnterior.Nomenclaturas,
                    FechaAlta = parcelaAnterior.FechaAlta,
                    SuperficieGrafica = parcelaAnterior.SuperficieGrafica,
                    Coordenadas = parcelaAnterior.Coordenadas,
                    CoordenadasLL84 = parcelaAnterior.CoordenadasLL84,
                    Zonificacion = parcelaAnterior.Zonificacion,
                    _Ip = Request.UserHostAddress,
                    _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
                };
                parcela.AtributosCrear(Observaciones, false);

                Nomenclatura nomenclatura = parcela.Nomenclaturas.OrderByDescending(n => n.FechaAlta.GetValueOrDefault()).FirstOrDefault();
                if (nomenclatura.Nombre != Nomenclatura)
                {
                    nomenclatura.Nombre = Nomenclatura;
                    EditNomenclatura(nomenclatura);
                }

                UnidadMantenimientoParcelario.OperacionesParcela.Clear();
                UnidadMantenimientoParcelario.OperacionesParcela.Add(new OperationItem<Parcela>
                {
                    Operation = parcela.FechaBaja.HasValue ? Operation.Remove : Operation.Update,
                    Item = parcela
                });

                if (CantidadUF > 0) AgregarUnidadesFuncionales(parcela, CantidadUF);

                List<UnidadTributaria> unidadesTributarias = parcelaAnterior.UnidadesTributarias.OrderBy(ut => ut.FechaAlta).ToList();
                if (unidadesTributarias.Count() > 1 && parcela.ClaseParcelaID != 3 && parcela.ClaseParcelaID != 4)
                {
                    unidadesTributarias.RemoveAt(0); // UT MADRE NO SE DA DE BAJA
                    AgregarOperacionBajaUT(unidadesTributarias);
                }

                if(parcela.FechaBaja != null)
                {
                    AgregarOperacionBajaUT(parcelaAnterior.UnidadesTributarias);
                }

                var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
                var response = cliente.PostAsync("api/Parcela/UpdateParcela", content).Result;
                response.EnsureSuccessStatusCode();
                if (!response.Content.ReadAsAsync<bool>().Result)
                {
                    return Json(new { eliminada = true });
                }
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                //Session[_modelSession] = null;
                parcela.UnidadesTributarias = parcelaAnterior.UnidadesTributarias;
                Session[_modelSession] = parcela;
                CargarDatosParcelaView(parcela.ParcelaID);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenedorParcelario-Update-Parcela", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public void AgregarOperacionBajaUT(ICollection<UnidadTributaria> listaUts)
        {
            foreach (UnidadTributaria ut in listaUts)
            {
                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
                {
                    Item = new UnidadTributaria
                    {
                        UnidadTributariaId = ut.UnidadTributariaId,
                        CodigoProvincial = ut.CodigoProvincial,
                        ParcelaID = ut.ParcelaID,
                        CodigoMunicipal = ut.CodigoMunicipal,
                        UnidadFuncional = ut.UnidadFuncional,
                        PorcentajeCopropiedad = ut.PorcentajeCopropiedad,
                        JurisdiccionID = ut.JurisdiccionID,
                        UsuarioAltaID = ut.UsuarioAltaID,
                        FechaAlta = ut.FechaAlta,
                        UsuarioModificacionID = ut.UsuarioModificacionID,
                        FechaModificacion = ut.FechaModificacion,
                        UsuarioBajaID = Usuario.Id_Usuario,
                        FechaBaja = DateTime.Now,
                        FechaVigenciaDesde = ut.FechaVigenciaDesde,
                        FechaVigenciaHasta = ut.FechaVigenciaHasta,
                        Observaciones = ut.Observaciones,
                        Designaciones = ut.Designaciones,
                        PorcientoCopropiedadTotal = ut.PorcientoCopropiedadTotal,
                        TipoUnidadTributariaID = ut.TipoUnidadTributariaID,
                        PlanoId = ut.PlanoId,
                        Superficie = ut.Superficie,
                        Piso = ut.Piso,
                        Unidad = ut.Unidad,
                        Vigencia = ut.Vigencia,
                        TipoUnidadTributaria = ut.TipoUnidadTributaria
                    },
                    Operation = Operation.Remove
                });
            }
        }

        [HttpPost]
        public ActionResult UpdateUnidadTributaria(int TipoUTiD, string Piso, string Designacion, string Observaciones, long UnidadTributariaId, string Partida, string UnidadFuncional, decimal PorcentajeCopropiedad, DateTime? FechaBaja)
        {
            try
            {
                var parcela = (Parcela)Session[_modelSession];
                UnidadTributaria utAnterior = parcela.UnidadesTributarias.Single(x => x.UnidadTributariaId == UnidadTributariaId);

                if (!ValidarPartida(utAnterior.UnidadTributariaId, Partida))
                {
                    return Json(new { partidaRepetida = true }); ;
                }

                var unidadTributaria = new UnidadTributaria
                {
                    UnidadTributariaId = utAnterior.UnidadTributariaId,
                    ParcelaID = utAnterior.ParcelaID,
                    TipoUnidadTributariaID = TipoUTiD,
                    CodigoMunicipal = utAnterior.CodigoMunicipal,
                    CodigoProvincial = Partida,
                    UnidadFuncional = UnidadFuncional,
                    Observaciones = Observaciones,
                    Designaciones = Designacion,
                    PorcentajeCopropiedad = PorcentajeCopropiedad,
                    Piso = Piso,
                    Unidad = utAnterior.Unidad,
                    JurisdiccionID = utAnterior.JurisdiccionID,
                    PlanoId = utAnterior.PlanoId,
                    Superficie = utAnterior.Superficie,
                    FechaVigenciaDesde = utAnterior.FechaVigenciaDesde,
                    FechaVigenciaHasta = utAnterior.FechaVigenciaHasta,
                    Vigencia = utAnterior.Vigencia,
                    FechaBaja = FechaBaja,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    _Ip = Request.UserHostAddress,
                    _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
                };

                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Clear();
                UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
                {
                    Item = unidadTributaria,
                    Operation = unidadTributaria.FechaBaja.HasValue ? Operation.Remove : Operation.Update
                });

                var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
                var response = cliente.PostAsync("api/Parcela/PostUnidadTributaria", content).Result;
                response.EnsureSuccessStatusCode();
                if (!response.Content.ReadAsAsync<bool>().Result)
                {
                    return Json(new { eliminada = true });
                }
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                TitularesDominio = new Dictionary<long, List<TitularViewModel>>();
                //Session[_modelSession] = null;
                //InicializarParcelaSession((long)unidadTributaria.ParcelaID);
                parcela.UnidadesTributarias = ActualizarListaUT(parcela.UnidadesTributarias.ToList(), utAnterior, TipoUTiD, Partida, UnidadFuncional, Observaciones, Designacion, PorcentajeCopropiedad, Piso, FechaBaja);
                CargarDatosUtView(utAnterior.UnidadTributariaId);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenedorParcelario-Update-UT", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        private List<UnidadTributaria> ActualizarListaUT(List<UnidadTributaria> listaUT, UnidadTributaria utAnterior, int TipoUTiD, string Partida, string UnidadFuncional, string Observaciones, string Designacion, decimal PorcentajeCopropiedad, string Piso, DateTime? FechaBaja)
        {
            int index = listaUT.IndexOf(utAnterior);
            if (index != -1)
            {
                var utActualizada = listaUT[index];
                utActualizada.UnidadTributariaId = utAnterior.UnidadTributariaId;
                utActualizada.ParcelaID = utAnterior.ParcelaID;
                utActualizada.TipoUnidadTributariaID = TipoUTiD;
                utActualizada.CodigoMunicipal = utAnterior.CodigoMunicipal;
                utActualizada.CodigoProvincial = Partida;
                utActualizada.UnidadFuncional = UnidadFuncional;
                utActualizada.Observaciones = Observaciones;
                utActualizada.Designaciones = Designacion;
                utActualizada.PorcentajeCopropiedad = PorcentajeCopropiedad;
                utActualizada.Piso = Piso;
                utActualizada.Unidad = utAnterior.Unidad;
                utActualizada.JurisdiccionID = utAnterior.JurisdiccionID;
                utActualizada.PlanoId = utAnterior.PlanoId;
                utActualizada.Superficie = utAnterior.Superficie;
                utActualizada.FechaVigenciaDesde = utAnterior.FechaVigenciaDesde;
                utActualizada.FechaVigenciaHasta = utAnterior.FechaVigenciaHasta;
                utActualizada.Vigencia = utAnterior.Vigencia;
                utActualizada.FechaBaja = FechaBaja;
            }
            return listaUT;
        }

        public bool ValidarPartida(long IdUnidadTributaria, string CodigoProvincial)
        {
            if (CodigoProvincial.Trim() == "0")
            {
                return true;
            }
            using (var resp = cliente.GetAsync($"api/UnidadTributaria/GetPartidaDisponible?idUnidadTributaria={IdUnidadTributaria}&partida={CodigoProvincial}").Result)
            {
                return resp.IsSuccessStatusCode;
            }
        }

        #region Alta Alfanumérica

        [HttpPost]
        public ActionResult AgregarAltaAlfanumerica(long tipoParcelaID, long claseParcelaID, long superficieRegistrada, string observacionesParcela, string nomenclatura, int tipoUTid, string piso, string observacionesUT, string partida, string unidadFuncional, int cantidadUF)
        {
            if (!ValidarPartida(0, partida))
            {
                return Json(new { partidaRepetida = true }); ;
            }
            Parcela nuevaParcela = AgregarParcela(tipoParcelaID, claseParcelaID, superficieRegistrada, observacionesParcela, nomenclatura);
            AgregarNuevaNomenclatura(nomenclatura, nuevaParcela);
            AgregarUnidadTributaria(nuevaParcela, tipoUTid, piso, observacionesUT, partida, unidadFuncional, cantidadUF);
            return Json(new { success = true, ParcelaID = nuevaParcela.ParcelaID });
        }

        public ActionResult AgregarUnidadTributaria(Parcela parcela, int tipoUTid, string piso, string observacionesUT, string partida, string unidadFuncional, int cantidadUF)
        {
            try
            {
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                var ut = CrearUnidadTributaria(parcela, tipoUTid, piso, observacionesUT, partida, unidadFuncional);
                AgregarOperacionUnidadTributaria(ut);
                if (cantidadUF > 0)
                {
                    AgregarUnidadesFuncionales(parcela, cantidadUF);
                }
                var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
                var response = cliente.PostAsync("api/Parcela/PostUnidadTributaria", content).Result;
                response.EnsureSuccessStatusCode();
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenedorParcelario-Save-Nueva-UT", ex);
                return null;
            }
        }

        private UnidadTributaria CrearUnidadTributaria(Parcela parcela, int tipoUTid, string piso, string observacionesUT, string partida, string unidadFuncional)
        {
            return new UnidadTributaria
            {
                ParcelaID = parcela.ParcelaID,
                CodigoProvincial = partida,
                UnidadFuncional = unidadFuncional,
                TipoUnidadTributariaID = tipoUTid,
                Piso = piso,
                Observaciones = observacionesUT,
                PorcentajeCopropiedad = 100,
                UsuarioModificacionID = Usuario.Id_Usuario,
                FechaAlta = DateTime.Now,
                FechaModificacion = DateTime.Now,
                UsuarioAltaID = Usuario.Id_Usuario,
                _Ip = Request.UserHostAddress,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
            };
        }

        private void AgregarOperacionUnidadTributaria(UnidadTributaria unidadTributaria)
        {
            UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Clear();
            UnidadMantenimientoParcelario.OperacionesUnidadTributaria.Add(new OperationItem<UnidadTributaria>
            {
                Item = unidadTributaria,
                Operation = Operation.Add,
            });
        }

        private void AgregarUnidadesFuncionales(Parcela parcela, int cantidadUF)
        {
            for (int i = 0; i < cantidadUF; i++)
            {
                
                var nuevaUF = new UnidadTributaria
                {
                    ParcelaID = parcela.ParcelaID,
                    CodigoProvincial = "0",
                    UnidadFuncional = "0",
                    TipoUnidadTributariaID = (parcela.ClaseParcelaID == 3) ? 3 : 5,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaAlta = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    _Ip = Request.UserHostAddress,
                    _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
                };
                AddUnidadTributaria(nuevaUF);
            }
        }
        /*
        public TipoUnidadTributaria GetTipoUtByID(int tipoUTid)
        {
            // Cambiar a GetAsync con el id en la URL
            var response = cliente.GetAsync($"api/TipoUnidadTributaria/GetById/{tipoUTid}").Result;
            response.EnsureSuccessStatusCode();

            // Deserializar la respuesta al tipo esperado
            return response.Content.ReadAsAsync<TipoUnidadTributaria>().Result;
        }
        */

        [HttpPost]
        public ActionResult AgregarNuevaNomenclatura(string nomenclatura, Parcela parcela)
        {
            parcela.Nomenclaturas = new List<Nomenclatura>();
            var nuevaNomenclatura = new Nomenclatura
            {
                Nombre = nomenclatura.ToUpper(),
                ParcelaID = parcela.ParcelaID,
                TipoNomenclaturaID = 0,
                Tipo = new TipoNomenclatura
                {
                    TipoNomenclaturaID = 0,
                    Descripcion = "NO ESPECIFICADO",
                    ExpresionRegular = "(?<Dep>[0-9]{2})(?<Circ>[0-9]{3})(?<Sec>[0-9a-zA-Z]{2})(?<Cha>[0-9a-zA-Z]{4})(?<Qui>[0-9a-zA-Z]{4})(?<Fra>[0-9a-zA-Z]{4})(?<Man>[0-9a-zA-Z]{4})(?<Par>[0-9a-zA-Z]{5})",
                    Observaciones = ""
                }
            };
            parcela.Nomenclaturas.Add(nuevaNomenclatura);
            Session[_modelSession] = parcela;
            AddNomenclatura(nuevaNomenclatura);

            UnidadMantenimientoParcelario.OperacionesParcela.Clear();
            UnidadMantenimientoParcelario.OperacionesParcela.Add(new OperationItem<Parcela>
            {
                Operation = Operation.Update,
                Item = parcela,
            });

            var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
            var response = cliente.PostAsync("api/Parcela/AddNuevaNomenclatura", content).Result;
            response.EnsureSuccessStatusCode();
            UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public Parcela AgregarParcela(long tipoParcelaID, long claseParcelaID, long superficieRegistrada, string observaciones, string nomenclatura)
        {
            try
            {
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                var parcela = new Parcela
                {
                    AtributoZonaID = 0,
                    ClaseParcelaID = claseParcelaID,
                    EstadoParcelaID = 1,
                    OrigenParcelaID = 1,
                    Superficie = superficieRegistrada,
                    TipoParcelaID = tipoParcelaID,
                    UsuarioModificacionID = Usuario.Id_Usuario,
                    FechaAlta = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    UsuarioAltaID = Usuario.Id_Usuario,
                    _Ip = Request.UserHostAddress,
                    _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress).ToLower(),
                };
                parcela.AtributosCrear(observaciones, false);

                UnidadMantenimientoParcelario.OperacionesParcela.Clear();
                UnidadMantenimientoParcelario.OperacionesParcela.Add(new OperationItem<Parcela>
                {
                    Operation = Operation.Add,
                    Item = parcela,
                });

                var content = new ObjectContent<UnidadMantenimientoParcelario>(UnidadMantenimientoParcelario, new JsonMediaTypeFormatter());
                var response = cliente.PostAsync("api/Parcela/AddNuevaParcela", content).Result;
                response.EnsureSuccessStatusCode();
                //Parcela parcelaNueva = JsonConvert.DeserializeObject<Parcela>(response.Content.ReadAsStringAsync().Result);
                UnidadMantenimientoParcelario = new UnidadMantenimientoParcelario();
                return JsonConvert.DeserializeObject<Parcela>(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError("MantenedorParcelario-Save-Nueva-Parcela", ex);
                return null;
            }
        }
        #endregion

        #endregion

        public JsonResult GetListaUnidadesTributarias()
        {
            var parcela = (Parcela)Session[_modelSession];
            parcela.UnidadesTributarias = GetUTSbyParcelaId(parcela.ParcelaID);

            var listaUT = parcela.UnidadesTributarias
                .OrderBy(ut => (ut.TipoUnidadTributariaID == 6 || ut.TipoUnidadTributariaID == 5) ? 1 : 0)
                .ThenBy(ut => ut.TipoUnidadTributaria.Abreviacion)
                .ThenBy(ut =>
                {
                    var valor = (ut.TipoUnidadTributariaID == 6 || ut.TipoUnidadTributariaID == 3 || ut.TipoUnidadTributariaID == 5)
                                ? ut.UnidadFuncional
                                : ut.CodigoProvincial;
                    return int.TryParse(valor, out var num) ? num : int.MaxValue;
                })
                .Select(ut => new
                {
                    text = (parcela.UnidadesTributarias.Count(uf => uf.TipoUnidadTributariaID == 3) > 1 && parcela.UnidadesTributarias.Count(uc => uc.TipoUnidadTributariaID == 6) > 1 && (ut.TipoUnidadTributariaID == 3 || ut.TipoUnidadTributariaID == 6))
                            ? ut.UnidadFuncional
                            : (ut.TipoUnidadTributariaID == 6 || ut.TipoUnidadTributariaID == 3 || ut.TipoUnidadTributariaID == 5
                                ? ut.TipoUnidadTributaria.Abreviacion + " " + ut.UnidadFuncional
                                : ut.TipoUnidadTributaria.Abreviacion + " " + ut.CodigoProvincial),
                    data = new { UnidadTributariaID = ut.UnidadTributariaId },
                    UnidadTributariaId = ut.UnidadTributariaId,
                    FechaAlta = ut.FechaAlta,
                    TipoUnidadTributariaID = ut.TipoUnidadTributariaID
                })
                .ToList();

            return Json(listaUT, JsonRequestBehavior.AllowGet);
        }

        public List<UnidadTributaria> GetUTSbyParcelaId(long idParcela)
        {
            var result = cliente.GetAsync($"api/UnidadTributaria/GetUnidadesTributariasActivas?idParcela={idParcela}&incluirTitulares=false&esHistorico=true").Result.EnsureSuccessStatusCode();
            return (List<UnidadTributaria>)result.Content.ReadAsAsync<ICollection<UnidadTributaria>>().Result;
        }

    }
}