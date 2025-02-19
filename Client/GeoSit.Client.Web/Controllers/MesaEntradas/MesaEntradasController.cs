using GeoSit.Client.Web.Helpers;
using GeoSit.Client.Web.Models;
using GeoSit.Client.Web.Models.FormularioValuacion;
using GeoSit.Client.Web.ViewModels;
using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.BusinessEntities.ValidacionesDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using static GeoSit.Data.BusinessEntities.Common.Enumerators;

namespace GeoSit.Client.Web.Controllers
{
    public class MesaEntradasController : Controller
    {
        private readonly HttpClient _cliente;
        private string uploadTempFolder = string.Empty;

        private UsuariosModel Usuario
        {
            get { return (UsuariosModel)Session["usuarioPortal"]; }
        }

        private List<PerfilFuncion> FuncionesHabilitadas
        {
            get { return (List<PerfilFuncion>)Session["FuncionesHabilitadas"]; }
        }

        private List<METramiteDocumento> NotasTramiteActual  
        {
            get { return (List<METramiteDocumento>)Session["NotasTramiteActual"]; }
            set { Session["NotasTramiteActual"] = value; }
        }

        private List<MEMovimiento> MovimientosTramiteActual  
        {
            get { return (List<MEMovimiento>)Session["MovimientosTramiteActual"]; }
            set { Session["MovimientosTramiteActual"] = value; }
        }

        private ArchivoDescarga ArchivoInforme
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }

        private ArchivoDescarga ArchivoDescarga
        {
            get { return Session["ArchivoDescarga"] as ArchivoDescarga; }
            set { Session["ArchivoDescarga"] = value; }
        }

        private FormularioValuacionModel ValuacionTemporal  
        {
            get { return (FormularioValuacionModel)Session["ValuacionTemporal"]; }
            set { Session["ValuacionTemporal"] = value; }
        }

        private SuperficieModel[] ValuacionTemporalSuperficies  
        {
            get { return (SuperficieModel[])Session["ValuacionTemporalSuperficies"]; }
            set { Session["ValuacionTemporalSuperficies"] = value; }
        }

        public MesaEntradasController()
        {
            _cliente = new HttpClient()
            {
                BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]),
                Timeout = TimeSpan.FromMinutes(60)
            };

            uploadTempFolder = GetParamtrosGeneralesByClave("RUTA_DOCUMENTOS");

        }

        public async Task<ActionResult> Index()
        {
            using (_cliente)
            {
                var estados = GetEstados();
                var prioridades = GetPrioridades();
                var asuntos = GetAsuntos();
                var causas = GetCausasByAsunto();
                var sectores = GetSectores();
                var sectorUsuario = GetSectorUsuario();
                var model = new BandejaMesaEntradasViewModel()
                {
                    EsProfesional = !await EsUsuarioInterno(),
                    Estados = (await estados).ToSelectList("-- Todos --"),
                    Causas = (await causas).ToSelectList("-- Todas --"),
                    Prioridades = (await prioridades).ToSelectList("-- Todas --"),
                    Asuntos = (await asuntos).ToSelectList("-- Todos --"),
                    Sectores = (await sectores).ToSelectList("-- Todos --")
                };
                ViewData["Sector"] = (await sectorUsuario).Nombre;
                return PartialView(model);
            }
        }

        public async Task<ActionResult> GetCausas(long idAsunto, bool bandeja = false)
        {
            using (_cliente)
            {
                return Json((await GetCausasByAsunto(idAsunto))
                                .ToSelectList(bandeja ? "-- Todas --" : "-- Seleccionar --"), JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> SearchTramites(int role, DataTableParameters filters)
        {
            string bandeja = GetBandejaByRole(role);
            using (var resp = await _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{bandeja}?idUsuario={Usuario.Id_Usuario}", filters, new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return Json(await resp.Content.ReadAsAsync<DataTableResult<GrillaTramite>>());
                }
                MvcApplication.GetLogger().LogError("SearchTramites", $"Falló la búsqueda de trámites: {resp.ReasonPhrase}");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ActionResult> ReloadTramite(bool soloLectura = true)
        {
            return await LoadTramiteForm(await GetTramite(Convert.ToInt32(Session["Current_IdTramite"])), soloLectura);
        }

        [HttpPost]
        public async Task<ActionResult> GetAvailableActions(int role, GrillaTramite[] selection)
        {
            var result = await TryGetAvailableActions(role, selection?.Select(t => t.IdTramite)?.ToArray(), true);
            return result.Item1
                        ? Json(result.Item2) as ActionResult
                        : new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
        }

        [HttpPost]
        public ActionResult DatosEspecificosOrigen(short tipo, long[] ids)
        {
            using (var resp = _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/datosespecificos/origen/objetos/tipos/{tipo}", ids).Result)
            {
                resp.EnsureSuccessStatusCode();
                var objetos = resp.Content.ReadAsAsync<List<MEDatosEspecificos>>().Result;
                return Json(objetos);
            }
        }

        [HttpPost]
        public async Task<ActionResult> ExecutableAction(int role, long action, GrillaTramite[] selection)
        {
            var tramites = selection?.Select(t => t.IdTramite) ?? new int[0];
            if (!AccionDisponible(role, action, tramites, out string error))
            {
                MvcApplication.GetLogger().LogError("ExecutableAction", $"La acción ejecutada, \"{action}\", no está disponible para los trámites seleccionados: {error}");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }

            if (action == Convert.ToInt64(FuncionesTramite.Nuevo))
            {
                return await LoadTramiteForm(null, false);
            }
            else if (action == Convert.ToInt64(FuncionesTramite.Consultar) || action == Convert.ToInt64(FuncionesTramite.Editar))
            {
                return await LoadTramiteForm(await GetTramite(tramites.First()), action == Convert.ToInt64(FuncionesTramite.Consultar));
            }
            else if(action == Convert.ToInt64(FuncionesTramite.Derivar))
            {
                return await LoadDerivacionForm(tramites);
            }
            else if(action == Convert.ToInt64(FuncionesTramite.Recibir))
            {
                return await Recibir(tramites);
            }
            else if(action == Convert.ToInt64(FuncionesTramite.Reasignar))
            {
                var usuarios = selection?.Select(t => t.UsuarioSectorActual).Distinct();
                return await LoadReasignacionForm(tramites, usuarios);
            }
            return Json(new { action });
        }

        public async Task<ActionResult> LoadDerivacionForm(IEnumerable<int> tramites)
        {
            ViewData["Sectores"] = (await GetSectores(true)).ToSelectList("-- Seleccionar --");
            var model = new Derivacion()
            {
                IdTramitesSeleccionados = tramites.ToArray()
            };
            return PartialView("DerivarTramite", model);
        }

        public async Task<ActionResult> LoadReasignacionForm(int tramite)
        {
            var operadorSector = await GetOperadorTramite(tramite);
            return await LoadReasignacionForm(new[] { tramite }, new[] { operadorSector.NombreApellidoCompleto });
        }

        private async Task<ActionResult> LoadReasignacionForm(IEnumerable<int> tramites, IEnumerable<string> usuarios)
        {
            ViewData["Usuarios"] = (await GetUsuariosSector(Usuario.IdSector.Value, usuarios)).ToSelectList("-- Seleccionar --");
            var model = new Asignacion()
            {
                IdTramitesAsignados = tramites.ToArray()
            };
            return PartialView("ReasignarTramite", model);
        }

        public ActionResult LoadObservacionForm(int tramite)
        {
            var model = new Observacion()
            {
                IdTramite = tramite
            };
            return PartialView("ObservarTramite", model);
        }

        public async Task<ActionResult> Derivar(Derivacion derivacion)
        {
            derivacion.IdUsuarioOperacion = Usuario.Id_Usuario;
            derivacion.Ip = Request.UserHostAddress;
            derivacion.MachineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            using (var resp = await _cliente.PutAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/Sector", derivacion, new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                IEnumerable<string> mensajes = new[]
                {
                    $"Ha ocurrido un error al derivar {(derivacion.IdTramitesSeleccionados.Length == 1 ? "el" : "los")} trámite{(derivacion.IdTramitesSeleccionados.Length == 1 ? "" : "s")}.",
                };

                if(resp.StatusCode == HttpStatusCode.ExpectationFailed)
                {
                    var ex = await resp.Content.ReadAsAsync<ValidacionException>();
                    mensajes = mensajes.Append("").Concat(ex.Errores);
                }
                return Json(new { error = true, mensajes });
            }
        }

        public async Task<ActionResult> Observar(Observacion observacion)
        {
            observacion.IdUsuarioOperacion = Usuario.Id_Usuario;
            observacion.Ip = Request.UserHostAddress;
            observacion.MachineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            using (var resp = await _cliente.PutAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/observacion", observacion, new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return Json(new { error = true, mensajes = new[] { $"Ha ocurrido un error al derivar el trámite." } });
            }
        }

        public async Task<ActionResult> Recibir(IEnumerable<int> tramites)
        {
            var asignacion = new Asignacion()
            {
                IdTramitesAsignados = tramites.ToArray(),
                IdUsuarioAsignado = Usuario.Id_Usuario,
                IdUsuarioOperacion = Usuario.Id_Usuario,
                Ip = Request.UserHostAddress,
                MachineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress),
            };
            using (var resp = await _cliente.PutAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/Usuario", asignacion, new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return Json(new { error = true, mensajes = new[] { $"Ha ocurrido un error al recepcionar {(tramites.Count() == 1 ? "el" : "los")} trámite{(tramites.Count() == 1 ? "" : "s")}." } });
            }
        }

        public async Task<ActionResult> Reasignar(Asignacion asignacion)
        {
            asignacion.Reasigna = true;
            asignacion.IdUsuarioOperacion = Usuario.Id_Usuario;
            asignacion.Ip = Request.UserHostAddress;
            asignacion.MachineName = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            using (var resp = await _cliente.PutAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/Usuario", asignacion, new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                return Json(new { error = true, mensajes = new[] { $"Ha ocurrido un error al reasignar {(asignacion.IdTramitesAsignados.Count() == 1 ? "el" : "los")} trámite{(asignacion.IdTramitesAsignados.Count() == 1 ? "" : "s")}." } });
            }
        }

        public async Task<ActionResult> GetTramiteActions(int idTramite, int idAsunto)
        {
            return PartialView("Partial/AccionesTramite", await TryGetTramiteActions(idTramite, idAsunto, false));
        }
        public async Task<List<Accion>> TryGetTramiteActions(int idTramite, int idAsunto, bool soloLectura)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{idTramite}/AccionesDisponibles?idUsuario={Usuario.Id_Usuario}&idTipoTramite={idAsunto}&soloLectura={soloLectura}"))
            {
                var acciones = new List<Accion>();
                if (resp.IsSuccessStatusCode)
                {
                    acciones = (await resp.Content.ReadAsAsync<AccionesTramites>()).Seleccion;
                }

                var nuevo = acciones.SingleOrDefault(a => a.IdAccion == Convert.ToInt64(FuncionesTramite.Nuevo) || a.IdAccion == Convert.ToInt64(FuncionesTramite.Editar));
                if (nuevo != null)
                {
                    acciones = new[] { new Accion() { IdAccion = 0, Nombre = "Grabar" } }.Concat(acciones.Except(new[] { nuevo })).ToList();
                }
                return acciones;
            }
        }
        private async Task<ActionResult> LoadTramiteForm(METramite tramite, bool openReadonly = true)
        {
            int idTramiteTemporal = Convert.ToInt32(Usuario.Id_Usuario) * -1;
            var model = tramite ?? new METramite() { IdTramite = idTramiteTemporal, UsuarioAlta = Usuario.Id_Usuario, IdPrioridad = 1, Movimientos = new List<MEMovimiento>() };
            MovimientosTramiteActual = model.Movimientos?.ToList() ?? new List<MEMovimiento>();
            NotasTramiteActual = model.TramiteDocumentos?.ToList() ?? new List<METramiteDocumento>();
            try
            {
                var prioridades = GetPrioridades();
                var asuntos = GetAsuntos();
                var causas = GetCausasByAsunto(model.IdTipoTramite);
                var accionesTramite = TryGetTramiteActions(model.IdTramite, model.IdTipoTramite, openReadonly);

                var directorio = Path.Combine(uploadTempFolder, idTramiteTemporal.ToString(), "temporales");
                if (Directory.Exists(directorio))
                {
                    Directory.Delete(directorio, true);
                }
                bool esUsuarioInterno =  await EsUsuarioInterno();
                bool tieneAntecedentesGenerados = await TieneAntecedentesGenerados(model.IdTramite);
                ViewData["EsProfesional"] = !esUsuarioInterno;
                ViewData["Iniciador"] = model.Iniciador?.NombreCompleto;
                ViewData["Prioridades"] = (await prioridades).ToSelectList("", model.IdPrioridad);
                ViewData["Asuntos"] = (await asuntos).ToSelectList("-- Seleccionar --", model.IdTipoTramite);
                ViewData["Causas"] = (await causas).ToSelectList("-- Seleccionar --", model.IdObjetoTramite);
                ViewData["tiposDocumentos"] = new SelectList(await GetTiposDocumentos(), "Value", "Text");
                ViewData["Acciones"] = await accionesTramite;
                ViewData["Accion"] = openReadonly
                                        ? "Consulta"
                                        : tramite == null ? "Nuevo" : "Editar";

                var profesionalTramite = await GetUsuario(model.UsuarioAlta);
                ViewData["sectorProfesional"] = profesionalTramite.SectorUsuario?.Nombre?.Trim();
                ViewData["loginProfesional"] = profesionalTramite.Login.Trim();
                ViewData["nombreProfesional"] = $"{profesionalTramite.Apellido?.Trim()}, {profesionalTramite.Nombre?.Trim()}";
                ViewData["Readonly"] = openReadonly;
                ViewData["UsuarioNombre"] = $"{Usuario.Apellido}, {Usuario.Nombre}";
                ViewData["IdUsuario"] = Usuario.Id_Usuario.ToString();
                ViewData["AntecedentesGenerados"] = tieneAntecedentesGenerados;
                ViewData["Ingresado"] = (model.Movimientos ?? new MEMovimiento[0]).Any(m => (EnumTipoMovimiento)m.IdTipoMovimiento == EnumTipoMovimiento.Ingresar);

                bool reservasGeneradas = (model.Movimientos ?? new MEMovimiento[0]).Any(m => (EnumTipoMovimiento)m.IdTipoMovimiento == EnumTipoMovimiento.ConfirmarReservas);
                ViewData["ReservasGeneradas"] = reservasGeneradas;
                ViewData["PuedeAsignarReservaNumeroPlano"] = !openReadonly && !reservasGeneradas && esUsuarioInterno && FuncionesHabilitadas.Any(f => f.Id_Funcion == long.Parse(FuncionesTramite.ReservarNumeroPlano));
                ViewData["PuedeAsignarReservasNomenclaturas"] = !openReadonly && !reservasGeneradas && esUsuarioInterno && FuncionesHabilitadas.Any(f => f.Id_Funcion == long.Parse(FuncionesTramite.ReservarNomenclaturasPartidas));

                var resumenVisados = ResumenVisadosViewModel.Create(model);
                ViewData["ResumenVisados"] = resumenVisados;
                ViewData["PuedeValuar"] = esUsuarioInterno && !openReadonly
                                            && !resumenVisados.VisadoValuatorioCompleto && resumenVisados.VisadoGraficoCompleto
                                            && FuncionesHabilitadas.Any(f => f.Id_Funcion == long.Parse(FuncionesTramite.VisarDatosValuativos));

                Session["Current_IdTramite"] = model.IdTramite;
                return PartialView("Tramite", model);
            }
            catch (Exception ex)
            {
                MvcApplication.GetLogger().LogError($"MesaEntradas/LoadTramiteForm(id: {model.IdTramite})", ex);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        private async Task<bool> TieneAntecedentesGenerados(int idTramite)
        {
            using (var req = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/{idTramite}/antecedentes"))
            {
                return await req.Content.ReadAsAsync<bool>();
            }
        }

        public async Task<ActionResult> LoadDatosOrigen()
        {
            var datos = await GetDatosOrigen(Convert.ToInt32(Session["Current_IdTramite"]));
            return Json(datos, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> LoadDatosDestino()
        {
            var datos = await GetDatosDestino(Convert.ToInt32(Session["Current_IdTramite"]));
            return Json(datos, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadNotas()
        {
            var documentos = NotasTramiteActual.Select(nota => new METramiteDocumento()
            {
                id_documento = nota.id_documento,
                FechaAlta = nota.FechaAlta,
                FechaAprobacion = nota.FechaAprobacion,
                Documento = new Data.BusinessEntities.Documentos.Documento()
                {
                    id_documento = nota.id_documento,
                    id_tipo_documento = nota.Documento.id_tipo_documento,
                    Tipo = new Data.BusinessEntities.Documentos.TipoDocumento()
                    {
                        TipoDocumentoId = nota.Documento.id_tipo_documento,
                        Descripcion = nota.Documento.Tipo.Descripcion
                    },
                    descripcion = nota.Documento.descripcion,
                    observaciones = nota.Documento.observaciones,
                    nombre_archivo = nota.Documento.nombre_archivo,
                    extension_archivo = nota.Documento.extension_archivo
                },
                Usuario_Alta = new Usuarios()
                {
                    Nombre = nota.Usuario_Alta.Nombre,
                    Apellido = nota.Usuario_Alta.Apellido,
                },
                UsuarioAlta = nota.UsuarioAlta,
            }).ToList();
            NotasTramiteActual.Clear();
            NotasTramiteActual = null;
            return Json(documentos, JsonRequestBehavior.AllowGet); 
        }

        public async Task<ActionResult> LoadGenerador(int idTramite, long idObjetoTramite)
        {
            GeneradorDestinos generador;
            string title, view, url;
            bool esCausaPHEspecial = idObjetoTramite == 8;
            if(esCausaPHEspecial)
            {
                title = "Generar Partidas Destino";
                view = "GeneradorPartidas";
                url = "GenerarPartidas";
                generador = new GeneradorPartidas() { IdTramite = idTramite };
                ViewData["TiposUT"] = (await GetTiposUnidadTributaria()).ToSelectList();
            }
            else
            {
                title = "Generar Parcelas Destino";
                view = "GeneradorParcelas";
                url = "GenerarParcelas";
                generador = new GeneradorParcelas() { IdTramite = idTramite };
                var tiposParcela = GetTiposParcela();
                var clasesParcela = GetClasesParcela();
                var estadosParcela = GetEstadosParcela();
                ViewData["TiposParcela"] = (await tiposParcela).ToSelectList();
                ViewData["ClasesParcela"] = (await clasesParcela).ToSelectList(long.Parse(ClasesParcelas.ParcelaComun));
                ViewData["EstadosParcela"] = (await estadosParcela).ToSelectList(long.Parse(EstadosParcelas.Baldio));
            }
            generador.Cantidad = 1;
            ViewData["title"] = title;
            ViewData["view"] = $"partial/{view}";
            ViewData["url"] = url;
            return PartialView("GeneradorDestinos", generador);
        }

        public ActionResult LoadMovimientos()
        {
            var movimientos = MovimientosTramiteActual.Select(mta => new 
            {
                mta.IdMovimiento,
                Fecha = mta.FechaAlta,
                SectorDestino = mta.SectorDestino.Nombre,
                Tipo = mta.TipoMovimiento.Descripcion,
                Usuario = mta.Usuario_Alta.NombreApellidoCompleto,
                Estado = mta.Estado.Descripcion,
                Remito = mta.Remito?.IdRemito.ToString() ?? string.Empty,
                mta.Observacion
            }).ToList();
            MovimientosTramiteActual.Clear();
            MovimientosTramiteActual = null;
            return Json(movimientos, JsonRequestBehavior.AllowGet); 
        }

        public async Task<ActionResult> GenerarPartidas(GeneradorPartidas model)
        {
            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/partidas/generador", model))
            {
                object data = null;
                string[] mensajes = null;
                bool error = !resp.IsSuccessStatusCode;
                if (error)
                {
                    mensajes = new[] { "No se pudieron generar las partidas destino." };
                }
                else
                {
                    data = await resp.Content.ReadAsAsync<List<MEDatosEspecificos>>();
                }
                return Json(new { error, mensajes, data });
            }
        }

        public async Task<ActionResult> GenerarParcelas(GeneradorParcelas model)
        {
            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/parcelas/generador", model))
            {
                object data = null;
                string[] mensajes = null;
                bool error = !resp.IsSuccessStatusCode;
                if (error)
                {
                    mensajes = new[] { "No se pudieron generar las parcelas destino." };
                }
                else
                {
                    data = await resp.Content.ReadAsAsync<List<MEDatosEspecificos>>();
                }
                return Json(new { error, mensajes, data });
            }
        }

        [HttpPost]
        public async Task<ActionResult> LoadFormularioValuacion(int idTramite, long idUT, Propiedad superficies = null)
        {
            ViewData["esTemporal"] = true;
            ValuacionTemporalSuperficies = superficies == null ? null : JsonConvert.DeserializeObject<SuperficieModel[]>(superficies.Value);
            ValuacionTemporal = FormularioValuacionModel.FromTemporalEntity(await GetValuacionVigenteByIdUT(idTramite, idUT), ValuacionTemporalSuperficies);
            return PartialView("~/Views/Valuaciones/Formulario.cshtml", ValuacionTemporal);
        }

        /*
        public async Task<ActionResult> GenerarAntecedentes(int id)
        {
            var tramite = new METramite()
            {
                IdTramite = id,
                UsuarioAlta = Usuario.Id_Usuario
            };
            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/antecedentes", tramite))
            {
                if (resp.IsSuccessStatusCode)
                {
                    var bytes = await resp.Content.ReadAsAsync<byte[]>();
                    ArchivoDescarga = new ArchivoDescarga(bytes, $"Antecedentes Trámite {id}.zip", "application/zip");
                }
                return new HttpStatusCodeResult(resp.StatusCode);
            }
        }
        */

        private async Task<VALValuacionTemporal> GetValuacionVigenteByIdUT(int tramite, long ut)
        {
            using(var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/{tramite}/unidadestributarias/{ut}/valuacion"))
            {
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsAsync<VALValuacionTemporal>();
            }
        }

        private async Task<List<MEDatosEspecificos>> GetDatosDestino(int? tramite)
        {
            if (tramite == null) return new List<MEDatosEspecificos>();

            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/{tramite}/datosespecificos/destino"))
            {
                resp.EnsureSuccessStatusCode();
                return (await resp.Content.ReadAsAsync<IEnumerable<MEDatosEspecificos>>()).ToList();
            }
        }
        private async Task<List<MEDatosEspecificos>> GetDatosOrigen(int? tramite)
        {
            if (tramite == null) return new List<MEDatosEspecificos>();

            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/{tramite}/datosespecificos/origen"))
            {
                resp.EnsureSuccessStatusCode();
                return (await resp.Content.ReadAsAsync<IEnumerable<MEDatosEspecificos>>()).ToList();
            }
        }

        private async Task<METramite> GetTramite(int id)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{id}"))
            {
                resp.EnsureSuccessStatusCode();
                var tramite = await resp.Content.ReadAsAsync<METramite>();
                return tramite;
            }
        }

        private async Task<ValueTuple<bool, AccionesTramites>> TryGetAvailableActions(int role, int[] selection, bool readOnly)
        {
            var tramites = selection ?? new int[0];
            string bandeja = GetBandejaByRole(role);

            using (var resp = await _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{bandeja}/AccionesDisponibles?idUsuario={Usuario.Id_Usuario}&soloLectura={readOnly}", tramites.ToArray(), new JsonMediaTypeFormatter()))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return new ValueTuple<bool, AccionesTramites>(true, await resp.Content.ReadAsAsync<AccionesTramites>());
                }
                MvcApplication.GetLogger().LogError("TryGetAvailableActions", $"Falló la recuperación de acciones disponibles para los trámites seleccionados en la bandeja \"{bandeja}\": {resp.ReasonPhrase}");
                return new ValueTuple<bool, AccionesTramites>(false, null);
            }
        }
        private async Task<Usuarios> GetOperadorTramite(int tramite)
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{tramite}/Operador"))
            {
                if (resp.IsSuccessStatusCode)
                {
                    return await resp.Content.ReadAsAsync<Usuarios>(); 
                }
                return null;
            }
        }

        private bool AccionDisponible(int role, long action, IEnumerable<int> tramites, out string error)
        {
            error = string.Empty;
            string bandeja = GetBandejaByRole(role);
            using (var resp = _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/MesaEntradas/Tramites/{bandeja}/Acciones/{action}/Disponible?idUsuario={Usuario.Id_Usuario}", tramites.ToArray(), new JsonMediaTypeFormatter()).Result)
            {
                return resp.IsSuccessStatusCode && resp.Content.ReadAsAsync<bool>().Result;
            }
        }

        private static string GetBandejaByRole(int role)
        {
            string bandeja = "Detalle";
            switch (role)
            {
                case 1:
                    bandeja = "Propios";
                    break;
                case 2:
                    bandeja = "Sector";
                    break;
                case 3:
                    bandeja = "Catastro";
                    break;
            }
            return bandeja;
        }

        private async Task<Sector> GetSectorUsuario()
        {
            using (var req = await _cliente.GetAsync($"api/seguridadservice/getsectorusuario?idUsuario={Usuario.Id_Usuario}"))
            {
                return await req.Content.ReadAsAsync<Sector>();
            }
        }

        private async Task<List<METipoTramite>> GetAsuntos()
        {
            using (var req = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/asuntos"))
            {
                var tiposTramite = new List<METipoTramite>();
                if (req.IsSuccessStatusCode)
                {
                    tiposTramite = await req.Content.ReadAsAsync<List<METipoTramite>>();
                }
                return tiposTramite;
            }
        }

        private async Task<List<MEPrioridadTramite>> GetPrioridades()
        {
            using (var req = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/prioridades"))
            {
                var prioridades = new List<MEPrioridadTramite>();
                if (req.IsSuccessStatusCode)
                {
                    prioridades = await req.Content.ReadAsAsync<List<MEPrioridadTramite>>();
                }
                return prioridades;
            }
        }

        private async Task<List<MEEstadoTramite>> GetEstados()
        {
            using (var req = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/estados"))
            {
                var tiposTramite = new List<MEEstadoTramite>();
                if (req.IsSuccessStatusCode)
                {
                    tiposTramite = await req.Content.ReadAsAsync<List<MEEstadoTramite>>();
                }
                return tiposTramite;
            }
        }

        private async Task<List<MEObjetoTramite>> GetCausasByAsunto(long asunto = 0)
        {
            using (var req = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/asuntos/{asunto}/causas"))
            {
                var causas = new List<MEObjetoTramite>();
                if (req.IsSuccessStatusCode)
                {
                    causas = await req.Content.ReadAsAsync<List<MEObjetoTramite>>();
                }
                return causas;
            }
        }

        private async Task<Usuarios> GetUsuario(long idUsuario)
        {
            using (var resp = await _cliente.GetAsync($"api/SeguridadService/GetUsuarioById/{idUsuario}?incluirSector=true"))
            {
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsAsync<Usuarios>();
            }
        }

        private async Task<List<Sector>> GetSectores(bool excluirSectorUsuario = false)
        {
            using (var req = await _cliente.GetAsync("api/SeguridadService/GetSectores"))
            {
                var sectores = new List<Sector>();
                if (req.IsSuccessStatusCode)
                {
                    sectores = await req.Content.ReadAsAsync<List<Sector>>();
                    if (excluirSectorUsuario)
                    {
                        sectores = sectores.Where(s => s.IdSector != Usuario.IdSector).ToList();
                    }
                }
                return sectores;
            }
        }

        private async Task<List<Usuarios>>GetUsuariosSector(int idSector, IEnumerable<string> usuarios)
        {
            using (var req = await _cliente.GetAsync($"api/SeguridadService/GetUsuariosMismoSector?idSector={idSector}"))
            {
                var usuariosSector = new List<Usuarios>();
                if (req.IsSuccessStatusCode)
                {
                    usuariosSector = await req.Content.ReadAsAsync<List<Usuarios>>();
                }
                return usuariosSector
                            .Where(u => !usuarios.Contains(u.NombreApellidoCompleto))
                            .ToList();
            }
        }

        private async Task<List<TipoParcela>> GetTiposParcela()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/Tipos"))
            {
                var tipos = new List<TipoParcela>();
                if (resp.IsSuccessStatusCode)
                {
                    tipos = await resp.Content.ReadAsAsync<List<TipoParcela>>();
                }
                return tipos;
            }
        }
        private async Task<List<ClaseParcela>> GetClasesParcela()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/Clases"))
            {
                var clases = new List<ClaseParcela>();
                if (resp.IsSuccessStatusCode)
                {
                    clases = await resp.Content.ReadAsAsync<List<ClaseParcela>>();
                }
                return clases;
            }
        }
        private async Task<List<EstadoParcela>> GetEstadosParcela()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/Parcelas/Estados"))
            {
                var estados = new List<EstadoParcela>();
                if (resp.IsSuccessStatusCode)
                {
                    estados = await resp.Content.ReadAsAsync<List<EstadoParcela>>();
                }
                return estados;
            }
        }
        private async Task<List<TipoUnidadTributaria>> GetTiposUnidadTributaria()
        {
            using (var resp = await _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/UnidadesTributarias/Tipos"))
            {
                var tipos = new List<TipoUnidadTributaria>();
                if (resp.IsSuccessStatusCode)
                {
                    tipos = await resp.Content.ReadAsAsync<List<TipoUnidadTributaria>>();
                }
                return tipos;
            }
        }
        private async Task<bool> EsUsuarioInterno()
        {
            using (var req = await _cliente.GetAsync($"api/SeguridadService/Usuarios/{Usuario.Id_Usuario}/Interno"))
            {
                return await req.Content.ReadAsAsync<bool>();
            }
        }

        public async Task<List<SelectListItem>> GetTiposDocumentos(string opcionVacia = "")
        {
            using (var resp = await _cliente.GetAsync("api/TipoDocumentoService/GetTiposDocumento"))
            {
                var tipos = await resp.Content.ReadAsAsync<IEnumerable<TiposDocumentosModel>>();
                return new[] { new SelectListItem { Text = opcionVacia, Value = "" } }
                            .Concat(tipos.OrderBy(x => x.Descripcion)
                                         .Select(x => new SelectListItem { Text = x.Descripcion, Value = x.TipoDocumentoId.ToString() }))
                            .ToList();
            }
        }

        public ActionResult Preview(long idCausa, MEDatosEspecificos origen)
        {
            string url = string.Empty;
            string nombre = string.Empty;
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            if (idCausa == Convert.ToInt32(ObjetosTramites.CertificadoValuacionFiscal))
            {
                var prop = origen.Propiedades.Single(p => p.Id == KeysDatosEspecificos.KeyIdUnidadTributaria);
                url = $"CertificadoValuatorio/Get?idUnidadTributaria={prop.Value}&usuario={usuario}";

                string identificador = $"Partida {prop.Text}";
                if (prop.Text == "0")
                {
                    prop = origen.Propiedades.Single(p => p.Id == KeysDatosEspecificos.KeyIdParcela);
                    identificador = $"Parcela {prop.Text}";
                }
                prop = origen.Propiedades.Single(p => p.Id == KeysDatosEspecificos.KeyUnidadFuncional);
                nombre = $"Certificado de Valuación Fiscal - {identificador} (UF {(string.IsNullOrEmpty(prop.Text) ? "-" : prop.Text)}).pdf";
            }
            else if (idCausa == Convert.ToInt64(ObjetosTramites.InformeCatastral) || idCausa == Convert.ToInt64(ObjetosTramites.ConstanciaNomenclaturaCatastral))
            {
                var prop = origen.Propiedades.Single(p => p.Id == KeysDatosEspecificos.KeyIdParcela);
                url = $"InformeParcelario/GetInforme/{prop.Value}/?padronPartidaId={Resources.@Recursos.MostrarPadrónPartida}&usuario={usuario}";
                nombre = $"Informe Catastral - Parcela {prop.Text}.pdf";
            }
            else if (idCausa == Convert.ToInt64(ObjetosTramites.PlanoAprobado))
            {
                
                string id = origen.Propiedades.Single(p => p.Id == KeysDatosEspecificos.KeyIdMensura).Value;
                url = $"InformePlanoAprobado/GetInformePlanoAprobado?idMensura={id}&usuario={usuario}";
                nombre = "Plano Aprobado.pdf";
                
            }

            using (var apiReportes = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/{url}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                byte[] bytes = Convert.FromBase64String(bytes64);
                ArchivoDescarga = new ArchivoDescarga(bytes, nombre, "application/pdf");
                ViewData["filename"] = ArchivoDescarga.NombreArchivo;
                return PartialView("~/Views/PdfInternalViewer/View.cshtml", Convert.ToBase64String(bytes));
            }
        }

        public FileResult Download()
        {
            return File(ArchivoDescarga.Contenido, ArchivoDescarga.MimeType, ArchivoDescarga.NombreArchivo);
        }

        [HttpPost]
        public JsonResult UploadDocumento()
        {
            if (this.Request.Files?.Count != 1)
            {
                throw new Exception("Hubo un error con el archivo recibido");
            }
            int idTramite = Convert.ToInt32(this.Request.Form["idTramite"]);
            var archivo = this.Request.Files[0];

            string nombreArchivoFinal = Path.GetFileName(archivo.FileName);
            var directorio = Directory.CreateDirectory(Path.Combine(uploadTempFolder, idTramite.ToString(), "temporales"));
            string nombreArchivoFinalFull = Path.Combine(directorio.FullName, nombreArchivoFinal);
            bool fileExist = System.IO.File.Exists(nombreArchivoFinalFull);

            if (fileExist)
            {
                string fechaNombre = DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
                nombreArchivoFinal = nombreArchivoFinal.Substring(0, nombreArchivoFinal.Length - 4) + "_" + fechaNombre + nombreArchivoFinal.Substring(nombreArchivoFinal.Length - 4);
                nombreArchivoFinalFull = Path.Combine(directorio.FullName, nombreArchivoFinal);
            }
            archivo.SaveAs(nombreArchivoFinalFull);
            return Json(new { nombreArchivo = nombreArchivoFinal });
        }

        [HttpGet]
        public ActionResult ExisteArchivo(string tramite, string archivo)
        {
            ArchivoDescarga = null;
            string path;
            archivo = Encoding.Default.GetString(Convert.FromBase64String(archivo));
            if (System.IO.File.Exists((path = Path.Combine(uploadTempFolder, tramite, "temporales", archivo))) ||
                System.IO.File.Exists((path = Path.Combine(uploadTempFolder, tramite, "documentos", archivo))))
            {
                ArchivoDescarga = new ArchivoDescarga(System.IO.File.ReadAllBytes(path), archivo, "application/octet-stream");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            return new HttpStatusCodeResult(HttpStatusCode.NotFound);
        }

        public async Task<ActionResult> ExecuteDirectAction(int idTramite, long idAction)
        {
            var obj = new METramite()
            {
                IdTramite = idTramite,
                _Ip = Request.UserHostAddress,
                _Id_Usuario = Usuario.Id_Usuario,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress)
            };

            using (var resp = await _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/actions/{idAction}", new[] { obj }, new JsonMediaTypeFormatter()))
            {
                ActionResult rta;
                if (resp.StatusCode == HttpStatusCode.Conflict)
                {
                    var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(await resp.Content.ReadAsStringAsync());
                    rta = Json(new { mensajes = ex.Errores.ToList(), error = true });
                }
                else 
                {
                    rta = new HttpStatusCodeResult(resp.StatusCode);
                }
                return rta;
            }

        }
        public ActionResult TramiteSave(METramite tramite, METramiteDocumento[] documentos, MEDatosEspecificos[] datosOrigen, MEDatosEspecificos[] datosDestino, bool ingresar)
        {
            tramite._Ip = Request.UserHostAddress;
            tramite._Id_Usuario = Usuario.Id_Usuario;
            tramite._Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            MapSuperficiesValuacionToValSuperficies(datosDestino?.Select(d => d.Propiedades.SingleOrDefault(p => p.Id == KeysDatosEspecificos.KeyValuacion)).Where(p => p != null));

            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = documentos,
                DatosOrigen = datosOrigen,
                DatosDestino = datosDestino,
                TipoMovimiento = (int)(ingresar ? EnumTipoMovimiento.Ingresar : EnumTipoMovimiento.None)
            };

            using (var resp = _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites", tramiteParameters, new JsonMediaTypeFormatter()).Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    ActionResult rta;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            rta = Json(new { mensajes = new[] { res.Message?.Value }, error = true });
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            Session["Current_IdTramite"] = ex.IdTramite;
                            rta = Json(new { mensajes = ex.Errores.ToList(), error = resp.StatusCode != HttpStatusCode.Conflict });
                            break;
                        default:
                            rta = Json(new { mensajes = new[] { resp.ReasonPhrase }, error = true });
                            break;
                    }
                    return rta;
                }
                Session["Current_IdTramite"] = resp.Content.ReadAsAsync<int>().Result;
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public async Task<ActionResult> SolicitarReservas(METramite tramite, METramiteDocumento[] documentos, MEDatosEspecificos[] datosOrigen, MEDatosEspecificos[] datosDestino)
        {
            tramite._Ip = Request.UserHostAddress;
            tramite._Id_Usuario = Usuario.Id_Usuario;
            tramite._Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = documentos,
                DatosOrigen = datosOrigen,
                DatosDestino = datosDestino
            };

            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/reservas/solicitud", tramiteParameters))
            {
                if (resp.IsSuccessStatusCode)
                {
                    Session["Current_IdTramite"] = await resp.Content.ReadAsAsync<int>();
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    string[] mensajes;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            mensajes = new string[] { res.Message?.Value };
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            mensajes = ex.Errores.ToArray();
                            break;
                        default:
                            mensajes = new string[] { "Ha ocurrido un error la solicitar la reserva." };
                            break;
                    }
                    return Json(new { mensajes , error = true });
                }
            }
        }

        public async Task<ActionResult> ConfirmarReservas(METramite tramite, METramiteDocumento[] documentos, MEDatosEspecificos[] datosOrigen, MEDatosEspecificos[] datosDestino)
        {
            tramite._Ip = Request.UserHostAddress;
            tramite._Id_Usuario = Usuario.Id_Usuario;
            tramite._Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = documentos,
                DatosOrigen = datosOrigen,
                DatosDestino = datosDestino
            };

            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/reservas", tramiteParameters))
            {
                if (resp.IsSuccessStatusCode)
                {
                    Session["Current_IdTramite"] = await resp.Content.ReadAsAsync<int>();
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    string[] mensajes;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            mensajes = new string[] { res.Message?.Value };
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            mensajes = ex.Errores.ToArray();
                            break;
                        default:
                            mensajes = new string[] { "Ha ocurrido un error la solicitar la reserva." };
                            break;
                    }
                    return Json(new { mensajes , error = true });
                }
            }
        }
        public async Task<ActionResult> GenerarAntecedentes(METramite tramite, METramiteDocumento[] documentos, MEDatosEspecificos[] datosOrigen, MEDatosEspecificos[] datosDestino)
        {
            tramite._Ip = Request.UserHostAddress;
            tramite._Id_Usuario = Usuario.Id_Usuario;
            tramite._Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress);

            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = documentos,
                DatosOrigen = datosOrigen,
                DatosDestino = datosDestino,
            };

            using (var resp = await _cliente.PostAsJsonAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/antecedentes", tramiteParameters))
            {
                if (resp.IsSuccessStatusCode)
                {
                    Session["Current_IdTramite"] = await resp.Content.ReadAsAsync<int>();
                    return new HttpStatusCodeResult(HttpStatusCode.OK);
                }
                else
                {
                    ActionResult rta;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            rta = new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                            break;
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            rta = Json(new { mensajes = new[] { res.Message?.Value }, error = true });
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            Session["Current_IdTramite"] = ex.IdTramite;
                            rta = Json(new { mensajes = ex.Errores.ToList(), error = resp.StatusCode != HttpStatusCode.Conflict });
                            break;
                        default:
                            rta = Json(new { mensajes = new[] { resp.ReasonPhrase }, error = true });
                            break;
                    }
                    return rta;
                }
            }
        }

        [HttpPost]
        public ActionResult TramiteSaveInforme(int idTramite, METramiteDocumento informe)
        {
            var tramite = new METramite()
            {
                IdTramite = idTramite,

                _Ip = Request.UserHostAddress,
                _Id_Usuario = Usuario.Id_Usuario,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress),
            };
            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = new[] { informe },
            };

            using (var resp = _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/informe", tramiteParameters, new JsonMediaTypeFormatter()).Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    ActionResult rta;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            rta = Json(new { mensajes = new[] { res.Message?.Value }, error = true });
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            Session["Current_IdTramite"] = ex.IdTramite;
                            rta = Json(new { mensajes = ex.Errores.ToList(), error = resp.StatusCode != HttpStatusCode.Conflict });
                            break;
                        default:
                            rta = Json(new { mensajes = new[] { resp.ReasonPhrase }, error = true });
                            break;
                    }
                    return rta;
                }
                Session["Current_IdTramite"] = resp.Content.ReadAsAsync<int>().Result;
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        [HttpPost]
        public ActionResult TramiteUpdateInforme(int idTramite, bool firmado, METramiteDocumento informe)
        {
            var tramite = new METramite()
            {
                IdTramite = idTramite,

                _Ip = Request.UserHostAddress,
                _Id_Usuario = Usuario.Id_Usuario,
                _Machine_Name = AuditoriaHelper.ReverseLookup(Request.UserHostAddress),
            };
            var tramiteParameters = new METramiteParameters
            {
                Tramite = tramite,
                TramitesDocumentos = new[] { informe },
            };

            using (var resp = _cliente.PostAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/informe/version?final={firmado}", tramiteParameters, new JsonMediaTypeFormatter()).Result)
            {
                if (!resp.IsSuccessStatusCode)
                {
                    ActionResult rta;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                            dynamic res = JsonConvert.DeserializeObject(resp.Content.ReadAsStringAsync().Result);
                            rta = Json(new { mensajes = new[] { res.Message?.Value }, error = true });
                            break;
                        case HttpStatusCode.Conflict:
                        case HttpStatusCode.PreconditionFailed:
                        case HttpStatusCode.ExpectationFailed:
                            var ex = JsonConvert.DeserializeObject<ValidacionTramiteException>(resp.Content.ReadAsStringAsync().Result);
                            Session["Current_IdTramite"] = ex.IdTramite;
                            rta = Json(new { mensajes = ex.Errores.ToList(), error = resp.StatusCode != HttpStatusCode.Conflict });
                            break;
                        default:
                            rta = Json(new { mensajes = new[] { resp.ReasonPhrase }, error = true });
                            break;
                    }
                    return rta;
                }
                Session["Current_IdTramite"] = resp.Content.ReadAsAsync<int>().Result;
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }

        public List<ParametrosGenerales> GetParamtrosGenerales()
        {
            using (var resp = _cliente.GetAsync($"{MvcApplication.V2_API_PREFIX}/mesaentradas/tramites/parametrosgenerales").Result)
            {
                resp.EnsureSuccessStatusCode();
                return resp.Content.ReadAsAsync<List<ParametrosGenerales>>().Result;
            }
        }

        public string GetParamtrosGeneralesByClave(string clave)
        {
            var param = GetParamtrosGenerales().Where(p => p.Clave == clave).FirstOrDefault();
            return param?.Valor ?? string.Empty;
        }

        public FileResult DownloadArchivo() => getArchivoDescargable();

        private FileResult getArchivoDescargable()
        {
            Response.AppendHeader("Content-Disposition", new ContentDisposition { FileName = ArchivoDescarga.NombreArchivo, Inline = true }.ToString());
            return File(ArchivoDescarga.Contenido, ArchivoDescarga.MimeType);
        }
        private void MapSuperficiesValuacionToValSuperficies(IEnumerable<Propiedad> propiedades)
        {
            if (propiedades == null) return;

            foreach (var p in propiedades.Where(p => !string.IsNullOrEmpty(p.Value)))
            {
                var superficies = JsonConvert.DeserializeObject<SuperficieModel[]>(p.Value);
                var valsuperficies = superficies.Select(s => new VALSuperficie()
                {
                    Superficie = Convert.ToDouble(s.SuperficieHa),
                    IdAptitud = s.Aptitud.IdAptitud,
                    Caracteristicas = s.Caracteristicas.Select(c => new DDJJSorCar() { IdSorCar = c }).ToList(),
                    Puntaje = s.Puntaje,
                    TrazaDepreciable = s.TrazaDepreciable,
                });
                p.Value = JsonConvert.SerializeObject(valsuperficies);
            }
        }

        public ActionResult GetInformeHojaDeRuta(int id)
        {
            string usuario = $"{((UsuariosModel)Session["usuarioPortal"]).Nombre} {((UsuariosModel)Session["usuarioPortal"]).Apellido}";
            string url = $"InformesMesaEntradas/GetInformeHojaDeRuta?id={id}&usuario={usuario}";
            using (var apiReportes = new HttpClient { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesUrl"]) })
            using (var resp = apiReportes.GetAsync($"api/{url}").Result)
            {
                resp.EnsureSuccessStatusCode();
                string nombre = $"HojaDeRuta_Tramite_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string bytes64 = resp.Content.ReadAsAsync<string>().Result;
                byte[] bytes = Convert.FromBase64String(bytes64);
                ArchivoDescarga = new ArchivoDescarga(bytes, nombre, "application/pdf");
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
        }
    }
}
static class XTMethods
{
    internal static SelectList ToSelectList(this IEnumerable<METipoTramite> tipos, string defaultText, long selectedValue = 0)
    {
        tipos = tipos.OrderBy(t => t.Descripcion);
        if (selectedValue == 0)
        {
            tipos = new[] { new METipoTramite() { IdTipoTramite = 0, Descripcion = defaultText } }.Concat(tipos);
        }
        return new SelectList(tipos, "IdTipoTramite", "Descripcion", selectedValue);
    }
    internal static SelectList ToSelectList(this IEnumerable<MEObjetoTramite> objetos, string defaultText, long selectedValue = 0)
    {
        objetos = objetos.OrderBy(t => t.Descripcion);
        if (selectedValue == 0)
        {
            objetos = new[] { new MEObjetoTramite() { IdObjetoTramite = 0, Descripcion = defaultText } }.Concat(objetos);
        }
        return new SelectList(objetos, "IdObjetoTramite", "Descripcion", selectedValue);
    }
    internal static SelectList ToSelectList(this IEnumerable<MEEstadoTramite> estados, string defaultText, long selectedValue = 0)
    {
        estados = estados.OrderBy(t => t.Descripcion);
        if (selectedValue == 0)
        {
            estados = new[] { new MEEstadoTramite() { IdEstadoTramite = 0, Descripcion = defaultText } }.Concat(estados);
        }
        return new SelectList(estados, "IdEstadoTramite", "Descripcion", selectedValue);
    }
    internal static SelectList ToSelectList(this IEnumerable<MEPrioridadTramite> prioridades, string defaultText, long selectedValue = 0)
    {
        prioridades = prioridades.OrderBy(p=>p.Descripcion);
        if (selectedValue == 0)
        {
            prioridades = new[] { new MEPrioridadTramite() { IdPrioridadTramite = 0, Descripcion = defaultText } }.Concat(prioridades);
        }
        return new SelectList(prioridades, "IdPrioridadTramite", "Descripcion", selectedValue);
    }
    internal static SelectList ToSelectList(this IEnumerable<Sector> sectores, string defaultText)
    {
        sectores = new[] { new Sector() { IdSector = 0, Nombre = defaultText } }.Concat(sectores.OrderBy(t => t.Nombre));
        return new SelectList(sectores, "IdSector", "Nombre");
    }
    internal static SelectList ToSelectList(this IEnumerable<Usuarios> usuarios, string defaultText)
    {
        usuarios = new[] { new Usuarios() { Id_Usuario = 0, Nombre = defaultText } }.Concat(usuarios.OrderBy(t => t.NombreApellidoCompleto));
        return new SelectList(usuarios, "Id_Usuario", "NombreApellidoCompleto");
    }
    internal static SelectList ToSelectList(this IEnumerable<TipoParcela> tipos)
    {
        return new SelectList(tipos.OrderBy(t => t.Descripcion), "TipoParcelaID", "Descripcion");
    }
    internal static SelectList ToSelectList(this IEnumerable<ClaseParcela> clases, long selected)
    {
        return new SelectList(clases.OrderBy(t => t.Descripcion), "ClaseParcelaID", "Descripcion", selected);
    }
    internal static SelectList ToSelectList(this IEnumerable<EstadoParcela> estados, long selected)
    {
        return new SelectList(estados.OrderBy(t => t.Descripcion), "EstadoParcelaID", "Descripcion", selected);
    }
    internal static SelectList ToSelectList(this IEnumerable<TipoUnidadTributaria> tipos)
    {
        return new SelectList(tipos.OrderBy(t => t.Descripcion), "TipoUnidadTributariaID", "Descripcion");
    }
}