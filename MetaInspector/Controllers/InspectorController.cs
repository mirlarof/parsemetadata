using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;

namespace MetaInspector.Controllers
{
    [Route("")]
    public class InspectorController : Controller
    {
        // GET api/values
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public MetaData Get([FromQuery]string url)
        {
            using (var client = new HttpClient())
            {
                using (var content = client.GetAsync(url).Result.Content.ReadAsStringAsync())
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content.Result);
                    var metaTags = doc.DocumentNode.SelectNodes("//meta");
                    var metaData = new MetaData();
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
            }
        }
    }
}