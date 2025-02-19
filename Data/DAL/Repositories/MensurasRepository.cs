using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.DAL.Common;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;
using GeoSit.Data.DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace GeoSit.Data.DAL.Repositories
{
    internal class MensurasRepository : BaseRepository<Mensura, long>, IMensurasRepository
    {
        public MensurasRepository(GeoSITMContext context)
            : base(context) { }
        public Mensura GetById(long id)
        {
            var mensura = ObtenerDbSet()
                                    .IncludeFilter(m => m.TipoMensura)
                                    .IncludeFilter(m => m.EstadoMensura)
                                    .IncludeFilter(m => m.ParcelasMensuras.Where(pm => pm.FechaBaja == null))
                                    .IncludeFilter(m => m.MensurasRelacionadasOrigen.Where(mro => mro.FechaBaja == null))
                                    .IncludeFilter(m => m.MensurasRelacionadasDestino.Where(mrd => mrd.FechaBaja == null))
                                    .IncludeFilter(m => m.MensurasRelacionadasOrigen.Where(mro => mro.FechaBaja == null).Select(mro => mro.MensuraOrigen))
                                    .IncludeFilter(m => m.MensurasRelacionadasOrigen.Where(mro => mro.FechaBaja == null).Select(mro => mro.MensuraOrigen.TipoMensura))
                                    .IncludeFilter(m => m.MensurasRelacionadasOrigen.Where(mro => mro.FechaBaja == null).Select(mro => mro.MensuraOrigen.EstadoMensura))
                                    .IncludeFilter(m => m.MensurasRelacionadasDestino.Where(mrd => mrd.FechaBaja == null).Select(mrd => mrd.MensuraDestino))
                                    .IncludeFilter(m => m.MensurasRelacionadasDestino.Where(mrd => mrd.FechaBaja == null).Select(mrd => mrd.MensuraDestino.TipoMensura))
                                    .IncludeFilter(m => m.MensurasRelacionadasDestino.Where(mrd => mrd.FechaBaja == null).Select(mrd => mrd.MensuraDestino.EstadoMensura))
                                    .IncludeFilter(m => m.MensurasDocumentos.Where(md => md.FechaBaja == null))
                                    .IncludeFilter(m => m.MensurasDocumentos.Where(md => md.FechaBaja == null).Select(md => md.Documento))
                                    .IncludeFilter(m => m.MensurasDocumentos.Where(md => md.FechaBaja == null).Select(md => md.Documento.Tipo))
                                    .Where(a => a.IdMensura == id).SingleOrDefault();

            foreach (var parMensura in mensura.ParcelasMensuras)
            {
                GetContexto().Entry(parMensura).Reference(pm => pm.Parcela)
                    .Query().IncludeFilter(p => p.Nomenclaturas.Where(n => n.FechaBaja == null))
                    .Load();
            }
            return mensura;
        }

        public List<ParcelaMensura> GetParcelasMensuraByParcelaId(long idParcela)
        {
            using (var context = GetContexto())  
            {
                var parcelasMensuras = context.ParcelaMensura
                    .Where(ipm => ipm.IdParcela == idParcela)
                    .OrderBy(ipm => ipm.FechaBaja.HasValue)
                    .ThenByDescending(ipm => ipm.FechaBaja)
                    .ThenByDescending(ipm => ipm.FechaAlta)
                    .ToList();
                return parcelasMensuras;
            }
        }

        public List<Mensura> GetMensuras()
        {
            return GetContexto().Mensura.Where(a => a.FechaBaja == null).ToList();
        }

        public List<TipoMensura> GetTiposMensura()
        {
            return GetContexto().TipoMensura.Where(x => x.FechaBaja == null).ToList();
        }

        public List<TipoMensura> GetTiposMensuraSaneamiento()
        {
            var tipos = new[]
            {
                long.Parse(TiposMensuras.Mensura),
                long.Parse(TiposMensuras.MensuraYSubdivision),
                long.Parse(TiposMensuras.MensuraYUnificacion),
                long.Parse(TiposMensuras.MensuraSubdivisionYUnificacion),
                long.Parse(TiposMensuras.MensuraUnificacionYSubdivision),
                long.Parse(TiposMensuras.PrescripcionAdquisitiva),
                long.Parse(TiposMensuras.MensurasOficiales),
            };
            var query = GetContexto()
                        .TipoMensura
                        .Where(x => tipos.Contains(x.IdTipoMensura))
                        .Where(x => x.FechaBaja == null);

            return new[] 
            { 
                new TipoMensura()
                {
                    IdTipoMensura = long.Parse(TiposMensuras.Saneamiento),
                    Descripcion = "Saneamiento"
                }
            }.Concat(query).ToList();
        }

        public async Task Save(Mensura mensura, List<long> parcelas, List<long> mensurasOrigen, List<long> mensurasDestino, List<long> documentos, long usuarioOperacion, string ip, string machineName)
        {
            using (var transaction = GetContexto().Database.BeginTransaction())
            {
                parcelas = parcelas ?? new List<long>();
                mensurasOrigen = mensurasOrigen ?? new List<long>();
                mensurasDestino = mensurasDestino ?? new List<long>();
                documentos = documentos ?? new List<long>();
                try
                {
                    string evento = Eventos.ModificacionDeMensura;
                    string tipoOperacion = TiposOperacion.Modificacion;
                    var current = await ObtenerDbSet().FindAsync(mensura.IdMensura);
                    DateTime now = DateTime.Now;
                    if (current == null)
                    {
                        evento = Eventos.AltaDeMensura;
                        tipoOperacion = TiposOperacion.Alta;

                        current = ObtenerDbSet().Add(new Mensura()
                        {
                            IdUsuarioAlta = usuarioOperacion,
                            FechaAlta = now
                        });
                    }
                    current.IdUsuarioModif = usuarioOperacion;
                    current.FechaModif = now;

                    current.Anio = mensura.Anio;
                    current.Departamento = mensura.Departamento;
                    current.Descripcion = string.IsNullOrEmpty(mensura.Descripcion) ? $"{mensura.Departamento}-{mensura.Numero}-{mensura.Anio}" : mensura.Descripcion;
                    current.FechaAprobacion = mensura.FechaAprobacion;
                    current.FechaPresentacion = mensura.FechaPresentacion;
                    current.IdEstadoMensura = mensura.IdEstadoMensura;
                    current.IdTipoMensura = mensura.IdTipoMensura;
                    current.Numero = mensura.Numero;
                    current.Observaciones = mensura.Observaciones;

                    #region Parcelas Relacionadas
                    var parcelasVigentes = await GetContexto().ParcelaMensura.Where(pm => pm.IdMensura == current.IdMensura && pm.FechaBaja == null).ToArrayAsync();

                    var parcelasNuevas = parcelas.Where(p => !parcelasVigentes.Any(pv => p == pv.IdParcela))
                                                 .Select(p => new ParcelaMensura()
                                                 {
                                                     IdParcela = p,
                                                     FechaAlta = now,
                                                     IdUsuarioAlta = usuarioOperacion,
                                                     FechaModif = now,
                                                     IdUsuarioModif = usuarioOperacion,
                                                     Mensura = current
                                                 });
                    _ = GetContexto().ParcelaMensura.AddRange(parcelasNuevas);

                    var parcelasEliminadas = parcelasVigentes.Where(pv => !parcelas.Any(p => p == pv.IdParcela));
                    foreach (var eliminada in parcelasEliminadas)
                    {
                        eliminada.IdUsuarioBaja = eliminada.IdUsuarioModif = usuarioOperacion;
                        eliminada.FechaBaja = eliminada.FechaModif = now;
                    }
                    #endregion

                    #region Mensuras Origen Relacionadas
                    var mensurasOrigenVigentes = await GetContexto().MensurasRelacionadas.Where(mr => mr.IdMensuraDestino == current.IdMensura && mr.FechaBaja == null).ToArrayAsync();

                    var mensurasOrigenNuevas = mensurasOrigen.Where(p => !mensurasOrigenVigentes.Any(pv => p == pv.IdMensuraOrigen))
                                                             .Select(p => new MensuraRelacionada()
                                                             {
                                                                 IdMensuraOrigen = p,
                                                                 FechaAlta = now,
                                                                 IdUsuarioAlta = usuarioOperacion,
                                                                 FechaModif = now,
                                                                 IdUsuarioModif = usuarioOperacion,
                                                                 MensuraDestino = current
                                                             });
                    _ = GetContexto().MensurasRelacionadas.AddRange(mensurasOrigenNuevas);

                    foreach (var eliminada in mensurasOrigenVigentes.Where(pv => !mensurasOrigen.Any(p => p == pv.IdMensuraOrigen)))
                    {
                        eliminada.IdUsuarioBaja = eliminada.IdUsuarioModif = usuarioOperacion;
                        eliminada.FechaBaja = eliminada.FechaModif = now;
                    }
                    #endregion

                    #region Mensuras Destino Relacionadas
                    var mensurasDestinoVigentes = await GetContexto().MensurasRelacionadas.Where(mr => mr.IdMensuraOrigen == current.IdMensura && mr.FechaBaja == null).ToArrayAsync();

                    var mensurasDestinoNuevas = mensurasDestino.Where(p => !mensurasDestinoVigentes.Any(pv => p == pv.IdMensuraDestino))
                                                             .Select(p => new MensuraRelacionada()
                                                             {
                                                                 IdMensuraDestino = p,
                                                                 FechaAlta = now,
                                                                 IdUsuarioAlta = usuarioOperacion,
                                                                 FechaModif = now,
                                                                 IdUsuarioModif = usuarioOperacion,
                                                                 MensuraOrigen = current
                                                             });
                    _ = GetContexto().MensurasRelacionadas.AddRange(mensurasDestinoNuevas);

                    foreach (var eliminada in mensurasDestinoNuevas.Where(pv => !mensurasDestino.Any(p => p == pv.IdMensuraDestino)))
                    {
                        eliminada.IdUsuarioBaja = eliminada.IdUsuarioModif = usuarioOperacion;
                        eliminada.FechaBaja = eliminada.FechaModif = now;
                    }
                    #endregion

                    #region Documentos Relacionados
                    var documentosVigentes = await GetContexto().MensuraDocumento.Where(md => md.IdMensura == current.IdMensura && md.FechaBaja == null).ToArrayAsync();

                    var documentosNuevos = documentos.Where(d => !documentosVigentes.Any(dv => dv.IdDocumento == d))
                                                     .Select(d => new MensuraDocumento()
                                                     {
                                                         IdDocumento = d,
                                                         FechaAlta = now,
                                                         IdUsuarioAlta = usuarioOperacion,
                                                         FechaModif = now,
                                                         IdUsuarioModif = usuarioOperacion,
                                                         Mensura = current
                                                     });
                    _ = GetContexto().MensuraDocumento.AddRange(documentosNuevos);

                    var documentosEliminados = documentosVigentes.Where(dv => !documentos.Any(d => d == dv.IdDocumento));
                    foreach (var eliminado in documentosEliminados)
                    {
                        eliminado.IdUsuarioBaja = eliminado.IdUsuarioModif = usuarioOperacion;
                        eliminado.FechaBaja = eliminado.FechaModif = now;
                    }
                    #endregion

                    await GetContexto().SaveChangesAsync();

                    #region Documentos Parcela
                    long ID_TIPO_DOCUMENTO_PLANO_ESCANEADO = Convert.ToInt64(GetContexto().ParametrosGenerales.Single(pg => pg.Clave == "PLANO_DE_MENSURA_APROBADO").Valor);

                    var planosAgregadosMensura = await (from mensuraDocumento in GetContexto().MensuraDocumento
                                                         join documento in GetContexto().Documento on mensuraDocumento.IdDocumento equals documento.id_documento
                                                         where documento.fecha_baja_1 == null && mensuraDocumento.FechaBaja == null &&
                                                               mensuraDocumento.IdMensura  == current.IdMensura &&
                                                               documento.id_tipo_documento == ID_TIPO_DOCUMENTO_PLANO_ESCANEADO
                                                         from parcela in parcelas
                                                         select new { parcela, mensuraDocumento.IdDocumento }).ToArrayAsync();

                    var documentosAsociados = await (from parcelaDocumento in GetContexto().ParcelasDocumentos
                                                     join parcela in parcelas on parcelaDocumento.ParcelaID equals parcela
                                                     select parcelaDocumento).ToArrayAsync();

                    var docsNoAsociados = planosAgregadosMensura.Where(p => !documentosAsociados.Any(da => p.parcela == da.ParcelaID && p.IdDocumento == da.DocumentoID))
                                                                .Select(p => new ParcelaDocumento()
                                                                {
                                                                    DocumentoID = p.IdDocumento,
                                                                    ParcelaID = p.parcela,
                                                                    FechaAlta = now,
                                                                    UsuarioAltaID = usuarioOperacion,
                                                                    FechaModificacion = now,
                                                                    UsuarioModificacionID = usuarioOperacion
                                                                });
                    _ = GetContexto().ParcelasDocumentos.AddRange(docsNoAsociados);

                    var parcelasEliminadasId = parcelasEliminadas.Select(pe => pe.IdParcela).ToArray();
                    var documentosEliminadosId = documentosEliminados.Select(de => de.IdDocumento).ToArray();
                    var docsAsociadosEliminados = await (from docParcela in GetContexto().ParcelasDocumentos 
                                                         join docMensura in GetContexto().MensuraDocumento on docParcela.DocumentoID equals docMensura.IdDocumento 
                                                         where docMensura.IdMensura == current.IdMensura &&
                                                               (parcelasEliminadasId.Contains(docParcela.ParcelaID) || documentosEliminadosId.Contains(docParcela.DocumentoID))
                                                         select docParcela).ToArrayAsync();

                    foreach (var eliminado in docsAsociadosEliminados)
                    {
                        eliminado.UsuarioBajaID = eliminado.UsuarioModificacionID = usuarioOperacion;
                        eliminado.FechaBaja = eliminado.FechaModificacion = now;
                    }
                    #endregion

                    GetContexto().Auditoria.Add(new Auditoria(usuarioOperacion, evento, null, machineName, ip, "S", null, null, "Mensura", 1, tipoOperacion));

                    await GetContexto().SaveChangesAsync();
                    transaction.Commit();
                    return;
                }
                catch(Exception ex)
                {
                    transaction.Rollback();
                    GetContexto().GetLogger().LogError("MensuraRepository/Save", ex);
                    throw;
                }
            }
        }

        public async Task<bool> Delete(Mensura mensura)
        {
            var activityInDb = await ObtenerDbSet()
                                        .Include("ParcelasMensuras")
                                        .Include("MensurasRelacionadasOrigen")
                                        .Include("MensurasRelacionadasDestino")
                                        .Include("MensurasDocumentos")
                                        .Include("MensurasDocumentos.Documento")
                                        .Where(x => x.IdMensura == mensura.IdMensura)
                                        .FirstOrDefaultAsync();

            if (activityInDb == null) return false;

            DateTime now = DateTime.Now;
            activityInDb.IdUsuarioBaja = activityInDb.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
            activityInDb.FechaBaja = activityInDb.FechaModif = now;

            foreach (var item in activityInDb.ParcelasMensuras ?? new List<ParcelaMensura>())
            {
                item.IdUsuarioBaja = item.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
                item.FechaBaja = item.FechaModif = now;
            }

            foreach (var item in activityInDb.MensurasRelacionadasOrigen ?? new List<MensuraRelacionada>())
            {
                item.IdUsuarioBaja = item.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
                item.FechaBaja = item.FechaModif = now;
            }

            foreach (var item in activityInDb.MensurasRelacionadasDestino ?? new List<MensuraRelacionada>())
            {
                item.IdUsuarioBaja = item.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
                item.FechaBaja = item.FechaModif = now;
            }

            foreach (var item in activityInDb.MensurasDocumentos ?? new List<MensuraDocumento>())
            {
                item.IdUsuarioBaja = item.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
                item.FechaBaja = item.FechaModif = now;
                item.Documento.id_usu_baja = item.Documento.id_usu_modif = mensura.IdUsuarioBaja.Value;
                item.Documento.fecha_baja_1 = item.Documento.fecha_modif = now;

                var parcelasDocumentos = GetContexto().ParcelasDocumentos.Where(x => x.DocumentoID == item.Documento.id_documento).ToList();

                foreach (var pDoc in parcelasDocumentos ?? new List<ParcelaDocumento>())
                {
                    pDoc.FechaBaja = pDoc.FechaModificacion = now;
                    pDoc.UsuarioBajaID = pDoc.UsuarioModificacionID = mensura.IdUsuarioBaja.Value;
                }
            }

            try
            {
                GetContexto().Auditoria.Add(new Auditoria(mensura.IdUsuarioBaja.Value, Eventos.EliminarMensura, Mensajes.EliminarMensuraOK, mensura._Machine_Name,
                                                          mensura._Ip, "S", GetContexto().Entry(activityInDb).OriginalValues.ToObject(), activityInDb, "Mensura", 1,
                                                          TiposOperacion.Baja));
                await GetContexto().SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
        }

        public async Task<bool> DeleteRelacionParcelaMensura(Mensura mensura, long idParcelaMensura)
        {
            var activityInDb = await ObtenerDbSet()
                                        .Include("ParcelasMensuras")
                                        .Where(x => x.IdMensura == mensura.IdMensura)
                                        .FirstOrDefaultAsync();
            foreach (var item in activityInDb.ParcelasMensuras ?? new List<ParcelaMensura>())
            {
                if (item.IdParcelaMensura == idParcelaMensura)
                {
                    item.IdUsuarioBaja = item.IdUsuarioModif = mensura.IdUsuarioBaja.Value;
                    item.FechaBaja = item.FechaModif = DateTime.Now;
                }
            }
            try
            {
                var auditoria = new Auditoria(
                    mensura.IdUsuarioBaja.Value,
                    Eventos.EliminarMensura,
                    Mensajes.EliminarMensuraOK,
                    mensura._Machine_Name,
                    mensura._Ip,
                    "S",
                    GetContexto().Entry(activityInDb).OriginalValues.ToObject(),
                    activityInDb,
                    "Mensura",
                    1,
                    TiposOperacion.Baja
                );
                GetContexto().Auditoria.Add(auditoria);
                await GetContexto().SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
        }

        public DataTableResult<GrillaMensura> SearchByText(DataTableParameters parametros)
        {
            string texto = (parametros.search?.value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(texto))
            {
                return new DataTableResult<GrillaMensura>()
                {
                    draw = parametros.draw,
                    data = new GrillaMensura[0]
                };
            }
            var joins = new Expression<Func<Mensura, dynamic>>[] { x => x.TipoMensura, x => x.EstadoMensura };
            var sorts = new List<SortClause<Mensura>>();
            var filtros = new List<Expression<Func<Mensura, bool>>>
            {
                m => m.FechaBaja == null ,
                m=> m.Descripcion.Contains(texto) || (m.Departamento + "-" + m.Numero + "-" + m.Anio).Contains(texto)
            };

            bool asc = parametros.order.FirstOrDefault().dir == "asc";
            switch (parametros.columns[parametros.order.FirstOrDefault().column].name)
            {
                case "Descripcion":
                    sorts.Add(new SortClause<Mensura>() { Expresion = tramite => tramite.Descripcion, ASC = asc });
                    break;
                case "Tipo":
                    sorts.Add(new SortClause<Mensura>() { Expresion = tramite => tramite.TipoMensura.Descripcion, ASC = asc });
                    break;
                case "Estado":
                    sorts.Add(new SortClause<Mensura>() { Expresion = tramite => tramite.EstadoMensura.Descripcion, ASC = asc });
                    break;
               default:
                    OrdenDefaultASC = asc;
                    break;
            }
            return ObtenerPagina(GetBaseQuery(joins, filtros, sorts), parametros, mapGrilla);
        }

        protected override DbSet<Mensura> ObtenerDbSet()
        {
            return this.GetContexto().Mensura;
        }

        protected override Expression<Func<Mensura, object>> OrdenDefault()
        {
            return mensura => mensura.IdMensura;
        }

        private GrillaMensura mapGrilla(Mensura mensura)
        {
            return new GrillaMensura()
            {
                IdMensura = mensura.IdMensura,
                Descripcion = mensura.Descripcion,
                Tipo = mensura.TipoMensura.Descripcion,
                Estado = mensura.EstadoMensura.Descripcion,
            };
        }
    }
}
