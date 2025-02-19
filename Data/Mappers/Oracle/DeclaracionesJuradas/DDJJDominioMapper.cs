using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJDominioMapper : EntityTypeConfiguration<DDJJDominio>
    {
        public DDJJDominioMapper()
        {
            ToTable("VAL_DDJJ_DOMINIO")
                .HasKey(a => a.IdDominio);

            Property(a => a.IdDominio)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_DDJJ_DOMINIO");

            Property(a => a.IdTipoInscripcion)
                .IsRequired()
                .HasColumnName("ID_TIPO_INSCRIPCION");

            Property(a => a.Inscripcion)
                .IsRequired()
                .HasColumnName("INSCRIPCION");

            Property(a => a.Fecha)
                .IsRequired()
                .HasColumnName("FECHA");

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

            HasRequired(a => a.TipoInscripcion)
                .WithMany()
                .HasForeignKey(a => a.IdTipoInscripcion);

            Ignore(a => a.NombreTipoInscripcion);
        }
    }
}
