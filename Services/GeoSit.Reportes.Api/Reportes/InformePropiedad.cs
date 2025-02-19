using DevExpress.XtraReports.UI;
using GeoSit.Data.BusinessEntities.Inmuebles;
using System.Collections.Generic;
using System.Linq;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class InformePropiedad : XtraReport
    {
        public InformePropiedad()
        {
            InitializeComponent();
        }

        private void DetailReport_DataSourceRowChanged(object sender, DataSourceRowEventArgs e)
        {
            var ut = Report.GetCurrentRow() as UnidadTributaria;
            if (ut.Parcela.Designaciones.Any())
            {
                var designacion = ut.Parcela.Designaciones.ElementAt(e.CurrentRow);
                XRLabel Label = ((XRControl)sender).FindControl("lblDesignacion", true) as XRLabel;

                var partes = new List<string>();

                if (designacion != null && designacion.IdDesignacion != 0)
                {
                    if (!string.IsNullOrEmpty(designacion.Departamento))
                    {
                        partes.Add($"Departamento: {designacion.Departamento}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Localidad))
                    {
                        partes.Add($"Localidad: {designacion.Localidad}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Paraje))
                    {
                        partes.Add($"Paraje: {designacion.Paraje}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Calle))
                    {
                        partes.Add($"Calle: {designacion.Calle}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Numero))
                    {
                        partes.Add($"Número: {designacion.Numero}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Seccion))
                    {
                        partes.Add($"Sección: {designacion.Seccion}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Chacra))
                    {
                        partes.Add($"Chacra: {designacion.Chacra}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Quinta))
                    {
                        partes.Add($"Quinta: {designacion.Quinta}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Fraccion))
                    {
                        partes.Add($"Fracción: {designacion.Fraccion}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Manzana))
                    {
                        partes.Add($"Manzana: {designacion.Manzana}");
                    }
                    if (!string.IsNullOrEmpty(designacion.Lote))
                    {
                        partes.Add($"Lote: {designacion.Lote}");
                    }
                }
                Label.Text = string.Join(" ", partes);
            }
        }
        /*public InformePropiedad(AtributosDocumento atributoDoc):this()
{
   numMensura.Text = $"{ atributoDoc.numero_plano }-{ atributoDoc.letra_plano }";
   vigenciaMensura.Text = atributoDoc.fecha_vigencia_actual.ToString("dd/MM/yyyy");
}*/
    }
}
