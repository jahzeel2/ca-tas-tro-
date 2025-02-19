using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.BusinessEntities.Designaciones;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using GeoSit.Data.DAL.Valuaciones;
using GeoSit.Data.DAL.Valuaciones.Validators;
using GeoSit.Data.DAL.Valuaciones.Validators.Validations;
using GeoSit.Data.DAL.Valuaciones.Computations;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;
using OA = GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using System.Configuration;

namespace GeoSit.Data.DAL.Repositories
{
    public class DeclaracionJuradaRepository : IDeclaracionJuradaRepository
    {
        private readonly GeoSITMContext _context;

        public DeclaracionJuradaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public DecretoAplicado AplicarDecreto(long idDecreto, long idUsuario)
        {
            throw new NotImplementedException();
        }

        public bool AptitudCompleta(long idAptitud, long[] caracteristicas)
        {
            //var grupos = _context.DDJJSorCaracteristicas
            //                     .Include(c => c.TipoCaracteristica.GrupoCaracteristica.Aptitudes)
            //                     .AsNoTracking()
            //                     .ToArray();

            return true;
        }

        public VALSuperficie AptitudPuntaje(long idAptitud, VALSuperficie superficie)
        {
            superficie.IdAptitud = idAptitud;
            superficie.Superficie *= 10_000;
            superficie.Puntaje = new PuntajeSuperficieComputation(_context).Compute(superficie);
            return superficie;
        }

        public bool GenerarValuacion(DDJJ ddjj, long idUnidadTributaria, TipoValuacionEnum tipoValuacion, long idUsuario, string machineName, string ip)
        {
            try
            {
                var ut = _context.UnidadesTributarias
                                 .Include(x => x.Parcela)
                                 .Single(x => x.UnidadTributariaId == idUnidadTributaria);

                var fechaVigencia = ddjj.FechaVigencia.GetValueOrDefault().Date.AddDays(1).AddMilliseconds(-1);

                var decretos = _context.ValDecretos
                                       .Include(x => x.Jurisdiccion)
                                       .Include(x => x.Zona)
                                       .Where(x => !x.IdUsuarioBaja.HasValue && x.FechaInicio <= fechaVigencia && ((!x.FechaFin.HasValue) || (x.FechaFin >= fechaVigencia)) &&
                                                    x.Zona.Any(y => y.IdTipoParcela == ut.Parcela.TipoParcelaID && !y.IdUsuarioBaja.HasValue) &&
                                                    x.Jurisdiccion.Any(y => y.IdJurisdiccion == ut.JurisdiccionID && !y.IdUsuarioBaja.HasValue))
                                       .ToList();

                TerminarValuacionesVigentes(ObtenerValuacion(ddjj, ut, tipoValuacion, decretos, idUsuario));

                _context.SaveChanges(auditar(ddjj, Eventos.NuevaValuacion, TiposOperacion.Alta, machineName, ip));
                return true;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"GenerarValuacion({ddjj.IdUnidadTributaria})", ex);
                throw;
            }
        }

        private Auditoria auditar(DDJJ ddjj, string evento, string tipoOperacion, string machineName, string ip)
        {
            return new Auditoria(ddjj.IdUsuarioModif, evento, null, machineName, ip, "S", null, null, "UnidadTributaria", 1, tipoOperacion)
            {
                Fecha = ddjj.FechaModif,
                Id_Objeto = ddjj.IdUnidadTributaria,
            };
        }

        private void TerminarValuacionesVigentes(VALValuacion valuacion)
        {
            DateTime fecha = valuacion.FechaDesde.Date;
            var valuacionesVigentes = from val in _context.VALValuacion
                                      join val2 in (from oldVal in _context.VALValuacion
                                                    where oldVal.FechaBaja == null && oldVal.FechaDesde < fecha && oldVal.FechaHasta > fecha
                                                    group oldVal.FechaHasta by oldVal.IdUnidadTributaria into gp
                                                    select new { IdUnidadTributaria = gp.Key, MaxFechaPrevia = gp.Max() }) on val.IdUnidadTributaria equals val2.IdUnidadTributaria into lj
                                      from valAntigua in lj.DefaultIfEmpty()
                                      where val.IdUnidadTributaria == valuacion.IdUnidadTributaria && val.FechaBaja == null &&
                                            (val.FechaDesde >= fecha || val.FechaHasta == valAntigua.MaxFechaPrevia)
                                      select val;

            foreach (var v in valuacionesVigentes)
            {
                v.FechaHasta = valuacion.FechaDesde;
                v.FechaModif = valuacion.FechaAlta;
                v.IdUsuarioModif = valuacion.IdUsuarioAlta;
            }
        }

        public List<VALAptitudes> GetAptitudes(int? idVersion = null)
        {
            var result = _context.VALAptitudes.Where(a => a.FechaBaja == null);

            if (idVersion.GetValueOrDefault() > 0)
            {
                result = result.Where(w => w.IdVersion == idVersion);
            }

            return result.ToList();
        }

        public List<DDJJSorCaracteristicas> GetCaracteristicas()
        {
            var result = _context.DDJJSorCaracteristicas
                                 .Include("TipoCaracteristica")
                                 .Where(x => x.FechaBaja == null)
                                 .ToList();
            return result;
        }

        public async Task<DatosComputo> CalcularValuacionAsync(long idUnidadTributaria, VALSuperficie[] superficies)
        {
            var ddjj = new DDJJ()
            {
                FechaVigencia = DateTime.Today,
                IdUnidadTributaria = idUnidadTributaria,
                Sor = new DDJJSor()
                {
                    Superficies = superficies
                }
            };
            var validator = new DDJJValidator(ddjj)
                            .Validate(new SuperficiesValidation())
                            .Validate(new SuperficieParcelaRuralValidation(_context))
                            .Validate(new CaracteristicasValidation(_context));

            var generador = new Generator(new TierraRuralComputation(_context), validator);

            return await generador.SimulateAsync();
        }

        public DDJJDesignacion GetDDJJDesignacion(long idDeclaracionJurada)
        {
            /*
            var ddjj = _context.DDJJ
                               .Include(x => x.Designacion)
                               .AsNoTracking()
                               .FirstOrDefault(x => x.IdDeclaracionJurada == idDeclaracionJurada);

            return ddjj?.Designacion;
            */
            throw new InvalidOperationException("Deprecated");
        }

        public List<DDJJ> GetDeclaracionesJuradas(long idUnidadTributaria)
        {
            return GetDDJJQueryByIdUTNewestFirst(idUnidadTributaria)
                    .Take(1)
                    .ToList();
        }

        public List<DDJJ> GetDeclaracionesJuradasNoVigentes(long idUnidadTributaria)
        {
            return GetDDJJQueryByIdUTNewestFirst(idUnidadTributaria)
                    .Skip(1)
                    .ToList();
        }

        private IQueryable<DDJJ> GetDDJJQueryByIdUTNewestFirst(long idUnidadTributaria)
        {
            long ddjjTierraRural = long.Parse(VersionesDDJJ.SoR);

            return _context.DDJJ
                           .Where(x => x.IdUnidadTributaria == idUnidadTributaria
                                        && x.FechaBaja == null
                                        && x.IdVersion == ddjjTierraRural)
                           .OrderByDescending(x => x.FechaVigencia)
                           .ThenByDescending(x => x.FechaAlta)
                           .Include(x => x.Sor)
                           .Include(x => x.Origen)
                           .Include(x => x.Version)
                           .Include(x => x.Valuaciones);
        }

        public DDJJ GetDeclaracionJurada(long idDeclaracionJurada)
        {
            return _context.DDJJ.Find(idDeclaracionJurada);
        }

        public DDJJ GetDeclaracionJuradaCompleta(long idDeclaracionJurada)
        {
            var ddjj = _context.DDJJ
                               .Include(x => x.Origen)
                               .Include(x => x.Version)
                               .Single(x => x.IdDeclaracionJurada == idDeclaracionJurada);

            _context.Entry(ddjj)
                    .Collection(x => x.Dominios).Query()
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.PersonaDomicilio))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.PersonaDomicilio.Select(pd => pd.Domicilio)))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.PersonaDomicilio.Select(pd => pd.Domicilio.TipoDomicilio)))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.Persona))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.Persona.TipoDocumentoIdentidad))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.Persona.TipoPersona))
                    .IncludeFilter(d => d.Titulares.Where(t => t.FechaBaja == null).Select(t => t.TT))
                    .Load();

            _context.Entry(ddjj)
                    .Collection(x => x.Dominios).Query()
                    .Include(d => d.TipoInscripcion)
                    .Load();

            /*
            _context.Entry(ddjj)
                    .Reference(x => x.Designacion).Query()
                    .Where(d => d.FechaBaja == null)
                    .Load();
            */
            _context.Entry(ddjj.Version)
                    .Collection(x => x.OtrasCarsSor)
                    .Load();

            _context.Entry(ddjj)
                    .Reference(x => x.Sor).Query()
                    .Include(s => s.Mensuras)
                    .Load();

            _context.Entry(ddjj)
                    .Reference(x => x.Sor).Query()
                    .IncludeFilter(s => s.Superficies.Where(sup => sup.FechaBaja == null))
                    .IncludeFilter(s => s.Superficies.Where(sup => sup.FechaBaja == null).Select(sup => sup.Aptitud))
                    .IncludeFilter(s => s.Superficies.Where(sup => sup.FechaBaja == null).Select(sup => sup.Caracteristicas.Where(c => c.FechaBaja == null)))
                    .IncludeFilter(s => s.Superficies.Where(sup => sup.FechaBaja == null).Select(sup => sup.Caracteristicas.Where(c => c.FechaBaja == null).Select(c => c.Caracteristica.TipoCaracteristica)))
                    .Load();

            return ddjj;
        }

        public DDJJ GetDeclaracionJuradaVigenteSoR(long idUnidadTributaria)
        {
            long idTierraSor = long.Parse(VersionesDDJJ.SoR);

            long? dj = _context.DDJJ
                               .Where(x => x.IdUnidadTributaria == idUnidadTributaria 
                                            && x.IdVersion == idTierraSor
                                            && x.FechaBaja == null)
                               .OrderByDescending(x => x.FechaVigencia)
                               .ThenByDescending(x => x.FechaAlta)
                               .FirstOrDefault()?.IdDeclaracionJurada;
            if (dj.HasValue)
            {
                return GetDeclaracionJuradaCompleta(dj.Value);
            }
            return null;
        }

        public VALDecreto GetDecretoByNumero(long nroDecreto)
        {
            return _context.ValDecretos.FirstOrDefault(x => x.NroDecreto == nroDecreto);
        }

        public METramite GetTramite(long idDeclaracionJurada)
        {
            long id_componente = Convert.ToInt64(_context.ParametrosGenerales.FirstOrDefault(x => x.Clave == "ID_COMPONENTE_DDJJ").Valor);
            var tramiteEntrada = _context.TramitesEntradas
                                         .Where(x => x.IdComponente == id_componente && x.IdObjeto == idDeclaracionJurada)
                                         .FirstOrDefault();

            if (tramiteEntrada != null)
            {
                return _context.TramitesMesaEntrada.Find(tramiteEntrada.IdTramite);
            }

            return null;
        }

        public Designacion GetDesignacionByUt(long idUnidadTributaria)
        {
            var query = (from ut in _context.UnidadesTributarias
                         join par in _context.Parcelas on ut.ParcelaID equals par.ParcelaID
                         join desig in _context.Designacion on par.ParcelaID equals desig.IdParcela
                         where ut.UnidadTributariaId == idUnidadTributaria && desig.FechaBaja == null && desig.IdLocalidad.HasValue
                         select desig).FirstOrDefault();
            return query;
        }

        public IEnumerable<DDJJDominio> GetDominios(long idDeclaracionJurada)
        {
            var ddjj = _context.DDJJ
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null))
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null).Select(z => z.TipoInscripcion))
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null).Select(z => z.Titulares.Where(t => t.FechaBaja == null)))
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null).Select(z => z.Titulares.Where(t => t.FechaBaja == null).Select(t => t.TipoTitularidad)))
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null).Select(z => z.Titulares.Where(t => t.FechaBaja == null).Select(p => p.Persona.TipoDocumentoIdentidad)))
                               .IncludeFilter(x => x.Dominios.Where(y => y.FechaBaja == null).Select(z => z.Titulares.Where(t => t.FechaBaja == null).Select(p => p.PersonaDomicilio.Select(pd => pd.Domicilio.TipoDomicilio))))
                               .AsNoTracking()
                               .Single(x => x.IdDeclaracionJurada == idDeclaracionJurada);

            foreach (var titular in ddjj.Dominios.SelectMany(x => x.Titulares))
            {
                titular.NombreCompleto = titular.Persona.NombreCompleto;
                titular.TipoNoDocumento = $"{titular.Persona.TipoDocumentoIdentidad.Descripcion} / {titular.Persona.NroDocumento}";
                titular.Persona = null;

                foreach (var domicilio in titular.PersonaDomicilio)
                {
                    domicilio.Tipo = domicilio.Domicilio.TipoDomicilio.Descripcion;
                    domicilio.Provincia = domicilio.Domicilio.provincia;
                    domicilio.Localidad = domicilio.Domicilio.localidad;
                    domicilio.Barrio = domicilio.Domicilio.barrio;
                    domicilio.Calle = domicilio.Domicilio.ViaNombre;
                    domicilio.Altura = domicilio.Domicilio.numero_puerta;
                    domicilio.Piso = domicilio.Domicilio.piso;
                    domicilio.Departamento = domicilio.Domicilio.unidad;
                    domicilio.CodigoPostal = domicilio.Domicilio.codigo_postal;
                    domicilio.IdCalle = domicilio.Domicilio.ViaId;
                    domicilio.Municipio = domicilio.Domicilio.municipio;
                    domicilio.Pais = domicilio.Domicilio.pais;

                    domicilio.Domicilio = null;
                }
            }

            return ddjj.Dominios;
        }

        public IEnumerable<DDJJDominio> GetDominiosByIdUnidadTributaria(long idUT)
        {
            var dominios = _context.Dominios
                                   .IncludeFilter(x => x.TipoInscripcion)
                                   .IncludeFilter(x => x.Titulares.Where(t => t.FechaBaja == null))
                                   .IncludeFilter(x => x.Titulares.Where(t => t.FechaBaja == null).Select(t => t.TipoTitularidad))
                                   .IncludeFilter(x => x.Titulares.Where(t => t.FechaBaja == null).Select(p => p.Persona.TipoDocumentoIdentidad))
                                   .IncludeFilter(x => x.Titulares.Where(t => t.FechaBaja == null).Select(p => p.Persona.PersonaDomicilios.Where(pd => pd.FechaBaja == null)))
                                   .IncludeFilter(x => x.Titulares.Where(t => t.FechaBaja == null).Select(p => p.Persona.PersonaDomicilios.Where(pd => pd.FechaBaja == null).Select(pd => pd.Domicilio.TipoDomicilio)))
                                   .AsNoTracking()
                                   .Where(x => x.UnidadTributariaID == idUT && x.FechaBaja == null);

            return dominios.ToList().Select((d, didx) => new DDJJDominio()
            {
                IdDominio = ++didx * -1,
                Fecha = d.Fecha,
                IdTipoInscripcion = d.TipoInscripcionID,
                Inscripcion = d.Inscripcion,
                Titulares = d.Titulares
                            .Where(t => t.FechaBaja == null)
                            .Select((t, tidx) => new DDJJDominioTitular()
                            {
                                IdDominioTitular = ++tidx * -1,
                                PersonaDomicilio = t.Persona.PersonaDomicilios.Select((pd, pdidx) => new DDJJPersonaDomicilio()
                                {
                                    IdPersonaDomicilio = ++pdidx * -1,
                                    Altura = pd.Domicilio.numero_puerta,
                                    Barrio = pd.Domicilio.barrio,
                                    Calle = pd.Domicilio.ViaNombre,
                                    CodigoPostal = pd.Domicilio.codigo_postal,
                                    Departamento = pd.Domicilio.unidad,
                                    IdCalle = pd.Domicilio.ViaId,
                                    IdDomicilio = pd.DomicilioId,
                                    IdTipoDomicilio = pd.TipoDomicilioId,
                                    Localidad = pd.Domicilio.localidad,
                                    Municipio = pd.Domicilio.municipio,
                                    Pais = pd.Domicilio.pais,
                                    Piso = pd.Domicilio.piso,
                                    Provincia = pd.Domicilio.provincia,
                                    Tipo = pd.TipoDomicilio.Descripcion
                                }).ToList(),
                                IdPersona = t.PersonaID,
                                IdTipoTitularidad = t.TipoTitularidadID ?? 0,
                                NombreCompleto = t.Persona.NombreCompleto,
                                PorcientoCopropiedad = t.PorcientoCopropiedad,
                                TipoNoDocumento = $"{t.Persona.TipoDocumentoIdentidad.Descripcion} / {t.Persona.NroDocumento}",
                                TipoTitularidad = t.TipoTitularidad?.Descripcion
                            }).ToList()
            }).ToList();
        }

        public Mensura GetMensura(int idMensura)
        {
            return _context.Mensura.Find(idMensura);
        }

        public Objeto GetOAObjetoPorIdLocalidad(long idLocalidad)
        {
            return _context.Objetos.Find(idLocalidad);
        }

        public IEnumerable<OCObjeto> GetOCObjetos(int idSubtipoObjeto)
        {
            return _context.OCObjeto
                           .Where(x => x.IdSubtipoObjeto == idSubtipoObjeto)
                           .OrderBy(x => x.Nombre)
                           .ToList();
        }

        public List<DDJJPersonaDomicilio> GetPersonaDomicilios(long idPersona)
        {
            return (from perDom in _context.PersonaDomicilio
                    join dom in _context.Domicilios on perDom.DomicilioId equals dom.DomicilioId
                    join tipoDom in _context.TipoDomicilio on dom.TipoDomicilioId equals tipoDom.TipoDomicilioId
                    where perDom.PersonaId == idPersona && perDom.FechaBaja == null
                    select new DDJJPersonaDomicilio
                    {
                        IdDomicilio = dom.DomicilioId,
                        Altura = dom.numero_puerta,
                        Barrio = dom.barrio,
                        Calle = dom.ViaNombre,
                        CodigoPostal = dom.codigo_postal,
                        Departamento = dom.unidad,
                        IdCalle = dom.ViaId,
                        IdTipoDomicilio = dom.TipoDomicilioId,
                        Localidad = dom.localidad,
                        Municipio = dom.municipio,
                        Pais = dom.pais,
                        Piso = dom.piso,
                        Provincia = dom.provincia,
                        Tipo = tipoDom.Descripcion
                    }).ToList();
        }

        public List<DDJJSorCar> GetSorCar(long idSor)
        {
            throw new NotImplementedException("Arreglar este quilombo");
            //return _context.DDJJSorCar
            //               .Include("AptCar")
            //               .Include(x => x.Caracteristica)
            //               .Where(x => x.IdSor == idSor && !x.FechaBaja.HasValue)
            //               .ToList();
        }

        public IEnumerable<DDJJSorOtrasCar> GetSorOtrasCar(int idVersion)
        {
            return _context.DDJJSorOtrasCar.Where(x => x.IdVersion == idVersion).ToList();
        }

        public IEnumerable<DDJJSorTipoCaracteristica> GetSorTipoCaracteristicas(long idAptitud)
        {
            var filtro = _context.VALAptitudes
                                 .Include(a => a.GruposCaracteristicas)
                                 .Where(x => x.IdAptitud == idAptitud)
                                 .SelectMany(a => a.GruposCaracteristicas
                                                   .Select(g => g.IdGrupoCaracteristica));

            var asd = _context.DDJJSorTiposCaracteristica
                           .IncludeFilter(x => x.GrupoCaracteristica)
                           .IncludeFilter(x => x.Caracteristicas.Where(c => c.FechaBaja == null))
                           .Where(x => filtro.Contains(x.IdGrupoCaracteristica))
                           .ToList();

            return asd;
        }

        public List<VALSuperficie> GetValSuperficies(long id)
        {
            var superficiesValuacion = _context.DDJJ
                                               .IncludeFilter(d => d.Sor)
                                               .IncludeFilter(d => d.Sor.Superficies.Where(s => s.FechaBaja == null))
                                               .IncludeFilter(d => d.Sor.Superficies.Where(s => s.FechaBaja == null).Select(s => s.Aptitud))
                                               .IncludeFilter(d => d.Sor.Superficies.Where(s => s.FechaBaja == null).Select(s => s.Caracteristicas))
                                               .IncludeFilter(d => d.Sor.Superficies.Where(s => s.FechaBaja == null).Select(s => s.Caracteristicas.Select(c => c.Caracteristica)))
                                               .AsNoTracking()
                                               .Single(d => d.IdDeclaracionJurada == id)
                                               .Sor.Superficies.ToList();

            //var tiposCaracteristicas = superficiesValuacion.Select(d => d.Sor.Superficies.Select(s => new { aptitud = s.IdAptitud, tipos = s.Caracteristicas.Select(c => c.Caracteristica.IdSorTipoCaracteristica) }));

            return superficiesValuacion;
        }

        public VALValuacion GetValuacion(long id)
        {
            return _context.VALValuacion
                           .Include(x => x.DeclaracionJurada.Sor.Superficies.Select(s => s.Caracteristicas))
                           .Include(x => x.UnidadTributaria.Parcela)
                           .SingleOrDefault(x => x.IdValuacion == id);
        }

        public VALValuacion GetValuacionVigenteConsolidada(long idParcela, bool esHistorico = false)
        {
            var utUF = (long)TipoUnidadTributariaEnum.UnidadFuncionalPH;

            return (from val in _context.VALValuacion
                    join ut in _context.UnidadesTributarias on val.IdUnidadTributaria equals ut.UnidadTributariaId
                    where ut.ParcelaID == idParcela && val.FechaHasta == null && val.FechaBaja == null
                            && (esHistorico || ut.FechaBaja == null) && ut.TipoUnidadTributariaID != utUF
                    orderby val.FechaDesde descending, val.FechaAlta descending
                    select val)
                    .AsNoTracking()
                    .Include(x => x.ValuacionDecretos.Select(vd => vd.Decreto))
                    .FirstOrDefault();
        }

        public List<VALValuacion> GetValuaciones(long idUnidadTributaria)
        {
            return _context.VALValuacion
                           .Include("ValuacionDecretos")
                           .Include("ValuacionDecretos.Decreto")
                           .Where(x => x.IdUnidadTributaria == idUnidadTributaria && !x.FechaBaja.HasValue)
                           .OrderByDescending(x => x.FechaDesde)
                           .ToList();
        }

        public List<VALValuacion> GetValuacionesHistoricas(long idUnidadTributaria)
        {
            return _context.VALValuacion
                           .Where(x => x.IdUnidadTributaria == idUnidadTributaria &&
                                      !x.FechaBaja.HasValue && x.FechaHasta.HasValue)
                           .OrderByDescending(x => new { x.FechaDesde, x.FechaAlta })
                           .Include(v => v.ValuacionDecretos.Select(d => d.Decreto))
                           .Include(v => v.DeclaracionJurada)
                           .ToList();
        }

        public VALValuacion GetValuacionVigente(long idUnidadTributaria)
        {
            return (from val in _context.VALValuacion
                    where val.IdUnidadTributaria == idUnidadTributaria && val.FechaHasta == null && val.FechaBaja == null
                    orderby val.FechaDesde descending, val.FechaAlta descending
                    select val)
                    .Include(v => v.UnidadTributaria.Parcela)
                    .Include(v => v.ValuacionDecretos.Select(vd => vd.Decreto))
                    .Include(v => v.DeclaracionJurada)
                    .AsNoTracking()
                    .FirstOrDefault();
        }

        public DDJJVersion GetVersion(int idVersion)
        {
            return _context.DDJJVersion.Find(idVersion);
        }

        public IEnumerable<DDJJVersion> GetVersiones()
        {
            try
            {
                var versiones = from version in _context.DDJJVersion
                                where version.Habilitado == 1 && version.FechaBaja == null &&
                                        (version.FechaDesde == null || version.FechaDesde.Value <= DateTime.Now) &&
                                        (version.FechaHasta == null || version.FechaHasta.Value >= DateTime.Now)
                                select version;

                return versiones.ToList();
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("GetVersiones", ex);
                return null;
            }
        }

        public bool Revaluar(long idUnidadTributaria, long idUsuario, string machineName, string ip)
        {
            return GenerarValuacion(ObtenerDDJJTierraVigente(idUnidadTributaria),
                                    idUnidadTributaria, TipoValuacionEnum.Revaluacion,
                                    idUsuario, machineName, ip);
        }

        public bool SaveDDJJSor(DDJJ ddjj, DDJJSor ddjjSor, DDJJDesignacion ddjjDesignacion, List<DDJJDominio> dominios, List<DDJJSorCar> sorCar, List<VALSuperficie> superficies, long idUsuario, string machineName, string ip)
        {
            throw new NotImplementedException("Arreglar este quilombo");
            //try
            //{
            //    DDJJ ddjjActual = null;
            //    DateTime fechaActual = DateTime.Now;

            //    using (var transaction = this._context.Database.BeginTransaction())
            //    {
            //        try
            //        {
            //            string evento = Eventos.ModificarDDJJSoR;
            //            string tipoOperacion = TiposOperacion.Modificacion;
            //            bool esNueva = false;
            //            if (ddjj.IdDeclaracionJurada == 0)
            //            {
            //                evento = Eventos.AltaDDJJSoR;
            //                tipoOperacion = TiposOperacion.Alta;
            //                esNueva = true;

            //                ddjj.IdUsuarioAlta = idUsuario;
            //                ddjj.FechaAlta = fechaActual;
            //                ddjj.IdOrigen = (int)OrigenEnum.Presentada;
            //                ddjjActual = ddjj;
            //            }
            //            else
            //            {
            //                ddjjActual = _context.DDJJ
            //                    .Include("Sor")
            //                    .Include("Designacion")
            //                    .Include("Dominios")
            //                    .Include(x => x.Dominios.Select(y => y.Titulares))
            //                    .Include(x => x.Dominios.Select(y => y.Titulares.Select(z => z.PersonaDomicilio)))
            //                    .Include(x => x.Sor.Select(y => y.Superficies))
            //                    .Include(x => x.Sor.Select(y => y.Superficies.Select(s => s.Caracteristicas)))
            //                    .FirstOrDefault(x => x.IdDeclaracionJurada == ddjj.IdDeclaracionJurada);

            //                ddjjActual.IdOrigen = (int)OrigenEnum.Presentada;
            //                ddjjActual.IdPoligono = ddjj.IdPoligono;
            //                ddjjActual.FechaVigencia = ddjj.FechaVigencia.Value.Date;
            //            }
            //            ddjjActual.FechaModif = fechaActual;
            //            ddjjActual.IdUsuarioModif = idUsuario;

            //            if (ddjjSor.IdSor == 0)
            //            {
            //                ddjjSor.IdUsuarioAlta = ddjjSor.IdUsuarioModif = idUsuario;
            //                ddjjSor.FechaAlta = ddjjSor.FechaModif = fechaActual;
            //                ddjjActual.Sor = new List<DDJJSor>();

            //                foreach (var sc in sorCar)
            //                {
            //                    sc.IdUsuarioAlta = sc.IdUsuarioModif = idUsuario;
            //                    sc.FechaAlta = sc.FechaModif = fechaActual;
            //                }

            //                ddjjSor.SorCar = sorCar;

            //                foreach (var s in superficies)
            //                {
            //                    s.IdUsuarioAlta = s.IdUsuarioModif = idUsuario;
            //                    s.FechaAlta = s.FechaModif = fechaActual;
            //                }

            //                ddjjSor.Superficies = superficies;

            //                ddjjActual.Sor.Add(ddjjSor);

            //            }
            //            else
            //            {
            //                DDJJSor sorActual = ddjjActual.Sor.First();
            //                sorActual.FechaModif = fechaActual;
            //                sorActual.IdUsuarioModif = idUsuario;
            //                sorActual.IdCamino = ddjjSor.IdCamino;
            //                sorActual.IdLocalidad = ddjjSor.IdLocalidad;
            //                sorActual.IdMensura = ddjjSor.IdMensura;
            //                sorActual.NumeroHabitantes = ddjjSor.NumeroHabitantes;
            //                sorActual.DistanciaCamino = ddjjSor.DistanciaCamino;
            //                sorActual.DistanciaEmbarque = ddjjSor.DistanciaEmbarque;
            //                sorActual.DistanciaLocalidad = ddjjSor.DistanciaLocalidad;

            //                sorActual.SorCar = sorActual.SorCar ?? new List<DDJJSorCar>();

            //                if (sorActual.Superficies == null || sorActual.Superficies.Count == 0)
            //                {
            //                    sorActual.Superficies = new List<VALSuperficie>();

            //                    foreach (var sup in superficies)
            //                    {
            //                        sup.IdUsuarioAlta = sup.IdUsuarioModif = idUsuario;
            //                        sup.FechaAlta = sup.FechaModif = fechaActual;
            //                        sup.IdSor = sorActual.IdSor;
            //                        sorActual.Superficies.Add(sup);
            //                    }
            //                }
            //                else
            //                {
            //                    foreach (var sup in sorActual.Superficies)
            //                    {
            //                        double? superficie = superficies.FirstOrDefault(x => x.IdAptitud == sup.IdAptitud).Superficie.Value;

            //                        if (sup.Superficie != superficie)
            //                        {
            //                            sup.IdUsuarioModif = idUsuario;
            //                            sup.FechaModif = fechaActual;
            //                            sup.Superficie = superficie;
            //                        }
            //                    }

            //                    var newSuperficies = superficies.Where(x => !sorActual.Superficies.Any(y => x.IdAptitud == y.IdAptitud)).ToList();
            //                    foreach (var newSuperficie in newSuperficies)
            //                    {
            //                        newSuperficie.IdUsuarioAlta = newSuperficie.IdUsuarioModif = idUsuario;
            //                        newSuperficie.FechaAlta = newSuperficie.FechaModif = fechaActual;
            //                        sorActual.Superficies.Add(newSuperficie);
            //                    }
            //                }

            //                var deletedSorCar = sorActual.SorCar.Where(x => !x.FechaBaja.HasValue && sorCar.All(y => y.IdSorCar != x.IdSorCar)).ToList();
            //                foreach (var dSorCar in deletedSorCar)
            //                {
            //                    dSorCar.IdUsuarioBaja = idUsuario;
            //                    dSorCar.FechaBaja = fechaActual;
            //                }

            //                var activeSorCar = sorActual.SorCar.Where(x => !x.FechaBaja.HasValue).ToList();
            //                var newSorCar = sorCar.Where(x => activeSorCar.All(y => y.IdSorCar != x.IdSorCar)).ToList();

            //                foreach (var nSorCar in newSorCar)
            //                {
            //                    nSorCar.IdUsuarioAlta = nSorCar.IdUsuarioModif = idUsuario;
            //                    nSorCar.FechaAlta = nSorCar.FechaModif = fechaActual;
            //                    nSorCar.IdSor = sorActual.IdSor;
            //                    sorActual.SorCar.Add(nSorCar);
            //                }
            //            }

            //            if (ddjjDesignacion.IdDesignacion == 0)
            //            {
            //                ddjjDesignacion.IdUsuarioAlta = ddjjDesignacion.IdUsuarioModif = idUsuario;
            //                ddjjDesignacion.FechaAlta = ddjjDesignacion.FechaModif = fechaActual;
            //                ddjjDesignacion.IdTipoDesignador = (long)TipoDesignadorEnum.Catastro;
            //                ddjjActual.Designacion = new List<DDJJDesignacion>() { { ddjjDesignacion } };
            //            }
            //            else
            //            {
            //                var designacionActual = ddjjActual.Designacion.First();
            //                designacionActual.FechaModif = fechaActual;
            //                designacionActual.IdUsuarioModif = idUsuario;
            //                designacionActual.Fraccion = ddjjDesignacion.Fraccion;
            //                designacionActual.IdBarrio = ddjjDesignacion.IdBarrio;
            //                designacionActual.IdCalle = ddjjDesignacion.IdCalle;
            //                designacionActual.IdDepartamento = ddjjDesignacion.IdDepartamento;
            //                designacionActual.IdLocalidad = ddjjDesignacion.IdLocalidad;
            //                designacionActual.IdManzana = ddjjDesignacion.IdManzana;
            //                designacionActual.IdParaje = ddjjDesignacion.IdParaje;
            //                designacionActual.IdSeccion = ddjjDesignacion.IdSeccion;
            //                designacionActual.IdTipoDesignador = (long)TipoDesignadorEnum.Catastro;
            //                designacionActual.Barrio = ddjjDesignacion.Barrio;
            //                designacionActual.Calle = ddjjDesignacion.Calle;
            //                designacionActual.Chacra = ddjjDesignacion.Chacra;
            //                designacionActual.CodigoPostal = ddjjDesignacion.CodigoPostal;
            //                designacionActual.Departamento = ddjjDesignacion.Departamento;
            //                designacionActual.Fraccion = ddjjDesignacion.Fraccion;
            //                designacionActual.Localidad = ddjjDesignacion.Localidad;
            //                designacionActual.Lote = ddjjActual.IdPoligono;
            //                designacionActual.Manzana = ddjjDesignacion.Manzana;
            //                designacionActual.Numero = ddjjDesignacion.Numero;
            //                designacionActual.Paraje = ddjjDesignacion.Paraje;
            //                designacionActual.Quinta = ddjjDesignacion.Quinta;
            //                designacionActual.Seccion = ddjjDesignacion.Seccion;
            //            }

            //            if (ddjj.IdDeclaracionJurada == 0)
            //            {
            //                if (ddjj.Dominios == null)
            //                {
            //                    ddjj.Dominios = new List<DDJJDominio>();
            //                }

            //                this.SaveDominios(ddjj, dominios, idUsuario, fechaActual);

            //                this._context.DDJJ.Add(ddjj);
            //            }
            //            else
            //            {
            //                if (ddjjActual.Dominios == null)
            //                {
            //                    ddjjActual.Dominios = new List<DDJJDominio>();
            //                }

            //                this.SaveDominios(ddjjActual, dominios, idUsuario, fechaActual);
            //            }

            //            var auditorias = new List<Auditoria>()
            //            {
            //                {auditar(ddjj, evento, tipoOperacion, machineName, ip) }
            //            };
            //            var lstFechasValuables = new HashSet<DateTime?>() { ddjj.FechaVigencia };
            //            if (esNueva)
            //            {
            //                long versionSoR = long.Parse(VersionesDDJJ.SoR);
            //                var query = from dj in _context.DDJJ
            //                            where dj.IdUnidadTributaria == ddjj.IdUnidadTributaria &&
            //                                  dj.FechaBaja == null && dj.IdVersion == versionSoR
            //                            join val in (from vv in _context.VALValuacion
            //                                         where vv.IdUnidadTributaria == ddjj.IdUnidadTributaria && vv.FechaBaja == null
            //                                         group vv.FechaDesde < ddjj.FechaVigencia by vv.IdDeclaracionJurada into grp
            //                                         select new
            //                                         {
            //                                             IdDeclaracionJurada = grp.Key,
            //                                             tiene_no_afectadas = grp.Any(x => x),
            //                                             tiene_afectadas = grp.Any(x => !x)
            //                                         }) on dj.IdDeclaracionJurada equals val.IdDeclaracionJurada
            //                            where val.tiene_afectadas
            //                            orderby dj.FechaVigencia
            //                            select new { dj.IdDeclaracionJurada, Eliminable = !val.tiene_no_afectadas };

            //                foreach (var ddjjProcesable in query.ToList())
            //                {
            //                    var ddjjPosterior = _context.DDJJ
            //                                                .IncludeFilter(x => x.Sor.Where(s => s.FechaBaja == null))
            //                                                .IncludeFilter(x => x.Sor.Where(s => s.FechaBaja == null).Select(s => s.Superficies.Where(sup => sup.FechaBaja == null)))
            //                                                .IncludeFilter(x => x.Sor.Where(s => s.FechaBaja == null).Select(s => s.SorCar.Where(sc => sc.FechaBaja == null)))
            //                                                .IncludeFilter(x => x.Designacion.Where(d => d.FechaBaja == null))
            //                                                .IncludeFilter(x => x.Dominios.Where(d => d.FechaBaja == null))
            //                                                .IncludeFilter(x => x.Dominios.Where(d => d.FechaBaja == null).Select(d => d.Titulares.Where(t => t.FechaBaja == null)))
            //                                                .IncludeFilter(x => x.Valuaciones.Where(v => v.FechaBaja == null && v.FechaDesde >= ddjj.FechaVigencia))
            //                                                .Single(x => x.IdDeclaracionJurada == ddjjProcesable.IdDeclaracionJurada);

            //                    foreach (var valuacion in ddjjPosterior.Valuaciones.OrderBy(x => x.FechaDesde))
            //                    {
            //                        /* 
            //                         * no incluyo la que tenga como fecha de vigencia la que se está cargando 
            //                         * porque ya la estoy agregando como primera de la lista.
            //                         */
            //                        if (valuacion.FechaDesde != ddjj.FechaVigencia)
            //                        {
            //                            lstFechasValuables.Add(valuacion.FechaDesde);
            //                        }
            //                        valuacion.FechaBaja = valuacion.FechaModif = fechaActual;
            //                        valuacion.IdUsuarioBaja = valuacion.IdUsuarioModif = idUsuario;
            //                    }

            //                    if (!ddjjProcesable.Eliminable) continue;

            //                    ddjjPosterior.FechaBaja = ddjjPosterior.FechaModif = fechaActual;
            //                    ddjjPosterior.IdUsuarioBaja = ddjjPosterior.IdUsuarioModif = idUsuario;

            //                    foreach (var sor in ddjjPosterior.Sor)
            //                    {
            //                        sor.FechaBaja = sor.FechaModif = fechaActual;
            //                        sor.IdUsuarioBaja = sor.IdUsuarioModif = idUsuario;

            //                        foreach (var superficie in sor.Superficies)
            //                        {
            //                            superficie.FechaBaja = superficie.FechaModif = fechaActual;
            //                            superficie.IdUsuarioBaja = superficie.IdUsuarioModif = idUsuario;
            //                        }
            //                        foreach (var car in sor.SorCar)
            //                        {
            //                            car.FechaBaja = car.FechaModif = fechaActual;
            //                            car.IdUsuarioBaja = car.IdUsuarioModif = idUsuario;
            //                        }
            //                    }

            //                    foreach (var designacion in ddjjPosterior.Designacion)
            //                    {
            //                        designacion.FechaBaja = designacion.FechaModif = fechaActual;
            //                        designacion.IdUsuarioBaja = designacion.IdUsuarioModif = idUsuario;
            //                    }

            //                    foreach (var dominio in ddjjPosterior.Dominios)
            //                    {
            //                        dominio.FechaBaja = dominio.FechaModif = fechaActual;
            //                        dominio.IdUsuarioBaja = dominio.IdUsuarioModif = idUsuario;

            //                        foreach (var titular in dominio.Titulares)
            //                        {
            //                            titular.FechaBaja = titular.FechaModif = fechaActual;
            //                            titular.IdUsuarioBaja = titular.IdUsuarioModif = idUsuario;
            //                        }
            //                    }
            //                }
            //            }

            //            this._context.SaveChanges(auditorias);

            //            var ut = _context.UnidadesTributarias.Find(ddjj.IdUnidadTributaria);

            //            /*
            //                BORRADO LOGICO de Dominios y Dominio_titular todo junto
            //            */
            //            foreach (var domi in _context.Dominios.Include(x => x.Titulares).Where(x => x.UnidadTributariaID == ut.UnidadTributariaId).ToList())
            //            {
            //                // DominioTitular por cada Dominio
            //                foreach (var domi_ti in domi.Titulares)
            //                {
            //                    // Baja logica por cada dominio_titular por cada dominio
            //                    domi_ti.FechaBaja = fechaActual;
            //                    domi_ti.UsuarioBajaID = idUsuario;
            //                }
            //                // Baja logica inm_dominio
            //                domi.FechaBaja = fechaActual;
            //                domi.IdUsuarioBaja = idUsuario;
            //            }

            //            /*
            //                INM_PERSONA_DOMICILIO
            //             */
            //            // Este ya tiene los datos acá asi que no hacemos busqueda
            //            foreach (var dominio in dominios)
            //            {
            //                // Alta de inm_dominio
            //                Dominio new_domi = _context.Dominios.Add(new Dominio()
            //                {
            //                    Inscripcion = dominio.Inscripcion,
            //                    TipoInscripcionID = dominio.IdTipoInscripcion,
            //                    Fecha = dominio.Fecha,
            //                    UnidadTributariaID = ut.UnidadTributariaId,
            //                    FechaAlta = fechaActual,
            //                    FechaModif = fechaActual,
            //                    IdUsuarioAlta = idUsuario,
            //                    IdUsuarioModif = idUsuario,
            //                    Titulares = new List<DominioTitular>()
            //                });

            //                foreach (var titular in dominio.Titulares)
            //                {

            //                    // Alta de inm_dominio_titular
            //                    new_domi.Titulares.Add(new DominioTitular()
            //                    {
            //                        PersonaID = titular.IdPersona,
            //                        TipoTitularidadID = titular.IdTipoTitularidad,
            //                        UsuarioAltaID = idUsuario,
            //                        UsuarioModificacionID = idUsuario,
            //                        FechaAlta = fechaActual,
            //                        FechaModificacion = fechaActual,
            //                        PorcientoCopropiedad = titular.PorcientoCopropiedad

            //                    });

            //                    foreach (var per_dom in titular.PersonaDomicilio)
            //                    {
            //                        //chequear si existe ya en inm_persona_domicilio
            //                        if (per_dom.Domicilio != null)
            //                        {
            //                            var count_inm_persona_domicilio = _context.PersonaDomicilio.Where(x => x.DomicilioId == per_dom.Domicilio.DomicilioId && x.TipoDomicilioId == per_dom.Domicilio.TipoDomicilioId && titular.IdPersona == x.PersonaId).Count();
            //                            if (count_inm_persona_domicilio == 0)
            //                            {
            //                                // Alta de inm_persona_domicilio SIN baja logica
            //                                _context.PersonaDomicilio.Add(new BusinessEntities.Personas.PersonaDomicilio()
            //                                {
            //                                    PersonaId = titular.IdPersona,
            //                                    DomicilioId = per_dom.Domicilio.DomicilioId,
            //                                    TipoDomicilioId = per_dom.Domicilio.TipoDomicilioId,
            //                                    FechaAlta = fechaActual,
            //                                    UsuarioAltaId = idUsuario,
            //                                    FechaModif = fechaActual,
            //                                    UsuarioModifId = idUsuario
            //                                });
            //                            }
            //                        }
            //                    }
            //                }
            //            }

            //            /*
            //                INM_DESIGNACION
            //            */
            //            foreach (var d in _context.Designacion.Where(x => x.IdParcela == ut.ParcelaID).ToList())
            //            {
            //                if (d != null)
            //                {
            //                    // Baja logica INM_DESIGNACION
            //                    d.FechaBaja = fechaActual;
            //                    d.IdUsuarioBaja = idUsuario;
            //                }
            //            }

            //            // Adicion de INM_DESIGNACION
            //            _context.Designacion.Add(new Designacion()
            //            {
            //                FechaAlta = fechaActual,
            //                IdUsuarioAlta = idUsuario,
            //                FechaModif = fechaActual,
            //                IdUsuarioModif = idUsuario,
            //                Fraccion = ddjjDesignacion.Fraccion,
            //                IdBarrio = ddjjDesignacion.IdBarrio,
            //                IdCalle = ddjjDesignacion.IdCalle,
            //                IdDepartamento = ddjjDesignacion.IdDepartamento,
            //                IdLocalidad = ddjjDesignacion.IdLocalidad,
            //                IdManzana = ddjjDesignacion.IdManzana,
            //                IdParaje = ddjjDesignacion.IdParaje,
            //                IdSeccion = ddjjDesignacion.IdSeccion,
            //                IdTipoDesignador = (short)TipoDesignadorEnum.Catastro,
            //                Barrio = ddjjDesignacion.Barrio,
            //                Calle = ddjjDesignacion.Calle,
            //                Chacra = ddjjDesignacion.Chacra,
            //                CodigoPostal = ddjjDesignacion.CodigoPostal,
            //                Departamento = ddjjDesignacion.Departamento,
            //                Localidad = ddjjDesignacion.Localidad,
            //                Lote = ddjjActual.IdPoligono,
            //                Manzana = ddjjDesignacion.Manzana,
            //                Numero = ddjjDesignacion.Numero,
            //                Paraje = ddjjDesignacion.Paraje,
            //                Quinta = ddjjDesignacion.Quinta,
            //                Seccion = ddjjDesignacion.Seccion,
            //                IdParcela = ut.ParcelaID.Value
            //            });

            //            // Modificacion de INM_PARCELA
            //            double superficie_p = (from vd in _context.DDJJ
            //                                   join vds in _context.DDJJSor on vd.IdDeclaracionJurada equals vds.IdDeclaracionJurada
            //                                   join vs in _context.VALSuperficies on vds.IdSor equals vs.IdSor
            //                                   where vd.IdDeclaracionJurada == ddjj.IdDeclaracionJurada
            //                                   select vs.Superficie.Value).Sum();

            //            ut.Superficie = superficie_p;
            //            ut.Parcela.Superficie = (decimal)ut.Superficie;
            //            ut.Parcela.PlanoId = ddjj.IdPoligono;

            //            ut.FechaModificacion = ut.Parcela.FechaModificacion = fechaActual;
            //            ut.UsuarioModificacionID = ut.Parcela.UsuarioModificacionID = idUsuario;

            //            this._context.SaveChanges();

            //            foreach (var fechaValuable in lstFechasValuables.OrderBy(x => x))
            //            {
            //                ddjj.FechaVigencia = fechaValuable;
            //                GenerarValuacion(ddjj, ddjj.IdUnidadTributaria, TipoValuacionEnum.Sor, idUsuario, machineName, ip);
            //            }
            //            ddjj.FechaVigencia = lstFechasValuables.Min();
            //            this._context.SaveChanges();
            //            transaction.Commit();
            //        }
            //        catch (Exception)
            //        {
            //            transaction.Rollback();
            //            throw;
            //        }
            //        try
            //        {
            //            new ParcelaRepository(_context).RefreshVistaMaterializadaParcela();
            //        }
            //        catch (Exception ex)
            //        {
            //            _context.GetLogger().LogError("SaveDDJJSoR-RefreshVistaMateiralizada", ex);
            //        }
            //        return true;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _context.GetLogger().LogError("SaveDDJJSoR", ex);
            //    throw;
            //}
        }

        public bool SaveValuacion(VALValuacion valuacion, long idUsuario)
        {
            try
            {
                DateTime fechaActual = DateTime.Now;
                if (valuacion.IdValuacion > 0)
                {
                    var valuacionActual = _context.VALValuacion.Find(valuacion.IdValuacion);
                    valuacionActual.FechaDesde = valuacion.FechaDesde;
                    valuacionActual.ValorTierra = valuacion.ValorTierra;
                    valuacionActual.ValorTotal = valuacion.ValorTotal;
                    valuacionActual.Superficie = valuacion.Superficie;
                    valuacionActual.IdUsuarioModif = idUsuario;
                    valuacionActual.FechaModif = fechaActual;
                }
                else
                {
                    valuacion.IdUsuarioAlta = valuacion.IdUsuarioModif = idUsuario;
                    valuacion.FechaAlta = valuacion.FechaModif = fechaActual;
                    _context.VALValuacion.Add(valuacion);

                    TerminarValuacionesVigentes(valuacion);
                }
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("Save Valuacion", ex);
                return false;
            }
        }

        private VALValuacion ObtenerValuacion(DDJJ ddjj, UnidadTributaria ut, TipoValuacionEnum tipoValuacion, IEnumerable<VALDecreto> decretos, long idUsuario)
        {
            //DateTime ahora = DateTime.Now;

            //#region Calculo el valor de tierra de ser necesario
            //decimal valorTierra = ObtenerValorTierra(ddjj, (TipoParcelaEnum)ut.Parcela.TipoParcelaID, tipoValuacion, out double superficie, out DDJJ ddjjTierraVigente);
            //#endregion

            //var valuacion = new VALValuacion()
            //{
            //    IdDeclaracionJurada = ddjjTierraVigente.IdDeclaracionJurada,
            //    IdUnidadTributaria = ut.UnidadTributariaId,
            //    DeclaracionJurada = TipoValuacionEnum.Revaluacion != tipoValuacion ? ddjj : null,
            //    FechaAlta = ahora,
            //    FechaModif = ahora,
            //    IdUsuarioAlta = idUsuario,
            //    IdUsuarioModif = idUsuario,

            //    Superficie = superficie,
            //    FechaDesde = ddjjTierraVigente.FechaVigencia.GetValueOrDefault()
            //};

            //#region Aplico coeficientes de decretos de ser necesario
            //if ((decretos ?? new VALDecreto[0]).Any())
            //{
            //    valuacion.ValuacionDecretos = valuacion.ValuacionDecretos ?? new List<VALValuacionDecreto>();
            //    foreach (var decreto in decretos)
            //    {
            //        valuacion.ValuacionDecretos.Add(new VALValuacionDecreto()
            //        {
            //            IdDecreto = decreto.IdDecreto,
            //            IdUsuarioAlta = valuacion.IdUsuarioAlta,
            //            IdUsuarioModif = valuacion.IdUsuarioAlta,
            //            FechaAlta = valuacion.FechaAlta,
            //            FechaModif = valuacion.FechaAlta
            //        });
            //        valorTierra *= Convert.ToDecimal(decreto.Coeficiente ?? 1);
            //    }
            //}
            //#endregion

            //#region Aplico valores de tierra
            //valuacion.ValorTotal = valuacion.ValorTierra = valorTierra;
            //#endregion

            //return _context.VALValuacion.Add(valuacion);
            return null;
        }

        private decimal ObtenerValorTierra(DDJJ ddjj, TipoParcelaEnum tipoParcela, TipoValuacionEnum tipoValuacion, out double superficieParcela, out DDJJ ddjjTierraVigente)
        {
            superficieParcela = 0;
            ddjjTierraVigente = ddjj;

            //decimal valorTierra = 0;

            //var ddjjSor = ddjjTierraVigente.Sor.Single();

            //superficieParcela = ddjjSor.Superficies.Sum(x => x.Superficie) ?? 0;

            //double puntajeEmplazamiento = 0;
            //if (ddjjSor.IdCamino.HasValue && ddjjSor.DistanciaCamino.HasValue)
            //{
            //    var puntajeCamino = _context.VALPuntajesCaminos.FirstOrDefault(x => x.IdCamino == ddjjSor.IdCamino && x.DistanciaMinima <= ddjjSor.DistanciaCamino && x.DistanciaMaxima >= ddjjSor.DistanciaCamino && !x.IdUsuarioBaja.HasValue);
            //    puntajeEmplazamiento = puntajeCamino?.Puntaje ?? 0;
            //}

            //if (ddjjSor.DistanciaEmbarque.HasValue)
            //{
            //    var puntajeEmbarque = _context.VALPuntajesEmbarques.FirstOrDefault(x => x.DistanciaMinima <= ddjjSor.DistanciaEmbarque && x.DistanciaMaxima >= ddjjSor.DistanciaEmbarque && !x.IdUsuarioBaja.HasValue);
            //    puntajeEmplazamiento += puntajeEmbarque?.Puntaje ?? 0;
            //}

            //if (ddjjSor.IdLocalidad.HasValue && ddjjSor.DistanciaLocalidad.HasValue)
            //{
            //    var puntajeLocalidad = _context.VALPuntajesLocalidades.FirstOrDefault(x => x.IdLocalidad == ddjjSor.IdLocalidad && x.DistanciaMinima <= ddjjSor.DistanciaLocalidad && x.DistanciaMaxima >= ddjjSor.DistanciaLocalidad && !x.IdUsuarioBaja.HasValue);
            //    puntajeEmplazamiento += puntajeLocalidad?.Puntaje ?? 0;
            //}

            //var designacion = ddjjTierraVigente.Designacion.FirstOrDefault();

            //double valores_parciales = 0;
            //foreach (var sup in ddjjSor.Superficies)
            //{
            //    long subtotal_puntaje = ddjjSor.SorCar.Where(x => x.FechaBaja == null && x.AptCar.IdAptitud == sup.IdAptitud).Sum(x => x.AptCar.Puntaje);
            //    valores_parciales += (sup.Superficie ?? 0) * ((puntajeEmplazamiento + subtotal_puntaje) / 100);
            //}

            //double? valor_optimo = null;
            //if (TipoParcelaEnum.Suburbana == tipoParcela)
            //{
            //    double superficie = superficieParcela;
            //    var valor = _context.VALValoresOptSuburbanos.FirstOrDefault(x => x.IdLocalidad == designacion.IdLocalidad && x.SuperficieMinima <= superficie && x.SuperficieMaxima >= superficie && !x.IdUsuarioBaja.HasValue);
            //    valor_optimo = valor?.Valor;
            //}
            //else if (TipoParcelaEnum.Rural == tipoParcela)
            //{
            //    var valor = _context.VALValoresOptRurales.FirstOrDefault(x => x.IdDepartamento == designacion.IdDepartamento && !x.IdUsuarioBaja.HasValue);
            //    valor_optimo = valor?.Valor;
            //}

            //return (decimal)(valores_parciales * (valor_optimo ?? 1));
            return 0;
        }

        private DDJJ ObtenerDDJJTierraVigente(long idUnidadTributaria)
        {
            long ddjjVersionSoR = long.Parse(VersionesDDJJ.SoR);

            return _context.DDJJ
                           .Include(d => d.Sor)
                           .Include(d => d.Sor.Superficies.Select(x => x.Aptitud))
                           .Include(d => d.Sor.Superficies.Select(x => x.Caracteristicas.Select(c => c.Caracteristica)))
                           .Include(d => d.Valuaciones)
                           //.Include(d => d.Designacion)
                           .Include(d => d.Dominios.Select(dom => dom.Titulares))
                           .OrderByDescending(d => d.Valuaciones.Select(v => new { v.FechaDesde, v.FechaHasta }))
                           .Single(d => d.IdUnidadTributaria == idUnidadTributaria && d.IdVersion == ddjjVersionSoR);
        }

        public IEnumerable<TipoTitularidad> GetTiposTitularidad()
        {
            return _context.TiposTitularidad.ToList();
        }

        public async Task<bool> SaveFormularioAsync(long idUnidadTributaria, DatosFormulario formulario)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                DateTime now = DateTime.Now;

                var ddjj = new DDJJ()
                {
                    IdVersion = Convert.ToInt64(VersionesDDJJ.SoR),
                    FechaVigencia = now.Date,
                    IdUnidadTributaria = idUnidadTributaria,
                    Sor = new DDJJSor()
                    {
                        Superficies = formulario.Superficies,
                    },
                };

                ddjj.IdUsuarioModif = ddjj.IdUsuarioAlta = formulario.IdUsuarioOperacion;
                ddjj.FechaModif = ddjj.FechaAlta = now;
                ddjj.Sor.IdUsuarioModif = ddjj.Sor.IdUsuarioAlta = formulario.IdUsuarioOperacion;
                ddjj.Sor.FechaModif = ddjj.Sor.FechaAlta = now;

                foreach (var superficie in formulario.Superficies)
                {
                    superficie.IdUsuarioModif = superficie.IdUsuarioAlta = formulario.IdUsuarioOperacion;
                    superficie.FechaModif = superficie.FechaAlta = now;
                    foreach (var caracteristica in superficie.Caracteristicas)
                    {
                        caracteristica.IdUsuarioModif = caracteristica.IdUsuarioAlta = formulario.IdUsuarioOperacion;
                        caracteristica.FechaModif = caracteristica.FechaAlta = now;
                    }
                }

                var validator = new DDJJValidator(ddjj)
                                       .Validate(new SuperficiesValidation())
                                       .Validate(new SuperficieParcelaRuralValidation(_context))
                                       .Validate(new CaracteristicasValidation(_context));

                var generator = new Generator(new TierraRuralComputation(_context), validator);
                var valuacion = _context.VALValuacion.Add(await generator.Generate());

                TerminarValuacionesVigentes(valuacion);
                try
                {
                    _context.SaveChanges(auditar(ddjj, Eventos.NuevaValuacion, TiposOperacion.Alta, formulario.MachineName, formulario.IP));
                    transaction.Commit();
                }
                catch (Exception)
                {
                    throw;
                }

                return true;
            }
        }

        public bool DeleteValuacion(long idValuacion, long idUsuario)
        {
            try
            {
                var valuacion = _context.VALValuacion.Find(idValuacion);

                if (valuacion.FechaDesde <= DateTime.Now && (!valuacion.FechaHasta.HasValue || valuacion.FechaHasta.Value >= DateTime.Now)) // es vigente
                {
                    var valuacionAnterior = _context.VALValuacion
                                                    .Where(x => x.IdUnidadTributaria == valuacion.IdUnidadTributaria &&
                                                                x.FechaHasta.HasValue && !x.FechaBaja.HasValue)
                                                    .OrderByDescending(x => x.FechaHasta)
                                                    .FirstOrDefault();
                    if (valuacionAnterior != null)
                    {
                        valuacionAnterior.FechaHasta = null;
                        valuacionAnterior.FechaModif = DateTime.Now;
                        valuacionAnterior.IdUsuarioModif = idUsuario;
                    }
                }

                valuacion.FechaBaja = DateTime.Now;
                valuacion.IdUsuarioBaja = idUsuario;
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("DeleteValuacion", ex);
                return false;
            }
        }

        public long GetIdDepartamentoByCodigo(string codigo)
        {
            long idDepartamento = (from objeto in _context.Objetos
                                   where objeto.Codigo == codigo
                                   select objeto.ObjetoPadreId.Value)
                                   .FirstOrDefault();

            return idDepartamento;
        }

        private void SaveDominios(DDJJ ddjj, List<DDJJDominio> dominios, long idUsuario, DateTime fechaActual)
        {
            foreach (DDJJDominio d in ddjj.Dominios)
            {
                DDJJDominio dominio = dominios.FirstOrDefault(x => x.IdDominio == d.IdDominio);
                if (dominio == null)
                {
                    if (!d.IdUsuarioBaja.HasValue)
                    {
                        d.IdUsuarioBaja = idUsuario;
                        d.FechaBaja = fechaActual;

                        foreach (DDJJDominioTitular t in d.Titulares)
                        {
                            if (!t.IdUsuarioBaja.HasValue)
                            {
                                t.IdUsuarioBaja = idUsuario;
                                t.FechaBaja = fechaActual;

                                foreach (DDJJPersonaDomicilio pd in t.PersonaDomicilio)
                                {
                                    if (!pd.IdUsuarioBaja.HasValue)
                                    {
                                        pd.IdUsuarioBaja = idUsuario;
                                        pd.FechaBaja = fechaActual;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    d.IdUsuarioModif = idUsuario;
                    d.FechaModif = fechaActual;
                    d.Fecha = dominio.Fecha;
                    d.IdTipoInscripcion = dominio.IdTipoInscripcion;
                    d.Inscripcion = dominio.Inscripcion;

                    foreach (DDJJDominioTitular t in d.Titulares)
                    {
                        DDJJDominioTitular titular = dominios.FirstOrDefault(x => x.IdDominio == d.IdDominio).Titulares.FirstOrDefault(x => x.IdDominioTitular == t.IdDominioTitular);
                        if (titular == null) //deleted
                        {
                            if (!t.IdUsuarioBaja.HasValue)
                            {
                                t.IdUsuarioBaja = idUsuario;
                                t.FechaBaja = fechaActual;

                                foreach (DDJJPersonaDomicilio pd in t.PersonaDomicilio)
                                {
                                    pd.IdUsuarioBaja = idUsuario;
                                    pd.FechaBaja = fechaActual;
                                }
                            }
                        }
                        else
                        {
                            t.IdUsuarioModif = idUsuario;
                            t.FechaModif = fechaActual;
                            t.IdPersona = titular.IdPersona;
                            t.IdTipoTitularidad = titular.IdTipoTitularidad;
                            t.PorcientoCopropiedad = titular.PorcientoCopropiedad;

                            foreach (DDJJPersonaDomicilio pd in t.PersonaDomicilio)
                            {
                                DDJJPersonaDomicilio personaDomicilio = titular.PersonaDomicilio.FirstOrDefault(x => x.IdPersonaDomicilio == pd.IdPersonaDomicilio);

                                if (personaDomicilio == null)
                                {
                                    if (!pd.IdUsuarioBaja.HasValue)
                                    {
                                        pd.IdUsuarioBaja = idUsuario;
                                        pd.FechaBaja = fechaActual;
                                    }
                                }
                                else
                                {
                                    pd.IdUsuarioModif = idUsuario;
                                    pd.FechaModif = fechaActual;
                                    pd.IdTipoDomicilio = personaDomicilio.IdTipoDomicilio;
                                }
                            }

                            List<DDJJPersonaDomicilio> newPersonaDomicilios = titular.PersonaDomicilio.Where(x => x.IdPersonaDomicilio < 0).ToList();
                            foreach (DDJJPersonaDomicilio newPersonaDomicilio in newPersonaDomicilios)
                            {
                                newPersonaDomicilio.IdUsuarioAlta = newPersonaDomicilio.IdUsuarioModif = idUsuario;
                                newPersonaDomicilio.FechaAlta = newPersonaDomicilio.FechaModif = fechaActual;

                                if (newPersonaDomicilio.IdDomicilio == 0)
                                    AddDomicilio(idUsuario, fechaActual, newPersonaDomicilio);


                                t.PersonaDomicilio.Add(newPersonaDomicilio);
                            }
                        }
                    }

                    List<DDJJDominioTitular> newTitulares = dominio.Titulares.Where(x => x.IdDominioTitular < 0).ToList();
                    foreach (DDJJDominioTitular newTitular in newTitulares)
                    {
                        newTitular.IdUsuarioAlta = newTitular.IdUsuarioModif = idUsuario;
                        newTitular.FechaAlta = newTitular.FechaModif = fechaActual;
                        foreach (DDJJPersonaDomicilio newPersonaDomicilio in newTitular.PersonaDomicilio)
                        {
                            newPersonaDomicilio.IdUsuarioAlta = newPersonaDomicilio.IdUsuarioModif = idUsuario;
                            newPersonaDomicilio.FechaAlta = newPersonaDomicilio.FechaModif = fechaActual;

                            if (newPersonaDomicilio.IdDomicilio == 0)
                                AddDomicilio(idUsuario, fechaActual, newPersonaDomicilio);
                        }
                        d.Titulares.Add(newTitular);
                    }

                }
            }

            List<DDJJDominio> newDominios = dominios.Where(x => x.IdDominio < 0).ToList();
            foreach (DDJJDominio newDominio in newDominios)
            {
                newDominio.IdUsuarioAlta = newDominio.IdUsuarioModif = idUsuario;
                newDominio.FechaAlta = newDominio.FechaModif = fechaActual;
                foreach (DDJJDominioTitular newTitular in newDominio.Titulares)
                {
                    newTitular.IdUsuarioAlta = newTitular.IdUsuarioModif = idUsuario;
                    newTitular.FechaAlta = newTitular.FechaModif = fechaActual;
                    foreach (DDJJPersonaDomicilio newPersonaDomicilio in newTitular.PersonaDomicilio)
                    {
                        newPersonaDomicilio.IdUsuarioAlta = newPersonaDomicilio.IdUsuarioModif = idUsuario;
                        newPersonaDomicilio.FechaAlta = newPersonaDomicilio.FechaModif = fechaActual;

                        if (newPersonaDomicilio.IdDomicilio == 0)
                            AddDomicilio(idUsuario, fechaActual, newPersonaDomicilio);
                    }
                }

                ddjj.Dominios.Add(newDominio);
            }
        }

        private static void AddDomicilio(long idUsuario, DateTime fechaActual, DDJJPersonaDomicilio newPersonaDomicilio)
        {
            newPersonaDomicilio.Domicilio = new OA.Domicilio()
            {
                barrio = newPersonaDomicilio.Barrio,
                codigo_postal = newPersonaDomicilio.CodigoPostal,
                FechaModif = fechaActual,
                FechaAlta = fechaActual,
                localidad = newPersonaDomicilio.Localidad,
                numero_puerta = newPersonaDomicilio.Altura,
                piso = newPersonaDomicilio.Piso,
                unidad = newPersonaDomicilio.Departamento,
                provincia = newPersonaDomicilio.Provincia,
                TipoDomicilioId = newPersonaDomicilio.IdTipoDomicilio,
                ViaNombre = newPersonaDomicilio.Calle,
                ViaId = newPersonaDomicilio.IdCalle,
                municipio = newPersonaDomicilio.Municipio,
                pais = newPersonaDomicilio.Pais,
                UsuarioAltaId = idUsuario,
                UsuarioModifId = idUsuario
            };
        }

        public IEnumerable<VALValuacionTempCorrida> GetValuacionesTmpCorrida()
        {
            _context.Database.CommandTimeout = 600;
            var valuacionesTmpCorrida = _context.ValuacionTempCorrida.OrderByDescending(vtc => vtc.Corrida).ToList(); // Carga #tablaValuaciones 
            return valuacionesTmpCorrida;
        }

        public List<VALValuacionTempDepto> GetValuacionesTmpDepto(int corrida)
        {
            _context.Database.CommandTimeout = 600;
            var valuacionesTmpDpto = _context.ValuacionTempDepto.Where(vtd => vtd.Corrida == corrida).AsNoTracking().ToList();
            valuacionesTmpDpto.ForEach(item =>
            {
                if (DateTime.TryParse(item.FechaProc, out DateTime fecha))
                    item.FechaProc = fecha.ToString("dd-MM-yyyy HH:mm");
            });
            return valuacionesTmpDpto;
        }

        public bool EliminarCorridaTemporal(int corrida, long usuarioModificacionID)
        {
            try
            {
                using (var qb = _context.CreateSQLQueryBuilder())
                {
                    var fieldsToUpdate = new Dictionary<Atributo, object>
                    {
                        { new Atributo { Campo = "fecha_baja", TipoDatoId = 666 }, DateTime.Now },
                        { new Atributo { Campo = "fecha_modif", TipoDatoId = 666 }, DateTime.Now },
                        { new Atributo { Campo = "id_usu_baja" }, usuarioModificacionID },
                        { new Atributo { Campo = "id_usu_modif" }, usuarioModificacionID }
                    };

                    qb.AddTable(ConfigurationManager.AppSettings["DATABASE"], "VAL_VALUACION_TMP", null)
                      .AddFilter("corrida", corrida, Common.Enums.SQLOperators.EqualsTo)
                      .AddFieldsToUpdate(fieldsToUpdate.ToArray())
                      .ExecuteUpdate();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar registros: {ex.Message}");
                return false;
            }
        }

        public bool GenerarValuacionTemporal(long usuario)
        {
            try
            {
                _context.Database.CommandTimeout = 600;
                using (var qb = _context.CreateSQLQueryBuilder())
                {
                    qb.AddNoTable()
                      .AddFields(new Atributo
                      {
                          Funcion = $"pkg_valuacion.generar_valuacion_tmp({usuario})",
                          Campo = "generar"
                      })
                      .ExecuteQuery(_ => { });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} | {ex}");
                return false;
            }
        }

        public bool PasarValuacionTmpProduccion(int corrida, long usuario)
        {
            try
            {
                _context.Database.CommandTimeout = 600;
                using (var qb = _context.CreateSQLQueryBuilder())
                {
                    qb.AddNoTable()
                      .AddFields(new Atributo
                      {
                          Funcion = $"pkg_valuacion.insertar_valuacion({corrida},{usuario})",
                          Campo = "insertar"
                      })
                      .ExecuteQuery(_ => { });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} | {ex.ToString()}");
                return false;
            }
        }

        #region No Usadas - Borrar cuando sea conveniente

        //public void BajaMejoras(long idDDJJ, long idUsuario, string machineName, string ip)
        //{
        //    throw new NotImplementedException();
        //}

        //public Aforo BuscarAforoAlgoritmo(long idLocalidad, string calle, long? idVia, int? altura)
        //{
        //    throw new NotImplementedException();
        //}

        //public Aforo BuscarAforoPorId(long? idTramoVia, long? idVia)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<Aforo> BuscarAforosVia(IEnumerable<Tuple<long?, long?>> tramos_y_vias)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool DeleteValuacion(long idValuacion, long idUsuario)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool GetAplicarDecretoIsRunning()
        //{
        //    throw new NotImplementedException();
        //}

        //public string GetAplicarDecretoStatus()
        //{
        //    throw new NotImplementedException();
        //}

        //public object GetClaseParcelaByIdDDJJ(long idDeclaracionJurada)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<VALClasesParcelas> GetClasesParcelas()
        //{
        //    throw new NotImplementedException();
        //}

        //public List<VALClasesParcelas> GetClasesParcelasBySuperficie(decimal superficie)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<VALClasesParcelas> GetClasesParcelasFull()
        //{
        //    throw new NotImplementedException();
        //}

        //public string GetCroquisClaseParcela(int idClaseParcela)
        //{
        //    throw new NotImplementedException();
        //}

        //public DDJJ GetDeclaracionJuradaVigenteU(long idUnidadTributaria)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<EstadosConservacion> GetEstadosConservacion()
        //{
        //    throw new NotImplementedException();
        //}

        //public TramoVia GetGrfTramoVia(long idVia, int altura)
        //{
        //    throw new NotImplementedException();
        //}

        //public Via GetGrfVia(string calle)
        //{
        //    throw new NotImplementedException();
        //}

        //public long GetIdDepartamentoByCodigo(string codigo)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMCaracteristica> GetInmCaracteristicas(long idVersion)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMInciso> GetInmIncisos(long idVersion)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMMejoraCaracteristica> GetInmMejorasCaracteristicas(long idMejora)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMOtraCaracteristica> GetInmOtrasCaracteristicas(long idVersion)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMTipoCaracteristica> GetInmTipoCaracteristicas(long idVersion)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<DDJJUFracciones> GetMedidaLineasFromFraccionByIdU(int idU)
        //{
        //    throw new NotImplementedException();
        //}

        //public INMMejora GetMejora(long idDeclaracionJurada)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<INMMejoraOtraCar> GetMejoraOtraCar(long idMejora)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<VALTiposMedidasLineales> GetTipoMedidaLineales()
        //{
        //    throw new NotImplementedException();
        //}

        //public IEnumerable<TipoTitularidad> GetTiposTitularidad()
        //{
        //    throw new NotImplementedException();
        //}

        //public Tramite GetTramiteByNumero(long nroTramite)
        //{
        //    throw new NotImplementedException();
        //}

        //public IEnumerable<DDJJUOtrasCar> GetUOtrasCar(int idVersion)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool SaveDDJJU(DDJJ ddjj, DDJJU ddjjU, DDJJDesignacion ddjjDesignacion, List<DDJJDominio> dominios, long idUsuario, List<Web.Api.Models.ClaseParcela> clases, string machineName, string ip)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool SaveFormularioE1(DDJJ ddjj, INMMejora mejora, List<INMMejoraOtraCar> otrasCar, List<int> caracteristicas, long idUsuario, string machineName, string ip)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool SaveFormularioE2(DDJJ ddjj, INMMejora mejora, List<INMMejoraOtraCar> otrasCar, List<int> caracteristicas, long idUsuario, string machineName, string ip)
        //{
        //    throw new NotImplementedException();
        //}

        //public object ValoresAforoValido()
        //{
        //    throw new NotImplementedException();
        //} 
        #endregion
    }
}