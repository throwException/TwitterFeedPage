using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace TwitterFeedPage
{
    public class ImageCacheItem
    {
        public string Id { get; private set; }
        public string Url { get; private set; }
        public string ContentType { get; private set; }
        public DateTime Created { get; private set; }
        public MemoryStream Data { get; private set; }

        public static string UrlToId(string url)
        {
            return Encoding.UTF8.GetBytes(url).HashSha256().ToHexString();
        }
    
        public ImageCacheItem(string url, string contentType, MemoryStream data)
        {
            Url = url;
            Id = UrlToId(url);
            Created = DateTime.UtcNow;
            Data = data;
        }
    }

    public class ImageCache
    {
        private readonly Dictionary<string, ImageCacheItem> _cache;

        public ImageCache()
        {
            _cache = new Dictionary<string, ImageCacheItem>();
        }

        private Tuple<string, MemoryStream> Download(string path)
        {
            var memory = new MemoryStream();
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var result = client.GetAsync(path).Result;
            if (result.StatusCode == HttpStatusCode.OK)
            {
                using (var data = result.Content.ReadAsStreamAsync().Result)
                {
                    var buffer = new byte[4096];
                    var bytes = 1;
                    while (bytes > 0)
                    {
                        bytes = data.Read(buffer, 0, buffer.Length);
                        memory.Write(buffer, 0, bytes);
                    }
                }
                var contentType = result.Content.Headers.GetValues("content-type").FirstOrDefault() ?? string.Empty;
                return new Tuple<string, MemoryStream>(contentType, memory);
            }
            else
            {
                return null;
            }
        }

        public ImageCacheItem Set(string url)
        {
            lock (_cache)
            {
                ImageCacheItem item = null;
                var id = ImageCacheItem.UrlToId(url);

                if (_cache.ContainsKey(id))
                {
                    item = _cache[id];
                    if (DateTime.UtcNow.Subtract(item.Created).TotalMinutes >= 30)
                    {
                        var result = Download(url);
                        if (result != null)
                        {
                            item = new ImageCacheItem(url, result.Item1, result.Item2);
                            _cache[id] = item;
                        }
                    }
                }
                else
                {
                    var result = Download(url);
                    if (result != null)
                    {
                        item = new ImageCacheItem(url, result.Item1, result.Item2);
                        _cache.Add(id, item);
                    }
                }

                return item;
            }
        }

        public ImageCacheItem Get(string id)
        {
            lock (_cache)
            {
                if (_cache.ContainsKey(id))
                {
                    return _cache[id];
                }

                return null;
            }
        }
    }
}
