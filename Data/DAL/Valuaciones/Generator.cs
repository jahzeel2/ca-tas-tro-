using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Valuaciones.Computations;
using GeoSit.Data.DAL.Valuaciones.Validators;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GeoSit.Data.DAL.Valuaciones
{
    public class Generator
    {
        protected readonly IDDJJValidator _validador;
        protected readonly IComputation _computation;

        public Generator(IComputation computation, IDDJJValidator validador)
        {
            _validador = validador;
            _computation = computation;
        }

        public Task<DatosComputo> SimulateAsync()
        {
            return ComputeAsync();
        }

        public async Task<VALValuacion> Generate()
        {
            var result = await ComputeAsync();
            return new VALValuacion()
            {
                DeclaracionJurada = _validador.DDJJ,
                IdUnidadTributaria = _validador.DDJJ.IdUnidadTributaria,
                Superficie = (double)result.SuperficieValuadaMT2,
                FechaDesde = _validador.DDJJ.FechaVigencia.Value,
                ValorTotal = result.Valuacion,
                ValorTierra = result.Valuacion,
                ValorMejoras = 0m,

                IdUsuarioAlta = _validador.DDJJ.IdUsuarioAlta,
                FechaAlta = _validador.DDJJ.FechaAlta,
                IdUsuarioModif = _validador.DDJJ.IdUsuarioModif,
                FechaModif = _validador.DDJJ.FechaModif,
            };
        }

        protected Task<DatosComputo> ComputeAsync()
        {
            if (_validador.Errors.Any())
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, _validador.Errors));
            }
            return _computation.ComputeAsync(_validador.DDJJ);
        }
    }
}
