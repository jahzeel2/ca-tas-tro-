using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Tramites.Actions.Abstract;
using GeoSit.Data.BusinessEntities.GlobalResources;
using System;
using System.Data.Entity;
using System.Linq;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using System.Collections.Generic;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Linq.Expressions;
using GeoSit.Data.BusinessEntities.ValidacionesDB.Enums;

namespace GeoSit.Data.DAL.Tramites.Actions
{
    class ProcesarDeclaracionesJuradas : AccionEntrada
    {
        private MensuraTemporal _nuevaMensura;
        private Dictionary<long, List<DDJJTemporal>> _ddjjMejorasUTValuables;
        private Dictionary<UnidadTributariaTemporal, DDJJTemporal> _utValuables;
        public ProcesarDeclaracionesJuradas(METramite tramite, GeoSITMContext contexto)
            : base(Convert.ToInt32(Entradas.UnidadTributaria), tramite, contexto)
        {
            _ddjjMejorasUTValuables = new Dictionary<long, List<DDJJTemporal>>();
            _utValuables = new Dictionary<UnidadTributariaTemporal, DDJJTemporal>();
        }

        public override bool Execute()
        {
            _nuevaMensura = Contexto.ChangeTracker.Entries<MensuraTemporal>().Single().Entity;
            if (!base.Execute()) return false;
            try
            {
                foreach (var kvp in _utValuables)
                {
                    var ut = kvp.Key;
                    var parcela = Contexto.ChangeTracker
                                      .Entries<ParcelaTemporal>()
                                      .SingleOrDefault(e => e.Entity.ParcelaID == ut.ParcelaID)
                                      ?.Entity ?? Contexto.ParcelasTemporal.Single(pt => pt.ParcelaID == ut.ParcelaID && pt.IdTramite == ut.IdTramite);

                    GenerarValuacion(kvp.Value, ut, parcela.ClaseParcelaID);

                    bool esAfectacionPH = parcela.ClaseParcelaID == Convert.ToInt64(ClasesParcelas.PropiedadHorizontal);

                    #region Actualización de Estado Parcela
                    if (_ddjjMejorasUTValuables[ut.UnidadTributariaId].Any() || ut.TipoUnidadTributariaID == Convert.ToInt64(TipoUnidadTributariaEnum.PropiedaHorizontal))
                    {
                        parcela.EstadoParcelaID = Convert.ToInt64(EstadosParcelas.Edificado);
                    }
                    #endregion
                }
                Contexto.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Resultado = ResultadoValidacion.Error;
                Errores = new List<string>() { { ex.Message } };
                return false;
            }
        }
        protected override void ExecuteEntrada(METramiteEntrada entrada)
        {
            Expression<Func<DDJJTemporal, bool>> ddjjFilter = d => d.IdUnidadTributaria == entrada.IdObjeto && d.IdTramite == entrada.IdTramite;

            var ut = Contexto.ChangeTracker
                             .Entries<UnidadTributariaTemporal>()
                             .SingleOrDefault(e => e.Entity.UnidadTributariaId == entrada.IdObjeto)
                             ?.Entity ?? Contexto.UnidadesTributariasTemporal.Single(utt => utt.UnidadTributariaId == entrada.IdObjeto && utt.IdTramite == entrada.IdTramite);

            var ddjjsUTTramite = Contexto.DeclaracionesJuradasTemporal.Where(ddjjFilter);

            bool esUTExistente = Contexto.UnidadesTributarias.Any(x => x.UnidadTributariaId == ut.UnidadTributariaId);
            DDJJTemporal ddjjTierra;
            try
            {
                ddjjTierra = LoadDDJJTierra(ut, ddjjsUTTramite, !esUTExistente);
            }
            catch (InvalidOperationException ex)
            {
                Contexto.GetLogger().LogError($"ProcesarDeclaracionesJuradas-DDJJTierra (ut: {ut.UnidadTributariaId}", ex);
                throw;
            }
            if (esUTExistente)
            {/*Copio valuación vigente para setearle vigencia_hasta*/
                var valuacionVigente = Contexto.VALValuacion
                                               .OrderByDescending(x => x.FechaAlta)
                                               .FirstOrDefault(x => x.IdUnidadTributaria == ut.UnidadTributariaId && x.FechaBaja == null && x.FechaHasta == null);
                CopyToTemporal("VAL_VALUACION", new Dictionary<string, object>() { { "ID_VALUACION", valuacionVigente.IdValuacion } });
            }
            #region Designaciones
            #region Baja de Designaciones Actuales
            var queryDesignaciones = from designacion in Contexto.DesignacionesTemporal
                                     where designacion.IdParcela == ut.ParcelaID && designacion.IdTramite == Tramite.IdTramite
                                     select designacion;

            foreach (var designacion in queryDesignaciones)
            {
                designacion.IdUsuarioBaja = designacion.IdUsuarioModif = Tramite.UsuarioModif;
                designacion.FechaBaja = designacion.FechaModif = Tramite.FechaModif;
            }
            #endregion

            #region Alta de Designaciones
            Contexto.Entry(ddjjTierra).Reference(x => x.Designacion).Load();
            if (ddjjTierra.Designacion != null)
            {
                Contexto.DesignacionesTemporal.Add(new DesignacionTemporal()
                {
                    Barrio = ddjjTierra.Designacion.Barrio,
                    Calle = ddjjTierra.Designacion.Calle,
                    Chacra = ddjjTierra.Designacion.Chacra,
                    CodigoPostal = ddjjTierra.Designacion.CodigoPostal,
                    Departamento = ddjjTierra.Designacion.Departamento,
                    Fraccion = ddjjTierra.Designacion.Fraccion,
                    IdBarrio = ddjjTierra.Designacion.IdBarrio,
                    IdCalle = ddjjTierra.Designacion.IdCalle,
                    IdDepartamento = ddjjTierra.Designacion.IdDepartamento,
                    IdLocalidad = ddjjTierra.Designacion.IdLocalidad,
                    IdManzana = ddjjTierra.Designacion.IdManzana,
                    IdParaje = ddjjTierra.Designacion.IdParaje,
                    IdParcela = ut.ParcelaID.Value,
                    IdSeccion = ddjjTierra.Designacion.IdSeccion,
                    IdTipoDesignador = (short)ddjjTierra.Designacion.IdTipoDesignador,
                    Localidad = ddjjTierra.Designacion.Localidad,
                    Lote = ddjjTierra.Designacion.Lote,
                    Manzana = ddjjTierra.Designacion.Manzana,
                    Numero = ddjjTierra.Designacion.Numero,
                    Paraje = ddjjTierra.Designacion.Paraje,
                    Quinta = ddjjTierra.Designacion.Quinta,
                    Seccion = ddjjTierra.Designacion.Seccion,

                    IdTramite = Tramite.IdTramite,
                    FechaAlta = Tramite.FechaModif,
                    FechaModif = Tramite.FechaModif,
                    IdUsuarioAlta = Tramite.UsuarioModif,
                    IdUsuarioModif = Tramite.UsuarioModif
                });
            }
            #endregion
            #endregion

            #region Dominios
            #region Baja de Dominios y Titulares Actuales
            var queryDominios = Contexto.DominiosTemporal
                                                .Include(d => d.Titulares)
                                                .Where(d => d.IdTramite == Tramite.IdTramite && d.UnidadTributariaID == ut.UnidadTributariaId);

            foreach (var dominio in queryDominios)
            {
                foreach (var titular in dominio.Titulares)
                {
                    titular.UsuarioBajaID = titular.UsuarioModificacionID = Tramite.UsuarioModif;
                    titular.FechaBaja = titular.FechaModificacion = Tramite.FechaModif;
                }
                dominio.IdUsuarioBaja = dominio.IdUsuarioModif = Tramite.UsuarioModif;
                dominio.FechaBaja = dominio.FechaModif = Tramite.FechaModif;
            }
            #endregion

            #region Alta de Dominios y Titulares
            Contexto.Entry(ddjjTierra).Collection(x => x.Dominios).Query().Include(d => d.Titulares).Load();
            foreach (var ddjjDominio in ddjjTierra.Dominios ?? new DDJJDominioTemporal[0])
            {
                Contexto.DominiosTemporal.Add(new DominioTemporal()
                {
                    IdTramite = Tramite.IdTramite,
                    Fecha = ddjjDominio.Fecha,
                    Inscripcion = ddjjDominio.Inscripcion,
                    TipoInscripcionID = ddjjDominio.IdTipoInscripcion,
                    UnidadTributariaID = ut.UnidadTributariaId,
                    Titulares = ddjjDominio.Titulares.Select(t => new DominioTitularTemporal()
                    {
                        PersonaID = t.IdPersona,
                        PorcientoCopropiedad = t.PorcientoCopropiedad,
                        TipoTitularidadID = t.IdTipoTitularidad,

                        FechaAlta = Tramite.FechaModif,
                        FechaModificacion = Tramite.FechaModif,
                        UsuarioAltaID = Tramite.UsuarioModif,
                        UsuarioModificacionID = Tramite.UsuarioModif
                    }).ToList(),

                    FechaAlta = Tramite.FechaModif,
                    FechaModif = Tramite.FechaModif,
                    IdUsuarioAlta = Tramite.UsuarioModif,
                    IdUsuarioModif = Tramite.UsuarioModif
                });
            }
            #endregion
            #endregion

            #region Agrego UT si valúa
            if (!_utValuables.ContainsKey(ut))
            {
                _utValuables.Add(ut, ddjjTierra);
            }
            #endregion
            return;
        }

        private DDJJTemporal LoadDDJJTierra(UnidadTributariaTemporal ut, IQueryable<DDJJTemporal> ddjjTemporales, bool fallbackCurrent)
        {
            throw new NotImplementedException("Arreglar este quilombo");
            //var _versionesTierra = new long[] { Convert.ToInt64(VersionesDDJJ.U), Convert.ToInt64(VersionesDDJJ.SoR) };

            //var ddjj = ddjjTemporales.SingleOrDefault(d => _versionesTierra.Contains(d.IdVersion));
            //if (ddjj == null && !fallbackCurrent)
            //{
            //    throw new InvalidOperationException();
            //}
            //if (ddjj == null)
            //{
            //    var vigentes = from vigente in (from aux in Contexto.DDJJ
            //                                    where aux.IdUnidadTributaria == ut.UnidadTributariaId &&
            //                                          aux.FechaBaja == null &&
            //                                          _versionesTierra.Contains(aux.IdVersion)
            //                                    orderby aux.FechaVigencia descending
            //                                    select aux)
            //                   select vigente;
            //    ddjj = MapFromTierraVigente(vigentes.First());
            //}
            //else if (Convert.ToInt64(VersionesDDJJ.U) == ddjj.IdVersion)
            //{
            //    Contexto.Entry(ddjj).Reference(x => x.U).Query()
            //                        .Include(u => u.Fracciones.Select(f => f.MedidasLineales.Select(ml => ml.ClaseParcelaMedidaLineal)))
            //                        .Load();
            //    ddjj.U.Mensura = _nuevaMensura;
            //}
            //else
            //{
            //    Contexto.Entry(ddjj).Reference(x => x.Sor).Query()
            //                        .Include(sor => sor.SoRCars.Select(s => s.AptCar.SorCaracteristica))
            //                        .Include(sor => sor.Superficies.Select(s => s.Aptitud))
            //                        .Load();

            //    ddjj.Sor.Mensura = _nuevaMensura;
            //}
            //return ddjj;
        }

        protected override IQueryable<METramiteEntrada> GetEntradas(int idEntrada)
        {
            return from entrada in base.GetEntradas(idEntrada)
                   join ddjj in Contexto.DeclaracionesJuradasTemporal on entrada.IdObjeto equals ddjj.IdUnidadTributaria
                   join ut in Contexto.UnidadesTributariasTemporal on ddjj.IdUnidadTributaria equals ut.UnidadTributariaId
                   orderby ut.ParcelaID, ut.TipoUnidadTributariaID
                   where ddjj.IdTramite == entrada.IdTramite
                   group entrada by entrada.IdObjeto into grp
                   select grp.FirstOrDefault();
        }

        protected bool GenerarValuacion(DDJJTemporal ddjj, UnidadTributariaTemporal ut, long idClaseParcela)
        {
            try
            {
                var fechaVigencia = ddjj.FechaVigencia.GetValueOrDefault().Date.AddDays(1).AddMilliseconds(-1);

                var decretos = Contexto.ValDecretos
                                       .Include(x => x.Jurisdiccion)
                                       .Include(x => x.Zona)
                                       .Where(x => !x.IdUsuarioBaja.HasValue && x.FechaInicio <= fechaVigencia && ((!x.FechaFin.HasValue) || (x.FechaFin >= fechaVigencia)) &&
                                                    x.Zona.Any(y => y.IdTipoParcela == ut.Parcela.TipoParcelaID && !y.IdUsuarioBaja.HasValue) &&
                                                    x.Jurisdiccion.Any(y => y.IdJurisdiccion == ut.JurisdiccionID && !y.IdUsuarioBaja.HasValue))
                                       .ToList();

                TerminarValuacionesVigentes(ObtenerValuacion(ddjj, ut, decretos));
                return true;
            }
            catch (Exception ex)
            {
                Contexto.GetLogger().LogError($"GenerarValuacion({ddjj.IdDeclaracionJurada})", ex);
                throw;
            }
        }

        private DDJJTemporal MapFromTierraVigente(DDJJ ddjjTierra)
        {
            return new DDJJTemporal()
            {
                IdDeclaracionJurada = ddjjTierra.IdDeclaracionJurada,
                IdUnidadTributaria = ddjjTierra.IdUnidadTributaria,
                FechaModif = ddjjTierra.FechaModif,
                IdVersion = ddjjTierra.IdVersion,
                Sor = LoadSoR(ddjjTierra)
            };
        }

        private void TerminarValuacionesVigentes(VALValuacionTemporal valuacion)
        {
            var valuacionesVigentes = Contexto.ValuacionesTemporal
                                              .Where(x => x.IdUnidadTributaria == valuacion.IdUnidadTributaria &&
                                                          x.IdTramite == valuacion.IdTramite &&
                                                         !x.FechaBaja.HasValue && (!x.FechaHasta.HasValue || (x.FechaDesde <= valuacion.FechaDesde && x.FechaHasta.Value > valuacion.FechaDesde)));
            foreach (var v in valuacionesVigentes)
            {
                v.FechaHasta = valuacion.FechaDesde;
                v.FechaModif = valuacion.FechaAlta;
                v.IdUsuarioModif = valuacion.IdUsuarioAlta;
            }
        }

        private DDJJSorTemporal LoadSoR(DDJJ ddjjTierra)
        {
            if (ddjjTierra.IdVersion != Convert.ToInt64(VersionesDDJJ.U)) return null;

            Contexto.Entry(ddjjTierra)
                    .Reference(x=>x.Sor).Query()
                    .Include(x => x.Superficies.Select(s => s.Aptitud))
                    .Include(x => x.Superficies.Select(s => s.Caracteristicas.Select(c => c.Caracteristica)));

            return MapFromSoRVigente(ddjjTierra.Sor);
        }

        private DDJJSorTemporal MapFromSoRVigente(DDJJSor vigente)
        {
            throw new NotImplementedException("Arreglar este quilombo");
            //if (vigente == null) return null;

            //return new DDJJSorTemporal()
            //{
            //    DistanciaCamino = vigente.DistanciaCamino,
            //    DistanciaEmbarque = vigente.DistanciaEmbarque,
            //    DistanciaLocalidad = vigente.DistanciaLocalidad,
            //    IdCamino = vigente.IdCamino,
            //    IdLocalidad = vigente.IdLocalidad,
            //    NumeroHabitantes = vigente.NumeroHabitantes,
            //    SoRCars = vigente.SorCar
            //                     .Select(sc => new DDJJSorCarTemporal()
            //                     {
            //                         IdAptCar = sc.IdSorCar,
            //                         AptCar = sc.Caracteristica
            //                     }).ToList(),
            //    Superficies = vigente.Superficies
            //                         .Select(s => new VALSuperficieTemporal()
            //                         {
            //                             IdAptitud = s.IdAptitud,
            //                             Superficie = s.Superficie,
            //                             Aptitud = s.Aptitud
            //                         }).ToList()
            //};
        }

        private VALValuacionTemporal ObtenerValuacion(DDJJTemporal ddjj, UnidadTributariaTemporal ut, IEnumerable<VALDecreto> decretos)
        {
            var valuacionTemporal = new VALValuacionTemporal()
            {
                IdTramite = Tramite.IdTramite,
                IdUnidadTributaria = ut.UnidadTributariaId,
                FechaDesde = ddjj.FechaVigencia.Value,

                FechaAlta = Tramite.FechaModif,
                FechaModif = Tramite.FechaModif,
                IdUsuarioModif = Tramite.UsuarioModif,
                IdUsuarioAlta = Tramite.UsuarioModif,
            };

            decimal valorMejoras = 0;
            decimal valorTierra = ObtenerValorTierra(ddjj, (TipoParcelaEnum)ut.Parcela.TipoParcelaID);

            #region Determino la superficie de la valuacion (superficie de tierra)
            if (ddjj.U != null)
            {
                valuacionTemporal.Superficie = Convert.ToDouble(ddjj.U.SuperficiePlano ?? ddjj.U.SuperficieTitulo ?? 0);
            }
            else
            {
                valuacionTemporal.Superficie = ddjj.Sor.Superficies.Sum(x => x.Superficie) ?? 0;
            }
            #endregion

            #region Aplico coeficientes de decretos de ser necesario
            valuacionTemporal.Decretos = valuacionTemporal.Decretos ?? new List<VALValuacionDecretoTemporal>();
            foreach (var decretoAplicado in ObtenerDecretos(decretos))
            {
                valorTierra *= Convert.ToDecimal(decretoAplicado.Item1.Coeficiente ?? 1);
                valuacionTemporal.Decretos.Add(decretoAplicado.Item2);
            }
            #endregion

            valuacionTemporal.ValorTierra = valorTierra;
            valuacionTemporal.ValorMejoras = valorMejoras;
            valuacionTemporal.ValorTotal = valuacionTemporal.ValorTierra + valuacionTemporal.ValorMejoras.Value;
            ddjj.Valuaciones = new List<VALValuacionTemporal>() { { valuacionTemporal } };

            return valuacionTemporal;
        }

        private decimal ObtenerValorTierra(DDJJTemporal ddjj, TipoParcelaEnum tipoParcela)
        {

            throw new NotImplementedException("Arreglar este quilombo");
            //decimal valorTierra = 0;
            //if (ddjj.U != null)
            //{
            //    double valorTierraUrbana = 0;
            //    var ddjjU = ddjj.U;
            //    int idLocalidad = ddjj.Designacion.IdLocalidad ?? 0;

            //    double superficieParcela = (double?)ddjjU.SuperficiePlano ?? (double?)ddjjU.SuperficieTitulo ?? 0d;
            //    foreach (var fraccion in ddjjU.Fracciones)
            //    {
            //        switch ((ClasesEnum)fraccion.MedidasLineales.First().ClaseParcelaMedidaLineal.IdClaseParcela)
            //        {
            //            case ClasesEnum.PARCELA_RECTANGULAR_NO_EN_ESQUINA_HASTA_2000M2: // 1
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo }, fraccion))
            //                    {
            //                        valorTierraUrbana += ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo.ValorMetros.Value, ObtenerAforo(frente, idLocalidad), superficieParcela);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_INTERNA_CON_ACCESO_A_PASILLO_HASTA_2000M2: // 2
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo1, fondo2 }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                        double superficie = frente.ValorMetros.Value * fondo1.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo1.ValorMetros.Value, valorAforo, superficie);

            //                        superficie = frente.ValorMetros.Value * fondo2.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo, superficie);

            //                        valorTierraUrbana += (valor1 - valor2) / (fondo1.ValorMetros.Value * frente.ValorMetros.Value - fondo2.ValorMetros.Value * frente.ValorMetros.Value) * superficieParcela;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_DOS_CALLES_NO_OPUESTAS_HASTA_2000M2: // 3
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var fondoA1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOA1);
            //                    var fondoB1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOB1);
            //                    var fondoA2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOA2);
            //                    var fondoB2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOB2);

            //                    if (AllNotNullAndWithValue(new[] { frente, frente2, fondoA1, fondoB1, fondoA2, fondoB2 }, fraccion))
            //                    {
            //                        double fondo = (fondoA1.ValorMetros.Value + fondoB1.ValorMetros.Value) / 2;
            //                        double superficie = fondo * frente.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo, ObtenerAforo(frente, idLocalidad), superficie);

            //                        fondo = (fondoA2.ValorMetros.Value + fondoB2.ValorMetros.Value) / 2;
            //                        superficie = fondo * frente2.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela1(frente2.ValorMetros.Value, fondo, ObtenerAforo(frente2, idLocalidad), superficie);

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_MARTILLO_AL_FRENTE_HASTA_2000M2: // 4
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);
            //                    var contrafrente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.CONTRAFRENTE);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo1, fondo2, contrafrente }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                        if (Math.Abs(frente.ValorMetros.Value - contrafrente.ValorMetros.Value) > 4)
            //                        {

            //                            double superficie = fondo1.ValorMetros.Value * contrafrente.ValorMetros.Value;
            //                            double valor1 = ObtenerValuacionUTipoParcela1(contrafrente.ValorMetros.Value, fondo1.ValorMetros.Value, valorAforo, superficie);

            //                            superficie = fondo2.ValorMetros.Value * frente.ValorMetros.Value;
            //                            double valor2 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo, superficie);

            //                            superficie = fondo2.ValorMetros.Value * contrafrente.ValorMetros.Value;
            //                            double valor3 = ObtenerValuacionUTipoParcela1(contrafrente.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo, superficie);

            //                            valorTierraUrbana += valor1 + valor2 - valor3;
            //                        }
            //                        else
            //                        {
            //                            double superficie = fondo1.ValorMetros.Value * frente.ValorMetros.Value;
            //                            valorTierraUrbana += ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo1.ValorMetros.Value, valorAforo, superficie);
            //                        }
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_MARTILLO_AL_FONDO_HASTA_2000M2: // 5
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);
            //                    var contrafrente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.CONTRAFRENTE);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo1, fondo2, contrafrente }, fraccion))
            //                    {
            //                        double superficie = fondo1.ValorMetros.Value * frente.ValorMetros.Value;

            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);
            //                        double valor1 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo1.ValorMetros.Value, valorAforo, superficie);

            //                        superficie = (contrafrente.ValorMetros.Value - frente.ValorMetros.Value) * fondo1.ValorMetros.Value;
            //                        double mts = contrafrente.ValorMetros.Value - frente.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela1(mts, fondo1.ValorMetros.Value, valorAforo, superficie);

            //                        superficie = (contrafrente.ValorMetros.Value - frente.ValorMetros.Value) * fondo2.ValorMetros.Value;
            //                        mts = contrafrente.ValorMetros.Value - frente.ValorMetros.Value;
            //                        double valor3 = ObtenerValuacionUTipoParcela1(mts, fondo2.ValorMetros.Value, valorAforo, superficie);

            //                        valorTierraUrbana += (valor1 + valor2 - valor3);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_ROMBOIDAL_HASTA_2000M2: // 6
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela6(frente, fondo, ObtenerAforo(frente, idLocalidad), superficieParcela, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_EN_FALSA_ESCUADRA_HASTA_2000M2: // 7
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo, fondo2 }, fraccion))
            //                    {
            //                        double fondoX = (fondo.ValorMetros.Value + fondo2.ValorMetros.Value) / 2;
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                        valorTierraUrbana += ObtenerValuacionUTipoParcela6(frente, new DDJJUMedidaLinealTemporal() { ValorMetros = fondoX }, valorAforo, superficieParcela, fraccion);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_CONTRAFRENTE_EN_FALSA_ESCUADRA_HASTA_2000M2: // 8
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);

            //                    if (AllNotNullAndWithValue(new[] { frente, fondo, fondo2 }, fraccion))
            //                    {
            //                        double fondoX = (fondo.ValorMetros.Value + fondo2.ValorMetros.Value) / 2;
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                        valorTierraUrbana += ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondoX, valorAforo, superficieParcela);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_CALLES_OPUESTAS_HASTA_2000M2: // 9
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);

            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela9(frente1, frente2, fondo, idLocalidad, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_TRES_CALLES_HASTA_2000M2: // 10
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);


            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, frente3, fondo1, fondo2 }, fraccion))
            //                    {
            //                        double valor1 = ObtenerValuacionUTipoParcela9(frente1, frente3, fondo1, idLocalidad, fraccion);
            //                        double superficie = fondo2.ValorMetros.Value * frente2.ValorMetros.Value;
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valor2 = ObtenerValuacionUTipoParcela1(frente2.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo2, superficie);
            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_ESQUINA_CON_FRENTE_A_DOS_CALLES_HASTA_900M2: // 11
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    double aforoFrente1 = ObtenerAforo(frente1, idLocalidad);
            //                    double aforoFrente2 = ObtenerAforo(frente2, idLocalidad);

            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela11(frente1, frente2, aforoFrente1, aforoFrente2, superficieParcela, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_DOS_CALLES_OPUESTAS_MAYOR_A_2000M2: // 12
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela12(frente1, frente2, fondo, superficieParcela, idLocalidad, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_NO_EN_ESQUINA_CON_SUPERFICIE_ENTRE_2000M2_Y_15000M2: // 13
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela13(frente, fondo, valorAforo, superficieParcela, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_ESQUINA_DE_2000M2_Y_15000M2: // 14
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);

            //                    double valorAforo = ObtenerAforo(frente, idLocalidad);
            //                    double valorAforo1 = ObtenerAforo(frente1, idLocalidad);

            //                    double valorAforoMayor;

            //                    if (AllNotNullAndWithValue(new[] { frente, frente1 }, fraccion))
            //                    {
            //                        DDJJUMedidaLinealTemporal frenteMenorAforo;

            //                        if (valorAforo > valorAforo1)
            //                        {
            //                            frenteMenorAforo = frente1;
            //                            valorAforoMayor = valorAforo;
            //                        }
            //                        else
            //                        {
            //                            frenteMenorAforo = frente;
            //                            valorAforoMayor = valorAforo1;
            //                        }

            //                        var coeficiente = Contexto.VALCoef2a15k
            //                                                  .FirstOrDefault(x => x.FondoMinimo <= frenteMenorAforo.ValorMetros.Value && x.FondoMaximo >= frenteMenorAforo.ValorMetros.Value &&
            //                                                                       x.SuperficieMinima <= superficieParcela && x.SuperficieMaxima >= superficieParcela);
            //                        valorTierraUrbana += valorAforoMayor * ((coeficiente?.Coeficiente ?? 1) + 0.1) * superficieParcela;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_SUPERFICIE_MAYOR_A_15000M2: // 15
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);

            //                    if (AllNotNullAndWithValue(new[] { frente }, fraccion))
            //                    {
            //                        var coeficiente = Contexto.VALCoefMayor15k
            //                                                  .FirstOrDefault(x => x.SuperficieMinima <= superficieParcela && x.SuperficieMaxima >= superficieParcela);
            //                        valorTierraUrbana += (coeficiente?.Coeficiente ?? 1) * superficieParcela * ObtenerAforo(frente, idLocalidad);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_TRIANGULAR_CON_FRENTE_A_UNA_CALLE_HASTA_2000M2: // 16
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);
            //                    double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela16(frente, fondo, valorAforo, superficieParcela, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_TRIANGULAR_CON_VERTICE_A_UNA_CALLE_HASTA_2000M2: // 17
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);

            //                    valorTierraUrbana = ObtenerValuacionUTipoParcela17(frente, fondo, ObtenerAforo(frente, idLocalidad), superficieParcela, fraccion);
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_TRAPEZOIDAL_CON_FRENTE_MAYOR_A_UNA_CALLE_HASTA_2000M2: // 18
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var contrafrente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.CONTRAFRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);

            //                    if (AllNotNullAndWithValue(new[] { frente, contrafrente, fondo }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);
            //                        double superficie1 = fondo.ValorMetros.Value * contrafrente.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela1(contrafrente.ValorMetros.Value, fondo.ValorMetros.Value, valorAforo, superficie1);
            //                        double superficie2 = ((frente.ValorMetros.Value - contrafrente.ValorMetros.Value) * fondo.ValorMetros.Value) / 2;
            //                        var frenteX = new DDJJUMedidaLinealTemporal() { ValorMetros = frente.ValorMetros.Value - contrafrente.ValorMetros.Value };
            //                        double valor2 = ObtenerValuacionUTipoParcela16(frenteX, fondo, valorAforo, superficie2, fraccion);

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_TRAPEZOIDAL_CON_FRENTE_MENOR_A_UNA_CALLE_HASTA_2000M2: // 19
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var contrafrente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.CONTRAFRENTE);
            //                    var fondo = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO);

            //                    if (AllNotNullAndWithValue(new[] { frente, contrafrente, fondo }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);
            //                        double superficie1 = fondo.ValorMetros.Value * frente.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela1(frente.ValorMetros.Value, fondo.ValorMetros.Value, valorAforo, superficie1);

            //                        double superficie2 = ((contrafrente.ValorMetros.Value - frente.ValorMetros.Value) * fondo.ValorMetros.Value) / 2;
            //                        var frenteX = new DDJJUMedidaLinealTemporal()
            //                        {
            //                            ValorMetros = contrafrente.ValorMetros.Value - frente.ValorMetros.Value,
            //                        };
            //                        double valor2 = ObtenerValuacionUTipoParcela17(frenteX, fondo, valorAforo, superficie2, fraccion);

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_TRIANGULAR_CON_FRENTE_A_TRES_CALLES_HASTA_2000M2: // 20
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, frente3 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valorAforo3 = ObtenerAforo(frente3, idLocalidad);

            //                        double aforo = (valorAforo1 + valorAforo2 + valorAforo3) / 3;

            //                        valorTierraUrbana += aforo * superficieParcela;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_ESQUINA_CON_SUP_ENTRE_900M2_Y_2000M2: // 21
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        valorTierraUrbana += ObtenerValuacionUTipoParcela21(frente1, frente2, valorAforo1, valorAforo2, superficieParcela);
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_ESQUINA_CON_FRENTE_A_TRES_CALLES_Y_SUP_ENTRE_900M2_Y_2000M2: // 22
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, frente3 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valorAforo3 = ObtenerAforo(frente3, idLocalidad);

            //                        double superficie1 = (frente2.ValorMetros.Value / 2) * frente1.ValorMetros.Value;
            //                        double superficie2 = (frente2.ValorMetros.Value / 2) * frente3.ValorMetros.Value;

            //                        double valor1, valor2;
            //                        var frente1X = new DDJJUMedidaLinealTemporal() { ValorMetros = frente2.ValorMetros.Value / 2 };
            //                        double superficie = (frente2.ValorMetros.Value / 2) * frente3.ValorMetros.Value;
            //                        if (superficie1 > 900)
            //                        {
            //                            valor1 = ObtenerValuacionUTipoParcela21(frente1X, frente1, valorAforo2, valorAforo1, superficie1);
            //                            frente1X.ValorMetros = frente2.ValorMetros.Value / 2;
            //                            valor2 = ObtenerValuacionUTipoParcela21(frente1X, frente3, valorAforo2, valorAforo3, superficie);
            //                        }
            //                        else
            //                        {
            //                            valor1 = ObtenerValuacionUTipoParcela11(frente1X, frente1, valorAforo2, valorAforo1, superficie1, fraccion);
            //                            valor2 = ObtenerValuacionUTipoParcela11(frente1X, frente3, valorAforo2, valorAforo3, superficie, fraccion);
            //                        }

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_ESPECIAL: // 23
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);

            //                    if (AllNotNullAndWithValue(new[] { frente }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);

            //                        valorTierraUrbana += superficieParcela * valorAforo;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_ESQUINA_CON_FRENTE_A_TRES_CALLES_HASTA_900M2: // 24
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, frente3 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valorAforo3 = ObtenerAforo(frente3, idLocalidad);

            //                        double superficie1 = (frente2.ValorMetros.Value / 2) * frente1.ValorMetros.Value;
            //                        var frente1X = new DDJJUMedidaLinealTemporal() { ValorMetros = frente2.ValorMetros.Value / 2 };
            //                        double valor1 = ObtenerValuacionUTipoParcela11(frente1X, frente1, valorAforo2, valorAforo1, superficie1, fraccion);
            //                        double superficie2 = (frente2.ValorMetros.Value / 2) * frente3.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela11(frente1X, frente3, valorAforo2, valorAforo3, superficie2, fraccion);

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_DOS_CALLES_NO_OPUESTAS_MAYOR_A_2000M2: // 25
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var fondoA1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOA1);
            //                    var fondoA2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOA2);
            //                    var fondoB1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOB1);
            //                    var fondoB2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOB2);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, fondoA1, fondoA2, fondoB1, fondoB2 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);

            //                        double fondo1 = (fondoA1.ValorMetros.Value + fondoB1.ValorMetros.Value) / 2;
            //                        double fondo2 = (fondoA2.ValorMetros.Value + fondoB2.ValorMetros.Value) / 2;

            //                        double superficie1 = fondo1 * frente1.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela13(frente1, new DDJJUMedidaLinealTemporal() { ValorMetros = fondo1 }, valorAforo1, superficie1, fraccion);

            //                        double superficie2 = fondo2 * frente2.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela13(frente2, new DDJJUMedidaLinealTemporal() { ValorMetros = fondo2 }, valorAforo2, superficie2, fraccion);

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_SALIENTE_LATERAL_HASTA_2000M2: // 26
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);
            //                    var fondoSaliente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDOSALIENTE);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, fondo1, fondo2, fondoSaliente }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);

            //                        double superficie1 = fondo1.ValorMetros.Value * frente1.ValorMetros.Value;
            //                        double valor1 = ObtenerValuacionUTipoParcela1(frente1.ValorMetros.Value, fondo1.ValorMetros.Value, valorAforo1, superficie1);

            //                        double superficie2 = (fondo2.ValorMetros.Value + fondoSaliente.ValorMetros.Value) * frente2.ValorMetros.Value;
            //                        double valor2 = ObtenerValuacionUTipoParcela1(frente2.ValorMetros.Value, fondo2.ValorMetros.Value + fondoSaliente.ValorMetros.Value, valorAforo1, superficie2);

            //                        double superficie3 = frente2.ValorMetros.Value * fondo2.ValorMetros.Value;
            //                        double valor3 = ObtenerValuacionUTipoParcela1(frente2.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo1, superficie3);

            //                        valorTierraUrbana += valor1 + valor2 + valor3;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_EN_TODA_LA_MANZANA_Y_SUP_ENTRE_2000M2_Y_15000M2: // 27
            //                {
            //                    var frente = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE);
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);

            //                    if (AllNotNullAndWithValue(new[] { frente, frente1, frente2, frente3 }, fraccion))
            //                    {
            //                        double valorAforo = ObtenerAforo(frente, idLocalidad);
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valorAforo3 = ObtenerAforo(frente3, idLocalidad);

            //                        double[] aforoArray = { valorAforo, valorAforo1, valorAforo2, valorAforo3 };
            //                        int indexMaxAforo = Array.IndexOf(aforoArray, aforoArray.Max());

            //                        double fondo = new[] { frente, frente1, frente2, frente3 }[indexMaxAforo].ValorMetros.Value;

            //                        var coeficiente = Contexto.VALCoef2a15k.FirstOrDefault(x => x.FondoMinimo <= fondo && x.FondoMaximo >= fondo &&
            //                                                                                    x.SuperficieMinima <= superficieParcela && x.SuperficieMaxima >= superficieParcela);

            //                        double aforoTotal = (valorAforo + valorAforo1 + valorAforo2 + valorAforo3) / 4;
            //                        valorTierraUrbana += ((coeficiente?.Coeficiente ?? 1) + 0.1) * superficieParcela * aforoTotal;
            //                    }
            //                }
            //                break;
            //            case ClasesEnum.PARCELA_CON_FRENTE_A_TRES_CALLES_MAYOR_A_2000M2: // 28
            //                {
            //                    var frente1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE1);
            //                    var frente2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE2);
            //                    var frente3 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FRENTE3);
            //                    var fondo1 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO1);
            //                    var fondo2 = fraccion.MedidasLineales.FirstOrDefault(x => x.ClaseParcelaMedidaLineal.IdTipoMedidaLineal == (int)TipoMedidaLinealEnum.FONDO2);

            //                    if (AllNotNullAndWithValue(new[] { frente1, frente2, frente3, fondo1, fondo2 }, fraccion))
            //                    {
            //                        double valorAforo1 = ObtenerAforo(frente1, idLocalidad);
            //                        double valorAforo2 = ObtenerAforo(frente2, idLocalidad);
            //                        double valorAforo3 = ObtenerAforo(frente3, idLocalidad);

            //                        double valor1;
            //                        double superficie1 = frente1.ValorMetros.Value * fondo1.ValorMetros.Value;
            //                        if (superficie1 < 2000)
            //                        {
            //                            valor1 = ObtenerValuacionUTipoParcela9(frente1, frente3, fondo1, idLocalidad, fraccion);
            //                        }
            //                        else
            //                        {
            //                            valor1 = ObtenerValuacionUTipoParcela12(frente1, frente3, fondo1, superficie1, idLocalidad, fraccion);
            //                        }

            //                        double valor2;
            //                        double superficie2 = frente2.ValorMetros.Value * fondo2.ValorMetros.Value;
            //                        if (superficie2 < 2000)
            //                        {
            //                            valor2 = ObtenerValuacionUTipoParcela1(frente2.ValorMetros.Value, fondo2.ValorMetros.Value, valorAforo2, superficie2);
            //                        }
            //                        else
            //                        {
            //                            valor2 = ObtenerValuacionUTipoParcela13(frente2, fondo2, valorAforo2, superficie2, fraccion);
            //                        }

            //                        valorTierraUrbana += valor1 + valor2;
            //                    }
            //                }
            //                break;
            //        }
            //    }
            //    valorTierra = (decimal)valorTierraUrbana;
            //}
            //else if (ddjj.Sor != null)
            //{
            //    var ddjjSor = ddjj.Sor;

            //    double puntajeEmplazamiento = 0;
            //    if (ddjjSor.IdCamino.HasValue && ddjjSor.DistanciaCamino.HasValue)
            //    {
            //        var puntajeCamino = Contexto.VALPuntajesCaminos.FirstOrDefault(x => x.IdCamino == ddjjSor.IdCamino && x.DistanciaMinima <= ddjjSor.DistanciaCamino && x.DistanciaMaxima >= ddjjSor.DistanciaCamino && !x.IdUsuarioBaja.HasValue);
            //        puntajeEmplazamiento = puntajeCamino?.Puntaje ?? 0;
            //    }

            //    if (ddjjSor.DistanciaEmbarque.HasValue)
            //    {
            //        var puntajeEmbarque = Contexto.VALPuntajesEmbarques.FirstOrDefault(x => x.DistanciaMinima <= ddjjSor.DistanciaEmbarque && x.DistanciaMaxima >= ddjjSor.DistanciaEmbarque && !x.IdUsuarioBaja.HasValue);
            //        puntajeEmplazamiento += puntajeEmbarque?.Puntaje ?? 0;
            //    }

            //    if (ddjjSor.IdLocalidad.HasValue && ddjjSor.DistanciaLocalidad.HasValue)
            //    {
            //        var puntajeLocalidad = Contexto.VALPuntajesLocalidades.FirstOrDefault(x => x.IdLocalidad == ddjjSor.IdLocalidad && x.DistanciaMinima <= ddjjSor.DistanciaLocalidad && x.DistanciaMaxima >= ddjjSor.DistanciaLocalidad && !x.IdUsuarioBaja.HasValue);
            //        puntajeEmplazamiento += puntajeLocalidad?.Puntaje ?? 0;
            //    }

            //    var designacion = ddjj.Designacion;

            //    double valores_parciales = 0;
            //    foreach (var sup in ddjjSor.Superficies)
            //    {
            //        long subtotal_puntaje = ddjjSor.SoRCars.Where(x => x.AptCar.IdAptitud == sup.IdAptitud).Sum(x => x.AptCar.Puntaje);
            //        valores_parciales += (sup.Superficie ?? 0) * ((puntajeEmplazamiento + subtotal_puntaje) / 100);
            //    }

            //    double? valor_optimo = null;
            //    if (TipoParcelaEnum.Suburbana == tipoParcela)
            //    {
            //        double superficieTotal = ddjjSor.Superficies.Sum(x => x.Superficie) ?? 0;
            //        var valor = Contexto.VALValoresOptSuburbanos.FirstOrDefault(x => x.IdLocalidad == designacion.IdLocalidad && x.SuperficieMinima <= superficieTotal && x.SuperficieMaxima >= superficieTotal && !x.IdUsuarioBaja.HasValue);
            //        valor_optimo = valor?.Valor;
            //    }
            //    else if (TipoParcelaEnum.Rural == tipoParcela)
            //    {
            //        var valor = Contexto.VALValoresOptRurales.FirstOrDefault(x => x.IdDepartamento == designacion.IdDepartamento && !x.IdUsuarioBaja.HasValue);
            //        valor_optimo = valor?.Valor;
            //    }

            //    valorTierra = (decimal)(valores_parciales * (valor_optimo ?? 1));
            //}
            //else
            //{
            //    throw new InvalidOperationException($"La unidad tributaria {ddjj.IdUnidadTributaria}, perteneciente al trámite {ddjj.IdTramite}, no tiene tiene DDJJ de tierra definida.");
            //}
            //return valorTierra;
        }

        private IEnumerable<Tuple<VALDecreto, VALValuacionDecretoTemporal>> ObtenerDecretos(IEnumerable<VALDecreto> decretos)
        {
            foreach (var decreto in decretos ?? new VALDecreto[0])
            {
                yield return
                    new Tuple<VALDecreto, VALValuacionDecretoTemporal>(
                        decreto,
                        new VALValuacionDecretoTemporal()
                        {
                            IdTramite = Tramite.IdTramite,
                            IdDecreto = decreto.IdDecreto,
                            IdUsuarioModif = Tramite.UsuarioModif,
                            IdUsuarioAlta = Tramite.UsuarioModif,
                            FechaAlta = Tramite.FechaModif,
                            FechaModif = Tramite.FechaModif
                        });
            }
        }
    }
}
