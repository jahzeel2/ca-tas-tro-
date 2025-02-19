using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GeoSit.Client.Web.ViewModels
{
    public class ParcelaUTViewModel
    {
        public GeoSit.Data.BusinessEntities.Inmuebles.Parcela Parcela { get; set; }
        public GeoSit.Data.BusinessEntities.Inmuebles.UnidadTributaria UnidadTributaria { get; set; }
    }
}