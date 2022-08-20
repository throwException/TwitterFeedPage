using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace TwitterFeedPage
{
    public static class Global
    {
        private static PageConfig _config;
        private static Logger _logger;
        private static Twitter _twitter;
        private static Translation _translation;
        private static ImageCache _imageCache;
        private static TDotCo _tDotCo;

        public static PageConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = new PageConfig();
                }

                return _config;
            }
        }

        public static Logger Log
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Logger(Config.LogFilePrefix);
                }

                return _logger;
            }
        }

        public static Twitter Twitter
        {
            get
            {
                if (_twitter == null)
                {
                    _twitter = new Twitter(Config);
                }

                return _twitter;
            }
        }

        public static Translation Translation
        {
            get
            {
                if (_translation == null)
                {
                    _translation = new Translation();
                }

                return _translation;
            }
        }

        public static ImageCache ImageCache
        {
            get
            {
                if (_imageCache == null)
                {
                    _imageCache = new ImageCache();
                }

                return _imageCache;
            }
        }

        public static TDotCo TDotCo
        {
            get
            {
                if (_tDotCo == null)
                {
                    _tDotCo = new TDotCo();
                }

                return _tDotCo;
            }
        }
    }
}