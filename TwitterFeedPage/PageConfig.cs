using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace TwitterFeedPage
{
    public class FeedConfig : ConfigSection
    {
        public const string NameTag = "Name";
        public const string UsernameTag = "Username";
        public const string SlugTag = "Slug";
        public const string TweetCountTag = "TweetCount";

        public string Name { get; private set; }
        public string Username { get; private set; }
        public string Slug { get; private set; }
        public int TweetCount { get; private set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString(NameTag, v => Name = v);
                yield return new ConfigItemString(UsernameTag, v => Username = v);
                yield return new ConfigItemString(SlugTag, v => Slug = v);
                yield return new ConfigItemInt32(TweetCountTag, v => TweetCount = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs
        {
            get { return new SubConfig[0]; }
        }
    }

    public class PageConfig : Config
    {
        public const string FeedTag = "Feed";

        private readonly List<FeedConfig> _feeds;

        public IEnumerable<FeedConfig> Feeds { get { return _feeds; } }

        public PageConfig()
        {
            _feeds = new List<FeedConfig>();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                return new ConfigSection[0];
            }
        }

        public const string LogFilePrefixTag = "LogFilePrefix";
        public const string AccessTokenTag = "AccessToken";
        public const string AccessTokenSecretTag = "AccessTokenSecret";
        public const string ConsumerKeyTag = "ConsumerKey";
        public const string ConsumerSecretTag = "ConsumerSecret";
        public const string TranslationConfigFileTag = "TranslationConfigFile";
        public const string SiteUrlTag = "SiteUrl";

        public string LogFilePrefix { get; private set; }
        public string AccessToken { get; private set; }
        public string AccessTokenSecret { get; private set; }
        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }
        public string TranslationConfigFile { get; private set; }
        public string SiteUrl { get; private set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString(LogFilePrefixTag, v => LogFilePrefix = v);
                yield return new ConfigItemString(AccessTokenTag, v => AccessToken = v);
                yield return new ConfigItemString(AccessTokenSecretTag, v => AccessTokenSecret = v);
                yield return new ConfigItemString(ConsumerKeyTag, v => ConsumerKey = v);
                yield return new ConfigItemString(ConsumerSecretTag, v => ConsumerSecret = v);
                yield return new ConfigItemString(TranslationConfigFileTag, v => TranslationConfigFile = v);
                yield return new ConfigItemString(SiteUrlTag, v => SiteUrl = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs
        { 
            get
            {
                return new SubConfig[0]; 
            }
        }

        public override void Load(XElement root)
        {
            base.Load(root);

            _feeds.Clear();
            foreach (var calendarElement in root.Elements(FeedTag))
            {
                var config = new FeedConfig();
                config.Load(calendarElement);
                _feeds.Add(config);
            }
        }
    }
}
