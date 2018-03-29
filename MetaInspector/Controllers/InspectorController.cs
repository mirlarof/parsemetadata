using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using System;
using System.Threading.Tasks;
using System.Text;

namespace MetaInspector.Controllers
{
    [Route("")]
    public class InspectorController : Controller
    {
        // GET api/values
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public async Task<MetaData> Get([FromQuery]string url)
        {
            url = url.Replace("https://", "http://");
            url = url.StartsWith("http://") ? url : "http://" + url;

            using (var client = new HttpClient())
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var result))
                {
                    var response = await client.GetAsync(result);
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
                    } catch (Exception e) { }

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
                    if(metaData.Image.StartsWith('/'))
                    {
                        metaData.Image = result.Scheme + "://" + result.Host + metaData.Image;
                    }
                    return metaData;
                }

                return null;
            }
        }
    }
}