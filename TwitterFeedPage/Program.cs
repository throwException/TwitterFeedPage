using System;
using System.IO;
using Nancy.Hosting.Self;

namespace TwitterFeedPage
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if ((args.Length < 1) ||
                (!File.Exists(args[0])))
            {
                throw new FileNotFoundException("Config file not found");
            }

            Global.Config.Load(args[0]);

            var translationFile = Global.Config.TranslationConfigFile;
            if (!Path.IsPathRooted(translationFile))
            {
                translationFile = Path.Combine(Path.GetDirectoryName(args[0]), translationFile);
            }
            Global.Translation.Load(translationFile);

            var uri = "http://localhost:8888";
            Global.Log.Notice("Starting TwitterFeedPage filter  on " + uri);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(uri));
            host.Start();  // start hosting

            Global.Log.Notice("Application started");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
