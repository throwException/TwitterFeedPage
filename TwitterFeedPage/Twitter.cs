using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace TwitterFeedPage
{
    public class Twitter
    {
        private class GetCacheItem
        {
            public string Id { get; private set; }
            public IEnumerable<ITweet> Tweets { get; private set; }
            public DateTime Created { get; private set; }

            public GetCacheItem(string id, IEnumerable<ITweet> tweets)
            {
                Id = id;
                Tweets = tweets;
                Created = DateTime.UtcNow;
            }
        }

        private class AvatarCacheItem
        {
            public string Id { get; private set; }
            public MemoryStream Stream { get; private set; }
            public DateTime Created { get; private set; }

            public AvatarCacheItem(string id, MemoryStream stream)
            {
                Id = id;
                Stream = stream;
                Created = DateTime.UtcNow;
            }
        }

        private readonly PageConfig _config;
        private readonly TwitterClient _client;
        private readonly Dictionary<string, GetCacheItem> _getCache;
        private readonly Dictionary<string, AvatarCacheItem> _avatarCache;

        public Twitter(PageConfig config)
        {
            _getCache = new Dictionary<string, GetCacheItem>();
            _avatarCache = new Dictionary<string, AvatarCacheItem>();
            _config = config;
            _client = new TwitterClient(config.ConsumerKey, config.ConsumerSecret, config.AccessToken, config.AccessTokenSecret);
        }

        public IEnumerable<ITweet> Get(FeedConfig config)
        {
            lock (_client)
            {
                var cacheId = config.Name + "." + config.TweetCount;
                if (_getCache.ContainsKey(cacheId))
                {
                    var item = _getCache[cacheId];
                    if (DateTime.UtcNow.Subtract(item.Created).TotalMinutes > 10)
                    {
                        item = new GetCacheItem(cacheId, GetInternal(config, config.TweetCount));
                        _getCache[cacheId] = item;
                    }
                    return item.Tweets;
                }
                else
                {
                    var item = new GetCacheItem(cacheId, GetInternal(config, config.TweetCount));
                    _getCache.Add(cacheId, item);
                    return item.Tweets;
                }
            }
        }

        private IEnumerable<ITweet> GetInternal(FeedConfig config, int count)
        {
            var user = _client.Users.GetUserAsync(config.Username).Result;
            var list = _client.Lists.GetListAsync(config.Slug, user).Result;
            return list.GetTweetsAsync().Result
                .Where(t => !t.IsRetweet)
                .Where(t => !t.InReplyToUserId.HasValue || t.InReplyToUserId.Value == t.CreatedBy.Id)
                .Take(count).ToList();
        }

        public Stream GetAvatar(string username)
        {
            lock (_client)
            {
                if (_avatarCache.ContainsKey(username))
                {
                    var item = _avatarCache[username];
                    if (DateTime.UtcNow.Subtract(item.Created).TotalMinutes > 30)
                    {
                        item = new AvatarCacheItem(username, GetAvatarInternal(username));
                        _avatarCache[username] = item;
                    }
                    return new MemoryStream(item.Stream.ToArray());
                }
                else
                {
                    var item = new AvatarCacheItem(username, GetAvatarInternal(username));
                    _avatarCache.Add(username, item);
                    return new MemoryStream(item.Stream.ToArray());
                }
            }
        }

        private MemoryStream GetAvatarInternal(string username)
        {
            var user = _client.Users.GetUserAsync(username).Result;
            using (var stream = user.GetProfileImageStreamAsync(ImageSize.Bigger).Result)
            {
                var buffer = new byte[4096];
                var memory = new MemoryStream();
                int bytes = 1;
                while (bytes > 0)
                {
                    bytes = stream.Read(buffer, 0, buffer.Length);
                    memory.Write(buffer, 0, bytes);
                }
                return memory;
            }
        }
    }
}
