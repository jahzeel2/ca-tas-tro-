using GeoSit.Data.BusinessEntities.MesaEntradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.MesaEntradas
{
    public class MEObjetoTramiteMapper : EntityTypeConfiguration<MEObjetoTramite>
    {
        public MEObjetoTramiteMapper()
        {
            ToTable("ME_OBJETO_TRAMITE")
                .HasKey(a => a.IdObjetoTramite);

            Property(a => a.IdObjetoTramite)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .HasColumnName("ID_OBJETO_TRAMITE");

            Property(a => a.Descripcion)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DESCRIPCION");

            Property(a => a.IdTipoTramite)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .HasColumnName("ID_TIPO_TRAMITE");

            Property(a => a.Plantilla)
                .IsRequired()
                .HasColumnName("PLANTILLA");

            Property(a => a.IdSistemaExterno)
                .HasColumnName("ID_CAUSA_SGT");

            Property(a => a.UsuarioAlta)
                .IsRequired()
                .HasColumnName("USUARIO_ALTA");

            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.UsuarioModif)
                .IsRequired()
                .HasColumnName("USUARIO_MODIF");

            Property(a => a.FechaModif)
                .IsRequired()
                .HasColumnName("FECHA_MODIF");

            Property(a => a.UsuarioBaja)
                .HasColumnName("USUARIO_BAJA");

            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");
        }
    }
}
