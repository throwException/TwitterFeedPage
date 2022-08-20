using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TwitterFeedPage
{
    public enum Language
    { 
        English,
        German,
        French,
        Italien,
        Spanish
    }

    public static class LanguageExtensions
    {
        public static Language Parse(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "en":
                    return Language.English;
                case "de":
                    return Language.German;
                case "fr":
                    return Language.French;
                case "it":
                    return Language.Italien;
                case "sp":
                    return Language.Spanish;
                default:
                    throw new NotSupportedException("Language " + value.Trim().ToLowerInvariant() + " not known.");
            }
        }
    }

    public class Phrase
    {
        public const string PhraseTag = "Phrase";
        public const string TextTag = "Text";
        public const string LanguageAttribute = "Language";
        public const string KeyAttribute = "Key";

        private readonly Dictionary<Language, string> _texts;

        public string Key { get; }

        public Phrase(XElement config)
        {
            _texts = new Dictionary<Language, string>();
            Key = config.Attribute(KeyAttribute).Value;

            foreach (var element in config.Elements(TextTag))
            {
                var language = LanguageExtensions.Parse(element.Attribute(LanguageAttribute).Value);
                _texts.Add(language, element.Value);
            }
        }

        public string Get(Language language)
        {
            if (_texts.ContainsKey(language))
            {
                return _texts[language];
            }
            else
            {
                return _texts.Values.First();
            }
        }
    }

    public class Translation
    {
        private readonly Dictionary<string, Phrase> _phrases;

        public Translation()
        {
            _phrases = new Dictionary<string, Phrase>();
        }

        public void Load(string filename)
        {
            Load(XDocument.Load(filename));
        }

        public void Load(XDocument document)
        { 
            foreach (var element in document.Root.Elements(Phrase.PhraseTag))
            {
                var phrase = new Phrase(element);
                _phrases.Add(phrase.Key, phrase);
            }
        }

        public string Get(string key, Language language)
        {
            return _phrases[key].Get(language);
        }
    }
}
