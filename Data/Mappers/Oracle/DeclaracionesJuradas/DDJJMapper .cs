using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJMapper : EntityTypeConfiguration<DDJJ>
    {
        public DDJJMapper()
        {
            ToTable("VAL_DDJJ")
                .HasKey(a => a.IdDeclaracionJurada);

            Property(a => a.IdDeclaracionJurada)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_DDJJ");

            Property(a => a.IdVersion)
                .IsRequired()
                .HasColumnName("ID_DDJJ_VERSION");

            Property(a => a.IdOrigen)
                .HasColumnName("ID_DDJJ_ORIGEN");

            Property(a => a.IdUnidadTributaria)
                .HasColumnName("ID_UNIDAD_TRIBUTARIA");

            Property(a => a.IdPoligono)
                .HasColumnName("ID_PLANO");

            Property(a => a.IdTramiteObjeto)
                .HasColumnName("ID_TRAMITE_OBJETO");

            Property(a => a.FechaVigencia)
                .HasColumnName("FECHA_VIGENCIA");

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
                .WithMany()
                .HasForeignKey(a => a.IdVersion);

            HasRequired(a => a.Origen)
                .WithMany()
                .HasForeignKey(a => a.IdOrigen);

            HasRequired(a => a.Sor)
                .WithRequiredPrincipal()
                .Map(x => x.MapKey("ID_DDJJ"));

            HasMany(a => a.Valuaciones)
                .WithRequired(x => x.DeclaracionJurada)
                .Map(x => x.MapKey("ID_DDJJ"));

            HasMany(a => a.Dominios)
                .WithRequired()
                .Map(x => x.MapKey("ID_DDJJ"))
                .WillCascadeOnDelete();

            /*
            HasRequired(a => a.Designacion)
                .WithRequiredPrincipal()
                .Map(x => x.MapKey("ID_DDJJ"));
            */
        }
    }
}
