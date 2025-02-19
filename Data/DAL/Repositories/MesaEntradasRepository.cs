using EGIS.ShapeFileLib;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Documentos;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.BusinessEntities.ValidacionesDB;
using GeoSit.Data.BusinessEntities.ValidacionesDB.Enums;
using GeoSit.Data.BusinessEntities.ValidacionesDB.Implementaciones;
using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Common.ExtensionMethods.Atributos;
using GeoSit.Data.DAL.Common.ExtensionMethods.Data;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using GeoSit.Data.DAL.Models;
using GeoSit.Data.DAL.Valuaciones;
using GeoSit.Data.DAL.Valuaciones.Computations;
using GeoSit.Data.DAL.Valuaciones.Validators;
using GeoSit.Data.DAL.Valuaciones.Validators.Validations;
using Ionic.Zip;
using Newtonsoft.Json;
using SGTEntities;
using SGTEntities.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;
using static GeoSit.Data.BusinessEntities.Common.Enumerators;

namespace GeoSit.Data.DAL.Repositories
{
    public class MesaEntradasRepository : BaseRepository<METramite, int>, IMesaEntradasRepository
    {
        public enum Grilla { Propios, Sector, Catastro, DetalleTramite }

        private const string TIPO_ENTRADA_ANTECEDENTE = "A";
        private const string TIPO_ENTRADA_MENSURADA = "M";

        public MesaEntradasRepository(GeoSITMContext ctx)
            : base(ctx) { }

        public DataTableResult<GrillaTramite> RecuperarTramites(Grilla grilla, DataTableParameters parametros, long idUsuario)
        {
            var joins = new Expression<Func<METramite, dynamic>>[]
            {
                x => x.Prioridad, x => x.Profesional, x => x.UltimoOperador,
                x => x.Tipo, x => x.Objeto, x => x.Estado,
                x => x.Movimientos.Select(m => m.SectorOrigen),
                x => x.Movimientos.Select(m => m.SectorDestino)
            };
            var filtros = new List<Expression<Func<METramite, bool>>>();
            var sorts = new List<SortClause<METramite>>();

            var ctx = GetContexto();
            var ultimosMovimientos = from ultmov in ctx.MovimientosTramites
                                     where ultmov.IdMovimiento == (from mov in ctx.MovimientosTramites
                                                                   where mov.IdTramite == ultmov.IdTramite
                                                                   group mov.IdMovimiento by mov.IdTramite into grp
                                                                   select grp.Max(id => id)).FirstOrDefault()
                                     select ultmov;

            int idSectorUsuario = GetContexto().Usuarios.Find(idUsuario)?.IdSector ?? 0;
            int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
            bool esSectorProfesional = idSectorProfesional == idSectorUsuario;

            switch (grilla)
            {
                case Grilla.Propios:
                    {
                        if (esSectorProfesional)
                        {
                            // filtra los trámites que son del profesional:
                            // el usuario consultado pertenece al sector de profesionales y es el usuario de alta del trámite
                            filtros.Add(t => t.UsuarioAlta == idUsuario);
                        }
                        else
                        {
                            // filtra los que están siendo trabajados por el usuario:
                            // usuario de modificacion es el consultado y el último movimiento no lo saca del sector del usuario
                            filtros.Add(t => t.UsuarioModif == idUsuario
                                                    && ultimosMovimientos.Any(mov => mov.IdTramite == t.IdTramite && mov.IdSectorDestino == idSectorUsuario));
                        }

                        // hay que ver el estado del trámite y ver cuáles se excluyen
                        break;
                    }
                case Grilla.Sector:
                    {
                        // filtro los trámites que ve un usuario interno de Catastro y que actualmente están en su sector
                        filtros.Add(t => !esSectorProfesional
                                            && ultimosMovimientos.Any(mov => mov.IdTramite == t.IdTramite && mov.IdSectorDestino == idSectorUsuario));

                        //DUDA: ve los propios o se pueden filtrar?
                        break;
                    }
                case Grilla.Catastro:
                    {
                        // filtro los trámites que ve un usuario interno de Catastro. Solo veo los que hayan sido ingresados a Catastro
                        filtros.Add(t => !esSectorProfesional && t.Numero != null);
                        break;
                    }
            }

            foreach (var column in parametros.columns?.Where(c => !string.IsNullOrEmpty(c.search.value)) ?? new DataTableColumn[0])
            {
                switch (column.name)
                {
                    case "IdTramite":
                        {
                            int val = int.Parse(column.search.value);
                            if (val > 0)
                            {
                                filtros.Add(tramite => tramite.IdTramite == val);
                            }
                        }
                        break;
                    case "Numero":
                        {
                            string val = column.search.value.ToLower();
                            filtros.Add(tramite => tramite.Numero.ToLower().Contains(val));
                        }
                        break;
                    case "Profesional":
                        {
                            string[] valores = column.search.value.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            filtros.Add(tramite => valores.Contains(tramite.Profesional.Nombre.ToLower()) || valores.Contains(tramite.Profesional.Apellido));
                        }
                        break;
                    case "Asunto":
                        {
                            int val = int.Parse(column.search.value);
                            if (val > 0)
                            {
                                filtros.Add(tramite => tramite.IdTipoTramite == val);
                            }
                        }
                        break;
                    case "Causa":
                        {
                            int val = int.Parse(column.search.value);
                            if (val > 0)
                            {
                                filtros.Add(tramite => tramite.IdObjetoTramite == val);
                            }
                        }
                        break;
                    case "Estado":
                        {
                            int val = int.Parse(column.search.value);
                            if (val > 0)
                            {
                                filtros.Add(tramite => tramite.IdEstado == val);
                            }
                        }
                        break;
                    case "SectorActual":
                        { //esto aplica para grillas "Profesionales", "Catastro"
                            int val = int.Parse(column.search.value);

                            if (val > 0)
                            {
                                filtros.Add(tramite => ultimosMovimientos.Any(mov => mov.IdTramite == tramite.IdTramite && mov.IdSectorDestino == val));
                            }
                        }
                        break;
                    case "Prioridad":
                        {
                            int val = int.Parse(column.search.value);
                            if (val > 0)
                            {
                                filtros.Add(tramite => tramite.IdPrioridad == val);
                            }
                        }
                        break;
                    case "UsuarioSectorActual":
                        { //esto aplica para grilla "Mi Sector"
                            string val = column.search.value.ToLower();
                            filtros.Add(tramite => tramite.UltimoOperador.IdSector == idSectorUsuario 
                                                        && (tramite.UltimoOperador.Nombre.ToLower().Contains(val) 
                                                                    || tramite.UltimoOperador.Apellido.ToLower().Contains(val)));
                        }
                        break;
                    case "FechaUltimaActualizacion":
                        {
                            DateTime val = DateTime.Parse(column.search.value).Date;
                            filtros.Add(tramite => ultimosMovimientos.Any(mov => mov.IdTramite == tramite.IdTramite && DbFunctions.TruncateTime(mov.FechaAlta) == val));
                        }
                        break;
                    default:
                        break;
                }
            }
            bool asc = parametros.order?.FirstOrDefault()?.dir != "desc";
            if (parametros.columns?.Any() ?? false)
            {
                switch (parametros.columns[parametros.order.FirstOrDefault().column].name)
                {
                    case "Numero":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Numero, ASC = asc });
                        break;
                    case "Profesional":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => new { tramite.Profesional.Apellido, tramite.Profesional.Nombre }, ASC = asc });
                        break;
                    case "Asunto":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Tipo.Descripcion, ASC = asc });
                        break;
                    case "Causa":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Objeto.Descripcion, ASC = asc });
                        break;
                    case "Estado":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Estado.Descripcion, ASC = asc });
                        break;
                    case "Prioridad":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Prioridad.Descripcion, ASC = asc });
                        break;
                    case "FechaUltimaActualizacion":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Movimientos.Max(m => m.FechaAlta), ASC = asc });
                        break;
                    case "SectorActual":
                        sorts.Add(new SortClause<METramite>() { Expresion = tramite => tramite.Movimientos.Max(m => m.SectorDestino.Nombre), ASC = asc });
                        break;
                    default:
                        OrdenDefaultASC = asc;
                        break;
                }
            }
            return ObtenerPagina(GetBaseQuery(joins, filtros, sorts), parametros, mapParaGrilla);
        }

        public METramite GetTramite(int id, bool incluirEntradas)
        {
            try
            {
                var joins = new List<Expression<Func<METramite, dynamic>>>()
                {
                    x => x.Estado, x => x.Profesional, x => x.Prioridad, x => x.Tipo, x => x.Objeto, x => x.Iniciador,
                    x => x.TramiteDocumentos.Select(m => m.Documento.Tipo), x => x.TramiteDocumentos.Select(m => m.Usuario_Alta),
                    x => x.Movimientos.Select(m => m.TipoMovimiento), x => x.Movimientos.Select(m => m.Estado),
                    x => x.Movimientos.Select(m => m.SectorDestino), x => x.Movimientos.Select(m => m.Remito)
                };
                if (incluirEntradas)
                {
                    joins.AddRange(new Expression<Func<METramite, dynamic>>[]
                    {
                        x => x.TramiteEntradas.Select(e => e.TramiteEntradaRelaciones),
                        x => x.TramiteEntradas.Select(e => e.ObjetoEntrada),
                    });
                }
                var tramite = GetBaseQuery(joins, new Expression<Func<METramite, bool>>[] { t => t.IdTramite == id }).AsNoTracking().Single();

                DateTime.TryParse(GetContexto().ParametrosGenerales.SingleOrDefault(p => p.Clave == "FECHA_PROCESO_MIGRACION")?.Valor, out DateTime fechaMigracion);

                foreach (var movimiento in tramite.Movimientos)
                {
                    Expression<Func<Usuarios, bool>> filtroByIdUsuario = u => u.Id_Usuario == movimiento.UsuarioAlta;
                    if (movimiento.FechaAlta < fechaMigracion)
                    {
                        filtroByIdUsuario = u => u.IdISICAT == movimiento.UsuarioAlta;
                    }
                    movimiento.Usuario_Alta = GetContexto().Usuarios.Single(filtroByIdUsuario);
                }

                return tramite;
            }
            catch (Exception ex)
            {
                GetContexto().GetLogger().LogError($"GetTramiteById/{id}", ex);
                throw;
            }
        }

        public AccionesTramites RecuperarAccionesDisponibles(Grilla grilla, int[] tramites, long idUsuario)
        {
            var usuario = GetContexto().Usuarios.Find(idUsuario);
            var acciones = new AccionesTramites();
            if (usuario == null)
            {
                return acciones;
            }

            var funcionesPerfil = GetContexto().PerfilesFunciones;
            var perfilesUsuario = GetContexto().UsuariosPerfiles;
            var funciones = GetContexto().Funciones;

            var funcionesHabilitadas = (from perfilUsuario in perfilesUsuario
                                        where perfilUsuario.Id_Usuario == idUsuario && perfilUsuario.Fecha_Baja == null
                                        join funcionPerfil in funcionesPerfil on perfilUsuario.Id_Perfil equals funcionPerfil.Id_Perfil
                                        where funcionPerfil.Fecha_Baja == null
                                        join funcion in funciones on funcionPerfil.Id_Funcion equals funcion.Id_Funcion
                                        group funcion by funcion.Id_Funcion into grp
                                        select grp).SelectMany(x => x).ToList();

            if (!funcionesHabilitadas.Any())
            {
                return acciones;
            }

            int idSectorUsuario = usuario.IdSector ?? 0;
            int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
            bool esProfesional = idSectorProfesional == idSectorUsuario;

            var joins = new Expression<Func<METramite, dynamic>>[] { x => x.Movimientos, x => x.Movimientos.Select(m => m.Usuario_Alta) };
            var filtros = new Expression<Func<METramite, bool>>[] { x => tramites.Contains(x.IdTramite) };
            var seleccion = GetBaseQuery(joins, filtros).ToList();

            agregarAccionesGenerales(grilla, esProfesional, funcionesHabilitadas, acciones);
            agregarAccionesSeleccion(grilla, seleccion, usuario, esProfesional, funcionesHabilitadas, acciones);
            return acciones;
        }

        public AccionesTramites RecuperarAccionesTramite(int idTramite, long idTipoTramite, long idUsuario, bool soloLectura)
        {
            var usuario = GetContexto().Usuarios.Find(idUsuario);
            var acciones = new AccionesTramites();
            if (usuario == null)
            {
                return acciones;
            }

            var funcionesPerfil = GetContexto().PerfilesFunciones;
            var perfilesUsuario = GetContexto().UsuariosPerfiles;
            var funciones = GetContexto().Funciones;

            var funcionesHabilitadas = (from perfilUsuario in perfilesUsuario
                                        where perfilUsuario.Id_Usuario == idUsuario && perfilUsuario.Fecha_Baja == null
                                        join funcionPerfil in funcionesPerfil on perfilUsuario.Id_Perfil equals funcionPerfil.Id_Perfil
                                        where funcionPerfil.Fecha_Baja == null
                                        join funcion in funciones on funcionPerfil.Id_Funcion equals funcion.Id_Funcion
                                        group funcion by funcion.Id_Funcion into grp
                                        select grp)
                                        .SelectMany(x => x)
                                        .ToList();

            if (!funcionesHabilitadas.Any())
            {
                return acciones;
            }

            int idSectorUsuario = usuario.IdSector ?? 0;
            int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
            bool esProfesional = idSectorProfesional == idSectorUsuario;

            var joins = new Expression<Func<METramite, dynamic>>[] { x => x.Movimientos };
            var filtros = new Expression<Func<METramite, bool>>[] { x => x.IdTramite == idTramite };
            var tramite = GetBaseQuery(joins, filtros).FirstOrDefault();
            agregarAccionesTramite(tramite, idTipoTramite, usuario, esProfesional, funcionesHabilitadas, acciones, soloLectura);
            return acciones;
        }

        public bool ValidarDisponibilidadAccion(Grilla grilla, long accion, int[] tramites, long idUsuario)
        {
            var acciones = RecuperarAccionesDisponibles(grilla, tramites, idUsuario);

            return acciones.Generales.Concat(acciones.Seleccion).Any(a => a.IdAccion == accion);
        }

        public Usuarios GetOperadorTramite(int id)
        {
            var ctx = GetContexto();
            var query = from tramite in ctx.TramitesMesaEntrada
                        join usuario in ctx.Usuarios on tramite.UsuarioModif equals usuario.Id_Usuario
                        from ultmov in (from movimiento in ctx.MovimientosTramites
                                        where movimiento.IdTramite == id
                                        group movimiento by movimiento.IdTramite into grp
                                        select grp.OrderByDescending(x => x.FechaAlta).FirstOrDefault())
                        where tramite.IdTramite == id && ultmov.IdSectorOrigen == ultmov.IdSectorDestino && ultmov.IdSectorDestino == usuario.IdSector
                        select usuario;

            return query.SingleOrDefault();
        }

        public List<METipoTramite> GetTiposTramite()
        {
            return GetContexto().TiposTramites.Where(tt => tt.FechaBaja == null).ToList();
        }

        public List<MEObjetoTramite> GetObjetosTramiteByTipo(long idTipoTramite)
        {
            return GetContexto().ObjetosTramites.Where(ot => ot.FechaBaja == null && ot.IdTipoTramite == idTipoTramite).ToList();
        }

        public List<MEEstadoTramite> GetEstadosTramite()
        {
            return GetContexto().EstadosTramites.Where(et => et.FechaBaja == null).ToList();
        }

        public List<MEPrioridadTramite> GetPrioridadesTramite()
        {
            return GetContexto().PrioridadesTramites.Where(p => p.FechaBaja == null).ToList();
        }

        public VALValuacionTemporal ObtenerValuacion(int tramite, long ut)
        {
            var utTemporal = GetContexto()
                                    .UnidadesTributariasTemporal
                                    .Include(x => x.Parcela)
                                    .AsNoTracking()
                                    .SingleOrDefault(x => x.IdTramite == tramite && x.UnidadTributariaId == ut);

            var valuacion = GetContexto()
                                .ValuacionesTemporal
                                .Include(v => v.DeclaracionJurada)
                                .AsNoTracking()
                                .SingleOrDefault(v => v.IdTramite == tramite && v.IdUnidadTributaria == ut) ?? new VALValuacionTemporal();

            valuacion.UnidadTributaria = utTemporal;

            return valuacion;
        }

        public List<VALSuperficieTemporal> ObtenerSuperficiesValuacion(long idValuacion)
        {
            if(idValuacion <= 0)
            {
                return new List<VALSuperficieTemporal>();
            }
            return GetContexto()
                        .ValuacionesTemporal
                        .IncludeFilter(v => v.DeclaracionJurada)
                        .IncludeFilter(v => v.DeclaracionJurada.Sor)
                        .IncludeFilter(v => v.DeclaracionJurada.Sor.Superficies.Where(s=>s.FechaBaja == null))
                        .IncludeFilter(v => v.DeclaracionJurada.Sor.Superficies.Where(s=>s.FechaBaja == null).Select(s=>s.Aptitud))
                        .IncludeFilter(v => v.DeclaracionJurada.Sor.Superficies.Where(s=>s.FechaBaja == null).Select(s=>s.Caracteristicas))
                        .IncludeFilter(v => v.DeclaracionJurada.Sor.Superficies.Where(s=>s.FechaBaja == null).Select(s=>s.Caracteristicas.Select(c=>c.Caracteristica)))
                        .AsNoTracking()
                        .Single(v => v.DeclaracionJurada.IdDeclaracionJurada == idValuacion)
                        .DeclaracionJurada.Sor.Superficies.ToList();
        }

        public List<MEDatosEspecificos> GenerarDatoEspecificoOrigen(short tipo, long[] ids)
        {
            var objetos = new List<MEDatosEspecificos>();
            var mtes = ids.Select(id => new METramiteEntrada() { IdObjeto = id });
            switch (tipo)
            {
                case 1:
                    objetos.AddRange(mtes.Select(mte => GetDatoEspecificoParcelaByUTMadre(mte)));
                    break;
                case 2:
                    objetos.AddRange(mtes.Select(mte => GetDatoEspecificoUTById(mte)));
                    break;
                case 3:
                    objetos.AddRange(mtes.Select(mte => GetDatoEspecificoManzana(mte)));
                    break;
                case 4:
                    objetos.Add(GetDatoEspecificoMensura(mtes.Single()));
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return objetos;
        }

        public List<MEDatosEspecificos> GenerarDatosEspecificosDestino(GeneradorParcelas generador)
        {
            var parcelas = Enumerable.Range(1, generador.Cantidad)
                                     .Select(parcela =>
                                                {
                                                    long id = parcela + DateTime.Now.Ticks * -1;

                                                    return new ParcelaTemporal()
                                                    {
                                                        ParcelaID = id,
                                                        TipoParcelaID = generador.IdTipo,
                                                        ClaseParcelaID = generador.IdClase,
                                                        Superficie = 0,
                                                        UnidadesTributarias = new[]
                                                        {
                                                            new UnidadTributariaTemporal()
                                                            {
                                                                UnidadTributariaId = id,
                                                                ParcelaID = id,
                                                                TipoUnidadTributariaID = (int)TipoUnidadTributariaEnum.Comun,
                                                                CodigoProvincial = "",
                                                                PorcentajeCopropiedad = 100,
                                                                Superficie = 0
                                                            }
                                                        }
                                                    };
                                                });
            return parcelas.Select(parcela =>
                                   {
                                       var dato = GetDatoEspecificoDestino(parcela);
                                       dato.Propiedades.Find(p => p.Id == KeysDatosEspecificos.KeyIdParcela).Label = KeysDatosEspecificos.LabelNombre;
                                       dato.TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Parcela) };
                                       return dato;
                                   }).ToList();
        }

        public List<MEDatosEspecificos> GenerarDatosEspecificosDestino(GeneradorPartidas generador)
        {
            var uts = Enumerable.Range(1, generador.Cantidad)
                                     .Select(ut =>
                                                {
                                                    long id = ut + DateTime.Now.Ticks * -1;

                                                    return new UnidadTributariaTemporal()
                                                    {
                                                        UnidadTributariaId = id,

                                                        TipoUnidadTributariaID = generador.IdTipo,
                                                        CodigoProvincial = "",
                                                        PorcentajeCopropiedad = 0,
                                                        Superficie = 0,
                                                    };
                                                });
            return uts.Select(ut =>
                                {
                                    var dato = GetDatoEspecificoDestino(ut);
                                    return dato;
                                }).ToList();
        }

        public List<MEDatosEspecificos> GetDatosOrigenTramite(int tramite)
        {
            var ctx = GetContexto();
            var origenesByEntrada = (from tramiteEntrada in (from tramiteEntrada in ctx.TramitesEntradas
                                                             where tramiteEntrada.IdTramite == tramite
                                                                     && tramiteEntrada.FechaBaja == null
                                                                     && tramiteEntrada.TipoEntrada != TIPO_ENTRADA_MENSURADA
                                                             select tramiteEntrada)
                                     join objetoEntrada in ctx.ObjetosEntrada on tramiteEntrada.IdObjetoEntrada equals objetoEntrada.IdObjetoEntrada
                                     join entrada in ctx.Entradas on objetoEntrada.IdEntrada equals entrada.IdEntrada
                                     group tramiteEntrada by entrada into grp
                                     select new { Entradas = grp.Key, origenes = grp }).ToArray();

            using (var tokenSource = new CancellationTokenSource())
            {
                try
                {
                    var cancellationToken = tokenSource.Token;
                    cancellationToken.ThrowIfCancellationRequested();
                    var procesamientos = new Task<List<MEDatosEspecificos>>[origenesByEntrada.Length];
                    for (int idx = 0; idx < procesamientos.Length; idx++)
                    {
                        var grp = origenesByEntrada[idx];
                        procesamientos[idx] = ProcesarEntrada(grp.Entradas, grp.origenes.ToArray());
                    }
                    Task.WaitAll(procesamientos);
                    return procesamientos.SelectMany(proc => proc.Result).ToList();
                }
                catch (Exception ex)
                {
                    tokenSource.Cancel();
                    GetContexto().GetLogger().LogError($"GetDatosTramite({tramite})", ex);
                    throw;
                }
            }
        }

        public List<MEDatosEspecificos> GetDatosDestinoTramite(int tramite)
        {
            var ctx = GetContexto();
            var origenesByEntrada = (from tramiteEntrada in (from tramiteEntrada in ctx.TramitesEntradas
                                                             where tramiteEntrada.IdTramite == tramite
                                                                     && tramiteEntrada.FechaBaja == null
                                                                     && tramiteEntrada.TipoEntrada == TIPO_ENTRADA_MENSURADA
                                                             select tramiteEntrada)
                                     join objetoEntrada in ctx.ObjetosEntrada on tramiteEntrada.IdObjetoEntrada equals objetoEntrada.IdObjetoEntrada
                                     join entrada in ctx.Entradas on objetoEntrada.IdEntrada equals entrada.IdEntrada
                                     group tramiteEntrada by entrada into grp
                                     select new { Entradas = grp.Key, destinos = grp }).ToArray();

            using (var tokenSource = new CancellationTokenSource())
            {
                try
                {
                    var cancellationToken = tokenSource.Token;
                    cancellationToken.ThrowIfCancellationRequested();
                    var procesamientos = new Task<List<MEDatosEspecificos>>[origenesByEntrada.Length];
                    for (int idx = 0; idx < procesamientos.Length; idx++)
                    {
                        var grp = origenesByEntrada[idx];
                        procesamientos[idx] = ProcesarEntradaDestino(grp.Entradas, grp.destinos.ToArray());
                    }
                    Task.WaitAll(procesamientos);
                    return procesamientos.SelectMany(proc => proc.Result).ToList();
                }
                catch (Exception ex)
                {
                    tokenSource.Cancel();
                    GetContexto().GetLogger().LogError($"GetDatosTramite({tramite})", ex);
                    throw;
                }
            }
        }

        public void DerivarTramites(Derivacion derivacion)
        {
            int[] seleccion = derivacion.IdTramitesSeleccionados;

            var joins = new Expression<Func<METramite, dynamic>>[] { x => x.Movimientos };
            var filtros = new Expression<Func<METramite, bool>>[] { x => seleccion.Contains(x.IdTramite) };
            var tramites = GetBaseQuery(joins, filtros).ToList();

            var usuarioOperacion = GetContexto().Usuarios.Find(derivacion.IdUsuarioOperacion);
            var destino = GetContexto().Sectores.Find(derivacion.IdSector);

            var auditorias = new List<Auditoria>();
            var allErrores = new List<string>();
            var allDerivaciones = ResultadoValidacion.Ok;
            foreach (var tramite in tramites)
            {
                TryEjecucion(tramite, usuarioOperacion, null, null, usuarioOperacion.IdSector, tramite.IdEstado, EnumTipoMovimiento.Derivar,
                             destino.IdSector, derivacion.Observacion, Eventos.DerivarTramite, FuncionValidable.Derivar,
                             derivacion.Ip, derivacion.MachineName, allDerivaciones == ResultadoValidacion.Error, out ResultadoValidacion resultadoValidacion, out List<string> errores, out Auditoria auditoria);

                if (resultadoValidacion != ResultadoValidacion.Ok)
                {
                    allDerivaciones = ResultadoValidacion.Error;
                    allErrores.AddRange(errores);
                    continue;
                }

                auditorias.Add(auditoria);
            }
            if (allDerivaciones != ResultadoValidacion.Ok)
            {
                auditorias.Clear();
                throw new ValidacionException(allDerivaciones, allErrores);
            }
            GetContexto().SaveChanges(auditorias);
            InformarMovimientoSGT(tramites, destino);
        }
        public void AsignarTramites(Asignacion asignacion)
        {
            int[] seleccion = asignacion.IdTramitesAsignados;

            var usuarioOperacion = GetContexto().Usuarios.Find(asignacion.IdUsuarioOperacion);
            var usuarioAsignado = GetContexto().Usuarios.Find(asignacion.IdUsuarioAsignado);

            var joins = new Expression<Func<METramite, dynamic>>[] { x => x.Movimientos };
            var filtros = new Expression<Func<METramite, bool>>[] { x => x.UsuarioModif != usuarioAsignado.Id_Usuario && seleccion.Contains(x.IdTramite) };
            var tramites = GetBaseQuery(joins, filtros).ToList();

            var tipoMovimiento = asignacion.Reasigna ? EnumTipoMovimiento.Reasignar : EnumTipoMovimiento.Recibir;
            var evento = asignacion.Reasigna ? Eventos.ReasignarTramite : Eventos.RecibirTramite;

            var auditorias = new List<Auditoria>();
            foreach (var tramite in tramites)
            {
                TryEjecucion(tramite, usuarioOperacion, null, null, usuarioAsignado.IdSector, tramite.IdEstado, tipoMovimiento,
                             usuarioAsignado.IdSector.Value, asignacion.Observacion, evento, FuncionValidable.Ninguna,
                             asignacion.Ip, asignacion.MachineName, false, out _, out _, out Auditoria auditoria);

                tramite.UsuarioModif = usuarioAsignado.Id_Usuario;
                auditorias.Add(auditoria);
            }

            GetContexto().SaveChanges(auditorias);
        }
        public void ObservarTramite(Observacion observacion)
        {
            int idTramite = observacion.IdTramite;
            var joins = new Expression<Func<METramite, dynamic>>[] { x => x.Movimientos };
            var filtros = new Expression<Func<METramite, bool>>[] { x => idTramite == x.IdTramite };
            var tramite = GetBaseQuery(joins, filtros).Single();

            int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
            var usuarioOperacion = GetContexto().Usuarios.Find(observacion.IdUsuarioOperacion);

            TryEjecucion(tramite, usuarioOperacion, null, null, usuarioOperacion.IdSector, (int)EnumEstadoTramite.Observado, EnumTipoMovimiento.Observar,
                         idSectorProfesional, observacion.Motivo, Eventos.ObservarTramite, FuncionValidable.Ninguna,
                         observacion.Ip, observacion.MachineName, false, out _, out _, out Auditoria auditoria);

            GetContexto().SaveChanges(auditoria);
            InformarNovedadSGT(tramite, new GeneradorObservacionTramite(observacion));
        }
        public bool TieneAntecedentesGenerados(int id)
        {
            long tipoDocumentoAntecedentes = long.Parse(GetContexto().ParametrosGenerales.Single(a => a.Clave == "ID_TIPO_DOCUMENTO_ANTECEDENTE").Valor);
            var query = from tramiteDocumento in GetContexto().TramitesDocumentos
                        join documento in GetContexto().Documento on tramiteDocumento.id_documento equals documento.id_documento
                        where tramiteDocumento.IdTramite == id && documento.id_tipo_documento == tipoDocumentoAntecedentes
                        select 1;

            return query.Any();
        }
        public int GenerarAntecedentes(METramiteParameters parameters)
        {
            parameters.TipoMovimiento = (int)EnumTipoMovimiento.GenerarAntecedentes;

            var objetoValidable = new ObjetoValidable()
            {
                Funcion = FuncionValidable.GenerarAntecedentes,
                TipoObjeto = TipoObjetoValidable.Tramite,
                IdObjeto = parameters.Tramite.IdObjetoTramite,
                IdTramite = parameters.Tramite.IdTramite,
                Valor = parameters.Tramite._Id_Usuario.ToString()
            };

            var resultadoValidacion = new ValidacionDBRepository(this.GetContexto()).Validar(objetoValidable, out List<string> errores);
            if (resultadoValidacion != ResultadoValidacion.Ok)
            {
                GetContexto().GetLogger()
                             .LogError($"GenerarAntecedentes({objetoValidable.IdTramite})", 
                                        new Exception(string.Join(Environment.NewLine, errores)));
                throw new UnauthorizedAccessException();
            }
            /*
            if (parameters.Tramite.IdTramite > 0 && !GetContexto().TramitesMesaEntrada.Any(t => t.IdTramite == parameters.Tramite.IdTramite && t.UsuarioAlta == parameters.Tramite._Id_Usuario))
            {
                throw new UnauthorizedAccessException();
            }
            */

            var tramite = InternalSave(parameters);

            var fields = new DbfFieldDesc[5]
            {
                new DbfFieldDesc()
                {
                    FieldName = "tipo_par",
                    FieldType = DbfFieldType.Number,
                    FieldLength = 10,
                    DecimalCount = 0
                },
                new DbfFieldDesc()
                {
                    FieldName = "nomenc",
                    FieldType = DbfFieldType.Character,
                    FieldLength = 28
                },
                new DbfFieldDesc()
                {
                    FieldName = "partida",
                    FieldType = DbfFieldType.Character,
                    FieldLength = 10
                },
                new DbfFieldDesc()
                {
                    FieldName = "superficie",
                    FieldType = DbfFieldType.Number,
                    FieldLength = 10,
                    DecimalCount = 8
                },
                new DbfFieldDesc()
                {
                    FieldName = "umed_sup",
                    FieldType = DbfFieldType.Character,
                    FieldLength = 2
                },
            };
            using (var shxMS = new MemoryStream())
            using (var shpMS = new MemoryStream())
            using (var dbfMS = new MemoryStream())
            using (var prjMS = new MemoryStream())
            using (var writer = ShapeFileWriter.CreateWriter(shxMS, shpMS, dbfMS, prjMS, ShapeType.Polygon, fields, ConfigurationManager.AppSettings["DBSRIDDefinition"]))
            {
                using (var qbuilder = GetContexto().CreateSQLQueryBuilder())
                {
                    qbuilder.AddFunctionTable($"obtener_antecedentes({tramite.IdTramite})", null)
                            .AddFields(fields.Select(f => f.FieldName).ToArray())
                            .AddGeometryField(qbuilder.CreateGeometryFieldBuilder(new Atributo() { Campo = "geom" }, null).ToWKT(), "geom")
                            .ExecuteQuery(reader =>
                            {
                                string[] data = new string[fields.Length];
                                for (int i = 0; i < data.Length; i++)
                                {
                                    data[i] = reader.GetStringOrEmpty(reader.GetOrdinal(fields[i].FieldName));
                                }
                                var geom = reader.GetGeometryFromField(reader.GetOrdinal("geom"));
                                var points = Enumerable.Range(1, geom.PointCount.Value)
                                                       .Select(idx =>
                                                       {
                                                           var pto = geom.PointAt(idx);
                                                           return new PointD(pto.XCoordinate.Value, pto.YCoordinate.Value);
                                                       });
                                writer.AddRecord(points.ToArray(), points.Count(), data);
                            });
                }
                using (var zipFile = new ZipFile())
                using (var memstr = new MemoryStream())
                {
                    string partialfilename = $"Tramite_{tramite.IdTramite}";
                    string filename = $"{partialfilename}.zip";
                    writer.Close();
                    zipFile.AddEntry($"{partialfilename}.shx", shxMS.GetBuffer());
                    zipFile.AddEntry($"{partialfilename}.shp", shpMS.GetBuffer());
                    zipFile.AddEntry($"{partialfilename}.dbf", dbfMS.GetBuffer());
                    zipFile.AddEntry($"{partialfilename}.prj", prjMS.GetBuffer());
                    zipFile.Save(memstr);

                    long usuarioSistema = long.Parse(GetContexto().ParametrosGenerales.Single(a => a.Clave == "ID_USUARIO_SISTEMA").Valor);
                    long tipoDocumentoAntecedentes = long.Parse(GetContexto().ParametrosGenerales.Single(a => a.Clave == "ID_TIPO_DOCUMENTO_ANTECEDENTE").Valor);
                    var antecedentes = new METramiteDocumento()
                    {
                        IdTramite = tramite.IdTramite,
                        FechaAlta = tramite.FechaModif,
                        UsuarioAlta = usuarioSistema,
                        FechaModif = tramite.FechaModif,
                        UsuarioModif = usuarioSistema,
                        Documento = new Documento()
                        {
                            fecha = tramite.FechaModif,
                            id_tipo_documento = tipoDocumentoAntecedentes,
                            descripcion = $"Antecedentes Trámite {tramite.IdTramite}",
                            observaciones = $"Generado el {tramite.FechaModif:dd/MM/yyyy HH:mm}",
                            nombre_archivo = filename,
                            extension_archivo = Path.GetExtension(filename),
                            id_usu_alta = usuarioSistema,
                            fecha_alta_1 = tramite.FechaModif,
                            id_usu_modif = usuarioSistema,
                            fecha_modif = tramite.FechaModif
                        }
                    };

                    int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
                    var paramUploadFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS").FirstOrDefault();
                    string targetPath = Directory.CreateDirectory(Path.Combine(paramUploadFolder.Valor, tramite.IdTramite.ToString(), "documentos")).FullName;
                    antecedentes.Documento.ruta = Path.Combine(targetPath, filename);

                    using (var transaction = GetContexto().Database.BeginTransaction())
                    {
                        try
                        {
                            GetContexto().TramitesDocumentos.Add(antecedentes);
                            var movimiento = new MEMovimiento()
                            {
                                UsuarioAlta = tramite.UsuarioModif,
                                FechaAlta = DateTime.Now,
                                IdEstado = tramite.IdEstado,
                                IdSectorOrigen = idSectorProfesional,
                                IdSectorDestino = idSectorProfesional,
                                IdTipoMovimiento = (int)EnumTipoMovimiento.GenerarAntecedentes,
                                IdTramite = tramite.IdTramite,
                            };
                            GetContexto().MovimientosTramites.Add(movimiento);
                            GetContexto().SaveChanges();
                            var bytes = memstr.ToArray();
                            File.WriteAllBytes(antecedentes.Documento.ruta, bytes);
                            transaction.Commit();
                            return tramite.IdTramite;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            GetContexto().GetLogger().LogError($"Generación de Antecedentes ({tramite.IdTramite})", ex);
                            throw;
                        }
                    }
                }
            }
        }
        public int SolicitarReservas(METramiteParameters parameters)
        {
            parameters.TipoMovimiento = (int)EnumTipoMovimiento.Ingresar;
            if (parameters.Tramite.IdTramite > 0 && !GetContexto().TramitesMesaEntrada.Any(t => t.IdTramite == parameters.Tramite.IdTramite && t.UsuarioAlta == parameters.Tramite._Id_Usuario))
            {
                throw new UnauthorizedAccessException();
            }

            var tramite = InternalSave(parameters);
            tramite.Movimientos.Last().IdEstado = tramite.IdEstado = (int)EnumEstadoTramite.ReservasSolicitadas;
            GetContexto().SaveChanges();
            var observacion = new Observacion() { Motivo = "El profesional solicita que Catastro realice las reservas de nomenclaturas, partidas y plano." };
            InformarNovedadSGT(tramite, new GeneradorObservacionTramite(observacion));
            return tramite.IdTramite;
        }
        public int ConfirmarReservas(METramiteParameters parameters)
        {
            parameters.TipoMovimiento = (int)EnumTipoMovimiento.ConfirmarReservas;
            var errors = new List<string>();
            if (parameters.DatosDestino.Any(d => d.Propiedades.Any(p => (p.Id == KeysDatosEspecificos.KeyIdParcela || p.Id == KeysDatosEspecificos.KeyIdUnidadTributaria)
                                                                                    && string.IsNullOrEmpty(p.Text))))
            {
                errors.Add("Las reservas no están completas.");
            }

            if (errors.Any())
            {
                throw new ValidacionTramiteException(parameters.Tramite.IdTramite, ResultadoValidacion.Error, errors);
            }
            var tramite = InternalSave(parameters);
            var movimientos = tramite.Movimientos.OrderBy(x => x.FechaAlta);
            var primerMovimiento = movimientos.First();
            var ultimoMovimiento = movimientos.Last();
            var mov = new MEMovimiento()
            {
                IdTramite = tramite.IdTramite,
                IdTipoMovimiento = (int)EnumTipoMovimiento.ConfirmarReservas,
                IdEstado = (int)EnumEstadoTramite.ReservasConfirmadas,
                IdSectorDestino = primerMovimiento.IdSectorOrigen,
                IdSectorOrigen = ultimoMovimiento.IdSectorOrigen,
                FechaAlta = DateTime.Now.AddSeconds(1),
                UsuarioAlta = tramite.UsuarioModif,
            };
            tramite.Movimientos.Add(mov);
            tramite.IdEstado = mov.IdEstado;
            tramite.FechaModif = mov.FechaAlta;
            GetContexto().SaveChanges();
            var observacion = new Observacion() { Motivo = "Las reservas solicitadas han sido completadas.\nEl trámite vuelve al profesional." };
            InformarNovedadSGT(tramite, new GeneradorObservacionTramite(observacion));
            return tramite.IdTramite;
        }
        public void ExecuteAction(METramite[] tramites, long action)
        {
            if (!tramites?.Any() ?? false) return;
            var usuario = GetContexto().Usuarios.Find(tramites[0]._Id_Usuario);

            EnumTipoMovimiento tipoMovimiento = EnumTipoMovimiento.Editar;
            FuncionValidable funcion = FuncionValidable.Ninguna;
            string evento = Eventos.ModificarTramites;
            int? idEstado = null;
            Action<METramite> doMore = (_) => { return; };

            Action<METramite,Funciones> notify = (tramite, func) => InformarNovedadSGT(tramite, new GeneradorNovedadTramite(func.Nombre));

            if (action == long.Parse(FuncionesTramite.VisarDatosCatastrales))
            {
                evento = Eventos.VisarCatastro;
                tipoMovimiento = EnumTipoMovimiento.VisarCatastro;
                funcion = FuncionValidable.VisarCatastro;
            }
            else if (action == long.Parse(FuncionesTramite.VisarDatosDominiales))
            {
                evento = Eventos.VisarDominios;
                tipoMovimiento = EnumTipoMovimiento.VisarDiminios;
                funcion = FuncionValidable.VisarDominios;
            }
            else if (action == long.Parse(FuncionesTramite.VisarDatosTopograficos))
            {
                evento = Eventos.VisarTopografia;
                tipoMovimiento = EnumTipoMovimiento.VisarTopografia;
                funcion = FuncionValidable.VisarTopografia;
            }
            else if (action == long.Parse(FuncionesTramite.VisarDatosValuativos))
            {
                evento = Eventos.VisarValuaciones;
                tipoMovimiento = EnumTipoMovimiento.VisarValuaciones;
                funcion = FuncionValidable.VisarValuaciones;
            }
            else if (action == long.Parse(FuncionesTramite.AprobarTramite))
            {
                evento = Eventos.AprobarTramite;
                tipoMovimiento = EnumTipoMovimiento.AprobarGeneral;
                funcion = FuncionValidable.AprobarGeneral;
                idEstado = (int)EnumEstadoTramite.Aprobado;
            }
            else if (action == long.Parse(FuncionesTramite.Finalizar))
            {
                evento = Eventos.FinalizarTramite;
                tipoMovimiento = EnumTipoMovimiento.Finalizar;
                funcion = FuncionValidable.FinalizarTramite;
                idEstado = (int)EnumEstadoTramite.Finalizado;
                doMore = (tramite) =>
                {
                    tramite.FechaModif = tramite.FechaModif.AddSeconds(1);
                    GetContexto().MovimientosTramites.Add(new MEMovimiento
                    {
                        Tramite = tramite,
                        IdSectorDestino = tramite.Movimientos.First().IdSectorOrigen,
                        IdSectorOrigen = (int)usuario.IdSector,
                        IdTipoMovimiento = (int)EnumTipoMovimiento.Derivar,
                        IdEstado = tramite.IdEstado,
                        FechaAlta = tramite.FechaModif,
                        UsuarioAlta = usuario.Id_Usuario
                    });
                };
                notify = (tramite, _) =>
                {
                    InformarNovedadSGT(tramite, new GeneradorObservacionTramite(new Observacion() { Motivo = "La mensura ha sido procesada." }));
                    ArchivarTramiteSGT(tramite, new Observacion() { Motivo = "El trámite se ha terminado y devuelto al profesional." });
                };
            }

            var ids = tramites.Select(x => x.IdTramite).ToArray();

            var dbTramites = GetContexto().TramitesMesaEntrada
                .Include(x => x.Movimientos)
                .Where(x => ids.Contains(x.IdTramite));

            using (var transaction = GetContexto().Database.BeginTransaction())
            {
                try
                {
                    foreach (var tramite in dbTramites.ToArray())
                    {
                        TryEjecucion(tramite, usuario, null, null, null, idEstado.GetValueOrDefault(tramite.IdEstado), tipoMovimiento, usuario.IdSector.Value,
                                     string.Empty, evento, funcion, tramites[0]._Ip, tramites[0]._Machine_Name, false,
                                     out ResultadoValidacion result, out List<string> errores, out Auditoria auditoria);

                        if(result != ResultadoValidacion.Ok)
                        {
                            throw new ValidacionTramiteException(tramite.IdTramite, result, errores);
                        }
                        doMore(tramite);
                        GetContexto().SaveChanges(auditoria);
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            var dbFuncion = GetContexto().Funciones.Find(action);
            foreach (var tramite in dbTramites)
            {
                notify(tramite, dbFuncion);
            }
        }

        protected override DbSet<METramite> ObtenerDbSet()
        {
            return GetContexto().TramitesMesaEntrada;
        }

        protected override Expression<Func<METramite, object>> OrdenDefault()
        {
            return row => row.IdTramite;
        }

        private Task<List<MEDatosEspecificos>> ProcesarEntrada(MEEntrada entrada, METramiteEntrada[] objetos)
        {
            if (entrada.IdEntrada == Convert.ToInt32(Entradas.Parcela))
            {
                return Task.FromResult(objetos.Select(e => GetDatoEspecificoParcela(e)).ToList());
            }
            else if (entrada.IdEntrada == Convert.ToInt32(Entradas.UnidadTributaria))
            {
                return Task.FromResult(objetos.Select(e => GetDatoEspecificoUTById(e)).ToList());
            }
            else if (entrada.IdEntrada == Convert.ToInt32(Entradas.Mensura))
            {
                return Task.FromResult(objetos.Select(e => GetDatoEspecificoMensura(e)).ToList());
            }
            else if (entrada.IdEntrada == Convert.ToInt32(Entradas.Manzana))
            {
                return Task.FromResult(objetos.Select(e => GetDatoEspecificoManzana(e)).ToList());
            }
            else
            {
                throw new InvalidOperationException($"La entrada {entrada.IdEntrada} no es procesable.");
            }
        }

        private Task<List<MEDatosEspecificos>> ProcesarEntradaDestino(MEEntrada entrada, METramiteEntrada[] objetos)
        {
            if (entrada.IdEntrada == Convert.ToInt32(Entradas.Parcela))
            {
                return Task.FromResult(objetos.Select(mte => GetDatoEspecificoParcelaDestino(mte)).ToList());
            }
            else if (entrada.IdEntrada == Convert.ToInt32(Entradas.Mensura))
            {
                return Task.FromResult(objetos.Select(mte => GetDatoEspecificoMensuraDestino(mte)).ToList());
            }
            else
            {
                throw new InvalidOperationException($"La entrada {entrada.IdEntrada} no es procesable.");
            }
        }

        private GrillaTramite mapParaGrilla(METramite tramite)
        {
            var ultimoMov = tramite.Movimientos.Single(m => m.IdMovimiento == tramite.Movimientos.Max(f => f.IdMovimiento));
            bool tramiteAsignado = ultimoMov.IdSectorDestino == ultimoMov.IdSectorOrigen;
            return new GrillaTramite()
            {
                IdTramite = tramite.IdTramite,
                Numero = tramite.Numero,
                Profesional = tramite.Profesional?.NombreApellidoCompleto ?? string.Empty,
                Asunto = tramite.Tipo.Descripcion,
                Causa = tramite.Objeto.Descripcion,
                Estado = tramite.Estado.Descripcion,
                Prioridad = tramite.Prioridad.Descripcion,
                SectorActual = ultimoMov.SectorDestino.Nombre,
                UsuarioSectorActual = tramiteAsignado ? tramite.UltimoOperador.NombreApellidoCompleto : string.Empty,
                FechaUltimaActualizacion = ultimoMov.FechaAlta.ToString("dd/MM/yyyy HH:mm"),
            };
        }

        private void agregarAccionesGenerales(Grilla grilla, bool esProfesional, IEnumerable<Funciones> funciones, AccionesTramites acciones)
        {
            agregarAccionHabilitada(FuncionesTramite.Nuevo, grilla == Grilla.Propios && esProfesional, funciones, acciones.Generales);
        }

        private void agregarAccionesSeleccion(Grilla grilla, List<METramite> tramites, Usuarios usuario, bool esProfesional, IEnumerable<Funciones> funciones, AccionesTramites acciones)
        {
            var seleccion = tramites ?? new List<METramite>();
            var movimientosByTramite = seleccion.Select(s => s.Movimientos.OrderBy(m => m.FechaAlta).ThenBy(m => m.IdMovimiento));
            bool soloUnoSeleccionado = seleccion.Count() == 1;
            agregarAccionHabilitada(FuncionesTramite.Consultar, soloUnoSeleccionado, funciones, acciones.Seleccion);

            bool puedeEditar = soloUnoSeleccionado
                                    // se puede editar si el único trámite seleccionado lo tiene asignado el profesional 
                                    // o si es el usuario de interno de catastro que lo recepcionó, el tramite es de mensura
                                    // y además tiene permiso de edición
                                    && !movimientosByTramite.Single().Any(x => (EnumTipoMovimiento)x.IdTipoMovimiento == EnumTipoMovimiento.Finalizar)
                                    && movimientosByTramite.Single().Last().IdSectorDestino == usuario.IdSector
                                    && (esProfesional && tramites.Single().UsuarioAlta == usuario.Id_Usuario
                                            || (EnumTipoTramite)tramites.Single().IdTipoTramite == EnumTipoTramite.Mensuras
                                                && !esProfesional && tramites.Single().UsuarioModif == usuario.Id_Usuario);
            agregarAccionHabilitada(FuncionesTramite.Editar, puedeEditar, funciones, acciones.Seleccion);

            bool puedeRecibir = seleccion.Any() && grilla == Grilla.Sector && !esProfesional
                                    //solo puede recibir un tramite si el sector origen del ultimo movimiento no es el del usuario pero el sector destino, si.
                                    && movimientosByTramite.All(m => m.Last().IdSectorOrigen != usuario.IdSector && m.Last().IdSectorDestino == usuario.IdSector);
            agregarAccionHabilitada(FuncionesTramite.Recibir, puedeRecibir, funciones, acciones.Seleccion);

            bool puedeDerivar = seleccion.Any() && grilla == Grilla.Propios && !esProfesional
                                    //solo se permite derivar si el tramite está en mi sector (ultimo movimiento) y el usuario que está trabajando el trámite es el usuario operando.
                                    //esto evita que otra persona de mi sector pueda derivarlo.
                                    //tambien evita que el usuario pueda derivar un tramite multiples veces.
                                    && movimientosByTramite.All(m => m.Last().IdSectorDestino == usuario.IdSector && seleccion.Single(t => t.IdTramite == m.Last().IdTramite).UsuarioModif == usuario.Id_Usuario);
            agregarAccionHabilitada(FuncionesTramite.Derivar, puedeDerivar, funciones, acciones.Seleccion);

            bool puedeReasignar = seleccion.Any() && grilla != Grilla.Catastro && !esProfesional
                                    && movimientosByTramite.All(m => m.Last().IdSectorDestino == usuario.IdSector && m.Last().Usuario_Alta.IdSector == usuario.IdSector);
            agregarAccionHabilitada(FuncionesTramite.Reasignar, puedeReasignar, funciones, acciones.Seleccion);
        }

        private void agregarAccionesTramite(METramite tramite, long currentIdTipoTramite, Usuarios usuario, bool esProfesional, IEnumerable<Funciones> funciones, AccionesTramites acciones, bool soloLectura)
        {
            bool esNuevo = tramite == null,
                 esTramiteMensura = (EnumTipoTramite)currentIdTipoTramite == EnumTipoTramite.Mensuras,
                 esTramiteResto = (EnumTipoTramite)currentIdTipoTramite != EnumTipoTramite.Mensuras;

            var estadoTramite = (EnumEstadoTramite)(tramite?.IdEstado ?? 0);

            var movimientos = tramite?.Movimientos?.OrderBy(mv => mv.FechaAlta).ToList() ?? new List<MEMovimiento>();
            bool existeMovimiento(EnumTipoMovimiento tipo)
            {
                return !esNuevo && movimientos.Any(x => (EnumTipoMovimiento)x.IdTipoMovimiento == tipo);
            }

            agregarAccionHabilitada(FuncionesTramite.ImprimirHojaRuta, !esNuevo, funciones, acciones.Seleccion);

            var finalizado = existeMovimiento(EnumTipoMovimiento.Finalizar);
            bool puedeGrabar = !finalizado && (esNuevo
                                                || !soloLectura
                                                        && tramite.UsuarioAlta == usuario.Id_Usuario && esProfesional
                                                        && movimientos.Last().IdSectorDestino == usuario.IdSector);
            agregarAccionHabilitada(FuncionesTramite.Nuevo, puedeGrabar, funciones, acciones.Seleccion); // si puedo dar de alta, puedo grabar. solo aplica a profesional

            puedeGrabar = !finalizado && !soloLectura
                                      && esTramiteMensura && !esProfesional
                                      && tramite.UsuarioModif == usuario.Id_Usuario
                                      && movimientos.Last().IdSectorDestino == usuario.IdSector;
            agregarAccionHabilitada(FuncionesTramite.Editar, puedeGrabar, funciones, acciones.Seleccion); // si no es profesional, pero tiene el tramite y es mensura

            bool puedeIngresar = !finalizado && esTramiteResto && esProfesional
                                            && (esNuevo
                                                    || estadoTramite == EnumEstadoTramite.Provisorio
                                                           && tramite.UsuarioAlta == usuario.Id_Usuario);
            agregarAccionHabilitada(FuncionesTramite.Ingresar, puedeIngresar, funciones, acciones.Seleccion); // sólo puede confirmar / ingresar un trámite si no ha sido iniciado todavía

            bool puedeSolicitarReserva = !finalizado && esTramiteMensura && esProfesional
                                                    && (esNuevo
                                                            || (estadoTramite == EnumEstadoTramite.Provisorio
                                                                    || estadoTramite == EnumEstadoTramite.Observado)
                                                               && tramite.UsuarioAlta == usuario.Id_Usuario
                                                               && !existeMovimiento(EnumTipoMovimiento.ConfirmarReservas));
            agregarAccionHabilitada(FuncionesTramite.SolicitarReserva, puedeSolicitarReserva, funciones, acciones.Seleccion); // sólo puede confirmar / ingresar un trámite si no ha sido iniciado todavía

            bool puedeReingresar = !finalizado && !esNuevo
                                            && (estadoTramite == EnumEstadoTramite.Observado || estadoTramite == EnumEstadoTramite.ReservasConfirmadas)
                                            && tramite.UsuarioAlta == usuario.Id_Usuario;
            agregarAccionHabilitada(FuncionesTramite.Reingresar, puedeReingresar, funciones, acciones.Seleccion); // sólo puede confirmar / ingresar un trámite si no ha sido iniciado todavía

            bool puedeGrabarReservaNomenclaturas = !finalizado && !esProfesional
                                                        && esTramiteMensura
                                                        && !existeMovimiento(EnumTipoMovimiento.ConfirmarReservas)
                                                        && movimientos.Last().IdSectorDestino == usuario.IdSector
                                                        && tramite.UsuarioModif == usuario.Id_Usuario;
            agregarAccionHabilitada(FuncionesTramite.ReservarNomenclaturasPartidas, puedeGrabarReservaNomenclaturas, funciones, acciones.Seleccion);

            bool puedeOperarConTramite = !finalizado && !esNuevo && !esProfesional
                                                            && estadoTramite != EnumEstadoTramite.Provisorio
                                                            && movimientos.Last().IdSectorDestino == usuario.IdSector
                                                            && tramite.UsuarioModif == usuario.Id_Usuario;

            agregarAccionHabilitada(FuncionesTramite.Derivar, puedeOperarConTramite, funciones, acciones.Seleccion);
            agregarAccionHabilitada(FuncionesTramite.Observar, puedeOperarConTramite, funciones, acciones.Seleccion);

            bool existeReserva = existeMovimiento(EnumTipoMovimiento.ConfirmarReservas);
            bool puedeVisar = !finalizado && esTramiteMensura && puedeOperarConTramite && existeReserva;

            bool existeVisadoCatastro = existeMovimiento(EnumTipoMovimiento.VisarCatastro);
            agregarAccionHabilitada(FuncionesTramite.VisarDatosCatastrales, puedeVisar && !existeVisadoCatastro, funciones, acciones.Seleccion);

            bool existeVisadoDominios = existeMovimiento(EnumTipoMovimiento.VisarDiminios);
            agregarAccionHabilitada(FuncionesTramite.VisarDatosDominiales, puedeVisar && !existeVisadoDominios, funciones, acciones.Seleccion);

            bool existeVisadoTopografico = existeMovimiento(EnumTipoMovimiento.VisarTopografia);
            agregarAccionHabilitada(FuncionesTramite.VisarDatosTopograficos, puedeVisar && !existeVisadoTopografico, funciones, acciones.Seleccion);

            bool existeVisadoGrafico = existeMovimiento(EnumTipoMovimiento.VisarGraficos);
            bool existeVisadoValuaciones = existeMovimiento(EnumTipoMovimiento.VisarValuaciones);
            agregarAccionHabilitada(FuncionesTramite.VisarDatosValuativos, puedeVisar && existeVisadoGrafico && !existeVisadoValuaciones, funciones, acciones.Seleccion);

            bool existeAprobacion = existeMovimiento(EnumTipoMovimiento.AprobarGeneral);
            bool puedeAprobar = puedeVisar && !existeAprobacion 
                                    && existeVisadoValuaciones && existeVisadoGrafico 
                                    && existeVisadoDominios && existeVisadoCatastro 
                                    && existeVisadoTopografico;
            agregarAccionHabilitada(FuncionesTramite.AprobarTramite, puedeAprobar && !existeAprobacion, funciones, acciones.Seleccion);
            agregarAccionHabilitada(FuncionesTramite.Finalizar, existeAprobacion, funciones, acciones.Seleccion);

            bool existeInformeAnexado = existeMovimiento(EnumTipoMovimiento.AnexarInforme);
            bool existeInformeFirmado = existeMovimiento(EnumTipoMovimiento.AnexarInformeFirmado);
            bool puedeOperarInforme = puedeOperarConTramite && esTramiteResto;
            agregarAccionHabilitada(FuncionesTramite.AnexarInforme, puedeOperarInforme && !existeInformeAnexado, funciones, acciones.Seleccion);

            agregarAccionHabilitada(FuncionesTramite.ActualizarInforme, puedeOperarInforme && existeInformeAnexado, funciones, acciones.Seleccion);

            bool puedeFirmarInforme =  existeInformeAnexado && !existeInformeFirmado;
            agregarAccionHabilitada(FuncionesTramite.AnexarInformeFirmado, puedeOperarInforme && puedeFirmarInforme, funciones, acciones.Seleccion);

            bool puedeRecibir = !finalizado && !esNuevo && !esProfesional
                                                    && movimientos.Last().IdSectorOrigen != usuario.IdSector
                                                    && movimientos.Last().IdSectorDestino == usuario.IdSector;
            agregarAccionHabilitada(FuncionesTramite.Recibir, puedeRecibir, funciones, acciones.Seleccion);

            bool puedeReasignar = !finalizado && !esProfesional
                                    && movimientos.Last().IdSectorDestino == usuario.IdSector && movimientos.Last().IdSectorOrigen == usuario.IdSector;
            agregarAccionHabilitada(FuncionesTramite.Reasignar, puedeReasignar, funciones, acciones.Seleccion);
        }

        private void agregarAccionHabilitada(string funcion, bool accionHabilitada, IEnumerable<Funciones> funcionesHabilitadas, List<Accion> acciones)
        {
            if (!accionHabilitada) return;

            long id = Convert.ToInt64(funcion);
            var funcionHabilitada = funcionesHabilitadas.SingleOrDefault(f => f.Id_Funcion == id);
            if (funcionHabilitada != null)
            {
                acciones.Add(new Accion() { IdAccion = funcionHabilitada.Id_Funcion, Nombre = funcionHabilitada.Nombre });
            }
        }

        private MEDatosEspecificos GetDatoEspecificoMensura(METramiteEntrada mte)
        {
            var repo = new MensurasRepository(GetContexto());

            var mensura = repo.GetById(mte.IdObjeto.Value);

            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyOperacion,
                    Value = Operacion.Origen.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdMensura,
                    Text = mensura.Descripcion,
                    Value = mensura.IdMensura.ToString(),
                    Label = KeysDatosEspecificos.LabelNombre
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyTipoMensura,
                    Text = mensura.TipoMensura.Descripcion,
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyEstadoMensura,
                    Text = mensura.EstadoMensura.Descripcion,
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                IdTramiteEntrada = mte.IdTramiteEntrada,
                TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Mensura) },
                Propiedades = propiedades,
            };
        }

        private MEDatosEspecificos GetDatoEspecificoMensuraDestino(METramiteEntrada tramiteEntrada)
        {
            var mensura = GetContexto().MensurasTemporal.Find(tramiteEntrada.IdObjeto.Value, tramiteEntrada.IdTramite);

            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyOperacion,
                    Value = Operacion.Destino.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdMensura,
                    Text = mensura.Descripcion,
                    Value = mensura.IdMensura.ToString(),
                    Label = KeysDatosEspecificos.LabelNombre
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada,
                TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Mensura) },
                Propiedades = propiedades,
            };
        }

        private MEDatosEspecificos GetDatoEspecificoManzana(METramiteEntrada mte)
        {
            var manzana = GetContexto().Divisiones.Find(mte.IdObjeto.Value);

            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdManzana,
                    Text = manzana.Nomenclatura,
                    Value = manzana.FeatId.ToString(),
                    Label = KeysDatosEspecificos.LabelNombre
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                IdTramiteEntrada = mte.IdTramiteEntrada,
                TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Manzana) },
                Propiedades = propiedades,
            };

        }

        private MEDatosEspecificos GetDatoEspecificoParcela(METramiteEntrada tramiteEntrada)
        {
            var repoUT = new UnidadTributariaRepository(GetContexto());

            var utMadre = repoUT.GetUnidadTributariaPrincipalByIdParcela(tramiteEntrada.IdObjeto.Value, true);
            var datoEspecifico = GetDatoEspecificoUnidadTributaria(null, utMadre);
            datoEspecifico.IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada;
            datoEspecifico.Propiedades.Find(p => p.Id == KeysDatosEspecificos.KeyIdParcela).Label = KeysDatosEspecificos.LabelNombre;
            datoEspecifico.TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Parcela) };
            return datoEspecifico;
        }

        private MEDatosEspecificos GetDatoEspecificoParcelaDestino(METramiteEntrada tramiteEntrada)
        {
            var parcela = GetContexto()
                            .ParcelasTemporal
                            .Include(p => p.UnidadesTributarias)
                            .Single(p => p.IdTramite == tramiteEntrada.IdTramite && p.ParcelaID == tramiteEntrada.IdObjeto);
            var datoEspecifico = GetDatoEspecificoDestino(parcela);
            datoEspecifico.IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada;
            datoEspecifico.Propiedades.Find(p => p.Id == KeysDatosEspecificos.KeyIdParcela).Label = KeysDatosEspecificos.LabelNombre;
            datoEspecifico.TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Parcela) };
            return datoEspecifico;
        }

        private MEDatosEspecificos GetDatoEspecificoParcelaByUTMadre(METramiteEntrada mte)
        {
            var datoEspecifico = GetDatoEspecificoUnidadTributaria(mte.IdObjeto.Value);
            datoEspecifico.IdTramiteEntrada = mte.IdTramiteEntrada <= 0 ? (long?)null : mte.IdTramiteEntrada;
            datoEspecifico.Propiedades.Find(p => p.Id == KeysDatosEspecificos.KeyIdParcela).Label = KeysDatosEspecificos.LabelNombre;
            datoEspecifico.TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.Parcela) };
            return datoEspecifico;
        }

        private MEDatosEspecificos GetDatoEspecificoUTById(METramiteEntrada mte)
        {
            var datoEspecifico = GetDatoEspecificoUnidadTributaria(mte.IdObjeto.Value);
            datoEspecifico.IdTramiteEntrada = mte.IdTramiteEntrada <= 0 ? (long?)null : mte.IdTramiteEntrada;
            datoEspecifico.Propiedades.Find(p => p.Id == KeysDatosEspecificos.KeyIdUnidadTributaria).Label = KeysDatosEspecificos.LabelNombre;
            datoEspecifico.TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.UnidadTributaria) };
            return datoEspecifico;
        }

        private MEDatosEspecificos GetDatoEspecificoUnidadTributaria(long? idUT, UnidadTributaria utMadre = null)
        {
            var repoUT = new UnidadTributariaRepository(GetContexto());
            var ut = utMadre ?? repoUT.GetUnidadTributariaById(idUT.Value, true);
            var tiposInscripcion = new Dictionary<long, string>();
            var dominios = new List<string>();

            if (ut.Dominios.Any())
            {
                tiposInscripcion = new TipoInscripcionRepository(GetContexto())
                                            .GetTipoInscripciones()
                                            .ToDictionary(ti => ti.TipoInscripcionID, ti => ti.Descripcion);
            }
            foreach (var dominio in ut.Dominios.Where(d => !d.FechaBaja.HasValue).OrderBy(d => d.Fecha).ThenBy(d => d.FechaAlta))
            {
                dominios.Add($"{tiposInscripcion[dominio.TipoInscripcionID]} {dominio.Inscripcion}");
            }

            var tipoParcela = new TipoParcelaRepository(GetContexto()).GetTipoParcela(ut.Parcela.TipoParcelaID);
            var nomenclatura = new NomenclaturaRepository(GetContexto()).GetByIdParcela(ut.ParcelaID.Value);
            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyOperacion,
                    Value = Operacion.Origen.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdUnidadTributaria,
                    Text = ut.CodigoProvincial,
                    Value = ut.UnidadTributariaId.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdParcela,
                    Text = nomenclatura.FirstOrDefault()?.Nombre,
                    Value = ut.ParcelaID.Value.ToString()
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyTipoParcela,
                    Text = tipoParcela.Descripcion,
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyDominios,
                    Text = string.Join(", ", dominios)
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyVigenciaDesde,
                    Text = ut.FechaVigenciaDesde?.ToString("dd/MM/yyyy") ?? string.Empty
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyVigenciaHasta,
                    Text = ut.FechaVigenciaHasta?.ToString("dd/MM/yyyy") ?? string.Empty
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyUnidadFuncional,
                    Text = ut.UnidadFuncional
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                Propiedades = propiedades,
            };
        }

        private MEDatosEspecificos GetDatoEspecificoDestino(ParcelaTemporal parcela)
        {
            var tipoParcela = new TipoParcelaRepository(GetContexto()).GetTipoParcela(parcela.TipoParcelaID);
            var tipoUT = new TipoUnidadTributariaRepository(GetContexto()).GetTipoUnidadTributaria(parcela.UnidadesTributarias.Single().TipoUnidadTributariaID);
            var clase = new ClaseParcelaRepository(GetContexto()).GetClaseParcela(parcela.ClaseParcelaID);
            var nomenclatura = GetContexto().NomenclaturasTemporal.SingleOrDefault(x => x.ParcelaID == parcela.ParcelaID);
            bool esUrbana = parcela.TipoParcelaID == (long)TipoParcelaEnum.Urbana;
            decimal superficie = Math.Round(parcela.Superficie / (esUrbana ? 1m : 10_000m), (esUrbana ? 2 : 8));
            string umed_superficie = esUrbana ? "m2" : "ha";

            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyOperacion,
                    Value = Operacion.Destino.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdUnidadTributaria,
                    Text = parcela.UnidadesTributarias.Single().CodigoProvincial,
                    Value = parcela.UnidadesTributarias.Single().UnidadTributariaId.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdTipoUnidadTributaria,
                    Text = tipoUT.Abreviacion,
                    Value = tipoUT.TipoUnidadTributariaID.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdParcela,
                    Text = nomenclatura?.Nombre,
                    Value = parcela.ParcelaID.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeySuperficieParcela,
                    Text = umed_superficie,
                    Value = superficie.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeySuperficieUT,
                    Value = parcela.UnidadesTributarias.Single().Superficie.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyPorcentajeCopropiedad,
                    Value = parcela.UnidadesTributarias.Single().PorcentajeCopropiedad.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyTipoParcela,
                    Value = tipoParcela.TipoParcelaID.ToString(),
                    Text = tipoParcela.Descripcion,
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyClaseParcela,
                    Value = clase.ClaseParcelaID.ToString(),
                    Text = clase.Descripcion,
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyUnidadFuncional,
                    Text = parcela.UnidadesTributarias.Single().UnidadFuncional
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                Propiedades = propiedades,
            };
        }

        private MEDatosEspecificos GetDatoEspecificoDestino(UnidadTributariaTemporal ut)
        {
            var tipoUT = new TipoUnidadTributariaRepository(GetContexto()).GetTipoUnidadTributaria(ut.TipoUnidadTributariaID);
            var nomenclatura = GetContexto().NomenclaturasTemporal.SingleOrDefault(x => x.ParcelaID == ut.ParcelaID);

            var propiedades = new List<Propiedad>()
            {
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyOperacion,
                    Value = Operacion.Destino.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdUnidadTributaria,
                    Label = KeysDatosEspecificos.LabelNombre,
                    Text = ut.CodigoProvincial,
                    Value = ut.UnidadTributariaId.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyIdTipoUnidadTributaria,
                    Text = tipoUT.Abreviacion,
                    Value = tipoUT.TipoUnidadTributariaID.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeySuperficieUT,
                    Text = "m2",
                    Value = ut.Superficie.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyPorcentajeCopropiedad,
                    Value = ut.PorcentajeCopropiedad.ToString(),
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyUnidadFuncional,
                    Text = ut.UnidadFuncional
                },
                new Propiedad()
                {
                    Id = KeysDatosEspecificos.KeyValuacion,
                    Text = ut.UnidadFuncional
                },
            };

            return new MEDatosEspecificos()
            {
                Guid = Guid.NewGuid(),
                TipoObjeto = new TipoDatoEspecifico() { Id = Convert.ToInt32(Entradas.UnidadTributaria) },
                Propiedades = propiedades,
            };
        }

        public int TramiteSave(METramiteParameters tramiteParameters)
        {
            return InternalSave(tramiteParameters).IdTramite;
        }

        public METramite InternalSave(METramiteParameters tramiteParameters)
        {
            DateTime fechaActual = DateTime.Now;
            var tramiteActual = default(METramite);
            var usuario = GetContexto().Usuarios.Find(tramiteParameters.Tramite._Id_Usuario);
            var tramite = tramiteParameters.Tramite;

            Dictionary<string, List<Documento>> procesarDocumentos()
            {
                var archivos = new Dictionary<string, List<Documento>>();
                void evaluarArchivo(string operacion, METramiteDocumento tramiteDocumento)
                {
                    if (string.IsNullOrEmpty(tramiteDocumento.Documento.nombre_archivo))
                    {
                        return;
                    }
                    if (!archivos.TryGetValue(operacion, out List<Documento> valores))
                    {
                        valores = new List<Documento>();
                        archivos.Add(operacion, valores);
                    }
                    valores.Add(tramiteDocumento.Documento);
                }
                IEnumerable<METramiteDocumento> tramitesDocumentosNuevos = (tramiteParameters.TramitesDocumentos != null ? tramiteParameters.TramitesDocumentos.Where(d => d.id_documento <= 0) : new METramiteDocumento[0]);
                IEnumerable<METramiteDocumento> tramitesDocumentosExistentes = new METramiteDocumento[0];
                IEnumerable<METramiteDocumento> tramitesDocumentosEliminados = new METramiteDocumento[0];
                if (tramiteActual.TramiteDocumentos != null)
                {
                    if (tramiteParameters.TramitesDocumentos != null)
                    {
                        tramitesDocumentosExistentes = tramiteActual.TramiteDocumentos.Where(d => tramiteParameters.TramitesDocumentos.Any(p => p.id_documento == d.id_documento)).ToArray();
                    }
                    tramitesDocumentosEliminados = tramiteActual.TramiteDocumentos.Except(tramitesDocumentosExistentes);
                }
                foreach (var tramitesDocumentosEliminado in tramitesDocumentosEliminados)
                {
                    tramitesDocumentosEliminado.FechaBaja = fechaActual;
                    tramitesDocumentosEliminado.UsuarioBaja = usuario.Id_Usuario;
                    tramitesDocumentosEliminado.FechaModif = fechaActual;
                    tramitesDocumentosEliminado.UsuarioModif = usuario.Id_Usuario;
                    tramitesDocumentosEliminado.Documento.fecha_baja_1 = fechaActual;
                    tramitesDocumentosEliminado.Documento.id_usu_baja = usuario.Id_Usuario;
                    tramitesDocumentosEliminado.Documento.id_usu_modif = usuario.Id_Usuario;
                    tramitesDocumentosEliminado.Documento.fecha_modif = fechaActual;
                    evaluarArchivo("B", tramitesDocumentosEliminado);
                }
                if (tramitesDocumentosNuevos.Count() > 0 && tramiteActual.TramiteDocumentos == null)
                {
                    tramiteActual.TramiteDocumentos = new List<METramiteDocumento>();
                }
                foreach (var tramitesDocumentosNuevo in tramitesDocumentosNuevos)
                {
                    var obj = new METramiteDocumento()
                    {
                        FechaAlta = fechaActual,
                        UsuarioAlta = usuario.Id_Usuario,
                        FechaModif = fechaActual,
                        UsuarioModif = usuario.Id_Usuario,
                        FechaAprobacion = tramitesDocumentosNuevo.FechaAprobacion,
                        Documento = new Documento()
                        {
                            fecha = fechaActual,
                            id_tipo_documento = tramitesDocumentosNuevo.Documento.id_tipo_documento,
                            descripcion = tramitesDocumentosNuevo.Documento.descripcion,
                            observaciones = tramitesDocumentosNuevo.Documento.observaciones,
                            nombre_archivo = tramitesDocumentosNuevo.Documento.nombre_archivo,
                            extension_archivo = tramitesDocumentosNuevo.Documento.extension_archivo,
                            id_usu_alta = usuario.Id_Usuario,
                            fecha_alta_1 = fechaActual,
                            id_usu_modif = usuario.Id_Usuario,
                            fecha_modif = fechaActual
                        }
                    };

                    tramiteActual.TramiteDocumentos.Add(obj);
                    evaluarArchivo("E", obj);

                }
                foreach (var tramiteDocumentoExistente in tramitesDocumentosExistentes)
                {
                    var tramiteDocumentoUI = tramiteParameters.TramitesDocumentos.Single(td => td.id_documento == tramiteDocumentoExistente.id_documento);

                    tramiteDocumentoExistente.FechaModif = fechaActual;
                    tramiteDocumentoExistente.UsuarioModif = usuario.Id_Usuario;

                    var documento = tramiteDocumentoExistente.Documento;

                    tramiteDocumentoExistente.FechaAprobacion = tramiteDocumentoUI.FechaAprobacion;

                    if (string.Compare($"{documento.nombre_archivo}{documento.extension_archivo}", $"{tramiteDocumentoUI.Documento.nombre_archivo}{tramiteDocumentoUI.Documento.extension_archivo}", false) != 0)
                    {
                        evaluarArchivo("B", tramiteDocumentoExistente);
                    }

                    documento.fecha_modif = fechaActual;
                    documento.id_usu_modif = usuario.Id_Usuario;
                    documento.nombre_archivo = tramiteDocumentoUI.Documento.nombre_archivo;
                    documento.observaciones = tramiteDocumentoUI.Documento.observaciones;
                    documento.descripcion = tramiteDocumentoUI.Documento.descripcion;
                    documento.id_tipo_documento = tramiteDocumentoUI.Documento.id_tipo_documento;
                    documento.extension_archivo = tramiteDocumentoUI.Documento.extension_archivo;

                    evaluarArchivo("E", tramiteDocumentoExistente);
                }

                return archivos;
            }
            void procesarArchivos()
            {
                var archivos = procesarDocumentos();
                string getDocumentoFilename(Documento doc) => $"{doc.nombre_archivo}";

                var paramUploadTempFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS_TEMPORAL").FirstOrDefault();
                string sourcePath = Path.Combine(paramUploadTempFolder.Valor, tramite.IdTramite.ToString(), "temporales");

                var paramUploadFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS").FirstOrDefault();
                string targetPath = Directory.CreateDirectory(Path.Combine(paramUploadFolder.Valor, tramiteActual.IdTramite.ToString(), "documentos")).FullName;
                if (archivos.TryGetValue("B", out List<Documento> elementos))
                {
                    foreach (var archivo in elementos)
                    {
                        string origen = Path.Combine(string.IsNullOrEmpty(archivo.ruta) ? targetPath : archivo.ruta, getDocumentoFilename(archivo));
                        if (File.Exists(origen))
                        {
                            File.Delete(origen);
                        }
                    }
                }
                if (archivos.TryGetValue("E", out elementos))
                {
                    foreach (var archivo in elementos)
                    {
                        string origen = Path.Combine(sourcePath, getDocumentoFilename(archivo));
                        if (File.Exists(origen))
                        {
                            archivo.ruta = Path.Combine(targetPath, Path.GetFileName(origen));
                            File.Move(origen, archivo.ruta);
                        }
                    }
                }
                if (tramite.IdTramite != tramiteActual.IdTramite)
                {
                    sourcePath = Directory.GetParent(sourcePath).FullName;
                }
                if (Directory.Exists(sourcePath))
                {
                    Directory.Delete(sourcePath, true);
                }
            }

            string tipoOperacion = TiposOperacion.Modificacion, observacion = string.Empty;

            bool nuevo = tramite.IdTramite <= 0;

            using (Stream lockFile = adquirirLockSecuenciaTramite(nuevo))
            using (var dbTrans = GetContexto().Database.BeginTransaction())
            {
                try
                {
                    int idTipoMovimiento = (int)(nuevo ? EnumTipoMovimiento.Crear : EnumTipoMovimiento.Editar);
                    int idSectorDestino = usuario.IdSector.GetValueOrDefault();
                    long idUsuarioModif = usuario.Id_Usuario;
                    if (nuevo)
                    {
                        tramiteActual = GetContexto().TramitesMesaEntrada.Add(new METramite
                        {
                            IdEstado = (int)EnumEstadoTramite.Provisorio,
                            FechaAlta = fechaActual,
                            UsuarioAlta = usuario.Id_Usuario,
                            FechaInicio = fechaActual,
                            Movimientos = new List<MEMovimiento>(),
                            TramiteDocumentos = new List<METramiteDocumento>(),
                            Desgloses = new List<MEDesglose>(),
                            TramiteEntradas = new List<METramiteEntrada>()
                        });
                        tipoOperacion = TiposOperacion.Alta;
                    }
                    else
                    {
                        tramiteActual = GetContexto()
                                            .TramitesMesaEntrada
                                            .Include("Profesional")
                                            .Include("Movimientos")
                                            .Include("TramiteDocumentos")
                                            .Include("TramiteDocumentos.Documento")
                                            .Single(t => t.IdTramite == tramite.IdTramite);

                        //tramiteActual.IdEstado = tramite.IdEstado;
                        tramiteActual.TramiteEntradas = new List<METramiteEntrada>();

                        GetContexto().Entry(tramiteActual).Property(x => x.UsuarioAlta).IsModified = false;
                        GetContexto().Entry(tramiteActual).Property(x => x.FechaAlta).IsModified = false;

                        GetContexto().Entry(tramiteActual).Property(x => x.FechaInicio).IsModified = false;
                        GetContexto().Entry(tramiteActual).Property(x => x.FechaIngreso).IsModified = false;
                        GetContexto().Entry(tramiteActual).Property(x => x.FechaLibro).IsModified = false;

                        idTipoMovimiento = (int)EnumTipoMovimiento.Editar;
                    }

                    var iniciador = GetContexto().Persona.Find(tramite.IdIniciador);

                    tramiteActual.IdIniciador = tramite.IdIniciador;
                    tramiteActual.IdPrioridad = tramite.IdPrioridad;
                    tramiteActual.IdTipoTramite = tramite.IdTipoTramite;
                    tramiteActual.IdObjetoTramite = tramite.IdObjetoTramite;
                    tramiteActual.Comprobante = tramite.Comprobante;
                    tramiteActual.Monto = tramite.Monto;
                    tramiteActual.Plano = tramite.Plano;

                    tramiteActual.FechaModif = fechaActual;
                    tramiteActual.UsuarioModif = usuario.Id_Usuario;

                    tramiteActual.Movimientos.Add(new MEMovimiento
                    {
                        IdSectorDestino = idSectorDestino,
                        IdSectorOrigen = usuario.IdSector.GetValueOrDefault(),
                        IdTipoMovimiento = idTipoMovimiento,
                        IdEstado = tramiteActual.IdEstado,
                        Observacion = observacion,
                        FechaAlta = tramiteActual.FechaModif,
                        UsuarioAlta = tramiteActual.UsuarioModif
                    });

                    GetContexto().SaveChanges(new Auditoria(tramiteActual.UsuarioModif,
                                                                    tramite.IdTramite <= 0 ? Eventos.AltadeTramites : Eventos.EditarTramite,
                                                                    tramite.IdTramite <= 0 ? Mensajes.AltadeTramitesOK : Mensajes.ModificarTramitesOK,
                                                                    tramite._Machine_Name, tramite._Ip, "S",
                                                                    tramite.IdTramite <= 0 ? null : GetContexto().Entry(tramiteActual).OriginalValues.ToObject(),
                                                                    GetContexto().Entry(tramiteActual).Entity, "Trámite", 1, tipoOperacion));

                    procesarArchivos();

                    #region Datos Especificos New
                    var entradasProcesadas = new List<long>();
                    // Lo muevo aca porque necesito asociar a las tablas temporales el id tramite
                    var lstEntradas = new List<RelacionDto>(); // Lista accesoria para chequear si el objeto esta creado o no
                                                               // Asigno a CrearTramiteEntradasTemporal, el proceso de carga temporal

                    var datosEspecificos = (tramiteParameters.DatosOrigen ?? new MEDatosEspecificos[0]).Concat(tramiteParameters.DatosDestino ?? new MEDatosEspecificos[0]);
                    CrearTramiteEntradasTemporal(lstEntradas, datosEspecificos.ToArray(), tramiteActual, usuario, entradasProcesadas);

                    GetContexto().SaveChanges();

                    var entradasFinales = (tramiteActual.TramiteEntradas ?? new List<METramiteEntrada>()).Select(te => te.IdTramiteEntrada).ToArray();
                    var tramiteRelacion = GetContexto().TramitesEntradasRelacion;
                    var entradasBorradas = (from tramiteEntrada in GetContexto().TramitesEntradas
                                            join entrada in GetContexto().ObjetosEntrada on tramiteEntrada.IdObjetoEntrada equals entrada.IdObjetoEntrada
                                            where tramiteEntrada.IdTramite == tramiteActual.IdTramite && !entradasFinales.Contains(tramiteEntrada.IdTramiteEntrada)
                                            select new
                                            {
                                                tramiteEntrada.IdTramiteEntrada,
                                                tramiteEntrada.IdComponente,
                                                idObjeto = tramiteEntrada.IdObjeto.Value,
                                                entrada.IdEntrada,
                                                tienePadre = tramiteRelacion.Any(x => x.IdTramiteEntrada == tramiteEntrada.IdTramiteEntrada)
                                            }).ToArray();

                    if (entradasBorradas.Any())
                    {
                        string entradas = string.Join(",", entradasBorradas.Select(e => e.IdTramiteEntrada));
                        using (var qb = GetContexto().CreateSQLQueryBuilder())
                        {
                            qb.AddTable("me_tramite_entrada_relacion", null)
                                .AddFilter("id_tramite_entrada", entradas, Common.Enums.SQLOperators.In)
                                .AddFilter("id_tramite_entrada_padre", entradas, Common.Enums.SQLOperators.In, Common.Enums.SQLConnectors.Or)
                                .ExecuteDelete();
                        }

                        using (var qb = GetContexto().CreateSQLQueryBuilder())
                        {
                            qb.AddTable("me_tramite_entrada", null)
                                .AddFilter("id_tramite_entrada", entradas, Common.Enums.SQLOperators.In)
                                .AddFilter("id_tramite", tramiteActual.IdTramite, Common.Enums.SQLOperators.EqualsTo, Common.Enums.SQLConnectors.And)
                                .ExecuteDelete();
                        }
                        //Preguntar que se hace comprobante de pago cuando se borra la entrada, si se borra de la tabla me_comprobante_pago o no
                        entradasBorradas = entradasBorradas.Where(e => !entradasProcesadas.Contains(e.IdTramiteEntrada)).ToArray();
                        //entradasBorradas = entradasBorradas.Where(e => e.IdEntrada != int.Parse(Entradas.Persona) && !entradasProcesadas.Contains(e.IdTramiteEntrada)).ToArray();

                        foreach (var grupo in entradasBorradas.GroupBy(e => new { e.IdComponente, e.IdEntrada }))
                        {
                            string idsObjetos = string.Join(",", grupo.Select(e => e.idObjeto));
                            long idComponente = grupo.Key.IdComponente;
                            var componente = GetContexto().Componente.Include(c => c.Atributos).SingleOrDefault(c => c.ComponenteId == idComponente);
                            Atributo campoClave = null;
                            try
                            {
                                campoClave = componente.Atributos.GetAtributoClave();
                            }
                            catch (ApplicationException ex)
                            {
                                GetContexto().GetLogger().LogError($"Componente (id: {componente.ComponenteId}) mal configurado.", ex);
                                throw;
                            }
                            if (grupo.Key.IdEntrada == Convert.ToInt32(Entradas.Parcela))
                            {
                                long idComponenteUt = long.Parse(GetContexto().ParametrosGenerales.SingleOrDefault(p => p.Clave == "ID_COMPONENTE_UNIDAD_TRIBUTARIA").Valor);
                                string tablaUt = GetContexto().Componente.Find(idComponenteUt).TablaTemporal;

                                foreach (var parcela in grupo)
                                {
                                    LimpiarRelacionesParcela(tramiteActual, parcela.idObjeto, campoClave, new[] { componente.TablaTemporal, componente.TablaGrafica, tablaUt, "inm_nomenclatura" });
                                }

                            }
                            if (grupo.Key.IdEntrada == Convert.ToInt32(Entradas.UnidadTributaria))
                            {
                                long idComponenteParcela = long.Parse(GetContexto().ParametrosGenerales.SingleOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA").Valor);
                                var campoClaveParcela = GetContexto().Atributo.Include(a => a.Componente).GetAtributoClaveByComponente(idComponenteParcela);
                                foreach (var ut in grupo)
                                {
                                    if (!ut.tienePadre)
                                    {
                                        var parcela = GetContexto().UnidadesTributariasTemporal.Find(ut.idObjeto, tramiteActual.IdTramite);
                                        LimpiarRelacionesParcela(tramiteActual, parcela.ParcelaID.Value, campoClaveParcela, new[] { campoClaveParcela.Componente.TablaTemporal, componente.TablaTemporal, campoClaveParcela.Componente.TablaGrafica });
                                    }
                                }
                            }
                            BorrarRegistrosTemporales(campoClave, componente.TablaTemporal ?? componente.Tabla, idsObjetos, Common.Enums.SQLOperators.In, tramiteActual.IdTramite);
                        }
                    }
                    #endregion

                    try
                    {
                        bool esIngreso = ((EnumTipoMovimiento)tramiteParameters.TipoMovimiento) == EnumTipoMovimiento.Ingresar;
                        bool esReingreso = esIngreso && tramiteActual.Movimientos.Any(m => (EnumTipoMovimiento)m.IdTipoMovimiento == EnumTipoMovimiento.Ingresar);
                        bool esConfirmacionReserva = ((EnumTipoMovimiento)tramiteParameters.TipoMovimiento) == EnumTipoMovimiento.ConfirmarReservas;
                        var obj = new ObjetoValidable()
                        {
                            IdTramite = tramiteActual.IdTramite,
                            TipoObjeto = TipoObjetoValidable.Tramite,
                            IdObjeto = tramiteActual.IdObjetoTramite
                        };
                        var valRepo = new ValidacionDBRepository(GetContexto());
                        List<string> erroresValidacion;
                        ResultadoValidacion resultado;
                        if (esIngreso)
                        {
                            obj.Grupo = GrupoValidable.ConfirmarTramite;
                            resultado = valRepo.ValidarFuncionGrupo(obj, out erroresValidacion);
                        }
                        else if(esConfirmacionReserva)
                        {
                            obj.Funcion = FuncionValidable.ConfirmarReservas;
                            resultado = valRepo.Validar(obj, out erroresValidacion);
                        }
                        else
                        {
                            obj.Funcion = FuncionValidable.GuardarTramite;
                            resultado = valRepo.Validar(obj, out erroresValidacion);
                        }
                        if (resultado == ResultadoValidacion.Advertencia || resultado == ResultadoValidacion.Ok)
                        {
                            if (esIngreso)
                            {
                                try
                                {
                                    int idSectorGestionInterna = int.Parse(GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_SECTOR_ME_GENERAL")?.Valor);
                                    var sectorGestionInterna = GetContexto().Sectores.Find(idSectorGestionInterna);

                                    InsertTramiteResponse tramiteIngresado = null;
                                    if (!esReingreso)
                                    {
                                        tramiteActual.FechaIngreso = fechaActual;

                                        GetContexto().Entry(tramiteActual).Reference(t => t.Objeto)
                                                                          .Query()
                                                                          .Include(x => x.TipoTramite).Load();
                                        GetContexto().Entry(tramiteActual).Reference(t => t.Prioridad).Load();
                                        var tramiteSGT = Tramite.Create(tramiteActual, usuario, sectorGestionInterna, new GeneradorResumenTramite(tramiteParameters.DatosOrigen));
                                        using (var httpClient = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["apiSGTUrl"]) })
                                        using (var resp = httpClient.PostAsJsonAsync("api/sgt/expedientes/ingreso", tramiteSGT).Result)
                                        {
                                            if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.BadRequest)
                                            {
                                                resp.EnsureSuccessStatusCode();
                                            }
                                            tramiteIngresado = resp.Content.ReadAsAsync<InsertTramiteResponse>().Result;
                                            if (!tramiteIngresado.IsOkey)
                                            {
                                                throw new ExternalException(tramiteIngresado.mensaje);
                                            }
                                        }
                                    }

                                    // revisar la función validable
                                    TryEjecucion(tramiteActual, usuario, null, null, null,
                                                (int)EnumEstadoTramite.Iniciado,
                                                esReingreso ? EnumTipoMovimiento.Reingresar : EnumTipoMovimiento.Ingresar,
                                                idSectorGestionInterna, string.Empty,
                                                esReingreso ? Eventos.ReingresarTramite : Eventos.IngresarTramite,
                                                FuncionValidable.Ninguna, tramite._Ip, tramite._Machine_Name, false,
                                                out resultado, out _, out Auditoria auditoria);

                                    if (resultado == ResultadoValidacion.Ok && !esReingreso)
                                    {
                                        tramiteActual.Numero = tramiteIngresado.TramiteIdentificacion;
                                        tramiteActual.IdSGT = tramiteIngresado.TramiteId;
                                    }
                                    GetContexto().SaveChanges(auditoria);
                                    if (esReingreso)
                                    {
                                        InformarMovimientoSGT(new[] { tramiteActual }, sectorGestionInterna);
                                    }
                                }
                                catch(ExternalException ex)
                                {
                                    resultado = ResultadoValidacion.Advertencia;
                                    erroresValidacion.Add($"No se pudo ingresar el trámite: {ex.Message}");
                                    GetContexto().GetLogger().LogError("API SGT - Ingreso", ex);
                                }
                                catch (HttpRequestException ex)
                                {
                                    resultado = ResultadoValidacion.Advertencia;
                                    erroresValidacion.Add($"No se pudo ingresar el trámite ya que no pudo obtenerse un número de actuación");
                                    GetContexto().GetLogger().LogError("API SGT - Ingreso", ex);
                                }
                            }

                            dbTrans.Commit();
                        }
                        else
                        {
                            dbTrans.Rollback();
                        }
                        if (resultado != ResultadoValidacion.Ok)
                        {
                            throw new ValidacionTramiteException(tramiteActual.IdTramite, resultado, erroresValidacion);
                        }
                        return tramiteActual;
                    }
                    catch (ValidacionTramiteException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        dbTrans.Rollback();
                        GetContexto().GetLogger().LogError("InternalSave", ex);
                        throw;
                    }
                }
                finally
                {
                    lockFile.Close();
                }
            }
        }

        public int TramiteSaveInforme(METramiteParameters tramiteParameters)
        {
            var informe = tramiteParameters.TramitesDocumentos.SingleOrDefault()?.Documento;
            if (informe == null)
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "Debe cargar un documento para el informe del trámite." });
            }
            if (string.IsNullOrEmpty(informe.nombre_archivo))
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "El documento especificado no tiene un nombre de archivo válido." });
            }

            using (var transaction = GetContexto().Database.BeginTransaction())
            {
                DateTime now = DateTime.Now;
                var usuarioOperacion = GetContexto().Usuarios.Find(tramiteParameters.Tramite._Id_Usuario);
                var tramite = GetContexto().TramitesMesaEntrada
                                           .Include(x => x.Movimientos)
                                           .Single(x => x.IdTramite == tramiteParameters.Tramite.IdTramite);

                GetContexto().Entry(tramite).Property(x => x.UsuarioAlta).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaAlta).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaInicio).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaIngreso).IsModified = false;

                tramite.UsuarioModif = usuarioOperacion.Id_Usuario;
                tramite.FechaModif = now;

                var obj = new METramiteDocumento()
                {
                    FechaAlta = now,
                    UsuarioAlta = usuarioOperacion.Id_Usuario,
                    FechaModif = now,
                    UsuarioModif = usuarioOperacion.Id_Usuario,
                    Documento = new Documento()
                    {
                        fecha = now.Date,
                        id_tipo_documento = informe.id_tipo_documento,
                        descripcion = informe.descripcion,
                        observaciones = informe.observaciones,
                        nombre_archivo = informe.nombre_archivo,
                        extension_archivo = informe.extension_archivo,
                        id_usu_alta = usuarioOperacion.Id_Usuario,
                        fecha_alta_1 = now,
                        id_usu_modif = usuarioOperacion.Id_Usuario,
                        fecha_modif = now
                    }
                };

                tramite.TramiteDocumentos = new[] { obj };

                var auditorias = new List<Auditoria>();

                var objetoValidable = new ObjetoValidable()
                {
                    IdTramite = tramite.IdTramite,
                    Codigo = obj.Documento.id_tipo_documento.ToString(),
                    Funcion = FuncionValidable.AnexarInforme,
                    TipoObjeto = TipoObjetoValidable.Tramite,
                    IdObjeto = tramite.IdObjetoTramite,
                };

                TryEjecucion(tramite, usuarioOperacion, null, null, null, tramite.IdEstado, EnumTipoMovimiento.Validar, usuarioOperacion.IdSector.Value,
                            null, Eventos.EditarTramite, objetoValidable, tramiteParameters.Tramite._Ip, tramiteParameters.Tramite._Machine_Name, true,
                            out ResultadoValidacion resultado, out List<string> errores, out _);
                if(resultado != ResultadoValidacion.Ok)
                {
                    transaction.Rollback();
                    throw new ValidacionTramiteException(tramite.IdTramite, resultado, errores);
                }

                TryEjecucion(tramite, usuarioOperacion, null, null, null, tramite.IdEstado, EnumTipoMovimiento.AnexarInforme, usuarioOperacion.IdSector.Value,
                            null, Eventos.AnexarInforme, FuncionValidable.Ninguna, tramiteParameters.Tramite._Ip, tramiteParameters.Tramite._Machine_Name, false,
                            out _, out _, out Auditoria auditoria);
                auditorias.Add(auditoria);

                GetContexto().SaveChanges(auditorias);

                try
                {
                    var paramUploadTempFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS_TEMPORAL").FirstOrDefault();
                    string sourcePath = Path.Combine(paramUploadTempFolder.Valor, tramite.IdTramite.ToString(), "temporales");

                    var paramUploadFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS").FirstOrDefault();
                    string targetPath = Directory.CreateDirectory(Path.Combine(paramUploadFolder.Valor, tramite.IdTramite.ToString(), "documentos")).FullName;
                    string origen = Path.Combine(sourcePath, obj.Documento.nombre_archivo);
                    if (File.Exists(origen))
                    {
                        obj.Documento.ruta = Path.Combine(targetPath, Path.GetFileName(origen));
                        File.Move(origen, obj.Documento.ruta);
                    }
                    if(Directory.Exists(sourcePath))
                    {
                        Directory.Delete(sourcePath, true);
                    }
                }
                catch (Exception ex)
                {
                    GetContexto().GetLogger().LogError($"TramiteSaveInforme({tramite.IdTramite}, {informe.id_documento})", ex);
                    transaction.Rollback();
                    throw;
                }
                GetContexto().SaveChanges();
                transaction.Commit();
                var observacion = new Observacion() { Motivo = "Se ha generado y adjuntado el documento solicitado." };
                InformarNovedadSGT(tramite, new GeneradorObservacionTramite(observacion));
                return tramite.IdTramite;
            }
        }
        public int TramiteSaveVersionInforme(bool final, METramiteParameters tramiteParameters)
        {
            Func<METramiteParameters, int> save = TramiteReplaceInforme;
            if(final) save = TramiteSaveInformeFirmado;

            return save(tramiteParameters);
        }

        private int TramiteReplaceInforme(METramiteParameters tramiteParameters)
        {
            try
            {
                var usuarioOperacion = GetContexto().Usuarios.Find(tramiteParameters.Tramite._Id_Usuario);
                IEnumerable<Auditoria> auditar(METramite tramite)
                {
                    TryEjecucion(tramite, usuarioOperacion, null, null, null, tramite.IdEstado, EnumTipoMovimiento.ActualizarInforme, usuarioOperacion.IdSector.Value,
                                null, Eventos.ActualizarInforme, FuncionValidable.Ninguna, tramiteParameters.Tramite._Ip, tramiteParameters.Tramite._Machine_Name, false,
                                out _, out _, out Auditoria auditoria);
                    yield return auditoria;
                }
                var ret = InternalInformeUpdate(usuarioOperacion, tramiteParameters, $"Actualizado por {usuarioOperacion.NombreApellidoCompleto}", auditar);
                return ret.IdTramite;
            }
            catch (AccessViolationException)
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "Debe cargar el informe actualizado." });
            }
            catch (FieldAccessException)
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "El informe especificado no tiene un nombre de archivo válido." });
            }
            catch (Exception ex)
            {
                GetContexto().GetLogger().LogError($"TramiteReplaceInforme()", ex);
                throw;
            }
        }

        private int TramiteSaveInformeFirmado(METramiteParameters tramiteParameters)
        {
            try
            {
                var usuarioOperacion = GetContexto().Usuarios.Find(tramiteParameters.Tramite._Id_Usuario);
                IEnumerable<Auditoria> auditar(METramite tramite)
                {
                    TryEjecucion(tramite, usuarioOperacion, null, null, null, tramite.IdEstado, EnumTipoMovimiento.AnexarInformeFirmado, usuarioOperacion.IdSector.Value,
                                null, Eventos.AnexarInformeFirmado, FuncionValidable.Ninguna, tramiteParameters.Tramite._Ip, tramiteParameters.Tramite._Machine_Name, false,
                                out _, out _, out Auditoria auditoria);
                    yield return auditoria;

                    int idSectorProfesional = Convert.ToInt32(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "ID_SECTOR_EXTERNO").Valor);
                    TryEjecucion(tramite, usuarioOperacion, null, null, null, (int)EnumEstadoTramite.Finalizado, EnumTipoMovimiento.Finalizar, idSectorProfesional,
                                null, Eventos.FinalizarTramite, FuncionValidable.Ninguna, tramiteParameters.Tramite._Ip, tramiteParameters.Tramite._Machine_Name, false,
                                out _, out _, out auditoria);
                    yield return auditoria;
                }
                string observaciones = $"Firmado por {usuarioOperacion.NombreApellidoCompleto}";
                var ret = InternalInformeUpdate(usuarioOperacion, tramiteParameters, observaciones, auditar);

                InformarNovedadSGT(ret, new GeneradorObservacionTramite(new Observacion() { Motivo = observaciones }));
                ArchivarTramiteSGT(ret, new Observacion () { Motivo = "El trámite se ha terminado y será devuelto al profesional."});

                return ret.IdTramite;
            }
            catch (AccessViolationException) 
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "Debe cargar el informe firmado para finalizar el trámite." });
            }
            catch(FieldAccessException)
            {
                throw new ValidacionTramiteException(tramiteParameters.Tramite.IdTramite, ResultadoValidacion.Error, new[] { "El informe especificado no tiene un nombre de archivo válido." });
            }
            catch (Exception ex)
            {
                GetContexto().GetLogger().LogError($"TramiteSaveInformeFirmado()", ex);
                throw;
            }
        }

        private METramite InternalInformeUpdate(Usuarios usuarioOperacion, METramiteParameters tramiteParameters, string observaciones, Func<METramite, IEnumerable<Auditoria>> auditar)
        {
            var informe = (tramiteParameters.TramitesDocumentos.SingleOrDefault()?.Documento) ?? throw new AccessViolationException();

            if (string.IsNullOrEmpty(informe.nombre_archivo))
            {
                throw new FieldAccessException();
            }

            using (var transaction = GetContexto().Database.BeginTransaction())
            {
                DateTime now = DateTime.Now;
                var tramite = GetContexto().TramitesMesaEntrada
                                           .Include(x => x.Movimientos)
                                           .Include(x => x.TramiteDocumentos.Select(td => td.Documento))
                                           .Single(x => x.IdTramite == tramiteParameters.Tramite.IdTramite);

                GetContexto().Entry(tramite).Property(x => x.UsuarioAlta).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaAlta).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaInicio).IsModified = false;
                GetContexto().Entry(tramite).Property(x => x.FechaIngreso).IsModified = false;

                tramite.UsuarioModif = usuarioOperacion.Id_Usuario;
                tramite.FechaModif = now;

                var obj = tramite.TramiteDocumentos.Single(x => x.UsuarioModif != tramite.UsuarioAlta);

                string archivoSinFirma = obj.Documento.ruta;

                obj.Documento.nombre_archivo = informe.nombre_archivo;
                obj.Documento.extension_archivo = informe.extension_archivo;
                obj.Documento.observaciones += $"{Environment.NewLine}{Environment.NewLine}{observaciones}";
                if (!string.IsNullOrEmpty((informe.observaciones ?? string.Empty).Trim()))
                {
                    obj.Documento.observaciones += $"{Environment.NewLine}{Environment.NewLine}{informe.observaciones}";
                }
                obj.Documento.fecha_modif = obj.FechaModif = now;
                obj.Documento.id_usu_modif = obj.UsuarioModif = usuarioOperacion.Id_Usuario;

                var auditorias = new List<Auditoria>();
                foreach(var auditoria in auditar(tramite))
                {
                    auditorias.Add(auditoria);
                }
                GetContexto().SaveChanges(auditorias);

                try
                {
                    if (File.Exists(archivoSinFirma))
                    {
                        File.Delete(archivoSinFirma);
                    }

                    var paramUploadTempFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS_TEMPORAL").FirstOrDefault();
                    string sourcePath = Path.Combine(paramUploadTempFolder.Valor, tramite.IdTramite.ToString(), "temporales");

                    var paramUploadFolder = GetContexto().ParametrosGenerales.ToList().Where(x => x.Clave == "RUTA_DOCUMENTOS").FirstOrDefault();
                    string targetPath = Directory.CreateDirectory(Path.Combine(paramUploadFolder.Valor, tramite.IdTramite.ToString(), "documentos")).FullName;
                    string origen = Path.Combine(sourcePath, obj.Documento.nombre_archivo);
                    if (File.Exists(origen))
                    {
                        obj.Documento.ruta = Path.Combine(targetPath, Path.GetFileName(origen));
                        File.Move(origen, obj.Documento.ruta);
                    }
                    if(Directory.Exists(sourcePath))
                    {
                        Directory.Delete(sourcePath, true);
                    }
                }
                catch (Exception ex)
                {
                    GetContexto().GetLogger().LogError($"InternalInformeUpdate({tramite.IdTramite}, {informe.id_documento})", ex);
                    transaction.Rollback();
                    throw;
                }
                GetContexto().SaveChanges();
                transaction.Commit();
                return tramite;
            }
        }

        private bool TryEjecucion(METramite tramite, Usuarios usuario, int? anteUltimoEstado, int? sectorAnterior,
            int? sectorOrigenDerivacion, int idEstado, EnumTipoMovimiento tipoMovimiento, int idSectorDestino,
            string observacion, string evento, FuncionValidable funcionValidable, string ip, string machineName,
            bool soloValidar, out ResultadoValidacion resultadoValidacion, out List<string> errores, out Auditoria auditoria)
        {
            var objetoValidable = new ObjetoValidable()
            {
                IdTramite = tramite.IdTramite,
                TipoObjeto = TipoObjetoValidable.Tramite,
                Funcion = funcionValidable,
                IdObjeto = tramite.IdObjetoTramite,
            };
            return TryEjecucion(tramite, usuario, anteUltimoEstado, sectorAnterior, sectorOrigenDerivacion, idEstado, tipoMovimiento,
                                idSectorDestino, observacion, evento, objetoValidable, ip, machineName, soloValidar,
                                out resultadoValidacion, out errores, out auditoria);
        }
        private bool TryEjecucion(METramite tramite, Usuarios usuario, int? anteUltimoEstado, int? sectorAnterior,
            int? sectorOrigenDerivacion, int idEstado, EnumTipoMovimiento tipoMovimiento, int idSectorDestino,
            string observacion, string evento, ObjetoValidable objetoValidable, string ip, string machineName,
            bool soloValidar, out ResultadoValidacion resultadoValidacion, out List<string> errores, out Auditoria auditoria)
        {
            auditoria = null;
            DateTime fechaActual = DateTime.Now;
            resultadoValidacion = ResultadoValidacion.Ok;
            var idUsuarioModif = usuario.Id_Usuario;
            bool? procesadoOk = null;
            try
            {
                resultadoValidacion = new ValidacionDBRepository(this.GetContexto()).Validar(objetoValidable, out errores);
                if (soloValidar || resultadoValidacion == ResultadoValidacion.Bloqueo || resultadoValidacion == ResultadoValidacion.Error)
                { //si hay errores de validacion (error o bloqueo) previos o actuales, solo evalúo las validaciones. ignoro lo demas
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }

            short cantDias = this.GetContexto().PrioridadesTramites.Find(tramite.IdPrioridad).CantDias;
            bool esProfesional = int.TryParse(this.GetContexto().ParametrosGenerales.Where(p => p.Clave == "ID_SECTOR_EXTERNO").FirstOrDefault()?.Valor, out int idSectorProfesional) &&
                                    usuario.IdSector == idSectorProfesional;

            if (tipoMovimiento == EnumTipoMovimiento.Derivar && new[] { EnumEstadoTramite.Finalizado, EnumEstadoTramite.Anulado }.Contains((EnumEstadoTramite)tramite.IdEstado))
            {
                idEstado = tramite.IdEstado;
            }
            else if (tipoMovimiento == EnumTipoMovimiento.Ingresar)
            {
                //TODO ver el tema de los feriados en CalcularFechaVenc()
                tramite.FechaVencimiento = CalcularFechaVenc(fechaActual.Date, cantDias);
            }
            else if (sectorAnterior.HasValue && tipoMovimiento == EnumTipoMovimiento.Rechazar)
            {
                idSectorDestino = sectorAnterior.Value;
            }
            else if (sectorOrigenDerivacion.HasValue && tipoMovimiento == EnumTipoMovimiento.Anular_derivacion)
            {
                idSectorDestino = sectorOrigenDerivacion.Value;
                observacion = null;
            }
            else if (tipoMovimiento == EnumTipoMovimiento.Reingresar)
            {
                if (!esProfesional)
                {
                    throw new NotImplementedException();
                }
            }
            else if (tipoMovimiento == EnumTipoMovimiento.AprobarGeneral && (EnumTipoTramite)tramite.IdTipoTramite == EnumTipoTramite.Mensuras)
            {
                using (var builder = GetContexto().CreateSQLQueryBuilder())
                {
                    builder.AddNoTable()
                           .AddFields(new Atributo { Funcion = $"pkg_tramites.aprobar_mensura({tramite.IdTramite},{usuario.Id_Usuario})", Campo = "aprobacion" })
                           .ExecuteQuery((System.Data.IDataReader reader) => true);
                }

            }
            else if (tipoMovimiento == EnumTipoMovimiento.Finalizar && (EnumTipoTramite)tramite.IdTipoTramite == EnumTipoTramite.Mensuras)
            {
                using (var builder = GetContexto().CreateSQLQueryBuilder())
                {
                    builder.AddNoTable()
                           .AddFields(new Atributo { Funcion = $"pkg_tramites.impactar_tramite({tramite.IdTramite},{usuario.Id_Usuario})", Campo = "finalizarjson" })
                           .ExecuteQuery((System.Data.IDataReader reader) => true);
                }
            }

            tramite.IdEstado = idEstado;
            tramite.FechaModif = fechaActual;
            tramite.UsuarioModif = idUsuarioModif;

            GetContexto().MovimientosTramites.Add(new MEMovimiento
            {
                Tramite = tramite,
                IdSectorDestino = idSectorDestino,
                IdSectorOrigen = (int)usuario.IdSector,
                IdTipoMovimiento = (int)tipoMovimiento,
                IdEstado = idEstado,
                Observacion = observacion,
                FechaAlta = fechaActual,
                UsuarioAlta = usuario.Id_Usuario
            });

            auditoria = new Auditoria(usuario.Id_Usuario, evento, Mensajes.ModificarTramitesOK,
                                        machineName, ip, "S", this.GetContexto().Entry(tramite).OriginalValues.ToObject(),
                                        GetContexto().Entry(tramite).Entity, "Trámite", 1, TiposOperacion.Modificacion);

            return procesadoOk.GetValueOrDefault(true); //devuelve false sólo cuando hubo un error en el procesamiento del tramite
        }


        private void InformarMovimientoSGT(IEnumerable<METramite> tramites, Sector destino)
        {
            var pasesInternosSGT = tramites.Select(tramite => PaseInternoTramite.Create(tramite, destino, GetContexto().Usuarios.Find(tramite.UsuarioModif)));
            using (var httpClient = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["apiSGTUrl"]) })
            using (var resp = httpClient.PostAsJsonAsync("api/sgt/expedientes/pases/internos", pasesInternosSGT).Result)
            {
                TramiteResponse movimientoInformado;
                if (!resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    string error = "Ha ocurrido un error al informar el movimiento del trámite.";
                    if(!(movimientoInformado = resp.Content.ReadAsAsync<TramiteResponse>().Result).IsOkey)
                    {
                        error = movimientoInformado.mensaje;
                    }
                    GetContexto().GetLogger().LogError($"Api SGT - Movimientos", error);
                }
            }
        }

        private void InformarNovedadSGT(METramite tramite, ITextGenerator<METramite> generador)
        {
            var novedadSGT = NovedadTramite.Create(tramite, GetContexto().Usuarios.Find(tramite.UsuarioModif), generador);
            using (var httpClient = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["apiSGTUrl"]) })
            using (var resp = httpClient.PostAsJsonAsync("api/sgt/expedientes/novedades", novedadSGT).Result)
            {
                TramiteResponse movimientoInformado;
                if (!resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    string error = "Ha ocurrido un error al informar las novedades del trámite.";
                    if (!(movimientoInformado = resp.Content.ReadAsAsync<TramiteResponse>().Result).IsOkey)
                    {
                        error = movimientoInformado.mensaje;
                    }
                    GetContexto().GetLogger().LogError($"Api SGT - Novedad (ID SGT: {tramite.IdSGT})", error);
                }
            }
        }

        private void ArchivarTramiteSGT(METramite tramite, Observacion observacion)
        {
            var finalizacionSGT = FinalizacionTramite.Create(tramite, GetContexto().Usuarios.Find(tramite.UsuarioModif), new GeneradorFinalizacionTramite(observacion));
            using (var httpClient = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["apiSGTUrl"]) })
            using (var resp = httpClient.PostAsJsonAsync("api/sgt/expedientes/finalizaciones", finalizacionSGT).Result)
            {
                TramiteResponse archivacionInformada;
                if (!resp.IsSuccessStatusCode && resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    string error = "Ha ocurrido un error al archivar el trámite.";
                    if (!(archivacionInformada = resp.Content.ReadAsAsync<TramiteResponse>().Result).IsOkey)
                    {
                        error = archivacionInformada.mensaje;
                    }
                    GetContexto().GetLogger().LogError($"Api SGT - Archivar Trámite (ID SGT: {tramite.IdSGT})", error);
                }
            }
        }

        private DateTime CalcularFechaVenc(DateTime fechaDesde, short cantDias)
        {
            return AddWorkDays(fechaDesde, cantDias);
        }

        private DateTime AddWorkDays(DateTime date, short workingDays)
        {
            short direction = (short)(workingDays < 0 ? -1 : 1);
            DateTime newDate = date;
            while (workingDays != 0)
            {
                newDate = newDate.AddDays(direction);
                if (newDate.DayOfWeek != DayOfWeek.Saturday &&
                    newDate.DayOfWeek != DayOfWeek.Sunday &&
                    !IsFeriado(newDate))
                {
                    workingDays -= direction;
                }
            }
            return newDate;
        }

        private bool IsFeriado(DateTime fecha)
        {
            return this.GetContexto().Feriados.Any(f => f.Fecha == fecha && f.Fecha_Baja == null);
        }

        private bool esEntradaOrigen(MEDatosEspecificos item)
        {
            return item.Propiedades.Any(t => t.Id == KeysDatosEspecificos.KeyOperacion && t.Value == Operacion.Origen.ToString());
        }

        private string getTipoEntrada(MEDatosEspecificos item)
        {
            return esEntradaOrigen(item) ? TIPO_ENTRADA_ANTECEDENTE : TIPO_ENTRADA_MENSURADA;
        }

        private void CrearTramiteEntradasTemporal(IList<RelacionDto> relaciones, MEDatosEspecificos[] datosEspecificos, METramite tramite, Usuarios usuario, List<long> entradasProcesadas)
        {
            if(!string.IsNullOrEmpty(tramite.Plano))
            {
                var mensuraTemporal = GetContexto().MensurasTemporal
                                                   .SingleOrDefault(m => m.IdTramite == tramite.IdTramite && m.FechaBaja == null)
                                            ?? GetContexto().MensurasTemporal
                                                            .Add(new MensuraTemporal()
                                                            {
                                                                IdTramite = tramite.IdTramite,
                                                                IdTipoMensura = tramite.IdObjetoTramite,
                                                                FechaAlta = DateTime.Now,
                                                                FechaModif = DateTime.Now,
                                                                IdUsuarioAlta = tramite.UsuarioModif,
                                                                IdUsuarioModif = tramite.UsuarioModif,
                                                            });

                var partes = tramite.Plano.Replace("/", "-").Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                mensuraTemporal.Departamento = partes[0];
                mensuraTemporal.Numero = partes[1];
                mensuraTemporal.Anio = partes[2];
                mensuraTemporal.Descripcion = tramite.Plano;
            }
            foreach (var item in datosEspecificos)
            {
                item.ParentGuids = item.ParentGuids ?? new List<Guid>();
                item.ParentIdTramiteEntradas = item.ParentIdTramiteEntradas ?? new List<int>();

                var entrada = this.GetContexto().Entradas.FirstOrDefault(t => t.IdEntrada == item.TipoObjeto.Id);
                var objetoEntrada = this.GetContexto().ObjetosEntrada.FirstOrDefault(t => t.IdEntrada == entrada.IdEntrada && t.IdObjetoTramite == tramite.IdObjetoTramite);
                long? idObjeto = CrearEditarDatoEspecificoTemporal(entrada, item, tramite, usuario, relaciones, entradasProcesadas);

                METramiteEntrada tramiteEntrada;
                if (item.IdTramiteEntrada.GetValueOrDefault(0) != 0)
                {
                    tramiteEntrada = this.GetContexto().TramitesEntradas.FirstOrDefault(t => t.IdTramiteEntrada == item.IdTramiteEntrada);
                }
                else
                {
                    tramite.TramiteEntradas.Add(new METramiteEntrada
                    {
                        UsuarioAlta = usuario.Id_Usuario,
                        FechaAlta = DateTime.Now,
                        IdComponente = entrada.IdComponente,
                        IdObjetoEntrada = objetoEntrada.IdObjetoEntrada,
                        TipoEntrada = getTipoEntrada(item)
                    });
                    tramiteEntrada = tramite.TramiteEntradas.Last();
                }
                tramiteEntrada.IdObjeto = idObjeto;
                tramiteEntrada.UsuarioModif = usuario.Id_Usuario;
                tramiteEntrada.FechaModif = DateTime.Now;

                this.GetContexto().SaveChanges();
                relaciones.Add(new RelacionDto { Guid = item.Guid, IdTramiteEntrada = item.IdTramiteEntrada, TramiteEntrada = tramiteEntrada });
            }
        }

        private long? CrearEditarDatoEspecificoTemporal(MEEntrada entrada, MEDatosEspecificos item, METramite tramite, Usuarios usuarioAlta, IList<RelacionDto> relaciones, List<long> entradasProcesadas)
        {
            entradasProcesadas = entradasProcesadas ?? new List<long>();

            var tramiteEntrada = GetContexto().TramitesEntradas.FirstOrDefault(t => t.IdTramiteEntrada == item.IdTramiteEntrada);

            if (entrada.IdEntrada == Convert.ToInt32(Entradas.Parcela))
            {
                long hdnObjeto = Convert.ToInt64(item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdParcela)?.Value);
                string hdnOperacion = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyOperacion)?.Value;

                if (hdnOperacion == Operacion.Origen.ToString())
                {
                    if (tramiteEntrada?.IdObjeto != hdnObjeto)
                    {
                        try
                        {
                            long idComponenteParcela = long.Parse(GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA").Valor);
                            var campoClave = GetContexto().Atributo.Include(a => a.Componente).GetAtributoClaveByComponente(idComponenteParcela);
                            var tablas = new[] { campoClave.Componente.TablaTemporal, campoClave.Componente.TablaGrafica, "inm_unidad_tributaria", "inm_nomenclatura", "inm_designacion" };
                            GetContexto().Entry(campoClave).State = EntityState.Detached;
                            var objetoEntrada = GetContexto().ObjetosEntrada.FirstOrDefault(x => x.IdEntrada == entrada.IdEntrada);
                            bool esMismoRegistro = false;
                            if (tramiteEntrada != null)
                            {
                                entradasProcesadas.Add(tramiteEntrada.IdTramiteEntrada);
                                LimpiarRelacionesParcela(tramite, tramiteEntrada.IdObjeto.Value, campoClave, tablas);
                            }
                            else if ((tramiteEntrada = GetContexto().TramitesEntradas.FirstOrDefault(p => p.IdObjeto == hdnObjeto && p.IdTramite == tramite.IdTramite && p.IdObjetoEntrada == objetoEntrada.IdObjetoEntrada)) != null)
                            {
                                esMismoRegistro = true;
                                item.IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada;
                            }
                            else
                            {
                                var utsAnteriores = GetContexto().UnidadesTributariasTemporal.Where(ut => ut.ParcelaID == hdnObjeto && ut.IdTramite == tramite.IdTramite);
                                if (utsAnteriores.Any())
                                {
                                    int idEntradaUt = int.Parse(Entradas.UnidadTributaria);
                                    int idObjetoUt = GetContexto().ObjetosEntrada.FirstOrDefault(o => o.IdEntrada == idEntradaUt).IdObjetoEntrada;
                                    var entradasAnteriores = GetContexto().TramitesEntradas.Where(p => utsAnteriores.Any(ut => ut.UnidadTributariaId == p.IdObjeto) && p.IdTramite == tramite.IdTramite && p.IdObjetoEntrada == idObjetoUt);
                                    GetContexto().TramitesEntradas.RemoveRange(entradasAnteriores);
                                    LimpiarRelacionesParcela(tramite, utsAnteriores.First().ParcelaID.Value, campoClave, tablas);
                                }
                            }
                            if (!esMismoRegistro)
                            {
                                foreach (string tabla in tablas)
                                {
                                    CopiarRegistros(campoClave, tabla, hdnObjeto, tramite.IdTramite);
                                }

                                var utParcela = GetContexto()
                                                    .UnidadesTributarias
                                                    .Single(ut => ut.ParcelaID == hdnObjeto && ut.TipoUnidadTributariaID != (int)TipoUnidadTributariaEnum.UnidadFuncionalPH && ut.FechaBaja == null);

                                CopiarRegistros(new Atributo() { Campo = "ID_UNIDAD_TRIBUTARIA" }, "inm_dominio", utParcela.UnidadTributariaId, tramite.IdTramite);

                                foreach (var dominio in GetContexto().Dominios.Where(d => d.UnidadTributariaID == utParcela.UnidadTributariaId && d.FechaBaja == null).ToArray())
                                {
                                    CopiarRegistros(new Atributo() { Campo = "ID_DOMINIO" }, "inm_dominio_titular", dominio.DominioID, tramite.IdTramite);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    return hdnObjeto;
                }

                string tipo = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyTipoParcela).Value,
                       clase = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyClaseParcela).Value,
                       superficie = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeySuperficieParcela).Value;

                ParcelaTemporal obj;
                if (tramiteEntrada == null) //Alta parcela
                {
                    obj = new ParcelaTemporal
                    {
                        IdTramite = tramite.IdTramite,
                        TipoParcelaID = Convert.ToInt64(tipo),
                        ClaseParcelaID = Convert.ToInt64(clase),
                        EstadoParcelaID = long.Parse(EstadosParcelas.Baldio),
                        OrigenParcelaID = 2,
                        UsuarioAltaID = usuarioAlta.Id_Usuario,
                        FechaAlta = DateTime.Now,
                        UsuarioModificacionID = usuarioAlta.Id_Usuario,
                        FechaModificacion = DateTime.Now
                    };
                    GetContexto().ParcelasTemporal.Add(obj);
                }
                else //Modificacion parcela
                {
                    obj = GetContexto().ParcelasTemporal.Find(tramiteEntrada.IdObjeto, tramiteEntrada.IdTramite);

                    string nomenclaturaText = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdParcela).Text;

                    if (!string.IsNullOrEmpty(nomenclaturaText))
                    {
                        var nomenclatura = GetContexto()
                                                .NomenclaturasTemporal
                                                .SingleOrDefault(n => n.ParcelaID == obj.ParcelaID && n.IdTramite == obj.IdTramite);

                        if (nomenclatura == null)
                        {
                            nomenclatura = GetContexto().NomenclaturasTemporal
                                                        .Add(new NomenclaturaTemporal
                                                        {
                                                            IdTramite = obj.IdTramite,
                                                            ParcelaID = obj.ParcelaID,
                                                            UsuarioAltaID = obj.UsuarioModificacionID,
                                                            FechaAlta = obj.FechaModificacion
                                                        });
                        }
                        nomenclatura.Nombre = nomenclaturaText;
                        nomenclatura.UsuarioModificacionID = obj.UsuarioModificacionID;
                        nomenclatura.FechaModificacion = obj.FechaModificacion;
                    }
                }

                obj.Superficie = Math.Round(Convert.ToDecimal(superficie) * (obj.TipoParcelaID != (long)TipoParcelaEnum.Urbana ? 10_000m : 1m), 2);
                obj.UsuarioModificacionID = usuarioAlta.Id_Usuario;
                obj.FechaModificacion = DateTime.Now;

                GetContexto().SaveChanges();

                var keyIdUT = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdUnidadTributaria);
                string tipoUT = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdTipoUnidadTributaria).Value,
                       uf = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyUnidadFuncional).Text,
                       porcentaje = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyPorcentajeCopropiedad).Text,
                       superficieUT = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeySuperficieUT).Text;

                var utt = GetContexto().UnidadesTributariasTemporal.Find(Convert.ToInt64(keyIdUT.Value), tramite.IdTramite);
                if (utt == null)
                {
                    utt = new UnidadTributariaTemporal
                    {
                        IdTramite = tramite.IdTramite,
                        ParcelaID = obj.ParcelaID,
                        TipoUnidadTributariaID = Convert.ToInt32(tipoUT),
                        UsuarioAltaID = obj.UsuarioModificacionID,
                        FechaAlta = obj.FechaModificacion,
                    };
                    GetContexto().UnidadesTributariasTemporal.Add(utt);
                }
                utt.CodigoProvincial = keyIdUT.Text;
                utt.Superficie = Convert.ToDouble(obj.Superficie);
                utt.UnidadFuncional = uf;
                utt.PorcentajeCopropiedad = Convert.ToDecimal(porcentaje ?? "100");
                utt.UsuarioModificacionID = obj.UsuarioModificacionID;
                utt.FechaModificacion = obj.FechaModificacion;

                GetContexto().SaveChanges();

                ActualizarDatosValuacion(utt, item.Propiedades.SingleOrDefault(t => t.Id == KeysDatosEspecificos.KeyValuacion));

                return obj.ParcelaID;
            }
            else
            if (entrada.IdEntrada == Convert.ToInt32(Entradas.UnidadTributaria))
            {
                long hdnObjeto = Convert.ToInt64(item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdUnidadTributaria)?.Value);
                string hdnOperacion = item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyOperacion)?.Value;

                if (hdnOperacion == Operacion.Origen.ToString())
                {
                    if (tramiteEntrada?.IdObjeto != hdnObjeto)
                    {
                        try
                        {
                            long idComponenteParcela = long.Parse(GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_COMPONENTE_PARCELA").Valor);
                            long idComponenteUT = long.Parse(GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_COMPONENTE_UNIDAD_TRIBUTARIA").Valor);

                            var componenteUt = GetContexto().Componente.Find(idComponenteUT);

                            var campoClave = GetContexto().Atributo.Include(a => a.Componente).GetAtributoClaveByComponente(idComponenteParcela);
                            var tablasClaveParcela = new[] { campoClave.Componente.TablaTemporal, campoClave.Componente.TablaGrafica };
                            var tablas = tablasClaveParcela.Append(componenteUt.TablaTemporal).ToArray();
                            GetContexto().Entry(campoClave).State = EntityState.Detached;
                            var objetoEntrada = GetContexto().ObjetosEntrada.FirstOrDefault(x => x.IdEntrada == entrada.IdEntrada);
                            bool esMismoRegistro = false;
                            if (tramiteEntrada != null)
                            {
                                long parcelaIdAnterior = GetContexto().UnidadesTributariasTemporal.Find(tramiteEntrada.IdObjeto.Value, tramite.IdTramite).ParcelaID.Value;
                                entradasProcesadas.Add(tramiteEntrada.IdTramiteEntrada);
                                LimpiarRelacionesParcela(tramite, parcelaIdAnterior, campoClave, tablas);
                            }
                            else if ((tramiteEntrada = GetContexto().TramitesEntradas.FirstOrDefault(p => p.IdObjeto == hdnObjeto && p.IdTramite == tramite.IdTramite && p.IdObjetoEntrada == objetoEntrada.IdObjetoEntrada)) != null)
                            {
                                esMismoRegistro = true;
                                item.IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada;
                            }
                            else
                            {
                                long? parcelaIdAnterior = GetContexto().UnidadesTributariasTemporal.Find(hdnObjeto, tramite.IdTramite)?.ParcelaID.Value;
                                if (parcelaIdAnterior.HasValue)
                                {
                                    int idEntradaParcela = int.Parse(Entradas.Parcela);
                                    int idObjetoEntradaParcela = GetContexto().ObjetosEntrada.FirstOrDefault(o => o.IdEntrada == idEntradaParcela).IdObjetoEntrada;
                                    var tramiteEntradaAnterior = GetContexto().TramitesEntradas.FirstOrDefault(p => p.IdObjeto == parcelaIdAnterior && p.IdTramite == tramite.IdTramite && p.IdObjetoEntrada == idObjetoEntradaParcela);
                                    GetContexto().Entry(tramiteEntradaAnterior).State = EntityState.Deleted;
                                    LimpiarRelacionesParcela(tramite, parcelaIdAnterior.Value, campoClave, tablas);
                                }
                            }
                            if (!esMismoRegistro)
                            {
                                long parcelaIdNueva = GetContexto().UnidadesTributarias.Find(hdnObjeto).ParcelaID.Value;
                                foreach (var tabla in tablasClaveParcela)
                                {
                                    CopiarRegistros(campoClave, tabla, parcelaIdNueva, tramite.IdTramite);
                                }

                                campoClave = GetContexto().Atributo.GetAtributoClaveByComponente(idComponenteUT);
                                GetContexto().Entry(campoClave).State = EntityState.Detached;
                                CopiarRegistros(campoClave, componenteUt.TablaTemporal, hdnObjeto, tramite.IdTramite);
                            }

                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    return hdnObjeto;
                }

                var tipo = item.Propiedades.FirstOrDefault(t => t.Id == "Tipo")?.Value;
                var plano = item.Propiedades.FirstOrDefault(t => t.Id == "Plano")?.Value;
                var piso = item.Propiedades.FirstOrDefault(t => t.Id == "Piso")?.Value;
                var unidad = item.Propiedades.FirstOrDefault(t => t.Id == "Unidad")?.Value;
                var superficie = item.Propiedades.FirstOrDefault(t => t.Id == "Superficie")?.Value;
                var vigencia = item.Propiedades.FirstOrDefault(t => t.Id == "vigencia")?.Value;


                UnidadTributariaTemporal obj;

                if (tramiteEntrada == null) //Unidad Tributaria Destino Alta
                {
                    var tramite_entrada_padre = relaciones.FirstOrDefault(t => t.Guid == item.ParentGuids.Single());

                    obj = new UnidadTributariaTemporal
                    {
                        TipoUnidadTributariaID = Convert.ToInt32(tipo),
                        PlanoId = plano,
                        Piso = piso,
                        Unidad = unidad,
                        Superficie = Convert.ToDouble(superficie),
                        UsuarioAltaID = usuarioAlta.Id_Usuario,
                        FechaAlta = DateTime.Now,
                        UsuarioModificacionID = usuarioAlta.Id_Usuario,
                        FechaModificacion = DateTime.Now,
                        IdTramite = tramite.IdTramite,
                        ParcelaID = tramite_entrada_padre.TramiteEntrada.IdObjeto,
                        Vigencia = Convert.ToDateTime(vigencia),
                    };

                    GetContexto().UnidadesTributariasTemporal.Add(obj);


                }
                else //Modificacion unidad funcional
                {
                    obj = GetContexto().UnidadesTributariasTemporal.FirstOrDefault(t => t.UnidadTributariaId == tramiteEntrada.IdObjeto);

                    obj.PlanoId = plano;
                    obj.Piso = piso;
                    obj.Unidad = unidad;
                    obj.Superficie = Convert.ToDouble(superficie);
                    obj.UsuarioModificacionID = usuarioAlta.Id_Usuario;
                    obj.FechaModificacion = DateTime.Now;
                    obj.TipoUnidadTributariaID = Convert.ToInt32(tipo);
                    obj.Vigencia = Convert.ToDateTime(vigencia);
                }

                GetContexto().SaveChanges();

                return obj.UnidadTributariaId;
            }
            else
            if (entrada.IdEntrada == Convert.ToInt32(Entradas.Mensura))
            {
                long nroMensura = Convert.ToInt64(item.Propiedades.FirstOrDefault(t => t.Id == KeysDatosEspecificos.KeyIdMensura)?.Value);
                try
                {
                    var cmpMensura = Convert.ToInt64(GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_COMPONENTE_MENSURA")?.Valor);
                    var campoClave = GetContexto().Atributo.Include(a => a.Componente).GetAtributoClaveByComponente(cmpMensura);
                    string tabla = campoClave.Componente.TablaTemporal ?? campoClave.Componente.Tabla;
                    GetContexto().Entry(campoClave).State = EntityState.Detached;
                    var objetoEntrada = GetContexto().ObjetosEntrada.FirstOrDefault(x => x.IdEntrada == entrada.IdEntrada);
                    bool esMismoRegistro = false;
                    if (tramiteEntrada != null)
                    {
                        BorrarRegistrosTemporales(campoClave, tabla, tramiteEntrada.IdObjeto.ToString(), Common.Enums.SQLOperators.EqualsTo, tramite.IdTramite);
                    }
                    else if ((tramiteEntrada = GetContexto().TramitesEntradas.FirstOrDefault(p => p.IdObjeto == nroMensura && p.IdTramite == tramite.IdTramite && p.IdObjetoEntrada == objetoEntrada.IdObjetoEntrada)) != null)
                    {
                        esMismoRegistro = true;
                        item.IdTramiteEntrada = tramiteEntrada.IdTramiteEntrada;
                    }
                    if (!esMismoRegistro)
                    {
                        CopiarRegistros(campoClave, tabla, nroMensura, tramite.IdTramite);
                    }

                    return nroMensura;

                }
                catch (Exception)
                {

                    throw;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void ActualizarDatosValuacion(UnidadTributariaTemporal ut, Propiedad propiedadValuacion)
        {
            if (propiedadValuacion == null || string.IsNullOrEmpty(propiedadValuacion.Value)) return;

            var datosActuales = GetContexto().DeclaracionesJuradasTemporal
                                             .Include(dj => dj.Sor)
                                             .Include(dj => dj.Sor.Superficies)
                                             .Include(dj => dj.Sor.Superficies.Select(s => s.Caracteristicas))
                                             .Include(dj => dj.Valuaciones)
                                             .SingleOrDefault(dj => dj.IdUnidadTributaria == ut.UnidadTributariaId && dj.IdTramite == ut.IdTramite);

            if(datosActuales != null)
            {
                GetContexto().DeclaracionesJuradasTemporal.Remove(datosActuales);
            }

            DateTime now = DateTime.Now;

            var ddjj = new DDJJ()
            {
                IdVersion = Convert.ToInt64(VersionesDDJJ.SoR),
                FechaVigencia = now.Date,
                IdUnidadTributaria = ut.UnidadTributariaId,
                Sor = new DDJJSor()
                {
                    Superficies = JsonConvert.DeserializeObject<VALSuperficie[]>(propiedadValuacion.Value),
                },
            };

            ddjj.IdUsuarioModif = ddjj.IdUsuarioAlta = ut.UsuarioModificacionID;
            ddjj.FechaModif = ddjj.FechaAlta = now;
            ddjj.Sor.IdUsuarioModif = ddjj.Sor.IdUsuarioAlta = ut.UsuarioModificacionID;
            ddjj.Sor.FechaModif = ddjj.Sor.FechaAlta = now;

            foreach (var superficie in ddjj.Sor.Superficies)
            {
                superficie.IdUsuarioModif = superficie.IdUsuarioAlta = ut.UsuarioModificacionID;
                superficie.FechaModif = superficie.FechaAlta = now;
                foreach (var caracteristica in superficie.Caracteristicas)
                {
                    caracteristica.IdUsuarioModif = caracteristica.IdUsuarioAlta = ut.UsuarioModificacionID;
                    caracteristica.FechaModif = caracteristica.FechaAlta = now;
                }
            }

            var validator = new DDJJTemporalValidator(ddjj)
                            .Validate(new SuperficiesValidation())
                            .Validate(new SuperficieParcelaRuralTemporalValidation(GetContexto()))
                            .Validate(new CaracteristicasValidation(GetContexto()));

            var generator = new TemporalGenerator(ut.IdTramite, new TierraRuralTemporalComputation(GetContexto()), validator);
            GetContexto().ValuacionesTemporal.Add(generator.Generate().Result);

            GetContexto().SaveChanges();
        }

        private void BorrarRegistrosTemporales(Atributo campoClave, string nombreTabla, string objetos, Common.Enums.SQLOperators operador, int idTramite)
        {
            using (var qb = this.GetContexto().CreateSQLQueryBuilder())
            {
                qb.AddTable("temporal", nombreTabla, null)
                    .AddFilter(campoClave.Campo, objetos, operador)
                    .AddFilter("id_tramite", idTramite, Common.Enums.SQLOperators.EqualsTo, Common.Enums.SQLConnectors.And)
                    .ExecuteDelete();
            }
        }

        private void CopiarRegistros(Atributo campoClave, string nombreTabla, long idObjeto, int IdTramite)
        {
            CopiarRegistros(new[] { campoClave }, nombreTabla, new long[] { idObjeto }, IdTramite);
        }

        private void CopiarRegistros(IEnumerable<Atributo> camposClaves, string nombreTabla, IEnumerable<long> ids, int IdTramite)
        {

            using (var queryEsquema = this.GetContexto().CreateSQLQueryBuilder())
            using (var insertBuilder = this.GetContexto().CreateSQLQueryBuilder())
            using (var querybuilder = this.GetContexto().CreateSQLQueryBuilder())
            {
                try
                {
                    var Tabla = new Componente()
                    {
                        ComponenteId = camposClaves.FirstOrDefault().ComponenteId,
                        Esquema = ConfigurationManager.AppSettings["DATABASE"],
                        Tabla = nombreTabla
                    };
                    var camposParcela = queryEsquema.GetTableFields(Tabla.Esquema, Tabla.Tabla)
                                .Select(c => new KeyValuePair<Atributo, object>(new Atributo() { Campo = c }, c))
                                .Concat(new[] { new KeyValuePair<Atributo, object>(new Atributo() { Campo = "id_tramite" }, IdTramite) });

                    querybuilder.AddTable(Tabla, null);
                    int i = 0;
                    Common.Enums.SQLConnectors conector = Common.Enums.SQLConnectors.None;
                    foreach (var campoClave in camposClaves)
                    {
                        campoClave.Componente = Tabla;
                        querybuilder.AddFilter(campoClave, ids.ElementAt(i), Common.Enums.SQLOperators.EqualsTo, conector);
                        i++;
                        conector = Common.Enums.SQLConnectors.And;
                    }


                    var fechaBaja = camposParcela.Select(c => c.Key).SingleOrDefault(c => c.Campo.ToLower() == "fecha_baja");

                    if (fechaBaja != null)
                    {
                        fechaBaja.Componente = Tabla;
                        querybuilder.AddFilter(fechaBaja, null, Common.Enums.SQLOperators.IsNull, Common.Enums.SQLConnectors.And);

                    }

                    insertBuilder.AddTable("temporal", Tabla.Tabla, null)
                        .AddQueryToInsert(querybuilder, camposParcela.ToArray())
                        .ExecuteInsert();

                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private void LimpiarRelacionesParcela(METramite tramite, long idParcela, Atributo campoClave, string[] tablas)
        {
            var query = from pmt in this.GetContexto().ParcelaMensurasTemporal
                        where pmt.IdParcela == idParcela && pmt.IdTramite == tramite.IdTramite
                        select pmt.IdMensura;

            if (query.Any())
            {
                long idCmp = long.Parse(this.GetContexto().ParametrosGenerales.FirstOrDefault(p => p.Clave == "ID_COMPONENTE_DESC_INMUEBLE").Valor);
                var cmp = this.GetContexto().Componente.Find(idCmp);
                string mensuras = string.Join(",", query.ToArray());
                foreach (string tablaMensura in new[] { "inm_parcela_mensura", cmp.TablaTemporal, "inm_mensura" })
                {
                    BorrarRegistrosTemporales(new Atributo() { Campo = "id_mensura" }, tablaMensura, mensuras, Common.Enums.SQLOperators.In, tramite.IdTramite);
                }

            }

            var queryLd = from ld in this.GetContexto().INMLibresDeDeudasTemporal
                          join ut in this.GetContexto().UnidadesTributariasTemporal on new { id = ld.IdUnidadTributaria, ld.IdTramite } equals new { id = ut.UnidadTributariaId, ut.IdTramite }
                          where ut.IdTramite == tramite.IdTramite && ut.ParcelaID == idParcela
                          select ld;

            var queryDi = from di in this.GetContexto().INMCertificadosCatastralesTemporal
                          join ut in this.GetContexto().UnidadesTributariasTemporal on new { id = di.UnidadTributariaId, di.IdTramite } equals new { id = ut.UnidadTributariaId, ut.IdTramite }
                          where ut.IdTramite == tramite.IdTramite && ut.ParcelaID == idParcela
                          select di;

            var queryDominios = from dominio in this.GetContexto().DominiosTemporal
                                join titular in this.GetContexto().DominiosTitularesTemporal on new { dominio.DominioID, dominio.IdTramite } equals new { titular.DominioID, titular.IdTramite }
                                join ut in this.GetContexto().UnidadesTributariasTemporal on new { id = dominio.UnidadTributariaID, dominio.IdTramite } equals new { id = ut.UnidadTributariaId, ut.IdTramite }
                                where ut.IdTramite == tramite.IdTramite && ut.ParcelaID == idParcela
                                group titular by dominio into gp
                                select new { dominio = gp.Key, titulares = gp };

            var queryDesignaciones = from designacion in this.GetContexto().DesignacionesTemporal
                                     where designacion.IdParcela == idParcela
                                     select designacion;

            var queryEspacioPublico = from espacioPublico in this.GetContexto().EspaciosPublicosTemporal
                                      where espacioPublico.ParcelaID == idParcela
                                      select espacioPublico;

            GetContexto().INMLibresDeDeudasTemporal.RemoveRange(queryLd);
            GetContexto().INMCertificadosCatastralesTemporal.RemoveRange(queryDi);
            GetContexto().DominiosTitularesTemporal.RemoveRange(queryDominios.SelectMany(g => g.titulares));
            GetContexto().DominiosTemporal.RemoveRange(queryDominios.Select(g => g.dominio));
            GetContexto().DesignacionesTemporal.RemoveRange(queryDesignaciones);
            GetContexto().EspaciosPublicosTemporal.RemoveRange(queryEspacioPublico);

            GetContexto().SaveChanges();
            foreach (string tabla in tablas.Reverse())
            {
                BorrarRegistrosTemporales(campoClave, tabla, idParcela.ToString(), Common.Enums.SQLOperators.EqualsTo, tramite.IdTramite);
            }
        }

        private Stream adquirirLockSecuenciaTramite(bool exclusivo) => adquirirLockSecuencia(exclusivo, $"locktramite");

        private Stream adquirirLockSecuencia(bool exclusivo, string lockname)
        {
            if (exclusivo)
            {
                DateTime inicio = DateTime.Now;
                TimeSpan maxTimeToWait = TimeSpan.FromMinutes(5);
                lockname = $"{lockname}.bin";
                while (DateTime.Now.Subtract(maxTimeToWait) <= inicio)
                {
                    try
                    {
                        return File.Create(Path.Combine(Path.GetTempPath(), lockname), 1, FileOptions.WriteThrough | FileOptions.DeleteOnClose);
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }
                throw new TimeoutException($"No se pudo adquirir el lock '{lockname}' para guardar el registro.");
            }
            else
            {
                return new MemoryStream();
            }
        }
    }
}