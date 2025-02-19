﻿using System;
using System.Collections.Generic;

namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DDJJSorTipoCaracteristica
    {
        public long IdSorTipoCaracteristica { get; set; }
        public string Descripcion { get; set; }   
        public short IdGrupoCaracteristica { get; set; }

        public long IdUsuarioAlta { get; set; }
        public DateTime FechaAlta { get; set; }
        public long IdUsuarioModif { get; set; }
        public DateTime FechaModif { get; set; }
        public long? IdUsuarioBaja { get; set; }
        public DateTime? FechaBaja { get; set; }

        public ICollection<DDJJSorCaracteristicas> Caracteristicas { get; set; }
        public DDJJSorGrupoCaracteristica GrupoCaracteristica { get; set; }
    }
}
