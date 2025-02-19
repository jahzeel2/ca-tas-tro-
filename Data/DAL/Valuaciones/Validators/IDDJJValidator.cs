using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;

namespace GeoSit.Data.DAL.Valuaciones.Validators
{
    public interface IDDJJValidator
    {
        string[] Errors { get; }
        DDJJ DDJJ { get; }

        IDDJJValidator Validate(IDDJJValidation validator);
    }
}
