using GeoSit.Data.BusinessEntities.Personas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class PersonaMapper : EntityTypeConfiguration<Persona>
    {
        public PersonaMapper()
        {
            ToTable("INM_PERSONA")
                .HasKey(a => a.PersonaId);

            Property(a => a.PersonaId)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_PERSONA");
            Property(a => a.TipoDocId)
                .IsRequired()
                .HasColumnName("ID_TIPO_DOC_IDENT");
            Property(a => a.NroDocumento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NRO_DOCUMENTO");
            Property(a => a.TipoPersonaId)
                .IsRequired()
                .HasColumnName("ID_TIPO_PERSONA");
            Property(a => a.NombreCompleto)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOMBRE_COMPLETO");
            Property(a => a.Nombre)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
            Property(a => a.Apellido)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("APELLIDO");
            Property(a => a.UsuarioAltaId)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");
            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");
            Property(a => a.UsuarioModifId)
                .HasColumnName("ID_USU_MODIF");
            Property(a => a.FechaModif)
                .HasColumnName("FECHA_MODIF");
            Property(a => a.UsuarioBajaId)
                .HasColumnName("ID_USU_BAJA");
            Property(a => a.FechaBaja)
                .IsOptional()
                .HasColumnName("FECHA_BAJA");
            Property(a => a.Sexo)
                .HasColumnName("SEXO");
            Property(a => a.EstadoCivil)
                .HasColumnName("ESTADOCIVIL");
            Property(a => a.Nacionalidad)
                .HasColumnName("NACIONALIDAD");
            Property(a => a.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TELEFONO");
            Property(a => a.Email)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            Property(a => a.CUIL)
                .IsFixedLength()
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("CUIL");

            //Relaciones
            HasRequired(p => p.TipoPersona)
                .WithMany(tp => tp.Personas)
                .HasForeignKey(p => p.TipoPersonaId);

            HasRequired(p => p.TipoDocumentoIdentidad)
                .WithMany()
                .HasForeignKey(p => p.TipoDocId);

            HasMany(p => p.PersonaDomicilios)
                .WithRequired()
                .HasForeignKey(p => p.PersonaId);
        }

    }

}
