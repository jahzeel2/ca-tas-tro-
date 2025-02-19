using DevExpress.XtraReports.UI;
using GeoSit.Data.BusinessEntities.Seguridad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class InformeUsuarios : DevExpress.XtraReports.UI.XtraReport
    {
        internal InformeUsuarios(List<Usuarios> usersToExport, string filtro, string usuario) : this()
        {
            DataSource = usersToExport;
            rptFooter.ReportSource = new MESubReporteFooter(usuario);
            rptHeader.ReportSource = new MESubReporteHeader2();
            rptHeader.ReportSource.FindControl("txtTitulo", true).Text = $"Informe de Usuarios con {filtro}";
            rptHeader.ReportSource.FindControl("lblPartida", true).Visible = false;
            rptHeader.ReportSource.FindControl("txtPartida", true).Visible = false;
            this.lblTotalUsuarios.Text = $"   Total de usuarios encontrados con {filtro} = {usersToExport.Count}";
        }

        public InformeUsuarios()
        {
            InitializeComponent();
        }

    }
}
