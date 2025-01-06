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

    public static class MethodUtilities
    {
        public static MCTG_Trimmel.HTTP.Response Response
        {
            get => default;
            set
            {
            }
        }

        public static MCTG_Trimmel.HTTP.StatusCode StatusCode
        {
            get => default;
            set
            {
            }
        }

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
    }
}
