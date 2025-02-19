using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class VALAptitudesMapper : EntityTypeConfiguration<VALAptitudes>
    {
        public VALAptitudesMapper()
        {
            ToTable("VAL_APTITUDES")
                .HasKey(a => a.IdAptitud);

            Property(a => a.IdAptitud)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_APTITUD");

            Property(a => a.IdVersion)
                .IsRequired()
                .HasColumnName("ID_DDJJ_VERSION");

            Property(a => a.Numero)
                .HasColumnName("NUMERO");

            Property(a => a.Descripcion)
                .HasColumnName("DESCRIPCION");

            Property(a => a.PuntajeDepreciable)
                .IsRequired()
                .HasColumnName("PUNTAJE_DEPRECIABLE");

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


            HasRequired(a => a.Version)
                .WithMany(a => a.Aptitudes)
                .HasForeignKey(a => a.IdVersion);
        }
    }
}
