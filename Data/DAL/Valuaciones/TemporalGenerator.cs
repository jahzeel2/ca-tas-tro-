using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.DAL.Valuaciones.Computations;
using GeoSit.Data.DAL.Valuaciones.Validators;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Valuaciones
{
    internal class TemporalGenerator : Generator
    {
        private readonly DDJJTemporal _ddjjTemporal;

        public TemporalGenerator(int idTramite, IComputation computation, IDDJJValidator validator)
            : base(computation, validator)
        {
            _ddjjTemporal = new DDJJTemporal
            {
                IdDeclaracionJurada = long.MinValue,
                IdTramite = idTramite,
                IdVersion = Convert.ToInt64(VersionesDDJJ.SoR),
                FechaVigencia = validator.DDJJ.FechaVigencia,
                UsuarioModif = validator.DDJJ.IdUsuarioModif,
                UsuarioAlta = validator.DDJJ.IdUsuarioAlta,
                FechaModif = validator.DDJJ.FechaModif,
                FechaAlta = validator.DDJJ.FechaAlta,
                IdUnidadTributaria = validator.DDJJ.IdUnidadTributaria,
                Sor = new DDJJSorTemporal()
                {
                    IdSor = long.MinValue,
                    IdTramite = idTramite,
                    UsuarioAlta = validator.DDJJ.Sor.IdUsuarioAlta,
                    UsuarioModif = validator.DDJJ.Sor.IdUsuarioAlta,
                    FechaModif = validator.DDJJ.Sor.FechaModif,
                    FechaAlta = validator.DDJJ.Sor.FechaAlta,
                    Superficies = validator.DDJJ.Sor.Superficies.Select((s, idx) => new VALSuperficieTemporal()
                    {
                        IdSuperficie = (idx + 1) * -1,
                        IdTramite = idTramite,
                        IdAptitud = s.IdAptitud,
                        Superficie = s.Superficie * 10_000,
                        TrazaDepreciable = s.TrazaDepreciable,
                        Puntaje = s.Puntaje,
                        Caracteristicas = s.Caracteristicas.Select((c, jdx) => new DDJJSorCarTemporal()
                        {
                            IdSorCar = c.IdSorCar,
                            IdTramite = idTramite,
                            UsuarioModif = c.IdUsuarioModif,
                            UsuarioAlta = c.IdUsuarioAlta,
                            FechaModif = c.FechaModif,
                            FechaAlta = c.FechaAlta
                        }).ToList()
                    }).ToList()
                },
            };
        }

        public new async Task<VALValuacionTemporal> Generate()
        {
            var result = await ComputeAsync();

            return new VALValuacionTemporal()
            {
                DeclaracionJurada = _ddjjTemporal,
                IdUnidadTributaria = _ddjjTemporal.IdUnidadTributaria, 
                IdTramite = _ddjjTemporal.IdTramite, 

                Superficie = (double)result.SuperficieValuadaMT2,
                FechaDesde = _ddjjTemporal.FechaVigencia.Value,
                ValorTotal = result.Valuacion,
                ValorTierra = result.Valuacion,
                ValorMejoras = 0m,

                IdUsuarioAlta = _ddjjTemporal.UsuarioAlta,
                FechaAlta = _validador.DDJJ.FechaAlta,
                IdUsuarioModif = _ddjjTemporal.UsuarioModif,
                FechaModif = _validador.DDJJ.FechaModif,
            };
        }
    }
}
