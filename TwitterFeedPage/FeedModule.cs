using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Nancy;
using Tweetinvi.Models;

namespace TwitterFeedPage
{
    public class TweetModel
    {
        public string CreatedBy;
        public string CreatedAt;
        public string Text;
        public string AvatarUrl;
        public string Retweets;
        public string Favorites;
        public string Link;
        public string RetweetLink;
        public string FavoriteLink;
        public string ReplyLink;
        public string PreviewSite;
    }

    public class FeedModel
    {
        public List<TweetModel> List;
        public string SiteUrl;

        public FeedModel()
        {
            List = new List<TweetModel>();
        }
    }

    public class FeedModule : NancyModule
    {
        private string Unwrap(string url)
        {
            if (TDotCo.IsWrapped(url))
            {
                return Global.TDotCo.Unwrap(url);
            }
            else
            {
                return url;
            }
        }

        private string FormatTwitterLink(string url)
        {
            if (TDotCo.IsWrapped(url))
            {
                url = Global.TDotCo.Unwrap(url);
            }

            return FormatLink(url);
        }

        private string FormatLink(string url)
        {
            return FormatLink(url, url);
        }

        private string FormatLink(string url, string text)
        {
            return string.Format("<a target=\"_blank\" href=\"{0}\">{1}</a>", url, text);
        }

        private string FormatHashTag(string hashTag)
        {
            return FormatLink("https://twitter.com/hashtag/" + hashTag.Substring(1), hashTag);
        }

        private string FormatUserTag(string username)
        {
            return FormatLink("https://twitter.com/" + username.Substring(1), username);
        }

        private int HashTagLength(string text)
        { 
            for (int i = 1; i < text.Length; i++)
            { 
                if (char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
                else if (text[i] == '_')
                { 
                    // ignore this
                }
                else if (char.IsSymbol(text[i]))
                {
                    return i;
                }
                else if (char.IsPunctuation(text[i]))
                {
                    return i;
                }
            }

            return text.Length;
        }

        private int LinkLength(string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return text.Length;
        }

        private string EndingUrl(ITweet tweet)
        {
            var text = tweet.FullText.Trim();
            var endingWord = text
                .Split(new string[] { " ", "\t", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;
            if (endingWord.StartsWith("https://", StringComparison.Ordinal) ||
                endingWord.StartsWith("http://", StringComparison.Ordinal))
            {
                return endingWord;
            }
            else
            {
                return null;
            }
        }

        private string FormatTweetText(ITweet tweet, int removeLastChars = 0)
        {
            var finishedText = string.Empty;
            var remainingText = tweet.FullText.Trim();
            if (removeLastChars > 0)
            {
                remainingText = remainingText
                    .Substring(0, remainingText.Length - removeLastChars)
                    .Trim();
            }

            while (remainingText.Length > 0)
            { 
                if (remainingText.StartsWith("@", StringComparison.Ordinal))
                {
                    var length = HashTagLength(remainingText);
                    var hashTag = remainingText.Substring(0, length);
                    finishedText += FormatUserTag(hashTag);
                    remainingText = remainingText.Substring(length);
                }
                else if (remainingText.StartsWith("#", StringComparison.Ordinal))
                {
                    var length = HashTagLength(remainingText);
                    var hashTag = remainingText.Substring(0, length);
                    finishedText += FormatHashTag(hashTag);
                    remainingText = remainingText.Substring(length);
                }
                else if (remainingText.StartsWith("http://", StringComparison.Ordinal))
                {
                    var length = LinkLength(remainingText);
                    var link = remainingText.Substring(0, length);
                    finishedText += FormatTwitterLink(link);
                    remainingText = remainingText.Substring(length);
                }
                else if (remainingText.StartsWith("https://", StringComparison.Ordinal))
                {
                    var length = LinkLength(remainingText);
                    var link = remainingText.Substring(0, length);
                    finishedText += FormatTwitterLink(link);
                    remainingText = remainingText.Substring(length);
                }
                else
                {
                    finishedText += remainingText.Substring(0, 1);
                    remainingText = remainingText.Substring(1);
                }
            }

            return finishedText;
        }

        public string FormatAgo(DateTime datetime, Language language)
        {
            var ago = DateTime.UtcNow.Subtract(datetime);

            if (ago.TotalDays >= 1.5d)
            {
                return string.Format(
                    Global.Translation.Get("ago-days", language), 
                    Math.Round(ago.TotalDays, 0));
            }
            else if (ago.TotalDays >= 0.95d)
            {
                return Global.Translation.Get("ago-day", language);
            }
            else if (ago.TotalHours >= 1.5d)
            {
                return string.Format(
                    Global.Translation.Get("ago-hours", language),
                    Math.Round(ago.TotalHours, 0));
            }
            else if (ago.TotalHours >= 0.95d)
            {
                return Global.Translation.Get("ago-hour", language);
            }
            else if (ago.TotalMinutes >= 1.5d)
            {
                return string.Format(
                    Global.Translation.Get("ago-minutes", language),
                    Math.Round(ago.TotalMinutes, 0));
            }
            else if (ago.TotalMinutes >= 0.95d)
            {
                return Global.Translation.Get("ago-minute", language);
            }
            else if (ago.TotalSeconds >= 1.5d)
            {
                return string.Format(
                    Global.Translation.Get("ago-seconds", language),
                    Math.Round(ago.TotalSeconds, 0));
            }
            else
            {
                return Global.Translation.Get("ago-second", language);
            }
        }

        public FeedModule()
        {
            Get("/feed/{feed}/{language}", parameters =>
            {
                string feedString = parameters.feed;
                string languageString = parameters.language;
                var language = LanguageExtensions.Parse(languageString);
                var feedConfig = Global.Config.Feeds.Single(f => f.Name == feedString);
                var tweets = Global.Twitter.Get(feedConfig);

                var model = new FeedModel();
                model.SiteUrl = Global.Config.SiteUrl;
                foreach (var tweet in tweets)
                {
                    var t = new TweetModel();
                    t.CreatedAt = FormatAgo(tweet.CreatedAt.DateTime, language);
                    t.CreatedBy = FormatLink("https://twitter.com/" + tweet.CreatedBy.ScreenName, tweet.CreatedBy.Name);
                    t.Text = FormatTweetText(tweet);
                    t.PreviewSite = string.Empty;
                    var endingUrl = EndingUrl(tweet);
                    if (!string.IsNullOrEmpty(endingUrl))
                    {
                        var endingUrlUnwrap = Global.TDotCo.Unwrap(endingUrl);
                        if (Regex.IsMatch(endingUrlUnwrap, "https\\://twitter.com/.+/status/.+"))
                        {
                            if (tweet.QuotedTweet != null)
                            {
                                t.Text = FormatTweetText(tweet, endingUrl.Length + 1);
                                if (tweet.QuotedTweet.CreatedBy.Id != tweet.CreatedBy.Id)
                                {
                                    var quotedTweetAvatarUrl = Global.Config.SiteUrl + "/avatar/" + tweet.QuotedTweet.CreatedBy.ScreenName + ".jpg";
                                    var preview = new StringBuilder();
                                    preview.AppendLine("<div class=\"preview-item\">");
                                    preview.AppendLine("<div class=\"flex-row\">");
                                    preview.AppendLine("<div class=\"flex-one\">");
                                    preview.AppendLine("<img width=\"100%\" alt=\"Avatar\" src=\"" + quotedTweetAvatarUrl + "\"/>");
                                    preview.AppendLine("</div>");
                                    preview.AppendLine("<div class=\"flex-nine\">");
                                    preview.AppendLine("<div class=\"flex-row\">");
                                    preview.AppendLine("<div class=\"flex-one half-width\">" + FormatLink("https://twitter.com/" + tweet.QuotedTweet.CreatedBy.ScreenName, tweet.QuotedTweet.CreatedBy.Name) + "</div>");
                                    preview.AppendLine("<div class=\"flex-one half-width right-align\">" + FormatAgo(tweet.QuotedTweet.CreatedAt.DateTime, language) + "</div>");
                                    preview.AppendLine("</div>");
                                    preview.AppendLine("<div class=\"flex-row\">");
                                    preview.AppendLine("<div class=\"flex-one full-width\">" + FormatTweetText(tweet.QuotedTweet) + "</div>");
                                    preview.AppendLine("</div>");
                                    preview.AppendLine("</div>");
                                    preview.AppendLine("</div>");
                                    preview.AppendLine("</div><br/>");
                                    t.PreviewSite = preview.ToString();
                                }
                            }
                        }
                        else
                        {
                            var sitePreview = new SitePreview(endingUrlUnwrap);
                            if (sitePreview.Valid)
                            {
                                t.Text = FormatTweetText(tweet, endingUrl.Length + 1);
                                var preview = new StringBuilder();
                                preview.AppendLine("<div class=\"preview-item\">");

                                var imageUrl = sitePreview.ImageUrl;
                                if (!string.IsNullOrEmpty(imageUrl))
                                {
                                    var imageItem = Global.ImageCache.Set(imageUrl);
                                    var previewImageUrl = Global.Config.SiteUrl + "/preview/images/" + imageItem.Id;

                                    preview.Append("<div class=\"preview-image\">");
                                    preview.Append("<a target=\"_blank\" href=\"" + endingUrlUnwrap + "\">");
                                    preview.Append("<img width=\"100%\" src=\"" + previewImageUrl + "\"/>");
                                    preview.Append("</a>");
                                    preview.AppendLine("</div>");
                                }

                                var title = sitePreview.Title;
                                var description = sitePreview.Description;

                                if (!string.IsNullOrEmpty(title))
                                {
                                    preview.Append("<div class=\"preview-title\">");
                                    preview.Append("<a target=\"_blank\" href=\"" + endingUrlUnwrap + "\">");
                                    preview.Append("<b>" + title + "</b>");
                                    preview.Append("</a>");
                                    preview.AppendLine("</div>");

                                    if (!string.IsNullOrEmpty(description))
                                    {
                                        preview.Append("<div class=\"preview-description\">");
                                        preview.Append(description);
                                        preview.AppendLine("</div>");
                                    }
                                }
                                else
                                {
                                    preview.Append("<div class=\"preview-description\">");
                                    preview.Append("<a target=\"_blank\" href=\"" + endingUrlUnwrap + "\">");
                                    preview.Append(description);
                                    preview.Append("</a>");
                                    preview.AppendLine("</div>");
                                }

                                preview.AppendLine("</div><br/>");
                                t.PreviewSite = preview.ToString();
                            }
                        }
                    }
                    t.AvatarUrl = Global.Config.SiteUrl + "/avatar/" + tweet.CreatedBy.ScreenName + ".jpg";
                    t.Retweets = tweet.RetweetCount.ToString();
                    t.Favorites = tweet.FavoriteCount.ToString();
                    t.Link = tweet.Url;
                    t.RetweetLink = "https://twitter.com/intent/retweet?tweet_id=" + tweet.Id.ToString();
                    t.FavoriteLink = "https://twitter.com/intent/favorite?tweet_id=" + tweet.Id.ToString();
                    t.ReplyLink = "https://twitter.com/intent/tweet?in_reply_to=" + tweet.Id.ToString();
                    model.List.Add(t);
                }

                return View["View/feed.sshtml", model];
            });
            Get("/avatar/{username}.jpg", parameters =>
            {
                string usernameString = parameters.username;
                return Response.FromStream(Global.Twitter.GetAvatar(usernameString), "image/jpeg");
            });
            Get("/preview/images/{id}", parameters =>
            {
                string idString = parameters.id;
                var item = Global.ImageCache.Get(idString);
                if (item != null)
                {
                    return Response.FromStream(new MemoryStream(item.Data.ToArray()), item.ContentType);
                }
                else
                {
                    return new NotFoundResponse();
                }
            });
        }
    }
}
