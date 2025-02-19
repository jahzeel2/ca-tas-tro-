using GeoSit.Data.BusinessEntities.Personas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class ProfesionMapper : EntityTypeConfiguration<Profesion>
    {
        public ProfesionMapper()
        {
            ToTable("INM_PROFESION")
                .HasKey(x => new { x.PersonaId, x.TipoProfesionId });

            Property(a => a.PersonaId)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .HasColumnName("ID_PERSONA");
            Property(a => a.TipoProfesionId)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .HasColumnName("ID_TIPO_PROFESION");
            Property(a => a.Matricula)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MATRICULA");

            HasRequired(x => x.TipoProfesion)
                .WithMany()
                .HasForeignKey(x => x.TipoProfesionId);
        }
    }
}
