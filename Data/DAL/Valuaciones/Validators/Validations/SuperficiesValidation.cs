using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Data.DAL.Valuaciones.Validators.Validations
{
    public class SuperficiesValidation : IDDJJValidation
    { 
        public Tuple<DDJJ,IEnumerable<string>> Validate(DDJJ ddjj)
        {
            var errores = new List<string>();
            if (!(ddjj.Sor?.Superficies?.Any() ?? false))
            {
                errores.Add("El formulario está incompleto. Se debe incluir al menos un código de terreno.");
            }
            else if (ddjj.Sor.Superficies.Any(s => (s.Superficie ?? 0d) == 0d))
            {
                errores.Add("El formulario está incompleto. Los códigos de terreno deben especificar la superficie.");
            }
            return Tuple.Create(ddjj, errores.AsEnumerable());
        }
    }
}
