using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using GeoSit.Data.BusinessEntities.MesaEntradas;

namespace GeoSit.Data.Mappers.Oracle.MesaEntradas
{
    public class METramiteEntradaMapper : EntityTypeConfiguration<METramiteEntrada>
    {
        public METramiteEntradaMapper()
        {
            ToTable("ME_TRAMITE_ENTRADA")
                .HasKey(a => a.IdTramiteEntrada);

            Property(a => a.IdTramiteEntrada)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_TRAMITE_ENTRADA");

            Property(a => a.IdTramite)
                .IsRequired()
                .HasColumnName("ID_TRAMITE");

            Property(a => a.IdComponente)
                .IsRequired()
                .HasColumnName("ID_COMPONENTE");

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

            Property(a => a.IdObjeto)
                .HasColumnName("ID_OBJETO");

            Property(a => a.IdObjetoEntrada)
                .HasColumnName("ID_OBJETO_ENTRADA");

            Property(a => a.TipoEntrada)
                .HasColumnName("TIPO_ENTRADA");

            HasRequired(a => a.ObjetoEntrada)
                .WithMany()
                .HasForeignKey(a => a.IdObjetoEntrada);
        }
    }
}
