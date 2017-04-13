using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using SqlToRestApi.Services;

namespace SqlToRestApi.Filters
{
    public class AuthorizationRequiredAttribute : ActionFilterAttribute
    {
        private const string ApiKey = "ApiKey";

        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            //  Get API key provider
            var provider = new ApiKeyService();

            var msgUnauthorized = new { Message = "Authorization required" };

            var reqOptions = filterContext.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);

            var keyValue = "";

            if (!Config.AuthorizationRequired)
            {
                keyValue = "None";
            }
             else if (filterContext.Request.Headers.Contains(ApiKey))
            {
                keyValue = filterContext.Request.Headers.GetValues(ApiKey).First();
            }
            else if (reqOptions.ContainsKey(ApiKey))
            {
                keyValue = reqOptions[ApiKey];
            }

            // Validate Token
            if (string.IsNullOrEmpty(keyValue) || !provider.ValidateKey(keyValue))
            {
                filterContext.Response = filterContext.Request.CreateResponse(HttpStatusCode.Unauthorized, msgUnauthorized);
                filterContext.Response.ReasonPhrase = "Unauthorized";
            }
            else
            {
                var controller = filterContext.ControllerContext.Controller;

                var controllerUser = controller.GetType().GetProperty("CurrentUser");
                if (controllerUser != null)
                {
                    var user = provider.ApiKey.User;
                    controllerUser.SetValue(controller, user);
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}