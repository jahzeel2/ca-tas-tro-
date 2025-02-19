using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Valuaciones.Validators.Validations
{
    public class FechaVigenciaValidation : IDDJJValidation
    {
        public Tuple<DDJJ, IEnumerable<string>> Validate(DDJJ ddjj)
        {
            string[] errores = new string[0];
            if (!ddjj.FechaVigencia.HasValue)
            {
                errores = new[] { "La DDJJ no está completa. Falta la fecha de vigencia" };
            }

            return Tuple.Create(ddjj, (IEnumerable<string>)errores);

        }
    }
}
