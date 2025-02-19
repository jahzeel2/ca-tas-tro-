using GeoSit.Data.BusinessEntities.MesaEntradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.MesaEntradas
{
    public class MEObjetoEntradaMapper : EntityTypeConfiguration<MEObjetoEntrada>
    {
        public MEObjetoEntradaMapper()
        {
            ToTable("ME_OBJETO_ENTRADA")
                .HasKey(a => a.IdObjetoEntrada);

            Property(a => a.IdObjetoEntrada)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_OBJETO_ENTRADA");

            Property(a => a.IdEntrada)
                .IsRequired()
                .HasColumnName("ID_ENTRADA");

            Property(a => a.IdObjetoTramite)
                .IsRequired()
                .HasColumnName("ID_OBJETO_TRAMITE");

            Property(a => a.Obligatorio)
                .IsRequired()
                .HasColumnName("OBLIGATORIO");

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
        }
    }
}
