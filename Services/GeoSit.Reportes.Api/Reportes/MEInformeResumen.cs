using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class MEInformeResumen : DevExpress.XtraReports.UI.XtraReport
    {
        internal MEInformeResumen(ResumenTramite resumen) : this()
        {
            DataSource = new[] { resumen };
            dtlOrigenes.Visible = !resumen.TipoPlano && resumen.Origenes.Length > 0;
            dtlMensura.Visible = resumen.TipoPlano;

            rptFooter.ReportSource = new MESubReporteFooter();

            rptHeader.ReportSource = new MESubReporteHeader2();
            rptHeader.ReportSource.FindControl("txtTitulo", true).Text = "Resumen de Trámite";
            rptHeader.ReportSource.FindControl("lblPartida", true).Visible = false;
            rptHeader.ReportSource.FindControl("txtPartida", true).Visible = false;

        }
        public MEInformeResumen()
        {
            InitializeComponent();
        }
    }
}
