using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.Mappers.Oracle.Temporal.Abstract;

namespace GeoSit.Data.Mappers.Oracle.Temporal.DDJJ
{
    public class DDJJSorCarTemporalMapper : TablaTemporalMapper<DDJJSorCarTemporal>
    {
        public DDJJSorCarTemporalMapper()
            : base("VAL_DDJJ_SOR_CAR")
        {
            HasKey(a => new { a.IdSuperficie, a.IdSorCar, a.IdTramite });

            Property(a => a.IdSorCar)
                .IsRequired()
                .HasColumnName("ID_SOR_CAR");

            Property(a => a.IdSuperficie)
                .IsRequired()
                .HasColumnName("ID_SUPERFICIE");

            Property(a => a.UsuarioAlta)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");

            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.UsuarioModif)
                .IsRequired()
                .HasColumnName("ID_USU_MODIF");

            Property(a => a.FechaModif)
                .IsRequired()
                .HasColumnName("FECHA_MODIF");

            Property(a => a.UsuarioBaja)
                .HasColumnName("ID_USU_BAJA");

            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");

            HasRequired(a => a.Caracteristica)
                .WithMany()
                .HasForeignKey(a => a.IdSorCar);
        }
    }
}
