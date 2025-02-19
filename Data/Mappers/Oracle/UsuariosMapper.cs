using GeoSit.Data.BusinessEntities.Seguridad;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class UsuariosMapper : EntityTypeConfiguration<Usuarios>
    {
        public UsuariosMapper()
        {
            ToTable("SE_USUARIO")
                .HasKey(a => a.Id_Usuario);

            Property(a => a.Id_Usuario)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_USUARIO");

            Property(a => a.Login)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("LOGIN");

            Property(a => a.Nombre)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");

            Property(a => a.Apellido)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("APELLIDO");

            Property(a => a.Sexo)
                .HasColumnName("SEXO");

            Property(a => a.Mail)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MAIL");

            Property(a => a.Sector)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("SECTOR");

            Property(a => a.IdSector)
                .HasColumnName("ID_SECTOR");

            Property(a => a.Id_tipo_doc)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ID_TIPO_DOC");

            Property(a => a.Nro_doc)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NRO_DOC");

            Property(a => a.CUIL)
                .IsFixedLength()
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("CUIL");

            Property(a => a.Domicilio)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("DOMICILIO");

            Property(a => a.Habilitado)
                .IsRequired()
                .HasColumnName("HABILITADO");

            Property(a => a.Cambio_pass)
                .HasColumnName("CAMBIO_PASS");

            Property(a => a.Usuario_alta)
                .IsRequired()
                .HasColumnName("USUARIO_ALTA");

            Property(a => a.Fecha_alta)
                .IsRequired()
                .IsConcurrencyToken()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.Usuario_modificacion)
                .IsRequired()
                .HasColumnName("USUARIO_MODIFICACION");

            Property(a => a.Fecha_modificacion)
                .IsRequired()
                .IsConcurrencyToken()
                .HasColumnName("FECHA_MODIFICACION");

            Property(a => a.Usuario_baja)
                .HasColumnName("USUARIO_BAJA");

            Property(a => a.Fecha_baja)
                .IsConcurrencyToken()
                .HasColumnName("FECHA_BAJA");

            Property(a => a.CantidadIngresosFallidos)
                .HasColumnName("CANT_INGR_FALLIDOS");

            Property(a => a.IdISICAT)
                .HasColumnName("ID_ISICAT")
                .IsOptional();

            Property(a => a.LoginISICAT)
                .HasColumnName("LOGIN_ISICAT")
                .IsOptional();

            Property(a => a.NombreApellidoISICAT)
                .HasColumnName("NOMBRE_APELLIDO_ISICAT")
                .IsOptional();

            Property(a => a.VigenciaDesdeISICAT)
                .HasColumnName("VIGENCIA_DESDE_ISICAT")
                .IsOptional();

            Property(a => a.VigenciaHastaISICAT)
                .HasColumnName("VIGENCIA_HASTA_ISICAT")
                .IsOptional();

            Ignore(a => a.Fecha_Operacion);
            Ignore(a => a.NombreApellidoCompleto);

            HasOptional(a => a.SectorUsuario)
                .WithMany()
                .HasForeignKey(a => a.IdSector);
        }
    }
}
