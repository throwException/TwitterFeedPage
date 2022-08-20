using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace TwitterFeedPage
{
    public static class IElementExtensions
    {
        public static bool HasAttributeValue(this IElement element, string attributeName, string attributeValue)
        {
            return element.HasAttribute(attributeName) &&
                element.GetAttribute(attributeName) == attributeValue;
        }

        public static string GetAttributeValue(this IElement element, string attributeName)
        {
            return element.HasAttribute(attributeName) ?
                element.GetAttribute(attributeName) : null;
        }

        public static IEnumerable<IElement> GetAllSubs(this IElement element, string tagName)
        { 
            foreach (var child in element.Children)
            { 
                if (child.TagName == tagName)
                {
                    yield return child;
                }
                else
                {
                    foreach (var sub in child.GetAllSubs(tagName))
                    {
                        yield return sub;
                    }
                }
            }
        }
    }

    public class SitePreview
    {
        private HtmlParser _parser;
        private IHtmlDocument _document;
        private IElement _head;
        private IElement _body;
        private List<IElement> _metas;

        public SitePreview(string url)
        {
            try
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var result = client.GetAsync(url).Result;
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    Load(result.Content.ReadAsStringAsync().Result);
                }
            }
            catch
            {
            }
        }

        public bool Valid
        { 
            get
            {
                return _parser != null &&
                    _document != null &&
                    _head != null &&
                    _body != null &&
                    _metas != null &&
                    !string.IsNullOrEmpty(Title) &&
                    (!string.IsNullOrEmpty(Description) ||
                     !string.IsNullOrEmpty(ImageUrl));
             }
        }

        private void Load(string text)
        {
            _parser = new HtmlParser();
            _document = _parser.ParseDocument(text);
            _head = _document.DocumentElement.Children
                .FirstOrDefault(e => e.TagName.ToLowerInvariant() == "head");
            _metas = _head == null ? new List<IElement>() :
                     _head.Children.Where(e => e.TagName.ToLowerInvariant() == "meta").ToList();
            _body = _document.DocumentElement.Children
                .FirstOrDefault(e => e.TagName.ToLowerInvariant() == "body");
        }

        public string Title
        {
            get
            {
                var result = string.Empty;

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("name", "twitter:title"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("property", "og:title"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _head.Children
                        .Where(e => e.TagName == "title")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("h1")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("h2")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("h3")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("h4")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("h5")
                        .Select(e => e.NodeValue)
                        .FirstOrDefault();
                }

                if (result == null)
                {
                    result = string.Empty;
                }

                return result;
            }
        }

        public string Description
        {
            get
            {
                var result = string.Empty;

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("name", "twitter:description"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("property", "og:description"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("p")
                        .Select(e => e.Text())
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.Text();
                }

                if (result == null)
                {
                    result = string.Empty;
                }

                return result;
            }
        }

        public string ImageUrl
        {
            get
            {
                var result = string.Empty;

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("name", "twitter:image"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _metas
                        .Where(e => e.HasAttributeValue("property", "og:image"))
                        .Select(e => e.GetAttributeValue("content"))
                        .FirstOrDefault();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = _body.GetAllSubs("img")
                        .Select(e => e.GetAttributeValue("src"))
                        .FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(result) &&
                    !result.StartsWith("http://", StringComparison.Ordinal) &&
                    !result.StartsWith("https://", StringComparison.Ordinal))
                {
                    result = string.Empty;
                }

                if (result == null)
                {
                    result = string.Empty;
                }

                return result;
            }
        }
    }
}
