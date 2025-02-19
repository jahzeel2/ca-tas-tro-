using GeoSit.Data.BusinessEntities.MesaEntradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.MesaEntradas
{
    public class METramiteMapper : EntityTypeConfiguration<METramite>
    {
        public METramiteMapper()
        {
            ToTable("ME_TRAMITE")
                .HasKey(a => a.IdTramite);

            Property(a => a.IdTramite)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_TRAMITE");

            Property(a => a.Numero)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("NUMERO");

            Property(a => a.IdSGT)
                .HasColumnName("ID_SGT");

            Property(a => a.IdPrioridad)
                .IsRequired()
                .HasColumnName("ID_PRIORIDAD");

            Property(a => a.IdIniciador)
                .IsRequired()
                .HasColumnName("ID_INICIADOR");

            Property(a => a.FechaInicio)
                .IsRequired()
                .HasColumnName("FECHA_INICIO");

            Property(a => a.FechaIngreso)
                .HasColumnName("FECHA_INGRESO");

            Property(a => a.FechaLibro)
                .HasColumnName("FECHA_LIBRO");

            Property(a => a.FechaVencimiento)
                .HasColumnName("FECHA_VENC");

            Property(a => a.IdTipoTramite)
                .IsRequired()
                .HasColumnName("ID_TIPO_TRAMITE");

            Property(a => a.IdObjetoTramite)
                .IsRequired()
                .HasColumnName("ID_OBJETO_TRAMITE");

            Property(a => a.Motivo)
                .HasMaxLength(4000)
                .IsUnicode(false)
                .HasColumnName("MOTIVO");

            Property(a => a.IdEstado)
                .IsRequired()
                .HasColumnName("ID_ESTADO");

            Property(a => a.Comprobante)
                .HasColumnName("COMPROBANTE");

            Property(a => a.Monto)
                .HasColumnName("MONTO");

            Property(a => a.Plano)
                .HasColumnName("NUMERO_PLANO");

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


            HasRequired(a => a.Iniciador)
                .WithMany()
                .HasForeignKey(a => a.IdIniciador);

            HasRequired(a => a.Profesional)
                .WithMany()
                .HasForeignKey(a => a.UsuarioAlta);

            HasRequired(a => a.UltimoOperador)
                .WithMany()
                .HasForeignKey(a => a.UsuarioModif);

            HasRequired(a => a.Prioridad)
                .WithMany()
                .HasForeignKey(a => a.IdPrioridad);

            HasRequired(a => a.Tipo)
                .WithMany()
                .HasForeignKey(a => a.IdTipoTramite);

            HasRequired(a => a.Objeto)
                .WithMany()
                .HasForeignKey(a => a.IdObjetoTramite);

            HasRequired(a => a.Estado)
                .WithMany()
                .HasForeignKey(a => a.IdEstado);
        }
    }
}
