﻿
using System;

namespace GeoSit.Data.BusinessEntities.Seguridad
{
    public class UsuariosRegistro : IEntity
    {
        public long Id_Usuario_Registro { get; set; }
        public long Id_Usuario { get; set; }
        public string Registro { get; set; }
        public long Usuario_Operacion { get; set; }
        public DateTime Fecha_Operacion { get; set; }

        public Usuarios Usuarios { get; set; }
    }
    


}

    
