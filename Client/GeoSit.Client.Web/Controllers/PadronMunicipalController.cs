using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using GeoSit.Client.Web.Models;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GeoSit.Client.Web.Controllers
{
    public class PadronMunicipalController : Controller
    {
        private readonly HttpClient _client = new HttpClient();
        private FileResult FileToDownload { get { return Session["file_to_download"] as FileResult; } set { Session["file_to_download"] = value; } }

        public class ResultadoConsulta
        {
            public string Nomenclatura { get; set; }
            public string DescripcionTipoParcela { get; set; }
            public string CodigoProvincial { get; set; }
            public int? uf { get; set; }
            public string Ubicacion { get; set; }
            public string coordenadas { get; set; }
            public string AfectacionPh { get; set; }
            public string Inscripcion { get; set; }
            public string FechaDesde { get; set; }
            public string ValorTierra { get; set; }
            public string ValorMejoras { get; set; }
            public string ValorTotal { get; set; }
            public string SuperficieRegistrada { get; set; }
            public string SuperficieGrafica { get; set; }
            public string unidadSup { get; set; }
        }

        public PadronMunicipalController()
        {
            _client.BaseAddress = new Uri(ConfigurationManager.AppSettings["WebApiUrl"]);
        }

        public FileResult DownloadFile()
        {
            return FileToDownload;
        }


        public ActionResult Index()
        {
            ViewData["Nombre"] = new SelectList(GetMunicipios(), "Value", "Text");

            return PartialView();
        }

        public List<SelectListItem> GetMunicipios()
        {
            try
            {
                using (var resp = _client.GetAsync($"api/objetoadministrativoservice/GetObjetoByTipo/{(long)TipoObjetoAdministrativoEnum.Municipio}").Result.EnsureSuccessStatusCode())
                {
                    return resp.Content.ReadAsAsync<IEnumerable<ObjetoAdministrativoModel>>().Result
                               .OrderBy(x => x.Nombre)
                               .Select(obj => new SelectListItem() { Text = obj.Nombre, Value = obj.FeatId.ToString() })
                               .ToList();
                }
            }
            catch (Exception)
            {
                return new List<SelectListItem>();
            }
        }

        public List<ResultadoConsulta> GetParcelasMunicipioVigentes(int idMunicipio)
        {
            HttpResponseMessage resp = _client.GetAsync("api/PadronMunicipal/GetParcelasMunicipioVigentes?idMunicipio=" + idMunicipio).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<ResultadoConsulta>>().Result;
        }

        public List<ResultadoConsulta> GetParcelasMunicipioHistorico(int idMunicipio)
        {
            HttpResponseMessage resp = _client.GetAsync("api/PadronMunicipal/GetParcelasMunicipioHistorico?idMunicipio=" + idMunicipio).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<ResultadoConsulta>>().Result;
        }

        public List<ResultadoConsulta> GetUTsMunicipioVigentes(int idMunicipio)
        {
            HttpResponseMessage resp = _client.GetAsync("api/PadronMunicipal/GetUTsMunicipioVigentes?idMunicipio=" + idMunicipio).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<ResultadoConsulta>>().Result;
        }

        public List<ResultadoConsulta> GetUTsMunicipioHistorico(int idMunicipio)
        {
            HttpResponseMessage resp = _client.GetAsync("api/PadronMunicipal/GetUTsMunicipioHistorico?idMunicipio=" + idMunicipio).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsAsync<List<ResultadoConsulta>>().Result;
        }

        public ActionResult InformePadronMunicipal(int idMunicipio, bool vigentes, bool tipoInformeParcelario, string nomMunicipio)
        {
            var lstPadron = new List<ResultadoConsulta>();
            string titulo1 = "";

            if (vigentes && tipoInformeParcelario)
            {
                lstPadron = GetParcelasMunicipioVigentes(idMunicipio);
                titulo1 = "Padrón Provincial de Parcelas / Inmuebles Vigentes";
               
            }
            else if (!vigentes && tipoInformeParcelario)
            {
                lstPadron = GetParcelasMunicipioHistorico(idMunicipio);
                titulo1 = "Padrón Provincial de Parcelas / Históricas";
            }
            else if (vigentes && !tipoInformeParcelario)
            {
                lstPadron = GetUTsMunicipioVigentes(idMunicipio);
                titulo1 = "Padrón Provincial de UTs / Inmuebles Vigentes";

            }
            else if (!vigentes && !tipoInformeParcelario)
            {
                lstPadron = GetUTsMunicipioHistorico(idMunicipio);
                titulo1 = "Padrón Provincial de UTs / Históricas";
            }


            ExcelPackage package = new ExcelPackage();
            package.Workbook.Worksheets.Add("Padrón Municipal");
            package.Workbook.Worksheets.Add("Instructivo");

            ExcelWorksheet ew1 = package.Workbook.Worksheets[0];
            ExcelWorksheet ew2 = package.Workbook.Worksheets[1];

            //Defino los colores de las celdas
            Color colorCelesteClaro = ColorTranslator.FromHtml("#BDD7EE");
            Color colorAmarilloClaro = ColorTranslator.FromHtml("#FFE699");
            Color colorVerdeClaro = ColorTranslator.FromHtml("#C6E0B4");
            Color colorNaranjaClaro = ColorTranslator.FromHtml("#F8CBAD");
            Color colorAzul = ColorTranslator.FromHtml("#0070C0");

            //Armo filas de padrón
            int rowActual = 5;
            foreach (var row in lstPadron)
            {
                ew1.Cells["A" + rowActual.ToString()].Value = row.Nomenclatura;
                ew1.Cells["A" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["A" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["B" + rowActual.ToString()].Value = row.DescripcionTipoParcela;
                ew1.Cells["B" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["B" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["C" + rowActual.ToString()].Value = row.CodigoProvincial;
                ew1.Cells["C" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["C" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["D" + rowActual.ToString()].Value = row.uf;
                ew1.Cells["D" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["D" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["E" + rowActual.ToString()].Value = row.Ubicacion;
                ew1.Cells["E" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["E" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["F" + rowActual.ToString()].Value = row.coordenadas;
                ew1.Cells["F" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["F" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["G" + rowActual.ToString()].Value = row.AfectacionPh;
                ew1.Cells["G" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["G" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["H" + rowActual.ToString()].Value = row.Inscripcion;
                ew1.Cells["H" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["H" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["I" + rowActual.ToString()].Value = row.FechaDesde;
                ew1.Cells["I" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["I" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["J" + rowActual.ToString()].Value = row.ValorTierra;
                ew1.Cells["J" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ew1.Cells["J" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["K" + rowActual.ToString()].Value = row.ValorMejoras;
                ew1.Cells["K" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ew1.Cells["K" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["L" + rowActual.ToString()].Value = row.ValorTotal;
                ew1.Cells["L" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ew1.Cells["L" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["M" + rowActual.ToString()].Value = row.SuperficieRegistrada;
                ew1.Cells["M" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ew1.Cells["M" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["N" + rowActual.ToString()].Value = row.SuperficieGrafica;
                ew1.Cells["N" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ew1.Cells["N" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ew1.Cells["O" + rowActual.ToString()].Value = row.unidadSup;
                ew1.Cells["O" + rowActual.ToString()].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ew1.Cells["O" + rowActual.ToString()].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                rowActual++;
            }

            ew1.Cells["A1:E1"].Merge = true;
            ew1.Cells["A2:E2"].Merge = true;
            ew1.Cells["A3:E3"].Merge = true;
            ew1.Column(1).Width = 35;
            ew1.Row(1).Height = 30;
            ew1.Cells["A1"].Value = titulo1;
            ew1.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["A1"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.White);
            ew1.Cells["A1"].Style.Font.Size = 14;
            ew1.Cells["A1"].Style.Font.Bold = true;
            ew1.Cells["A2"].Value = "Municipio de " + nomMunicipio;
            ew1.Cells["A2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["A2"].Style.Font.Size = 12;
            ew1.Cells["A2"].Style.Font.Bold = true;
            ew1.Cells["A2"].Style.WrapText = true;
            ew1.Cells["A2"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["A2"].Style.Fill.BackgroundColor.SetColor(Color.White);
            ew1.Cells["A3"].Value = "Fecha de creación: " + DateTime.Now.ToString("dd/MM/yyyy");
            ew1.Cells["A3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["A3"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["A3"].Style.Fill.BackgroundColor.SetColor(Color.White);
            ew1.Cells["A3"].Style.Font.Size = 11;
            ew1.Cells["A3"].Style.Font.Bold = false;
            ew1.Cells["A3"].Style.WrapText = true;
            ew1.Cells["A4"].Value = "Nomenclatura Catastral";
            ew1.Cells["A4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["A4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew1.Cells["A4"].Style.Font.Bold = true;
            ew1.Cells["A4"].Style.WrapText = true;
            ew1.Cells["A4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["A4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["A4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(2).Width = 16;
            ew1.Cells["B4"].Value = "Tipo Parcela";
            ew1.Cells["B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["B4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew1.Cells["B4"].Style.Font.Bold = true;
            ew1.Cells["B4"].Style.WrapText = true;
            ew1.Cells["B4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["B4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["B4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(3).Width = 20;
            ew1.Cells["C4"].Value = "Partida Inmobiliaria";
            ew1.Cells["C4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["C4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew1.Cells["C4"].Style.Font.Bold = true;
            ew1.Cells["C4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["C4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["C4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(4).Width = 10;
            ew1.Cells["D4"].Value = "UF de PH";
            ew1.Cells["D4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["D4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew1.Cells["D4"].Style.Font.Bold = true;
            ew1.Cells["D4"].Style.WrapText = true;
            ew1.Cells["D4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["D4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["D4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(5).Width = 100;
            ew1.Cells["E4"].Value = "Ubicación";
            ew1.Cells["E4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["E4"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew1.Cells["E4"].Style.Font.Bold = true;
            ew1.Cells["E4"].Style.WrapText = true;
            ew1.Cells["E4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["E4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["E4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(6).Width = 30;
            ew1.Cells["F4"].Value = "Centroide";
            ew1.Cells["F4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["F4"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew1.Cells["F4"].Style.Font.Bold = true;
            ew1.Cells["F4"].Style.WrapText = true;
            ew1.Cells["F4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["F4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["F4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(7).Width = 15;
            ew1.Cells["G4"].Value = "Afectación a PH";
            ew1.Cells["G4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["G4"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew1.Cells["G4"].Style.Font.Bold = true;
            ew1.Cells["G4"].Style.WrapText = true;
            ew1.Cells["G4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["G4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["G4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(8).Width = 20;
            ew1.Cells["H4"].Value = "Inscripción de dominio";
            ew1.Cells["H4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["H4"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew1.Cells["H4"].Style.Font.Bold = true;
            ew1.Cells["H4"].Style.WrapText = true;
            ew1.Cells["H4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["H4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["H4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(9).Width = 20;
            ew1.Cells["I4"].Value = "Fecha Valuación";
            ew1.Cells["I4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["I4"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew1.Cells["I4"].Style.Font.Bold = true;
            ew1.Cells["I4"].Style.WrapText = true;
            ew1.Cells["I4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["I4"].Style.Numberformat.Format = "dd/MM/yyyy";
            ew1.Cells["I4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["I4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(10).Width = 20;
            ew1.Cells["J4"].Value = "Valor Tierra";
            ew1.Cells["J4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["J4"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew1.Cells["J4"].Style.Font.Bold = true;
            ew1.Cells["J4"].Style.WrapText = true;
            ew1.Cells["J4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["J4"].Style.Numberformat.Format = "#,##0.00";
            ew1.Cells["J4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["J4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(11).Width = 20;
            ew1.Cells["K4"].Value = "Valor Mejora";
            ew1.Cells["K4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["K4"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew1.Cells["K4"].Style.Font.Bold = true;
            ew1.Cells["K4"].Style.WrapText = true;
            ew1.Cells["K4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["K4"].Style.Numberformat.Format = "#,##0.00";
            ew1.Cells["K4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["K4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(12).Width = 20;
            ew1.Cells["L4"].Value = "Valor Total";
            ew1.Cells["L4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["L4"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew1.Cells["L4"].Style.Font.Bold = true;
            ew1.Cells["L4"].Style.WrapText = true;
            ew1.Cells["L4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["L4"].Style.Numberformat.Format = "#,##0.00";
            ew1.Cells["L4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["L4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(13).Width = 13;
            ew1.Cells["M4"].Value = "Superficie Tierra Registrada";
            ew1.Cells["M4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["M4"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew1.Cells["M4"].Style.Font.Bold = true;
            ew1.Cells["M4"].Style.WrapText = true;
            ew1.Cells["M4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["M4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["M4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(14).Width = 13;
            ew1.Cells["N4"].Value = "Superficie Tierra Gráfica";
            ew1.Cells["N4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["N4"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew1.Cells["N4"].Style.Font.Bold = true;
            ew1.Cells["N4"].Style.WrapText = true;
            ew1.Cells["N4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["N4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["N4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ew1.Column(15).Width = 13;
            ew1.Cells["O4"].Value = "Unidad de Medida";
            ew1.Cells["O4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew1.Cells["O4"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew1.Cells["O4"].Style.Font.Bold = true;
            ew1.Cells["O4"].Style.WrapText = true;
            ew1.Cells["O4"].Style.Font.Color.SetColor(Color.Black);
            ew1.Cells["O4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew1.Cells["O4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //Inmovilizo las primeras 4 filas
            ew1.View.FreezePanes(5, 1);

            //Armo la hoja de Instructivo
            ew2.Cells["A1"].Value = "Campo";
            ew2.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A1"].Style.Fill.BackgroundColor.SetColor(colorAzul);
            ew2.Cells["A1"].Style.Font.Color.SetColor(Color.White);
            ew2.Cells["A1"].Style.Font.Bold = true;
            ew2.Cells["B1"].Value = "Significado";
            ew2.Cells["B1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B1"].Style.Fill.BackgroundColor.SetColor(colorAzul);
            ew2.Cells["B1"].Style.Font.Color.SetColor(Color.White);
            ew2.Cells["B1"].Style.Font.Bold = true;
            ew2.Cells["A2"].Value = "Nomenclatura Catastral";
            ew2.Cells["A2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A2"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["A2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ew2.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            string cellValue = "Se muestra la nomenclatura catastral, XXAAABBCCCCDDDDEEEEFFFFGGGGG, siendo XX departamento, AAA circuscripción, BB sección, CCCC chacra, DDDD quinta,\n" +
                       " EEEE fracción, FFFF manzana y GGGGG es parcela";
            ew2.Cells["B2"].Value = cellValue;
            ew2.Cells["B2"].Style.WrapText = true;
            ew2.Cells["B2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B2"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["A3"].Value = "Tipo Parcela";
            ew2.Cells["A3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A3"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["B3"].Value = "Se muestra el tipo de parcela, puede ser: URBANA, SUURBANA o RURAL";
            ew2.Cells["B3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B3"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["A4"].Value = "Partida Inmobiliaria";
            ew2.Cells["A4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["B4"].Value = "Se muestra la partida inmobiliaria";
            ew2.Cells["B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B4"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["A5"].Value = "UF de PH";
            ew2.Cells["A5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A5"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);
            ew2.Cells["B5"].Value = "Se muestra el número de unidad funcional de la propiedad horizontal";
            ew2.Cells["B5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B5"].Style.Fill.BackgroundColor.SetColor(colorAmarilloClaro);

            ew2.Cells["A6"].Value = "Ubicación";
            ew2.Cells["A6"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A6"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["B6"].Value = "Se muestra la designación";
            ew2.Cells["B6"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B6"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["A7"].Value = "Coordenadas";
            ew2.Cells["A7"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A7"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["B7"].Value = "Se muestran las coordenadas de un punto interno del inmueble";
            ew2.Cells["B7"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B7"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["A8"].Value = "Afectación a PH";
            ew2.Cells["A8"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A8"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["B8"].Value = "Se muestra si el inmueble es una propiedad horizontal";
            ew2.Cells["B8"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B8"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["A9"].Value = "Dominio";
            ew2.Cells["A9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A9"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);
            ew2.Cells["B9"].Value = "Se muestra el dominio, el cual puede ser: NO DISPONIBLE, MATRICULA, LEY, TOMO/FOLIO/FINCA, DESCONOCIDO, POSESION o NOTA MARGINAL";
            ew2.Cells["B9"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B9"].Style.Fill.BackgroundColor.SetColor(colorVerdeClaro);

            ew2.Cells["A10"].Value = "Fecha Valuación";
            ew2.Cells["A10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A10"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["B10"].Value = "Se muestra la fecha desde la cual esta vigente la última valuación";
            ew2.Cells["B10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B10"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["A11"].Value = "Valor Tierra";
            ew2.Cells["A11"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A11"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["B11"].Value = "Se muestra el valor tierra en pesos";
            ew2.Cells["B11"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B11"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["A12"].Value = "Valor Mejora";
            ew2.Cells["A12"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A12"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["B12"].Value = "Se muestra el valor de las mejoras en pesos";
            ew2.Cells["B12"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B12"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["A13"].Value = "Valor Total";
            ew2.Cells["A13"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A13"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);
            ew2.Cells["B13"].Value = "Se muestra el valor total en pesos";
            ew2.Cells["B13"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B13"].Style.Fill.BackgroundColor.SetColor(colorCelesteClaro);

            ew2.Cells["A14"].Value = "Superficie Tierra Registrada";
            ew2.Cells["A14"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A14"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Cells["B14"].Value = "Se muestra la superficie registrada";
            ew2.Cells["B14"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B14"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Cells["A15"].Value = "Superficie Tierra Gráfica";
            ew2.Cells["A15"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A15"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Cells["B15"].Value = "Se muestra la superficie obtenida a partir del gráfico";
            ew2.Cells["B15"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B15"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Cells["A16"].Value = "Unidad de Medida";
            ew2.Cells["A16"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["A16"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Cells["B16"].Value = "Se muestra la unidad de medida de las superficies, puede ser: ha para tipo parcela RURAL o SUBURBANA y m2 para tipo de parcela URBANA";
            ew2.Cells["B16"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ew2.Cells["B16"].Style.Fill.BackgroundColor.SetColor(colorNaranjaClaro);
            ew2.Column(1).Width = 45;
            ew2.Column(2).Width = 150;

            //Guardo el libro
            using (MemoryStream ms = new MemoryStream())
            {
                package.SaveAs(ms);

                string fileName = "PadronMunicipal_" + DateTime.Now.ToString("dd-MM-yyyy") + ".xlsx";
                FileToDownload = File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}
