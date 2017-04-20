using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

using SqlToRestApi.Filters;
using SqlToRestApi.Models;
using SqlToRestApi.Services;

namespace SqlToRestApi.Controllers
{
    [AuthorizationRequired]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ValuesController : ApiController
    {
        public HttpResponseMessage Get()
        {
            System.Web.Http.Routing.IHttpRouteData routeData = Request.GetRouteData();

            //var req = HttpUtility.ParseQueryString(Request.RequestUri.Query);
            var reqOptions = Request.GetQueryNameValuePairs();

            var controllerName = (string)routeData.Values["controller_name"];

            var controllers = ApiRepository.GetViews();

            if (!controllers.Contains(controllerName.ToUpper()))
            {
                var message = $"'{controllerName}' not found";
                var err = new HttpError(message);
                return Request.CreateResponse(HttpStatusCode.NotFound, err);
            }

            var colList = ApiRepository.GetColumns(controllerName);

            var colsStr = string.Join(", ", colList.ToArray());

            var parmCount = routeData.Route.Defaults.Keys.Count(p => p.Contains("parm"));

            var condition = "";
            if (routeData.Values.Count > 2)
            {
                for (var i = 0; i <= parmCount - 1; i++)
                {
                    if (colList.Count < i + 1)
                        continue;
                    var col = colList[i];

                    var parm = "parm" + (i).ToString();
                    if (!routeData.Values.ContainsKey(parm))                    
                        continue;                    
                    var val = (string)routeData.Values["parm" + (i).ToString()];

                    val = val.Replace("'", "''");


                    if (string.IsNullOrEmpty(val) || val.ToUpper().Trim() == "ANY")
                        continue;

                    var sign = "=";

                    val = string.Join(",", val.Split(',').Select(v => $"'{v}'"));
                    if (val.Contains(","))
                    {
                        sign = " in ";
                        val = $"({val})";
                    }

                    condition += $" and {col}{sign}{val}";
                }
            }

            if (colList.Contains("[PRICE]"))
            {
                int val;
                string sVal;               

                if (routeData.Values.ContainsKey("min_price"))
                {                    
                    sVal = (string)routeData.Values["min_price"];

                    if (!string.IsNullOrEmpty(sVal) && sVal.ToUpper().Trim() != "ANY")
                    {
                        if (int.TryParse(sVal, out val))
                        {
                            condition += $" and price>={val.ToString()}";
                        }
                    }
                }

                if (routeData.Values.ContainsKey("max_price"))
                {
                    sVal = (string)routeData.Values["max_price"];

                    if (!string.IsNullOrEmpty(sVal) && sVal.ToUpper().Trim() != "ANY")
                    {
                        if (int.TryParse(sVal, out val))
                        {
                            condition += $" and price<={val}";
                        }
                    }
                }
            }

            foreach (var reqParm in reqOptions)
            {
                var col = "[" + reqParm.Key.ToUpper() + "]";
                var val = reqParm.Value.Replace("'", "''");

                var sign = "=";

                if (col.Contains("MIN_"))
                {
                    col = col.Replace("MIN_", "");
                    sign = ">=";
                }

                if (col.Contains("MAX_"))
                {
                    col = col.Replace("MAX_", "");
                    sign = "<=";
                }

                val = string.Join(",", val.Split(',').Select(v => $"'{v}'"));
                if (val.Contains(","))
                {
                    sign = " in ";
                    val = $"({val})";
                }

                if (colList.Contains(col)) {
                    condition += $" and {col}{sign}{val}";
                }
            }

            var q = $"select {colsStr} from v_api_{controllerName.Trim()} where 1=1 {condition}";

            var dynamicContext = ApiRepository.GetDynData(q).Cast<DynamicContext>().ToArray();

            var response = Request.CreateResponse(HttpStatusCode.OK, dynamicContext);

            return response;
        }
    }
}
