using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Validators.Validations
{
    public class CaracteristicasValidation : IDDJJValidation
    {
        private readonly GeoSITMContext _context;
        public CaracteristicasValidation(GeoSITMContext ctx)
        {
            _context = ctx;
        }
        public Tuple<DDJJ, IEnumerable<string>> Validate(DDJJ ddjj)
        {
            var superficies = ddjj.Sor?.Superficies ?? new VALSuperficie[0];
            if (!superficies.Any())
            {
                return Tuple.Create(ddjj, (new[] { "No hay características configuradas." }).AsEnumerable());
            }
            return Tuple.Create(ddjj, superficies.SelectMany(Validate));
        }
        private IEnumerable<string> Validate(VALSuperficie superficie)
        {
            var errors = new List<string>();
            var aptitud = _context.VALAptitudes
                                  .Include(x => x.GruposCaracteristicas.Select(g => g.Tipos.Select(t => t.Caracteristicas)))
                                  .Single(x => x.IdAptitud == superficie.IdAptitud);

            var caracteristicasAptitud = aptitud.GruposCaracteristicas.SelectMany(g => g.Tipos.SelectMany(t => t.Caracteristicas));

            var grupos = caracteristicasAptitud
                                .GroupBy(c=>c.IdSorTipoCaracteristica)
                                .Select(g => new { tipo = g.Key, caracteristicas = g.Select(c => c.IdSorCaracteristica) })
                                .ToArray();
            var seleccionadas = superficie.Caracteristicas.Select(c => c.IdSorCar);

            bool hayGruposSinCaracteristicaSeleccionada = grupos.Any(g => g.caracteristicas.All(c => !seleccionadas.Any(s => c == s)));
            if (hayGruposSinCaracteristicaSeleccionada)
            {
                errors.Add($"Hay características del código de terreno {aptitud.Descripcion}({superficie.Superficie}) que no se han especificado.");
            }

            bool maximoUnaCaracteristicaPorTipo = false;
            int idx = 0;
            do
            {
                maximoUnaCaracteristicaPorTipo = grupos[idx++]
                                                    .caracteristicas
                                                    .Count(disponible => seleccionadas.Count(c => c == disponible) == 1) <= 1;
            }
            while (maximoUnaCaracteristicaPorTipo && idx < grupos.Length);

            if (!maximoUnaCaracteristicaPorTipo)
            {
                errors.Add($"Hay características del código de terreno {aptitud.Descripcion}({superficie.Superficie}) que tienen más de una opción configurada.");
            }

            bool sinCaracteristicasNoPertenecientesALaAptitud = seleccionadas.All(c => grupos.Any(g => g.caracteristicas.Any(d => d == c)));
            if (!sinCaracteristicasNoPertenecientesALaAptitud)
            {
                errors.Add($"Hay características configuradas en el código de terreno {aptitud.Descripcion}({superficie.Superficie}) que no pertenecen al código de terreno especificado.");
            }
            return errors;
        }
    }
}
