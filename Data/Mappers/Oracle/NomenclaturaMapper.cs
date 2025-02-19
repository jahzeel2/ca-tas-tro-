using GeoSit.Data.BusinessEntities.Inmuebles;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class NomenclaturaInmuebleMapper : EntityTypeConfiguration<Nomenclatura>
    {
        public NomenclaturaInmuebleMapper()
        {
            ToTable("INM_NOMENCLATURA").HasKey(n => n.NomenclaturaID);

            Property(n => n.Nombre)
                .HasColumnName("NOMENCLATURA")
                .IsOptional();

            Property(n => n.NomenclaturaID)
                .HasColumnName("ID_NOMENCLATURA")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .IsRequired();

            Property(n => n.ParcelaID)
                .HasColumnName("ID_PARCELA")
                .IsRequired();

            Property(n => n.TipoNomenclaturaID)
                .HasColumnName("ID_TIPO_NOMENCLATURA")
                .IsRequired();

            Property(n => n.FechaAlta)
                .HasColumnName("FECHA_ALTA")
                .IsOptional();

            Property(n => n.FechaBaja)
                .HasColumnName("FECHA_BAJA")
                .IsOptional();

            Property(n => n.FechaModificacion)
                .HasColumnName("FECHA_MODIF")
                .IsOptional();

            Property(n => n.UsuarioAltaID)
                .HasColumnName("ID_USU_ALTA")
                .IsOptional();

            Property(n => n.UsuarioBajaID)
                .HasColumnName("ID_USU_BAJA")
                .IsOptional();

            Property(n => n.UsuarioModificacionID)
                .HasColumnName("ID_USU_MODIF")
                .IsOptional();


            HasRequired(n => n.Tipo)
                .WithMany()
                .HasForeignKey(n => n.TipoNomenclaturaID);

            HasRequired(nomenc => nomenc.Parcela)
                .WithMany(par => par.Nomenclaturas)
                .HasForeignKey(p => p.ParcelaID);
        }
    }
}
