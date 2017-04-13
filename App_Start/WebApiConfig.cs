using System;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

namespace SqlToRestApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.EnableCors();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller_name}/{parm0}/{parm1}/{parm2}/{parm3}/{parm4}/{min_price}/{max_price}",
                defaults: new { controller = "values"
                , parm0 = RouteParameter.Optional 
                , parm1 = RouteParameter.Optional
                , parm2 = RouteParameter.Optional
                , parm3 = RouteParameter.Optional
                , parm4 = RouteParameter.Optional
                , min_price = RouteParameter.Optional
                , max_price = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "Help",
                routeTemplate: "help",
                defaults: new
                {  controller = "help" }
            );

            config.Formatters.Add(new BrowserJsonFormatter());
        }
    }

    public class BrowserJsonFormatter : JsonMediaTypeFormatter
    {
        public BrowserJsonFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            this.SerializerSettings.Formatting = Formatting.Indented;
        }

        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("application/json") {CharSet = mediaType.CharSet};
        }
    }
}
