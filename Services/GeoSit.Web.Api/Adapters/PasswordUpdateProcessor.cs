using System;
using System.Net;
using static GeoSit.Data.DAL.Repositories.UsuariosRepository;

namespace GeoSit.Web.Api.Adapters
{
    internal class PasswordUpdateProcessor
    {
        readonly Tuple<OperationResult, string[]> __result;
        public PasswordUpdateProcessor(Tuple<OperationResult, string[]> result)
        {
            __result = result;
        }
        public HttpStatusCode GetStatusCode()
        {
            HttpStatusCode statusCode;
            switch (__result.Item1)
            {
                case OperationResult.PasswordTooShort:
                case OperationResult.SamePassword:
                case OperationResult.InvalidFormat:
                case OperationResult.InvalidPasswordConfirmation:
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case OperationResult.InvalidPassword:
                    statusCode = HttpStatusCode.Forbidden;
                    break;
                case OperationResult.InvalidUser:
                    statusCode = HttpStatusCode.NotFound;
                    break;
                case OperationResult.Ok:
                    statusCode = HttpStatusCode.OK;
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    break;
            }
            return statusCode;
        }
        public string[] GetData()
        {
            return __result.Item2;
        }
    }
}