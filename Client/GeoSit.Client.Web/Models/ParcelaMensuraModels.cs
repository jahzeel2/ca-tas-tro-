﻿using System;

namespace GeoSit.Client.Web.Models
{
    public class ParcelaMensuraModels
    {
        public long IdParcelaMensura { get; set; }
        public long IdParcela { get; set; }
        public long IdMensura { get; set; }
        public string Nomenclatura { get; set; }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }
    }
}