using System;
using System.Collections.Generic;

namespace Trimmel_MCTG.HTTP
{
    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Patch
    }

    //public enum StatusCodes
    //{
    //    Ok = 200,
    //    Created = 201,
    //    Accepted = 202,
    //    NoContent = 204,
    //    BadRequest = 400,
    //    Unauthorized = 401,
    //    Forbidden = 403,
    //    NotFound = 404,
    //    Conflict = 409,
    //    InternalServerError = 500,
    //    NotImplemented = 501
    //}

    public static class MethodUtilities
    {
        public static HttpMethod GetMethod(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "DELETE" => HttpMethod.Delete,
                "PUT" => HttpMethod.Put,
                "PATCH" => HttpMethod.Patch,
                _ => throw new InvalidDataException($"Unknown HTTP method: {method}")
            };
        }

        //public static StatusCodes GetHttpStatusCode(int statusCode)
        //{
        //    return statusCode switch
        //    {
        //        200 => StatusCodes.Ok,
        //        201 => StatusCodes.Created,
        //        202 => StatusCodes.Accepted,
        //        204 => StatusCodes.NoContent,
        //        400 => StatusCodes.BadRequest,
        //        401 => StatusCodes.Unauthorized,
        //        403 => StatusCodes.Forbidden,
        //        404 => StatusCodes.NotFound,
        //        409 => StatusCodes.Conflict,
        //        500 => StatusCodes.InternalServerError,
        //        501 => StatusCodes.NotImplemented,
        //        _ => throw new ArgumentOutOfRangeException($"Unknown status code: {statusCode}")
        //    };
        //}

        
    }
}
