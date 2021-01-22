using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using System;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace MetaInspector.Controllers
{
    [Route("")]
    public class InspectorController : Controller
    {
        // GET api/values
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public async Task<MetaData> Get([FromQuery] string url, CancellationToken cancellationToken = default)
        {
            url = url.Trim();
            url = AddHttpUrl(url);

            var response = await TryGetResponseAsync(url, cancellationToken);
            if (response == null)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var charset = doc.DocumentNode.SelectSingleNode("//meta[@charset]")?.Attributes["charset"]?.Value ?? string.Empty;
            try
            {
                var encoding = Encoding.GetEncoding(charset);
                if (encoding != Encoding.UTF8)
                {
                    var contentBytes = await response.Content.ReadAsByteArrayAsync();
                    content = encoding.GetString(contentBytes);
                    doc.LoadHtml(content);
                }
            }
            catch (Exception e) { }

            var metaTags = doc.DocumentNode.SelectNodes("//meta");
            var metaData = new MetaData();

            if (metaTags == null) return null;

            foreach (var tag in metaTags)
            {
                var tagName = tag.Attributes["name"]?.Value.ToLower();
                var tagContent = tag.Attributes["content"];
                var tagProperty = tag.Attributes["property"]?.Value.ToLower();
                if (string.IsNullOrEmpty(metaData.Title) && (tagName == "title" || tagName == "twitter:title" || tagProperty == "og:title"))
                {
                    metaData.Title = tagContent.Value.Trim();
                }
                else if (string.IsNullOrEmpty(metaData.Description) && (tagName == "description" || tagName == "twitter:description" || tagProperty == "og:description"))
                {
                    metaData.Description = tagContent.Value.Trim();
                }
                else if (string.IsNullOrEmpty(metaData.Image) && (tagName == "twitter:image" || tagProperty == "og:image"))
                {
                    metaData.Image = tagContent.Value.Trim();
                }
            }

            // if no metadata title, get title
            if (string.IsNullOrEmpty(metaData.Title))
            {
                var title = doc.DocumentNode.SelectSingleNode("//title");
                metaData.Title = title?.InnerText;
            }
            // If using local path
            if (metaData.Image.StartsWith('/') && Uri.TryCreate(url, UriKind.Absolute, out var result))
            {
                metaData.Image = result.Scheme + "://" + result.Host + metaData.Image;
            }
            return metaData;
        }

        private async Task<HttpResponseMessage> TryGetResponseAsync(string url, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var result))
                {
                    var response = await client.GetAsync(result, cancellationToken);
                    if ((int)response.StatusCode >= 300 && (int)response.StatusCode <= 399)
                    {
                        response = await client.GetAsync(response.Headers.Location, cancellationToken);
                    }
                    return response;
                }
            }
            return null;
        }

        private string AddHttpUrl(string url)
        {
            url = url.Replace("https://", "http://");
            return url.StartsWith("http://") ? url : $"http://{url}";
        }
    }
}