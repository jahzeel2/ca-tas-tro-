
namespace GeoSit.Reportes.Api.Reportes
{
    partial class InformeUsuarios
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.rptFooter = new DevExpress.XtraReports.UI.XRSubreport();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.tblBodyUsers = new DevExpress.XtraReports.UI.XRTable();
            this.tableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
            this.cellLogin = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.tableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.cellPerfiles = new DevExpress.XtraReports.UI.XRTableCell();
            this.objectDataSource1 = new DevExpress.DataAccess.ObjectBinding.ObjectDataSource(this.components);
            this.Title = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailCaption1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailData1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DetailData3_Odd = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PageInfo = new DevExpress.XtraReports.UI.XRControlStyle();
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.tblMovimientos = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow5 = new DevExpress.XtraReports.UI.XRTableRow();
            this.TIPO = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.GroupFooter1 = new DevExpress.XtraReports.UI.GroupFooterBand();
            this.lblTotalUsuarios = new DevExpress.XtraReports.UI.XRLabel();
            this.rptHeader = new DevExpress.XtraReports.UI.XRSubreport();
            ((System.ComponentModel.ISupportInitialize)(this.tblBodyUsers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.objectDataSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tblMovimientos)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 30F;
            this.TopMargin.Name = "TopMargin";
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.rptFooter});
            this.BottomMargin.HeightF = 103F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // rptFooter
            // 
            this.rptFooter.LocationFloat = new DevExpress.Utils.PointFloat(0F, 9.999974F);
            this.rptFooter.Name = "rptFooter";
            this.rptFooter.SizeF = new System.Drawing.SizeF(780.2083F, 83.00005F);
            // 
            // ReportHeader
            // 
            this.ReportHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.rptHeader});
            this.ReportHeader.HeightF = 81.25F;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.tblBodyUsers});
            this.Detail.HeightF = 25F;
            this.Detail.Name = "Detail";
            // 
            // tblBodyUsers
            // 
            this.tblBodyUsers.BackColor = System.Drawing.Color.Transparent;
            this.tblBodyUsers.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.tblBodyUsers.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.tblBodyUsers.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.tblBodyUsers.Name = "tblBodyUsers";
            this.tblBodyUsers.OddStyleName = "DetailData3_Odd";
            this.tblBodyUsers.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.tableRow2});
            this.tblBodyUsers.SizeF = new System.Drawing.SizeF(780.2083F, 25F);
            this.tblBodyUsers.StylePriority.UseBackColor = false;
            this.tblBodyUsers.StylePriority.UseBorderColor = false;
            this.tblBodyUsers.StylePriority.UseFont = false;
            // 
            // tableRow2
            // 
            this.tableRow2.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.cellLogin,
            this.tableCell13,
            this.tableCell14,
            this.tableCell15,
            this.cellPerfiles});
            this.tableRow2.Name = "tableRow2";
            this.tableRow2.Weight = 11.5D;
            // 
            // cellLogin
            // 
            this.cellLogin.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.cellLogin.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.cellLogin.BorderWidth = 3F;
            this.cellLogin.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Login]")});
            this.cellLogin.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.cellLogin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(119)))), ((int)(((byte)(121)))), ((int)(((byte)(118)))));
            this.cellLogin.Name = "cellLogin";
            this.cellLogin.StyleName = "DetailData1";
            this.cellLogin.StylePriority.UseBorderColor = false;
            this.cellLogin.StylePriority.UseBorders = false;
            this.cellLogin.StylePriority.UseBorderWidth = false;
            this.cellLogin.StylePriority.UseFont = false;
            this.cellLogin.StylePriority.UseForeColor = false;
            this.cellLogin.StylePriority.UseTextAlignment = false;
            this.cellLogin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.cellLogin.Weight = 0.14464040994365268D;
            // 
            // tableCell13
            // 
            this.tableCell13.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.tableCell13.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.tableCell13.BorderWidth = 3F;
            this.tableCell13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Apellido]")});
            this.tableCell13.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.tableCell13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(119)))), ((int)(((byte)(121)))), ((int)(((byte)(118)))));
            this.tableCell13.Name = "tableCell13";
            this.tableCell13.StyleName = "DetailData1";
            this.tableCell13.StylePriority.UseBorderColor = false;
            this.tableCell13.StylePriority.UseBorders = false;
            this.tableCell13.StylePriority.UseBorderWidth = false;
            this.tableCell13.StylePriority.UseFont = false;
            this.tableCell13.StylePriority.UseForeColor = false;
            this.tableCell13.StylePriority.UseTextAlignment = false;
            this.tableCell13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell13.Weight = 0.14464314282156862D;
            // 
            // tableCell14
            // 
            this.tableCell14.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.tableCell14.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.tableCell14.BorderWidth = 3F;
            this.tableCell14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Nombre]")});
            this.tableCell14.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.tableCell14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(119)))), ((int)(((byte)(121)))), ((int)(((byte)(118)))));
            this.tableCell14.Name = "tableCell14";
            this.tableCell14.StyleName = "DetailData1";
            this.tableCell14.StylePriority.UseBorderColor = false;
            this.tableCell14.StylePriority.UseBorders = false;
            this.tableCell14.StylePriority.UseBorderWidth = false;
            this.tableCell14.StylePriority.UseFont = false;
            this.tableCell14.StylePriority.UseForeColor = false;
            this.tableCell14.StylePriority.UseTextAlignment = false;
            this.tableCell14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell14.TextFormatString = "{0:N0}";
            this.tableCell14.Weight = 0.14464317618514205D;
            // 
            // tableCell15
            // 
            this.tableCell15.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.tableCell15.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.tableCell15.BorderWidth = 3F;
            this.tableCell15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Sector]")});
            this.tableCell15.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.tableCell15.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(119)))), ((int)(((byte)(121)))), ((int)(((byte)(118)))));
            this.tableCell15.Name = "tableCell15";
            this.tableCell15.StyleName = "DetailData1";
            this.tableCell15.StylePriority.UseBorderColor = false;
            this.tableCell15.StylePriority.UseBorders = false;
            this.tableCell15.StylePriority.UseBorderWidth = false;
            this.tableCell15.StylePriority.UseFont = false;
            this.tableCell15.StylePriority.UseForeColor = false;
            this.tableCell15.StylePriority.UseTextAlignment = false;
            this.tableCell15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.tableCell15.TextFormatString = "{0:N0}";
            this.tableCell15.Weight = 0.18659436869499776D;
            // 
            // cellPerfiles
            // 
            this.cellPerfiles.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.cellPerfiles.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.cellPerfiles.BorderWidth = 3F;
            this.cellPerfiles.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Perfiles]")});
            this.cellPerfiles.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.cellPerfiles.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(119)))), ((int)(((byte)(121)))), ((int)(((byte)(118)))));
            this.cellPerfiles.Name = "cellPerfiles";
            this.cellPerfiles.StyleName = "DetailData1";
            this.cellPerfiles.StylePriority.UseBorderColor = false;
            this.cellPerfiles.StylePriority.UseBorders = false;
            this.cellPerfiles.StylePriority.UseBorderWidth = false;
            this.cellPerfiles.StylePriority.UseFont = false;
            this.cellPerfiles.StylePriority.UseForeColor = false;
            this.cellPerfiles.StylePriority.UseTextAlignment = false;
            this.cellPerfiles.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.cellPerfiles.TextFormatString = "{0:N0}";
            this.cellPerfiles.Weight = 0.26884593450163569D;
            // 
            // objectDataSource1
            // 
            this.objectDataSource1.DataSource = typeof(GeoSit.Data.BusinessEntities.Seguridad.Usuarios);
            this.objectDataSource1.Name = "objectDataSource1";
            // 
            // Title
            // 
            this.Title.BackColor = System.Drawing.Color.Transparent;
            this.Title.BorderColor = System.Drawing.Color.Black;
            this.Title.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Title.BorderWidth = 1F;
            this.Title.Font = new System.Drawing.Font("Arial", 14.25F);
            this.Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(75)))), ((int)(((byte)(75)))));
            this.Title.Name = "Title";
            this.Title.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
            // 
            // DetailCaption1
            // 
            this.DetailCaption1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(75)))), ((int)(((byte)(75)))));
            this.DetailCaption1.BorderColor = System.Drawing.Color.White;
            this.DetailCaption1.Borders = DevExpress.XtraPrinting.BorderSide.Left;
            this.DetailCaption1.BorderWidth = 2F;
            this.DetailCaption1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.DetailCaption1.ForeColor = System.Drawing.Color.White;
            this.DetailCaption1.Name = "DetailCaption1";
            this.DetailCaption1.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
            this.DetailCaption1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // DetailData1
            // 
            this.DetailData1.BorderColor = System.Drawing.Color.Transparent;
            this.DetailData1.Borders = DevExpress.XtraPrinting.BorderSide.Left;
            this.DetailData1.BorderWidth = 2F;
            this.DetailData1.Font = new System.Drawing.Font("Arial", 8.25F);
            this.DetailData1.ForeColor = System.Drawing.Color.Black;
            this.DetailData1.Name = "DetailData1";
            this.DetailData1.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
            this.DetailData1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // DetailData3_Odd
            // 
            this.DetailData3_Odd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(231)))), ((int)(((byte)(231)))));
            this.DetailData3_Odd.BorderColor = System.Drawing.Color.Transparent;
            this.DetailData3_Odd.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.DetailData3_Odd.BorderWidth = 1F;
            this.DetailData3_Odd.Font = new System.Drawing.Font("Arial", 8.25F);
            this.DetailData3_Odd.ForeColor = System.Drawing.Color.Black;
            this.DetailData3_Odd.Name = "DetailData3_Odd";
            this.DetailData3_Odd.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
            this.DetailData3_Odd.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // PageInfo
            // 
            this.PageInfo.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.PageInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(75)))), ((int)(((byte)(75)))), ((int)(((byte)(75)))));
            this.PageInfo.Name = "PageInfo";
            this.PageInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(6, 6, 0, 0, 100F);
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.tblMovimientos});
            this.GroupHeader1.HeightF = 22.22223F;
            this.GroupHeader1.Name = "GroupHeader1";
            // 
            // tblMovimientos
            // 
            this.tblMovimientos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(53)))), ((int)(((byte)(101)))));
            this.tblMovimientos.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(6)))), ((int)(((byte)(45)))), ((int)(((byte)(85)))));
            this.tblMovimientos.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.tblMovimientos.BorderWidth = 3F;
            this.tblMovimientos.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.tblMovimientos.ForeColor = System.Drawing.Color.White;
            this.tblMovimientos.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.tblMovimientos.Name = "tblMovimientos";
            this.tblMovimientos.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 96F);
            this.tblMovimientos.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow5});
            this.tblMovimientos.SizeF = new System.Drawing.SizeF(780.2083F, 22.22223F);
            this.tblMovimientos.StylePriority.UseBackColor = false;
            this.tblMovimientos.StylePriority.UseBorderColor = false;
            this.tblMovimientos.StylePriority.UseBorders = false;
            this.tblMovimientos.StylePriority.UseBorderWidth = false;
            this.tblMovimientos.StylePriority.UseFont = false;
            this.tblMovimientos.StylePriority.UseForeColor = false;
            this.tblMovimientos.StylePriority.UseTextAlignment = false;
            this.tblMovimientos.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTableRow5
            // 
            this.xrTableRow5.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.TIPO,
            this.xrTableCell5,
            this.xrTableCell3,
            this.xrTableCell4,
            this.xrTableCell1});
            this.xrTableRow5.Name = "xrTableRow5";
            this.xrTableRow5.Weight = 1D;
            // 
            // TIPO
            // 
            this.TIPO.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.TIPO.Multiline = true;
            this.TIPO.Name = "TIPO";
            this.TIPO.StylePriority.UseBorders = false;
            this.TIPO.Text = "USUARIO";
            this.TIPO.Weight = 4.5577474119308148D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell5.Multiline = true;
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.StylePriority.UseBorders = false;
            this.xrTableCell5.Text = "APELLIDO";
            this.xrTableCell5.Weight = 4.5578334260176785D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell3.Multiline = true;
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.StylePriority.UseBorders = false;
            this.xrTableCell3.Text = "NOMBRE";
            this.xrTableCell3.Weight = 4.55783342601768D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell4.Multiline = true;
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.StylePriority.UseBorders = false;
            this.xrTableCell4.Text = "SECTOR";
            this.xrTableCell4.Weight = 5.8797535248822754D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.Borders = ((DevExpress.XtraPrinting.BorderSide)((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell1.Multiline = true;
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.StylePriority.UseBorders = false;
            this.xrTableCell1.Text = "PERFILES";
            this.xrTableCell1.Weight = 8.4715749194298837D;
            // 
            // GroupFooter1
            // 
            this.GroupFooter1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblTotalUsuarios});
            this.GroupFooter1.HeightF = 34.99997F;
            this.GroupFooter1.Name = "GroupFooter1";
            // 
            // lblTotalUsuarios
            // 
            this.lblTotalUsuarios.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(137)))), ((int)(((byte)(141)))), ((int)(((byte)(187)))));
            this.lblTotalUsuarios.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(124)))), ((int)(((byte)(130)))), ((int)(((byte)(189)))));
            this.lblTotalUsuarios.BorderWidth = 3F;
            this.lblTotalUsuarios.Font = new System.Drawing.Font("Calibri", 14F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalUsuarios.ForeColor = System.Drawing.Color.White;
            this.lblTotalUsuarios.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.lblTotalUsuarios.Multiline = true;
            this.lblTotalUsuarios.Name = "lblTotalUsuarios";
            this.lblTotalUsuarios.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblTotalUsuarios.SizeF = new System.Drawing.SizeF(780.2083F, 34.91666F);
            this.lblTotalUsuarios.StylePriority.UseBackColor = false;
            this.lblTotalUsuarios.StylePriority.UseBorderColor = false;
            this.lblTotalUsuarios.StylePriority.UseBorderWidth = false;
            this.lblTotalUsuarios.StylePriority.UseFont = false;
            this.lblTotalUsuarios.StylePriority.UseForeColor = false;
            this.lblTotalUsuarios.StylePriority.UseTextAlignment = false;
            this.lblTotalUsuarios.Text = "   Total de Usuarios = XXX";
            this.lblTotalUsuarios.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // rptHeader
            // 
            this.rptHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.rptHeader.Name = "rptHeader";
            this.rptHeader.ReportSource = new GeoSit.Reportes.Api.Reportes.MESubReporteHeader2();
            this.rptHeader.SizeF = new System.Drawing.SizeF(780.2083F, 81.25F);
            // 
            // InformeUsuarios
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.BottomMargin,
            this.ReportHeader,
            this.Detail,
            this.GroupHeader1,
            this.GroupFooter1});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.objectDataSource1});
            this.DataSource = this.objectDataSource1;
            this.Font = new System.Drawing.Font("Arial", 9.75F);
            this.Margins = new System.Drawing.Printing.Margins(30, 38, 30, 103);
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.Title,
            this.DetailCaption1,
            this.DetailData1,
            this.DetailData3_Odd,
            this.PageInfo});
            this.Version = "21.1";
            ((System.ComponentModel.ISupportInitialize)(this.tblBodyUsers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.objectDataSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tblMovimientos)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.DataAccess.ObjectBinding.ObjectDataSource objectDataSource1;
        private DevExpress.XtraReports.UI.XRControlStyle Title;
        private DevExpress.XtraReports.UI.XRControlStyle DetailCaption1;
        private DevExpress.XtraReports.UI.XRControlStyle DetailData1;
        private DevExpress.XtraReports.UI.XRControlStyle DetailData3_Odd;
        private DevExpress.XtraReports.UI.XRControlStyle PageInfo;
        internal DevExpress.XtraReports.UI.XRSubreport rptHeader;
        private DevExpress.XtraReports.UI.XRSubreport rptFooter;
        private DevExpress.XtraReports.UI.GroupHeaderBand GroupHeader1;
        private DevExpress.XtraReports.UI.XRTable tblMovimientos;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow5;
        private DevExpress.XtraReports.UI.XRTableCell TIPO;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell1;
        private DevExpress.XtraReports.UI.XRTable tblBodyUsers;
        private DevExpress.XtraReports.UI.XRTableRow tableRow2;
        private DevExpress.XtraReports.UI.XRTableCell cellLogin;
        private DevExpress.XtraReports.UI.XRTableCell tableCell13;
        private DevExpress.XtraReports.UI.XRTableCell tableCell14;
        private DevExpress.XtraReports.UI.XRTableCell tableCell15;
        private DevExpress.XtraReports.UI.XRTableCell cellPerfiles;
        private DevExpress.XtraReports.UI.GroupFooterBand GroupFooter1;
        private DevExpress.XtraReports.UI.XRLabel lblTotalUsuarios;
    }
}
