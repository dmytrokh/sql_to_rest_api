using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Data;
using System.Text;
using System.Web;
using SqlToRestApi.Services;

namespace SqlToRestApi.Controllers
{
    public class HelpController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var response = new HttpResponseMessage();

            var docTable = GetDocTable();
            var resources = docTable.DefaultView.ToTable(true, new string[] { "resource","table_name","descr" });

            var sb = new StringBuilder();

            var appPath = HttpRuntime.AppDomainAppPath;

            var helpHtml = System.IO.File.ReadAllText(appPath + "help.html");

            var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            helpHtml = helpHtml.Replace("{BaseUrl}", baseUrl);

            foreach (DataRow row in resources.Rows)
            {
                sb.AppendLine($"<div id='{row["resource"]}'>");
                sb.AppendLine($"<h2>{row["table_name"]}</h2>");

                sb.AppendLine($"<h3>Resource: {row["resource"]}</h3>");

                sb.AppendLine($"<div><b>API GET</b>: {baseUrl}/api/{row["resource"]}</div>");

                if (!string.IsNullOrEmpty(row["descr"].ToString()))
                {
                    sb.AppendLine("<br/>");
                    sb.AppendLine($"<div><b>Description</b>: {row["descr"]}</div>");
                }
                
                sb.AppendLine("<h3>Fields:</h3>");

                sb.AppendLine("<table class='help-page-table'>");

                sb.AppendLine(@"<tr><th style='width: 15%;'>Name</th><th style='width: 15%;'>Type</th><th>Description</th></tr>");
                sb.AppendLine("<tbody>");

                var fields = docTable.Select($"resource = '{row["resource"]}'");

                foreach (var frow in fields)
                {
                    sb.AppendLine($"<tr><td>{frow["field_name"]}</td><td>{frow["type"]}</td><td>{frow["field_descr"]}</td></td>");
                }

                sb.AppendLine("</tbody></table></div>");
            }

            var resourcesHtml = sb.ToString();
            helpHtml = helpHtml.Replace("{Resources}", resourcesHtml);

            response.Content = new StringContent(helpHtml);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        private static DataTable GetDocTable()
        {
            var query = @"SELECT [resource]
                  ,[position]
                  ,[field_name]
                  ,[type]
                  ,[table_name]
                  ,[descr]
                  ,[field_descr]
              FROM [dbo].[doc]";

            return ApiRepository.GetTable(query);
        }
    }
}
