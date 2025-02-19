using System;
using System.Linq;
using System.Data.Entity;
using GeoSit.Data.BusinessEntities.ObrasParticulares;
using GeoSit.Data.DAL.Interfaces;
using System.Collections.Generic;
using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.BusinessEntities.GlobalResources;
using System.Threading.Tasks;
using GeoSit.Data.BusinessEntities.Seguridad;
using Z.EntityFramework.Plus;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;
using GeoSit.Data.DAL.Common.ExtensionMethods.CUIT_CUIL;
using System.IO;

namespace GeoSit.Data.DAL.Repositories
{
    public class PersonaRepository : IPersonaRepository
    {
        private readonly GeoSITMContext _context;

        public PersonaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public IEnumerable<PersonaExpedienteRolDomicilio> SearchPersona(string nombre)
        {
            nombre = nombre.ToLower();
            var tokens = nombre.Split(' ');

            var personaInmuebles = _context.Persona;
            var personaDomicilios = _context.PersonaDomicilio;
            var domicilioInmuebles = _context.Domicilios;

            return from personaInmueble in personaInmuebles
                   join personaDomicilio in personaDomicilios
                   on personaInmueble.PersonaId equals personaDomicilio.PersonaId
                   join domicilioInmueble in domicilioInmuebles
                   on personaDomicilio.DomicilioId equals domicilioInmueble.DomicilioId
                   where tokens.All(token => personaInmueble.NombreCompleto.ToLower().Contains(token))
                   select new PersonaExpedienteRolDomicilio
                   {
                       PersonaInmuebleId = personaInmueble.PersonaId,
                       NombreCompleto = personaInmueble.NombreCompleto,
                       DomicilioFisico = personaDomicilio != null ? personaDomicilio.Domicilio.ViaNombre : ""
                   };
        }

        public PersonaExpedienteRolDomicilio GetPersona(long idPersona)
        {
            var persona = _context.Persona.Find(idPersona);
            _context.Entry(persona).Collection(x => x.PersonaDomicilios).Load();
            var domicilio = persona.PersonaDomicilios != null ?
                persona.PersonaDomicilios.FirstOrDefault(pd => pd.TipoDomicilioId == 1) : null;
            if (domicilio != null) _context.Entry(domicilio).Reference(x => x.Domicilio).Load();
            return new PersonaExpedienteRolDomicilio
            {
                PersonaInmuebleId = persona.PersonaId,
                NombreCompleto = persona.NombreCompleto,
                DomicilioFisico = domicilio != null ? domicilio.Domicilio.ViaNombre : string.Empty
            };
        }

        public Persona GetPersonaDatos(long idPersona)
        {
            var persona = _context.Persona
                                   .Include("TipoDocumentoIdentidad")
                                   .Include("TipoPersona")
                                   .Include("PersonaDomicilios")
                                   .Include("PersonaDomicilios.Domicilio")
                                   .Include("PersonaDomicilios.TipoDomicilio")
                                   .SingleOrDefault(x => x.PersonaId == idPersona);

            persona.PersonaNacionalidad = _context.Nacionalidad.Where(n => n.NacionalidadId == persona.Nacionalidad).Select(n => n.Descripcion).FirstOrDefault();

            switch (persona.Sexo)
            {
                case 1:
                    persona.PersonaSexo = "Femenino";
                    break;
                case 2:
                    persona.PersonaSexo = "Masculino";
                    break;
                default:
                    persona.PersonaSexo = "Sin Identificar";
                    break;
            }

            switch (persona.EstadoCivil)
            {
                case 1:
                    persona.PersonaEstadoCivil = "Casado/a";
                    break;
                case 2:
                    persona.PersonaEstadoCivil = "Separado/a";
                    break;
                case 3:
                    persona.PersonaEstadoCivil = "Divorciado/a";
                    break;
                case 4:
                    persona.PersonaEstadoCivil = "Viudo/a";
                    break;
                case 5:
                    persona.PersonaEstadoCivil = "Soltero/a";
                    break;
                case 6:
                    persona.PersonaEstadoCivil = "Sin Identificar";
                    break;

                default:
                    persona.PersonaEstadoCivil = ("Sin Identificar");
                    break;
            }

            persona.NombreCompleto = persona.NombreCompleto.ToUpper();

            return persona;
        }

        public IEnumerable<Persona> GetPersonasByTramite(int tramite)
        {
            return new List<Persona>();
            /*
            int tipoEntradaPersona = Convert.ToInt32(Entradas.Persona);

            var query = from entradaTramite in _context.TramitesEntradas
                        join objetoEntrada in _context.ObjetosEntrada on entradaTramite.IdObjetoEntrada equals objetoEntrada.IdObjetoEntrada
                        join persona in _context.Persona on entradaTramite.IdObjeto.Value equals persona.PersonaId
                        where objetoEntrada.IdEntrada == tipoEntradaPersona && tramite == entradaTramite.IdTramite
                        select persona;

            return query.Include(x => x.TipoPersona).ToList();
            */
        }

        public IEnumerable<Persona> GetPersonasCompletas(long[] personas)
        {
            var listaPersonas = new List<Persona>();

            int MAX_CANT = 900;
            int procesados = 0;
            while (procesados < personas.Length)
            {
                var stepIds = personas.Skip(procesados).Take(MAX_CANT).ToArray();
                procesados += stepIds.Length;

                listaPersonas.AddRange(_context.Persona
                                               .Where(persona => stepIds.Contains(persona.PersonaId))
                                               .Include(p => p.TipoDocumentoIdentidad)
                                               .Include(p => p.PersonaDomicilios.Select(pd => pd.Domicilio))
                                               .Include(p => p.PersonaDomicilios.Select(pd => pd.TipoDomicilio))
                                               .ToList());
            }

            return listaPersonas;
        }

        public async Task<Persona> Save(Persona persona, IEnumerable<Domicilio> domicilios, IEnumerable<Profesion> profesiones, long usuarioOperacion, string ip, string machineName)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                if (persona == null)
                {
                    _context.GetLogger().LogError("PersonaRepository/Save", new Exception($"Datos incorrectos, Persona es null."));
                    throw new ArgumentNullException();
                }
                if ((persona?.ShouldValidateCUIL() ?? false) && (!persona?.IsValidCUIL() ?? false))
                {
                    _context.GetLogger().LogError("PersonaRepository/Save", new Exception($"El CUIL no es válido."));
                    throw new InvalidDataException();
                }

                var current = await _context.Persona.FindAsync(persona.PersonaId);
                if (current != null && current.FechaBaja.HasValue)
                {
                    _context.GetLogger().LogError("PersonaRepository/Save", new Exception($"No se puede dar de editar la persona con {persona.PersonaId}.{Environment.NewLine}No existe o no está activa."));
                    throw new NotSupportedException();
                }
                try
                {
                    string mensaje = Mensajes.ModificarPersonasOK;
                    string evento = Eventos.ModificarPersonas;
                    string tipoOperacion = TiposOperacion.Modificacion;

                    DateTime now = DateTime.Now;
                    if (current == null)
                    {
                        mensaje = Mensajes.AltadePersonasOK;
                        evento = Eventos.AltadePersonas;
                        tipoOperacion = TiposOperacion.Alta;

                        current = _context.Persona.Add(new Persona()
                        {
                            FechaAlta = now,
                            UsuarioAltaId = usuarioOperacion
                        });
                    }

                    current.FechaModif = now;
                    current.UsuarioModifId = usuarioOperacion;

                    current.Apellido = persona.Apellido;
                    current.CUIL = persona.CUIL;
                    current.Email = persona.Email;
                    current.EstadoCivil = persona.EstadoCivil;
                    current.Nacionalidad = persona.Nacionalidad;
                    current.Nombre = persona.Nombre;
                    current.NombreCompleto = $"{persona.Apellido ?? string.Empty} {persona.Nombre ?? string.Empty}".Trim();
                    current.NroDocumento = persona.NroDocumento;
                    current.Sexo = persona.Sexo;
                    current.Telefono = persona.Telefono;
                    current.TipoDocId = persona.TipoDocId;
                    current.TipoPersonaId = persona.TipoPersonaId;

                    current.PersonaDomicilios = await _context.PersonaDomicilio
                                                              .Include(pd => pd.Domicilio)
                                                              .Where(pd => pd.PersonaId == current.PersonaId && pd.FechaBaja == null)
                                                              .ToListAsync();
                    
                    domicilios = domicilios ?? new Domicilio[0];
                    var domiciliosNuevos = domicilios.Where(d => !current.PersonaDomicilios.Any(act => act.DomicilioId == d.DomicilioId));
                    var domiciliosVigentes = domicilios.Where(d => current.PersonaDomicilios.Any(act => act.DomicilioId == d.DomicilioId));
                    var domiciliosEliminados = current.PersonaDomicilios.Where(d => !domicilios.Any(grb => grb.DomicilioId == d.DomicilioId));

                    #region Agrego Nuevos Domicilios y Relaciones con Persona
                    foreach (var domicilio in domiciliosNuevos)
                    {
                        domicilio.ViaId = domicilio.ViaId == 0 ? null : domicilio.ViaId;
                        domicilio.TipoDomicilio = null;
                        domicilio.FechaAlta = domicilio.FechaModif = now;
                        domicilio.UsuarioAltaId = domicilio.UsuarioModifId = usuarioOperacion;

                        _context.Domicilios.Add(domicilio);

                        current.PersonaDomicilios.Add(new PersonaDomicilio()
                        {
                            TipoDomicilioId = domicilio.TipoDomicilioId,
                            Domicilio = domicilio,
                            UsuarioAltaId = usuarioOperacion,
                            UsuarioModifId = usuarioOperacion,
                            FechaAlta = now,
                            FechaModif = now
                        });
                    }
                    #endregion

                    #region Modifico Relaciones con persona procesada y/o Domicilio
                    foreach (var domicilioPersona in domiciliosVigentes)
                    {
                        var currRel = current.PersonaDomicilios.Single(act => act.DomicilioId == domicilioPersona.DomicilioId);
                        if (domicilioPersona.TipoDomicilioId != currRel.TipoDomicilioId)
                        {
                            currRel.TipoDomicilioId = domicilioPersona.TipoDomicilioId;
                            currRel.FechaModif = now;
                            currRel.UsuarioModifId = usuarioOperacion;
                        }
                        var domicilio = currRel.Domicilio;
                        if (domicilio.ViaNombre != domicilioPersona.ViaNombre
                            || domicilio.numero_puerta != domicilioPersona.numero_puerta
                            || domicilio.piso != domicilioPersona.piso
                            || domicilio.unidad != domicilioPersona.unidad
                            || domicilio.barrio != domicilioPersona.barrio
                            || domicilio.localidad != domicilioPersona.localidad
                            || domicilio.municipio != domicilioPersona.municipio
                            || domicilio.provincia != domicilioPersona.provincia
                            || domicilio.pais != domicilioPersona.pais
                            || domicilio.ubicacion != domicilioPersona.ubicacion
                            || domicilio.codigo_postal != domicilioPersona.codigo_postal
                            || domicilio.TipoDomicilioId != domicilioPersona.TipoDomicilioId
                            || domicilio.DomicilioId != domicilioPersona.DomicilioId
                            || domicilio.ViaId != domicilioPersona.ViaId
                            || domicilio.IdLocalidad != domicilioPersona.IdLocalidad
                            || domicilio.ProvinciaId != domicilioPersona.ProvinciaId)
                        {
                            domicilio.FechaModif = now;
                            domicilio.UsuarioModifId = usuarioOperacion;

                            domicilio.ViaNombre = domicilioPersona.ViaNombre;
                            domicilio.numero_puerta = domicilioPersona.numero_puerta;
                            domicilio.piso = domicilioPersona.piso;
                            domicilio.unidad = domicilioPersona.unidad;
                            domicilio.barrio = domicilioPersona.barrio;
                            domicilio.localidad = domicilioPersona.localidad;
                            domicilio.municipio = domicilioPersona.municipio;
                            domicilio.provincia = domicilioPersona.provincia;
                            domicilio.pais = domicilioPersona.pais;
                            domicilio.ubicacion = domicilioPersona.ubicacion;
                            domicilio.codigo_postal = domicilioPersona.codigo_postal;
                            domicilio.TipoDomicilioId = domicilioPersona.TipoDomicilioId;
                            domicilio.DomicilioId = domicilioPersona.DomicilioId;
                            domicilio.ViaId = domicilioPersona.ViaId == 0 ? null : domicilioPersona.ViaId;
                            domicilio.IdLocalidad = domicilioPersona.IdLocalidad;
                            domicilio.ProvinciaId = domicilioPersona.ProvinciaId;
                        }
                    }

                    #endregion

                    #region Baja de Relaciones y, si no quedan relaciones vigentes con domicilio, también Baja del Domicilio
                    foreach (var domicilioPersona in domiciliosEliminados)
                    {
                        domicilioPersona.FechaBaja = domicilioPersona.FechaModif = now;
                        domicilioPersona.UsuarioBajaId = domicilioPersona.UsuarioModifId = usuarioOperacion;

                        if (!await _context.PersonaDomicilio.AnyAsync(pd => pd.DomicilioId == domicilioPersona.DomicilioId
                                                                             && pd.FechaBaja == null
                                                                             && pd.PersonaId != persona.PersonaId))
                        {
                            domicilioPersona.Domicilio.FechaBaja = domicilioPersona.Domicilio.FechaModif = now;
                            domicilioPersona.Domicilio.UsuarioBajaId = domicilioPersona.Domicilio.UsuarioModifId = usuarioOperacion;
                        }
                    }
                    #endregion

                    var entry = _context.Entry(current);

                    var prev = entry.State == EntityState.Added ? null : entry.OriginalValues.ToObject();
                    await _context.SaveChangesAsync();
                    var vig = entry.CurrentValues.ToObject();

                    var currentProfesiones = _context.Profesion.Where(p => p.PersonaId == current.PersonaId);
                    profesiones = profesiones ?? new Profesion[0];

                    #region Elimino todas las profesiones ya que no tienen datos de auditoría
                    _context.Profesion.RemoveRange(currentProfesiones);
                    #endregion

                    #region Agrego Profesiones a Persona
                    foreach (var profesion in profesiones)
                    {
                        profesion.PersonaId = current.PersonaId;
                        profesion.TipoProfesion = null;
                    }
                    _context.Profesion.AddRange(profesiones);
                    #endregion

                    _context.Auditoria.Add(new Auditoria(usuarioOperacion, evento, mensaje, machineName, ip, "S", prev, vig, "Persona", 1, tipoOperacion));

                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return current;
                }
                catch (Exception ex)
                {
                    _context.GetLogger().LogError("PersonaRepository/Save", ex);
                    transaction.Rollback();
                    return null;
                }
            }
        }

        public async Task<bool> Delete(long id, long usuarioOperacion, string ip, string machineName)
        {
            var current = await _context.Persona
                                        .IncludeFilter(p => p.PersonaDomicilios.Where(pd => pd.FechaBaja == null))
                                        .SingleOrDefaultAsync(p => p.PersonaId == id);

            if (current == null || current.FechaBaja.HasValue)
            {
                _context.GetLogger().LogError("PersonaRepository/Delete", new Exception($"No se puede dar de baja la persona con {id}.{Environment.NewLine}No existe o no está activa."));
                return false;
            }

            try
            {
                current.FechaBaja = current.FechaModif = DateTime.Now;
                current.UsuarioBajaId = current.UsuarioModifId = usuarioOperacion;

                foreach (var relDomicilio in current.PersonaDomicilios)
                {
                    relDomicilio.FechaBaja = relDomicilio.FechaModif = current.FechaBaja.Value;
                    relDomicilio.UsuarioBajaId = relDomicilio.UsuarioModifId = usuarioOperacion;
                }

                _context.Auditoria.Add(new Auditoria(usuarioOperacion, Eventos.BajadePersonas, Mensajes.BajadePersonasOK, machineName, ip, "S", null, _context.Entry(current).OriginalValues.ToObject(), "Persona", 1, TiposOperacion.Baja));

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError("PersonaRepository/Delete", ex);
                return false;
            }
        }


        public async Task<List<Persona>> SearchByPattern(string pattern, int limit = 100)
        {
            var words = pattern.ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var query = _context.Persona.Where(p => p.FechaBaja == null);

            foreach (var word in words)
            {
                query = query.Where(x => (x.NombreCompleto != null && x.NombreCompleto.ToUpper().Contains(word)) ||
                                         (x.Nombre != null && x.Nombre.ToUpper().Contains(word)) ||
                                         (x.Apellido != null && x.Apellido.ToUpper().Contains(word)) ||
                                         (x.NroDocumento != null && x.NroDocumento.ToUpper().Contains(word)));
            }

            //EL BUSCADOR LIMITA A MAXIMO 100 resultados -Acollado.
            limit = Math.Max(Math.Min(limit, 100), 1);
            return await query.Take(limit)
                              .Include(x => x.TipoDocumentoIdentidad)
                              .ToListAsync();
        }

        public Persona GetPersonaById(long id)
        {
            var persona = _context.Persona
                                  .IncludeFilter(p => p.PersonaDomicilios.Where(x => x.FechaBaja == null))
                                  .IncludeFilter(p => p.PersonaDomicilios.Where(x => x.FechaBaja == null).Select(d => d.Domicilio))
                                  .IncludeFilter(p => p.PersonaDomicilios.Where(x => x.FechaBaja == null).Select(d => d.TipoDomicilio))
                                  .SingleOrDefault(x => x.PersonaId == id);
            return persona;
        }

        public List<Profesion> GetProfesionesByPersonaId(long id)
        {
            return _context.Profesion
                           .Include(a => a.TipoProfesion)
                           .Where(a => a.PersonaId == id).ToList();
        }
    }
}