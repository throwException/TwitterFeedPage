using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace TwitterFeedPage
{
    public class TDotCo
    {
        private readonly Dictionary<string, string> _cache;

        public TDotCo()
        {
            _cache = new Dictionary<string, string>();
        }

        public static bool IsWrapped(string link)
        {
            return link.StartsWith("https://t.co/", StringComparison.Ordinal) ||
                   link.StartsWith("http://t.co/", StringComparison.Ordinal);
        }

        public string Unwrap(string link)
        {
            if (_cache.ContainsKey(link))
            {
                return _cache[link];
            }
            else
            { 
                var handler = new HttpClientHandler();
                handler.AllowAutoRedirect = false;
                var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(5);
                var result = client.GetAsync(link).Result;
                if (result.StatusCode == HttpStatusCode.Redirect ||
                    result.StatusCode == HttpStatusCode.Moved)
                {
                    var newLink = result.Headers.Location.AbsoluteUri;
                    _cache.Add(link, newLink);
                    return newLink;
                }
                else
                {
                    _cache.Add(link, link);
                    return link;
                }
            }
        }
    }
}
