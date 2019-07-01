using System;
using System.IO;
using System.Text;
using System.Linq;

namespace adifpush
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Any(a => a.EndsWith("configure", StringComparison.OrdinalIgnoreCase)))
            {
                DoConfigureDialogue();
                return;
            }

            if (!File.Exists(ConfigFile))
            {
                Console.WriteLine("Config not found - run again with --configure");
                return;
            }

            ILinePusher linePusher = new CloudlogLinePusher();

            if (!args.Any(File.Exists))
            {
                Console.WriteLine("No existing ADIF file specified");
                return;
            }

            foreach (var file in args.Where(File.Exists))
            {
                var lines = File.ReadAllLines(file)
                    .Select(line => (AdifRecord.TryParse(line, out AdifRecord r, out _) ? r : null))
                    .Where(ar => ar != null)
                    .Select(ar => ar.ToString())
                    .ToArray();

                PushLineResult[] results = linePusher.PushLines(lines).Result;

                Console.WriteLine($"{file}: {results.Count(r => r.Success)} successful, {results.Count(r => !r.Success)} failure(s)");

                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i].Success)
                        continue;

                    Console.WriteLine($"  Line {i + 1}: {results[i].ErrorContent}");
                }
            }
        }

        internal static string ConfigFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".adifpush", "cloudlog");

        private static void DoConfigureDialogue()
        {
            Console.Write("URL? ");
            string url = Console.ReadLine();
            Console.Write("API key? ");
            string apikey = Console.ReadLine();

            var content = new StringBuilder();
            content.Append("url=");
            content.AppendLine(url);
            content.Append("apikey=");
            content.AppendLine(apikey);

            string targetDir = Path.GetDirectoryName(ConfigFile);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.WriteAllText(ConfigFile, content.ToString());
        }
    }
}
