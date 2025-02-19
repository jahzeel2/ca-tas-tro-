using GeoSit.Data.BusinessEntities.Documentos;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class DocumentoMapper : EntityTypeConfiguration<Documento>
    {
        public DocumentoMapper()
        {
            ToTable("DOC_DOCUMENTOS")
                .HasKey(t => t.id_documento);

            Property(a => a.id_documento)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_DOCUMENTO");

            Property(a => a.id_tipo_documento)
                .IsRequired()
                .HasColumnName("ID_TIPO_DOCUMENTO");

            Property(a => a.fecha)
                .HasColumnName("FECHA");

            Property(a => a.descripcion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DESCRIPCION");

            Property(a => a.observaciones)
                .IsUnicode(false)
                .HasColumnName("OBSERVACIONES");

            Property(a => a.nombre_archivo)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("NOMBRE_ARCHIVO");

            Property(a => a.extension_archivo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("EXTENSION_ARCHIVO");

            Property(a => a.id_usu_alta)
                .HasColumnName("ID_USU_ALTA");

            Property(a => a.fecha_alta_1)
                .HasColumnName("FECHA_ALTA");

            Property(a => a.id_usu_modif)
                .HasColumnName("ID_USU_MODIF");

            Property(a => a.fecha_modif)
                .HasColumnName("FECHA_MODIF");

            Property(a => a.id_usu_baja)
                .HasColumnName("ID_USU_BAJA");

            Property(a => a.fecha_baja_1)
                .HasColumnName("FECHA_BAJA");

            Property(a => a.contenido)
                .HasColumnName("CONTENIDO");

            Property(a => a.ruta)
                .HasColumnName("RUTA")
                .HasMaxLength(255);

            Property(a => a.atributos)
                .HasColumnName("ATRIBUTOS");


            /*Navegacion*/
            HasRequired(a => a.Tipo)
                .WithMany(a => a.Documentos)
                .HasForeignKey(d => d.id_tipo_documento);
        }
    }
}
