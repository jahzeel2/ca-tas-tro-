using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJSorMapper : EntityTypeConfiguration<DDJJSor>
    {
        public DDJJSorMapper()
        {
            ToTable("VAL_DDJJ_SOR")
                .HasKey(a => a.IdSor);

            Property(a => a.IdSor)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .IsRequired()
                .HasColumnName("ID_DDJJ_SOR");

            Property(a => a.IdMensura)
                .HasColumnName("ID_MENSURA");

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

            HasOptional(a => a.Mensuras)
                .WithMany()
                .HasForeignKey(a => a.IdMensura);
        }
    }
}
