using GeoSit.Data.DAL.Contexts;
using System;
using System.Data.Entity;
using System.Linq;
using GeoSit.Data.BusinessEntities.Seguridad;
using System.Linq.Expressions;
using GeoSit.Core.Utils.Crypto;
using System.Collections.Generic;
using GeoSit.Data.DAL.Common;

namespace GeoSit.Data.DAL.Repositories
{
    public class UsuariosRepository
    {
        public enum OperationResult
        {
            Ok,
            InvalidFormat,
            InvalidPassword,
            InvalidPasswordConfirmation,
            InvalidUser,
            PasswordTooShort,
            SamePassword,
        }
        readonly GeoSITMContext _context;
        public UsuariosRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public bool EsUsuarioAdmin(long idUsuario)
        {
            long idPerfilAdmin = Convert.ToInt64(_context.ParametrosGenerales.Single(x => x.Clave == "ID_PERFIL_ADMINISTRADOR").Valor);
            return (from usuario in _context.Usuarios
                    join perfil in _context.UsuariosPerfiles on usuario.Id_Usuario equals perfil.Id_Usuario
                    where perfil.Id_Perfil == idPerfilAdmin && perfil.Id_Usuario == idUsuario && perfil.Fecha_Baja == null
                    select 1).Any();
        }

        public Usuarios GetUsuarioByIdFecha(long id, long ticks)
        {
            DateTime fecha = new DateTime(ticks);
            DateTime.TryParse(_context.ParametrosGenerales.SingleOrDefault(p => p.Clave == "FECHA_PROCESO_MIGRACION")?.Valor, out DateTime fechaMigracion);

            Expression<Func<Usuarios, bool>> filtroByIdUsuario = u => u.Id_Usuario == id;
            if (fecha < fechaMigracion)
            {
                filtroByIdUsuario = u => u.IdISICAT == id;
            }
            return _context.Usuarios.SingleOrDefault(filtroByIdUsuario) ?? new Usuarios() { Apellido = "Usuario No Migrado", Nombre = $"ID {id}" };
        }

        public Tuple<OperationResult, string[]> UpdatePassword(long id, CambioPassword cambioPassword)
        {
            var datos = (from user in _context.Usuarios
                         join password in _context.UsuariosRegistro on user.Id_Usuario equals password.Id_Usuario into ljPasswd
                         from password in ljPasswd.DefaultIfEmpty()
                         where user.Id_Usuario == id && user.Fecha_baja == null && user.Habilitado
                         group password by user into grp
                         select new
                         {
                             usuario = grp.Key,
                             passwords = grp.OrderByDescending(x => x.Fecha_Operacion)
                                            .ThenByDescending(x => x.Id_Usuario_Registro)
                         }).SingleOrDefault();

            if (datos?.usuario == null)
            {
                return Tuple.Create(OperationResult.InvalidUser, new[] { "El usuario no existe o está inhabilitado." });
            }

            if (datos?.passwords.First().Registro != Hash.MD5(cambioPassword.Vigente))
            {
                return Tuple.Create(OperationResult.InvalidPassword, new[] { "La contraseña actual no es correcta." });
            }

            Tuple<OperationResult, string[]> validation = ValidateNewPassword(datos.passwords, cambioPassword, out string nuevaPassword);

            if (validation == null)
            {
                datos.usuario.Fecha_modificacion = DateTime.Now;
                datos.usuario.Usuario_modificacion = id;
                datos.usuario.Cambio_pass = false;
                _context.UsuariosRegistro.Add(new UsuariosRegistro()
                {
                    Fecha_Operacion = datos.usuario.Fecha_modificacion,
                    Id_Usuario = id,
                    Registro = nuevaPassword,
                    Usuario_Operacion = id
                });
                try
                {
                    _context.SaveChanges();
                    validation = Tuple.Create(OperationResult.Ok, new string[0]);
                }
                catch (Exception ex)
                {
                    _context.GetLogger().LogError("UsuariosRepository->UpdatePassword", ex);
                    throw;
                }
            }
            return validation;
        }

        public IEnumerable<Usuarios> GetUsuariosByIdSector(long idSector)
        {
            return _context.Usuarios
                           .Where(u => u.Fecha_baja == null && u.Habilitado && u.IdSector == idSector)
                           .ToList();
        }

        public Tuple<OperationResult, string[]> ValidatePassword(long id, CambioPassword cambioPassword)
        {
            var passwords = (from password in _context.UsuariosRegistro
                             where password.Id_Usuario == id
                             select password).ToList();
            return ValidateNewPassword(passwords, cambioPassword, out _) ?? Tuple.Create(OperationResult.Ok, new string[0]);
        }

        private Tuple<OperationResult, string[]> ValidateNewPassword(IEnumerable<UsuariosRegistro> oldPasswords, CambioPassword cambioPassword, out string hashedPassword)
        {
            hashedPassword = null;
            if (cambioPassword.Nueva != cambioPassword.Confirmacion)
            {
                return Tuple.Create(OperationResult.InvalidPassword, new[] { "La nueva contraseña y la confirmación son distintas." });
            }

            var claves = new[] { "ACTIVE_DIRECTORY_USAGE", "PASSWORD_REQUIREMENT" };
            var parametros = (from parametro in _context.ParametrosGenerales
                              where parametro.Agrupador == "ACTIVE_DIRECTORY_USAGE" || parametro.Agrupador == "PASSWORD_REQUIREMENT"
                              select new { parametro.Clave, parametro.Valor }).ToList();


            int longitudMinima = int.Parse(parametros.Single(x => x.Clave == "LONGITUD_MINIMA_CLAVE").Valor);
            if (cambioPassword.Nueva.Length < longitudMinima)
            {
                return Tuple.Create(OperationResult.PasswordTooShort, new[] { $"La contraseña debe contener al menos {longitudMinima} caracteres." });
            }

            string nuevaPassword = Hash.MD5(cambioPassword.Nueva);

            int clavesAnteriores = int.Parse(parametros.Single(x => x.Clave == "CLAVES_ALMACENADAS").Valor);
            if (oldPasswords.Take(clavesAnteriores).Any(x => x.Registro == nuevaPassword))
            {
                return Tuple.Create(OperationResult.SamePassword, new[] { $"La contraseña no debe ser igual a ninguna de las {clavesAnteriores} anteriores." });
            }

            bool esAD = parametros.Single(x => x.Clave == "Active_Directory").Valor == "1";
            bool exigeLetras = parametros.Single(x => x.Clave == "EXIGE_LETRAS").Valor == "1";
            bool exigeNumeros = parametros.Single(x => x.Clave == "EXIGE_NUMEROS").Valor == "1";
            bool exigeEspeciales = parametros.Single(x => x.Clave == "EXIGE_CARACTERES_ESPECIALES").Valor == "1";
            bool exigeMayusculas = parametros.Single(x => x.Clave == "EXIGE_MAYUSCULAS").Valor == "1";
            bool exigeMinusculas = parametros.Single(x => x.Clave == "EXIGE_MINUSCULAS").Valor == "1";
            
            string mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string minusculas = "abcdefghijklmnopqrstuvwxyz";
            string numeros = "0123456789";
            string especiales = "!#$%&/()=?'¡¿*~[]{}^,;.:-_@";
            bool tieneMayusculas = false;
            bool tieneMinusculas = false;
            bool tieneNumeros = false;
            bool tieneEspeciales = false;

            foreach (char caracter in cambioPassword.Nueva.ToCharArray().Distinct())
            {
                tieneMayusculas |= mayusculas.IndexOf(caracter) != -1;
                tieneMinusculas |= minusculas.IndexOf(caracter) != -1;
                tieneNumeros |= numeros.IndexOf(caracter) != -1;
                tieneEspeciales |= especiales.IndexOf(caracter) != -1;
            }

            var errores = new List<string>();
            if (exigeLetras && !(tieneMayusculas || tieneMinusculas))
            {
                errores.Add("La contraseña debe contener al menos 1 Letra.");
            }
            if (exigeMayusculas && !tieneMayusculas)
            {
                errores.Add("La contraseña debe contener al menos 1 Letra Mayúscula.");
            }
            if (exigeMinusculas && !tieneMinusculas)
            {
                errores.Add("La contraseña debe contener al menos 1 Letra Minúscula.");
            }
            if (exigeNumeros && !tieneNumeros)
            {
                errores.Add("La contraseña debe contener al menos 1 Número.");
            }
            if (exigeEspeciales && !tieneEspeciales)
            {
                errores.Add($"La contraseña debe contener al menos 1 caracter especial {especiales}.");
            }

            if (errores.Any())
            {
                return Tuple.Create(OperationResult.InvalidFormat, errores.ToArray());
            }
            hashedPassword = nuevaPassword;
            return null;
        }

        public Tuple<OperationResult, string[]> ResetPassword(string dni)
        {
            var usuario = _context.Usuarios.SingleOrDefault(x => x.Nro_doc == dni);
            if (usuario == null)
            {
                return Tuple.Create(OperationResult.InvalidUser, new string[0]);
            }

            string new_password = TextRandomizer.RandomizeGenericPattern(10);

            var registro = _context.UsuariosRegistro.Add(new UsuariosRegistro()
            {
                Usuario_Operacion = 1,
                Id_Usuario = usuario.Id_Usuario,
                Registro = Hash.MD5(new_password),
                Fecha_Operacion = DateTime.Now
            });

            usuario.Cambio_pass = true;
            try
            {
                _context.SaveChanges();

                return Tuple.Create(OperationResult.Ok, new[] { new_password, usuario.Mail });
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError($"UsuariosRepository->ResetPassword({dni})", ex);
                throw;
            }
        }

        public bool EsUsuarioInterno(long id)
        {
            long idSectorExterno = Convert.ToInt64(_context.ParametrosGenerales.Single(x => x.Clave == "ID_SECTOR_EXTERNO").Valor);
            var usuario = _context.Usuarios.Find(id);
            return usuario?.IdSector != idSectorExterno;
        }
    }
}