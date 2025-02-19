using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class VALDecretoMapper : EntityTypeConfiguration<VALDecreto>
    {
        public VALDecretoMapper()
        {
            ToTable("VAL_DECRETOS");

            HasKey(a => a.IdDecreto);

            Property(a => a.IdDecreto)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_DECRETO");

            Property(a => a.NroDecreto)
               .IsRequired()
               .HasColumnName("NRO_DECRETO");

            Property(a => a.AnioDecreto)
                .IsRequired()
                .HasColumnName("ANIO_DECRETO");

            Property(a => a.FechaDecreto)
                .IsRequired()
                .HasColumnName("FECHA_DECRETO");


            Property(a => a.Coeficiente)
                .IsRequired()
                .HasColumnName("COEFICIENTE");

            Property(a => a.FechaInicio)
                .HasColumnName("FECHA_INICIO");

            Property(a => a.FechaFin)
                .HasColumnName("FECHA_FIN");

            Property(a => a.Aplicado)
                .HasColumnName("APLICADO");

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

        }
    }
}
