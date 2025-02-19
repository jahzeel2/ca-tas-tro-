using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJSorCarMapper : EntityTypeConfiguration<DDJJSorCar>
    {
        public DDJJSorCarMapper()
        {
            ToTable("VAL_DDJJ_SOR_CAR")
                .HasKey(a => new { a.IdSuperficie, a.IdSorCar });

            Property(a => a.IdSorCar)
                .IsRequired()
                .HasColumnName("ID_SOR_CAR");

            Property(a => a.IdSuperficie)
                .IsRequired()
                .HasColumnName("ID_SUPERFICIE");

            Property(a => a.IdUsuarioAlta)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");

            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.IdUsuarioModif)
                .IsRequired()
                .HasColumnName("ID_USU_MODIF");

            Property(a => a.FechaModif)
                .IsRequired()
                .HasColumnName("FECHA_MODIF");

            Property(a => a.IdUsuarioBaja)
                .HasColumnName("ID_USU_BAJA");

            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");

            HasRequired(a => a.Caracteristica)
                .WithMany()
                .HasForeignKey(a => a.IdSorCar);
        }
    }
}