using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using System;
using System.Threading.Tasks;

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
            url = url.StartsWith("http://") ? url : "http://" + url;
            
            using (var client = new HttpClient())
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var result))
                {
                    var response = await client.GetAsync(result);
                    var content = await response.Content.ReadAsStringAsync();
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    var metaTags = doc.DocumentNode.SelectNodes("//meta");
                    var metaData = new MetaData();

                    if (metaTags == null) return null;

                    foreach (var tag in metaTags)
                    {
                        var tagName = tag.Attributes["name"]?.Value.ToLower();
                        var tagContent = tag.Attributes["content"];
                        var tagProperty = tag.Attributes["property"]?.Value.ToLower();
                        if (tagName == "title" || tagName == "twitter:title" || tagProperty == "og:title")
                        {
                            metaData.Title = string.IsNullOrEmpty(metaData.Title) ? tagContent.Value : metaData.Title;
                        }
                        else if (tagName == "description" || tagName == "twitter:description" || tagProperty == "og:description")
                        {
                            metaData.Description = string.IsNullOrEmpty(metaData.Description) ? tagContent.Value : metaData.Description;
                        }
                        else if (tagName == "twitter:image" || tagProperty == "og:image")
                        {
                            metaData.Image = string.IsNullOrEmpty(metaData.Image) ? tagContent.Value : metaData.Image;
                        }
                    }
                    return metaData;
                }

                return null;
            }
        }
    }
}