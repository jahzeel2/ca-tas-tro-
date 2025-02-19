using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System;
using System.Collections.Generic;

namespace GeoSit.Data.DAL.Valuaciones.Validators
{
    public interface IDDJJValidation
    {
        Tuple<DDJJ, IEnumerable<string>> Validate(DDJJ ddjj);
    }
}
