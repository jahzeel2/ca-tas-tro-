using DevExpress.XtraReports.UI;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class MEHojaDeRuta : DevExpress.XtraReports.UI.XtraReport
    {
        internal MEHojaDeRuta(METramite meTramite, string usuario) : this()
        {
            DataSource = new[] { meTramite };
            rptFooter.ReportSource = new MESubReporteFooter(usuario);
            rptHeader.ReportSource = new MESubReporteHeader2();
            rptHeader.ReportSource.FindControl("txtTitulo", true).Text = "Hoja de ruta";
            lblNumeroTramite.Text = "     SGT #: " + meTramite.Numero + "  |  DPCyC #: " + meTramite.IdTramite;
            rptHeader.ReportSource.FindControl("txtPartida", true).Visible = false;
            rptHeader.ReportSource.FindControl("lblPartida", true).Visible = false;
        }
        public MEHojaDeRuta()
        {
            InitializeComponent();
        }
    }
}
