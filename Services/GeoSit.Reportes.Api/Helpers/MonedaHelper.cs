using System;
using System.Text.RegularExpressions;

namespace GeoSit.Reportes.Api.Helpers
{
    public static class MonedaHelper
    {
        private static readonly string[] UNIDADES = { "", "un ", "dos ", "tres ", "cuatro ", "cinco ", "seis ", "siete ", "ocho ", "nueve " };
        private static readonly string[] DECENAS = {"diez ", "once ", "doce ", "trece ", "catorce ", "quince ", "dieciseis ",
        "diecisiete ", "dieciocho ", "diecinueve", "veinte ", "treinta ", "cuarenta ",
        "cincuenta ", "sesenta ", "setenta ", "ochenta ", "noventa "};
        private static readonly string[] CENTENAS = {"", "ciento ", "doscientos ", "trecientos ", "cuatrocientos ", "quinientos ", "seiscientos ",
        "setecientos ", "ochocientos ", "novecientos "};

        private static Regex r;

        public static string Convertir(string numero, bool mayusculas, string moneda = "SON PESOS")
        {
            string literal, parte_decimal;
            //si el numero utiliza (.) en lugar de (,) -> se reemplaza
            numero = numero.Replace(".", ",");

            //si el numero no tiene parte decimal, se le agrega ,00
            if (!numero.Contains(","))
            {
                numero = $"{numero},00";
            }
            //se valida formato de entrada -> 0,00 y 999 999 999,00
            r = new Regex(@"\d{1,9},\d{1,2}");
            MatchCollection mc = r.Matches(numero);
            if (mc.Count > 0)
            {
                //se divide el numero 0000000,00 -> entero y decimal
                string[] Num = numero.Split(',');

                //if (moneda != "SON PESOS")

                //de da formato al numero decimal
                //parte_decimal ="CON " + getDecenas(Num[1])+ getUnidades(Num[1]) + "CENTAVOS";
                if (int.Parse(Num[1]) > 9)
                {//si es decena
                    parte_decimal = getDecenas(Num[1]);
                }
                else if (int.Parse(Num[1]) < 9 && int.Parse(Num[1]) > 0)
                {//sino unidades -> 9
                    parte_decimal = getUnidades(Num[1]);
                }
                else
                {
                    parte_decimal = "CERO ";
                }
                //se convierte el numero a literal
                if (int.Parse(Num[0]) == 0)
                {//si el valor es cero
                    literal = "cero ";
                }
                else if (int.Parse(Num[0]) > 999999)
                {//si es millon
                    literal = getMillones(Num[0]);
                }
                else if (int.Parse(Num[0]) > 999)
                {//si es miles
                    literal = getMiles(Num[0]);
                }
                else if (int.Parse(Num[0]) > 99)
                {//si es centena
                    literal = getCentenas(Num[0]);
                }
                else if (int.Parse(Num[0]) > 9)
                {//si es decena
                    literal = getDecenas(Num[0]);
                }
                else
                {//sino unidades -> 9
                    literal = getUnidades(Num[0]);
                }
                //devuelve el resultado en mayusculas o minusculas
                string resultado = ("SON PESOS " + literal + "CON " + parte_decimal + "CENTAVOS").ToUpper();
                if (!mayusculas)
                {
                    return resultado.ToLower();
                }
                return resultado;
            }
            else
            {//error, no se puede convertir
                return literal = null;
            }
        }

        /* funciones para convertir los numeros a literales */

        private static string getUnidades(string numero)
        {   // 1 - 9
            //si tuviera algun 0 antes se lo quita -> 09 = 9 o 009=9
            string num = numero.Substring(numero.Length - 1);
            return UNIDADES[int.Parse(num)];
        }

        private static string getDecenas(string num)
        {// 99
            int n = int.Parse(num);
            if (n < 10)
            {//para casos como -> 01 - 09
                return getUnidades(num);
            }
            else if (n > 19)
            {//para 20...99
                string u = getUnidades(num);
                if (u.Equals(""))
                { //para 20,30,40,50,60,70,80,90
                    return DECENAS[int.Parse(num.Substring(0, 1)) + 8];
                }
                else
                {
                    return DECENAS[int.Parse(num.Substring(0, 1)) + 8] + "y " + u;
                }
            }
            else
            {//numeros entre 11 y 19
                return DECENAS[n - 10];
            }
        }

        private static string getCentenas(string num)
        {// 999 o 099
            if (int.Parse(num) > 99)
            {//es centena
                if (int.Parse(num) == 100)
                {//caso especial
                    return " cien ";
                }
                else
                {
                    return CENTENAS[int.Parse(num.Substring(0, 1))] + getDecenas(num.Substring(1));
                }
            }
            else
            {//por Ej. 099
                //se quita el 0 antes de convertir a decenas
                return getDecenas(int.Parse(num) + "");
            }
        }

        private static string getMiles(string numero)
        {// 999 999
            //obtiene las centenas
            string c = numero.Substring(numero.Length - 3);
            //obtiene los miles
            string m = numero.Substring(0, numero.Length - 3);
            string n = "";
            //se comprueba que miles tenga valor entero
            if (int.Parse(m) > 0)
            {
                n = getCentenas(m);
                return n + "mil " + getCentenas(c);
            }
            else
            {
                return "" + getCentenas(c);
            }

        }

        private static string getMillones(string numero)
        { //000 000 000
            //se obtiene los miles
            string miles = numero.Substring(numero.Length - 6);
            //se obtiene los millones
            string millon = numero.Substring(0, numero.Length - 6);
            string n = "";
            if (millon.Length > 1)
            {
                n = getCentenas(millon) + "millones ";
            }
            else
            {
                n = getUnidades(millon) + "millon ";
            }
            return n + getMiles(miles);
        }

    }
}