﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace Swagger.Net
{
    
    public static class SwaggerGen
    {
        public const string SWAGGER = "swagger";
        public const string SWAGGER_VERSION = "2.0";
        public const string FROMURI = "FromUri";
        public const string FROMBODY = "FromBody";
        public const string FROMUNKNOWN = "Unknown";
        public const string QUERY = "query";
        public const string PATH = "path";
        public const string BODY = "body";
        public static string API_VERSION = "0.4.0.0";// this doesn't work: Assembly.GetCallingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Create a resource listing
        /// </summary>
        /// <param name="actionContext">Current action context</param>
        /// <param name="includeResourcePath">Should the resource path property be included in the response</param>
        /// <returns>A resource Listing</returns>
        public static ResourceListing CreateResourceListing(HttpActionContext actionContext, bool includeResourcePath = true)
        {
            return CreateResourceListing(actionContext.ControllerContext, includeResourcePath);
        }

        /// <summary>
        /// Create a resource listing
        /// </summary>
        /// <param name="actionContext">Current controller context</param>
        /// <param name="includeResourcePath">Should the resource path property be included in the response</param>
        /// <returns>A resrouce listing</returns>
        public static ResourceListing CreateResourceListing(HttpControllerContext controllerContext, bool includeResourcePath = false)
        {
            Uri uri = controllerContext.Request.RequestUri;

            ResourceListing rl = new ResourceListing()
            {
                apiVersion = API_VERSION,  //TODO:: move this to a configuration item? Assembly.GetCallingAssembly().GetType().Assembly.GetName().Version.ToString(),
                swaggerVersion = SWAGGER_VERSION,
                basePath = uri.GetLeftPart(UriPartial.Authority) + HttpRuntime.AppDomainAppVirtualPath.TrimEnd('/'),
                apis = new List<ResourceApi>()
            };

            if (includeResourcePath) rl.resourcePath = controllerContext.ControllerDescriptor.ControllerName;

            return rl;
        }

        /// <summary>
        /// Create an api element 
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <returns>A resource api</returns>
        public static ResourceApi CreateResourceApi(ApiDescription api)
        {
            ResourceApi rApi = new ResourceApi()
            {
                path = "/" + api.RelativePath,
                description = api.Documentation,
                operations = new List<ResourceApiOperation>()
            };

            return rApi;
        }

        private static string GetCustomAttributesAsString(object [] actionDescriptor)
        {
            string _attributes = "";
            foreach (var attribute in actionDescriptor)
            {
                if (attribute.ToString().Contains("Restricted"))
                    _attributes += "[restricted]";
                else if (attribute.ToString().Contains("UserAndAppAuthorize"))
                    _attributes += "[auth:app & user]";
                else if (attribute.ToString().Contains("AppAuthorize"))
                        _attributes += "[auth:app]";
                else if (attribute.ToString().Contains("Experimental"))
                        _attributes += "[experimental]";
            }
            return _attributes;
        }

        /// <summary>
        /// Creates an api operation
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <param name="docProvider">Access to the XML docs written in code</param>
        /// <returns>An api operation</returns>
        public static ResourceApiOperation CreateResourceApiOperation(ApiDescription api,
            XmlCommentDocumentationProvider docProvider, HttpControllerDescriptor controllerDescriptor)

        {
            var parts = docProvider.GetNotes(api.ActionDescriptor).Split(new string[] { "schema=" }, StringSplitOptions.None);
            ReflectedHttpActionDescriptor actionDescriptor = (api.ActionDescriptor as ReflectedHttpActionDescriptor);
            string _attributes = "";
            _attributes += GetCustomAttributesAsString(actionDescriptor.MethodInfo.GetCustomAttributes(true));
            _attributes += GetCustomAttributesAsString(controllerDescriptor.ControllerType.GetCustomAttributes(true));
            
            ResourceApiOperation rApiOperation = new ResourceApiOperation()
            {
                httpMethod = api.HttpMethod.ToString(),
                nickname = docProvider.GetNickname(api.ActionDescriptor),
                responseClass = docProvider.GetResponseClass(api.ActionDescriptor),
                summary = api.Documentation+" "+_attributes,
                notes = parts[0],
                schema = parts.Length > 1 ? parts[1] : "",
                parameters = new List<ResourceApiOperationParameter>(),
            };

            return rApiOperation;
        }

        /// <summary>
        /// Creates an operation parameter
        /// </summary>
        /// <param name="api">Description of the api via the ApiExplorer</param>
        /// <param name="param">Description of a parameter on an operation via the ApiExplorer</param>
        /// <param name="docProvider">Access to the XML docs written in code</param>
        /// <returns>An operation parameter</returns>
        public static ResourceApiOperationParameter CreateResourceApiOperationParameter(ApiDescription api, ApiParameterDescription param, XmlCommentDocumentationProvider docProvider)
        {
            string paramType = (param.Source.ToString().Equals(FROMURI) || 
                param.Source.ToString().Equals(FROMUNKNOWN)) ? QUERY : BODY;
            ResourceApiOperationParameter parameter = new ResourceApiOperationParameter()
            {
                paramType = (paramType == "query" && 
                             api.RelativePath.IndexOf("{" + param.Name + "}") > -1 && 
                             param.ParameterDescriptor.DefaultValue == null) ? PATH : paramType,
                name = param.Name,
                description = param.Documentation,
                dataType = param.ParameterDescriptor.ParameterType.Name,
                required = docProvider.GetRequired(param.ParameterDescriptor)
            };

            return parameter;
        }
    }

    public class ResourceListing
    {
        public string apiVersion { get; set; }
        public string swaggerVersion { get; set; }
        public string basePath { get; set; }
        public string resourcePath { get; set; }
        public List<ResourceApi> apis { get; set; }
    }

    public class ResourceApi
    {
        public string path { get; set; }
        public string description { get; set; }
        public List<ResourceApiOperation> operations { get; set; }
    }

    public class ResourceApiOperation
    {
        public string httpMethod { get; set; }
        public string nickname { get; set; }
        public string responseClass { get; set; }
        public string summary { get; set; }
        public string notes { get; set; }

        public string schema { get; set; }
  
        public List<ResourceApiOperationParameter> parameters { get; set; }
    }

    public class ResourceApiOperationParameter
    {
        public string paramType { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string dataType { get; set; }
        public bool required { get; set; }
        public bool allowMultiple { get; set; }
        public OperationParameterAllowableValues allowableValues { get; set; }
    }

    public class OperationParameterAllowableValues
    {
        public int max { get; set; }
        public int min { get; set; }
        public string valueType { get; set; }
    }
}