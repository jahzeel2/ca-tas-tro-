using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using INM = GeoSit.Data.BusinessEntities.Inmuebles;
using System;
using System.Collections.Generic;
using System.Linq;
using GeoSit.Client.Web.Models;
using FormVal = GeoSit.Client.Web.Models.FormularioValuacion;
using GeoSit.Data.BusinessEntities.Temporal;

namespace GeoSit.Client.Web.Helpers.ExtensionMethods
{
    public static class ExtensionMethods
    {
        public static string ToStringOrDefault(this object obj)
        {
            return obj == null ? "N/D" : obj.ToString();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static List<TSource> MoveElementToFirstPosition<TSource>(this List<TSource> source, TSource element)
        {
            if (element != null)
            {
                source.RemoveAt(source.IndexOf(element));
                source.Insert(0, element);
            }
            return source;
        }

        public static IEnumerable<INM.TipoInscripcion> FilterTiposIncripcion(this IEnumerable<INM.TipoInscripcion> tipos, bool esParcelaPrescripcion)
        {
            string DESCRIPCION_POSESION = "POSESION";
            if (esParcelaPrescripcion)
            {
                tipos = tipos.Where(x => x.Descripcion.ToUpper() == DESCRIPCION_POSESION);
            }
            else
            {
                tipos = tipos.Where(x => x.Descripcion.ToUpper() != DESCRIPCION_POSESION).OrderBy(x => x.Descripcion);
            }
            return tipos;
        }

        public static IEnumerable<INM.TipoTitularidad> FilterTiposTitularidad(this IEnumerable<INM.TipoTitularidad> tipos, bool esParcelaPrescripcion)
        {
            string DESCRIPCION_POSEEDOR = "POSEEDOR";
            if (esParcelaPrescripcion)
            {
                tipos = tipos.Where(x => x.Descripcion.ToUpper() == DESCRIPCION_POSEEDOR);
            }
            else
            {
                tipos = tipos.Where(x => x.Descripcion.ToUpper() != DESCRIPCION_POSEEDOR).OrderBy(x => x.Descripcion);
            }
            return tipos;
        }

        public static bool IsPrescripcion(this INM.Parcela parcela)
        {
            long ID_CLASE_PRESCRIPCION = 2;
            return parcela.ClaseParcelaID == ID_CLASE_PRESCRIPCION;
        }
    }

    //namespace DeclaracionesJuradasTemporal
    //{
    //    internal static class DDJJXTMethods
    //    {
    //        internal static FormularioSoRModel ToFormularioSoR(this DDJJTemporal ddjjTemp, DDJJVersion version, IEnumerable<Persona> personas, IEnumerable<VALAptitudes> aptitudes)
    //        {
    //            return new FormularioSoRModel()
    //            {
    //                DDJJ = ddjjTemp.ToDDJJ(version),
    //                DDJJSor = ddjjTemp.Sor.ToDDJJSoR(),
    //                DDJJDesignacion = ddjjTemp.Designacion.ToDDJJDesignacion(),
    //                dominiosJSON = JsonConvert.SerializeObject(ddjjTemp.Dominios.ToDDJJDominios(personas)),
    //                AptitudesDisponibles = ddjjTemp.Sor.ToDDJJAptitudesInput(aptitudes)
    //            };
    //        }
    //        private static DDJJ ToDDJJ(this DDJJTemporal tempObj, DDJJVersion version)
    //        {
    //            return new DDJJ
    //            {
    //                IdDeclaracionJurada = tempObj.IdDeclaracionJurada,
    //                IdVersion = tempObj.IdVersion,
    //                IdOrigen = tempObj.IdOrigen,
    //                IdUnidadTributaria = tempObj.IdUnidadTributaria,
    //                IdPoligono = tempObj.IdPoligono,
    //                FechaVigencia = tempObj.FechaVigencia,

    //                Version = version,
    //            };
    //        }
    //        private static DDJJDesignacion ToDDJJDesignacion(this DDJJDesignacionTemporal tempObj)
    //        {
    //            return new DDJJDesignacion
    //            {
    //                IdTipoDesignador = tempObj.IdTipoDesignador,

    //                Barrio = tempObj.Barrio,
    //                Calle = tempObj.Calle,
    //                Chacra = tempObj.Chacra,
    //                CodigoPostal = tempObj.CodigoPostal,
    //                Departamento = tempObj.Departamento,
    //                Fraccion = tempObj.Fraccion,
    //                Localidad = tempObj.Localidad,
    //                Lote = tempObj.Lote,
    //                Manzana = tempObj.Manzana,
    //                Numero = tempObj.Numero,
    //                Paraje = tempObj.Paraje,
    //                Quinta = tempObj.Quinta,
    //                Seccion = tempObj.Seccion,
    //                IdBarrio = tempObj.IdBarrio,
    //                IdCalle = tempObj.IdCalle,
    //                IdDepartamento = tempObj.IdDepartamento,
    //                IdLocalidad = tempObj.IdLocalidad,
    //                IdManzana = tempObj.IdManzana,
    //                IdParaje = tempObj.IdParaje,
    //                IdSeccion = tempObj.IdSeccion,

    //                IdUsuarioAlta = tempObj.UsuarioAlta,
    //                FechaAlta = tempObj.FechaAlta,
    //                IdUsuarioModif = tempObj.UsuarioModif,
    //                FechaModif = tempObj.FechaModif
    //            };
    //        }
    //        private static List<DDJJDominio> ToDDJJDominios(this IEnumerable<DDJJDominioTemporal> dominiosTemp, IEnumerable<Persona> personas)
    //        {
    //            return dominiosTemp.Select(dt =>
    //            {
    //                return new DDJJDominio()
    //                {
    //                    Fecha = dt.Fecha,
    //                    IdDominio = dt.IdDominio,
    //                    IdTipoInscripcion = dt.IdTipoInscripcion,
    //                    TipoInscripcion = dt.TipoInscripcion,
    //                    Inscripcion = dt.Inscripcion,
    //                    Titulares = dt.Titulares.ToDDJJDominiosTitulares(personas),
    //                };
    //            }).ToList();
    //        }
    //        private static List<DDJJDominioTitular> ToDDJJDominiosTitulares(this IEnumerable<DDJJDominioTitularTemporal> titularesTemp, IEnumerable<Persona> personas)
    //        {
    //            return titularesTemp
    //                    .Select(tt =>
    //                    {
    //                        var persona = tt.BuscarPersona(personas);
    //                        return new DDJJDominioTitular()
    //                        {
    //                            IdDominioTitular = tt.IdDominioTitular,
    //                            IdDominio = tt.IdDominio,
    //                            IdPersona = tt.IdPersona,
    //                            IdTipoTitularidad = tt.IdTipoTitularidad,
    //                            NombreCompleto = persona.NombreCompleto,
    //                            PersonaDomicilio = tt.PersonaDomicilios.ToDDJJPersonaDomicilios(),
    //                            PorcientoCopropiedad = tt.PorcientoCopropiedad,
    //                            TipoNoDocumento = $"{persona.TipoDocumentoIdentidad.Descripcion} / {persona.NroDocumento}",
    //                            TipoTitularidad = tt.TipoTitularidad.Descripcion,
    //                        };
    //                    })
    //                    .ToList();
    //        }
    //        private static ICollection<DDJJPersonaDomicilio> ToDDJJPersonaDomicilios(this IEnumerable<DDJJPersonaDomicilioTemporal> personaDomicilios)
    //        {
    //            return personaDomicilios.Select(pd => new DDJJPersonaDomicilio()
    //            {
    //                Altura = pd.Domicilio.numero_puerta,
    //                Barrio = pd.Domicilio.barrio,
    //                Calle = pd.Domicilio.ViaNombre,
    //                CodigoPostal = pd.Domicilio.codigo_postal,
    //                Departamento = pd.Domicilio.unidad,
    //                IdCalle = pd.Domicilio.ViaId,
    //                IdDomicilio = pd.IdDomicilio,
    //                IdDominioTitular = pd.IdDominioTitular,
    //                IdTipoDomicilio = pd.IdTipoDomicilio,
    //                Localidad = pd.Domicilio.localidad,
    //                Municipio = pd.Domicilio.municipio,
    //                Pais = pd.Domicilio.pais,
    //                Piso = pd.Domicilio.piso,
    //                Provincia = pd.Domicilio.provincia,
    //                Tipo = pd.TipoDomicilio.Descripcion
    //            }).ToList();
    //        }

    //        private static DDJJSor ToDDJJSoR(this DDJJSorTemporal tempObj)
    //        {
    //            return new DDJJSor
    //            {
    //                IdMensura = tempObj.IdMensura,

    //                IdUsuarioAlta = tempObj.UsuarioAlta,
    //                FechaAlta = tempObj.FechaAlta,
    //                IdUsuarioModif = tempObj.UsuarioModif,
    //                FechaModif = tempObj.FechaModif
    //            };
    //        }
    //        private static List<VALAptitudInput> ToDDJJAptitudesInput(this DDJJSorTemporal tempObj, IEnumerable<VALAptitudes> aptitudes)
    //        {
    //            return aptitudes.Select(apt =>
    //                            {
    //                                var superficie = tempObj.Superficies.SingleOrDefault(s => s.IdAptitud == apt.IdAptitud);
    //                                return new VALAptitudInput()
    //                                {
    //                                    IdAptitud = apt.IdAptitud,
    //                                    Superficie = (superficie?.Superficie ?? 0d).ToString("F4"),
    //                                    RelieveSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.Relieve),
    //                                    AguasDelSubsueloSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.AguaEnSubsuelo),
    //                                    CapacidadesGanaderasSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.CapacidadGanadera),
    //                                    ColoresTierraSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.ColorDeLaTierra),
    //                                    EspesoresCapaArableSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.EspesorCapaArable),
    //                                    EstadosMonteSeleccionado = tempObj.SoRCars.ObtenerValorCaracteristica(apt, TiposCaracteristicas.CalidadMonte)
    //                                };
    //                            }).ToList();
    //        }
    //        private static string ObtenerValorCaracteristica(this IEnumerable<DDJJSorCarTemporal> caracteristicas, VALAptitudes aptitud, TiposCaracteristicas caracteristica)
    //        {
    //            throw new NotImplementedException("Arreglar este quilombo");
    //            return caracteristicas
    //                    .SingleOrDefault(x => x.AptCar.IdAptitud == aptitud.IdAptitud &&
    //                                         (TiposCaracteristicas)x.AptCar.SorCaracteristica.IdSorTipoCaracteristica == caracteristica)
    //                    ?.AptCar
    //                    ?.IdSorCar.ToString() ?? string.Empty;
    //        }
    //        private static Persona BuscarPersona(this DDJJDominioTitularTemporal domTitularTemp, IEnumerable<Persona> personas)
    //        {
    //            return personas.Single(p => p.PersonaId == domTitularTemp.IdPersona);
    //        }
    //    }
    //}

    namespace FormularioValuacion
    {
        public static class XTMethods
        {
            public static FormVal.DDJJModel ToDDJJVigenteModel(this DDJJ ddjj)
            {
                return FormVal.DDJJModel.FromEntityVigente(ddjj);
            }

            public static FormVal.DDJJModel ToDDJJHistoricaModel(this DDJJ ddjj)
            {
                return FormVal.DDJJModel.FromEntityHistorica(ddjj);
            }

            public static FormVal.ValuacionModel ToValuacionModel(this VALValuacion valuacion)
            {
                return FormVal.ValuacionModel.FromEntity(valuacion);
            }

            public static FormVal.AptitudModel ToAptitudModel(this VALAptitudes aptitud)
            {
                return FormVal.AptitudModel.FromEntity(aptitud);
            }

            public static FormVal.SuperficieModel ToSuperficieModel(this VALSuperficie superficie, bool copia)
            {
                return FormVal.SuperficieModel.FromEntity(superficie, copia);
            }

            public static FormVal.SuperficieModel ToSuperficieModel(this VALSuperficieTemporal superficie)
            {
                return FormVal.SuperficieModel.FromTemporal(superficie);
            }

            public static List<FormVal.GrupoCaracteristicaModel> ToListOfGruposCaracteristicasModel(this IEnumerable<DDJJSorTipoCaracteristica> tipos)
            {
                return tipos.GroupBy(t => t.GrupoCaracteristica)
                            .OrderBy(grp => grp.Key.IdGrupoCaracteristica)
                            .Select(grp => FormVal.GrupoCaracteristicaModel.Create(grp.Key, grp))
                            .ToList();
            }

        }
    }

    namespace Personas
    {
        public static class XTMethods
        {
            public static PersonaModel ToPersonaModel(this Data.BusinessEntities.Personas.Persona persona)
            {
                return new PersonaModel
                {
                    Apellido = persona.Apellido,
                    CUIL = persona.CUIL,
                    Email = persona.Email,
                    EstadoCivil = persona.EstadoCivil,
                    Nacionalidad = persona.Nacionalidad,
                    Nombre = persona.Nombre,
                    NroDocumento = persona.NroDocumento,
                    PersonaId = persona.PersonaId,
                    Sexo = persona.Sexo,
                    Telefono = persona.Telefono,
                    TipoDocId = persona.TipoDocId,
                    TipoDocumento = persona.TipoDocumentoIdentidad?.Descripcion,
                    TipoPersonaId = persona.TipoPersonaId
                };
            }

            public static DomicilioModel ToDomicilioModel(this Data.BusinessEntities.Personas.PersonaDomicilio personaDomicilio)
            {
                return new DomicilioModel
                {
                    barrio = personaDomicilio.Domicilio.barrio,
                    codigo_postal = personaDomicilio.Domicilio.codigo_postal,
                    DomicilioId = personaDomicilio.Domicilio.DomicilioId,
                    IdLocalidad = personaDomicilio.Domicilio.IdLocalidad,
                    localidad = personaDomicilio.Domicilio.localidad,
                    municipio = personaDomicilio.Domicilio.municipio,
                    numero_puerta = personaDomicilio.Domicilio.numero_puerta,
                    pais = personaDomicilio.Domicilio.pais,
                    piso = personaDomicilio.Domicilio.piso,
                    provincia = personaDomicilio.Domicilio.provincia,
                    ProvinciaId = personaDomicilio.Domicilio.ProvinciaId,
                    TipoDomicilioId = personaDomicilio.TipoDomicilioId,
                    TipoDomicilio = personaDomicilio.TipoDomicilio,
                    ubicacion = personaDomicilio.Domicilio.ubicacion,
                    unidad = personaDomicilio.Domicilio.unidad,
                    ViaId = personaDomicilio.Domicilio.ViaId,
                    ViaNombre = personaDomicilio.Domicilio.ViaNombre,
                };
            }
        }
    }

    namespace Mensuras
    {
        public static class XTMethods
        {
            public static MensuraModel ToMensuraModel(this INM.Mensura mensura)
            {
                return new MensuraModel()
                {
                    IdMensura = mensura.IdMensura,

                    Numero = mensura.Numero,
                    Anio = mensura.Anio,
                    Departamento = mensura.Departamento,
                    Descripcion = mensura.Descripcion,
                    IdEstadoMensura = mensura.IdEstadoMensura,
                    IdTipoMensura = mensura.IdTipoMensura,
                    FechaAprobacion = mensura.FechaAprobacion,
                    FechaPresentacion = mensura.FechaPresentacion,
                    Observaciones = mensura.Observaciones,
                };
            }
            public static ParcelaMensuraModels ToParcelaMensuraModel(this INM.ParcelaMensura parcelaMensura)
            {
                return new ParcelaMensuraModels()
                {
                    IdMensura = parcelaMensura.IdMensura,
                    IdParcela = parcelaMensura.IdParcela,
                    IdParcelaMensura = parcelaMensura.IdParcelaMensura,
                    Nomenclatura = parcelaMensura.Parcela.Nomenclaturas.FirstOrDefault()?.Nombre
                };
            }
            public static MensuraRelacionadaModels ToMensuraDestinoRelacionadaModel(this INM.MensuraRelacionada mensuraRelacionada)
            {
                return new MensuraRelacionadaModels()
                {
                    IdMensuraRelacionada = mensuraRelacionada.IdMensuraRelacionada,
                    MensuraDescripcion = $"{mensuraRelacionada.MensuraDestino.Departamento.PadLeft(2, '0')}-{mensuraRelacionada.MensuraDestino.Numero.PadLeft(3, '0')}-{mensuraRelacionada.MensuraDestino.Anio}",
                    IdMensuraDestino = mensuraRelacionada.IdMensuraDestino
                };
            }
            public static MensuraRelacionadaModels ToMensuraOrigenRelacionadaModel(this INM.MensuraRelacionada mensuraRelacionada)
            {
                return new MensuraRelacionadaModels()
                {
                    IdMensuraRelacionada = mensuraRelacionada.IdMensuraRelacionada,
                    MensuraDescripcion = $"{mensuraRelacionada.MensuraOrigen.Departamento.PadLeft(2, '0')}-{mensuraRelacionada.MensuraOrigen.Numero.PadLeft(3, '0')}-{mensuraRelacionada.MensuraOrigen.Anio}",
                    IdMensuraOrigen = mensuraRelacionada.IdMensuraOrigen
                };
            }
            public static MensuraDocumentoModels ToMensuraDocumentoModel(this INM.MensuraDocumento mensuraDocumento)
            {
                return new MensuraDocumentoModels()
                {
                    IdMensura = mensuraDocumento.IdMensura,
                    IdDocumento = mensuraDocumento.IdDocumento,
                    IdMensuraDocumento = mensuraDocumento.IdMensuraDocumento,
                    Extension = mensuraDocumento.Documento.extension_archivo,
                    Nombre = mensuraDocumento.Documento.nombre_archivo,
                    Tipo = mensuraDocumento.Documento.Tipo?.Descripcion ?? "N/D",
                    Fecha = mensuraDocumento.FechaAlta,
                    Descripcion = mensuraDocumento.Documento.descripcion ?? mensuraDocumento.Documento.nombre_archivo
                };
            }
        }
    }
}