using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace adifpush
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showProgress = args.Any(a => a == "--show-progress");

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

            if (!args.Any())
            {
                string targetFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WSJT-X", "wsjtx_log.adi");

                string lastValidLine = null;
                if (File.Exists(targetFile))
                {
                    lastValidLine = File.ReadAllLines(targetFile).Where(line => !string.IsNullOrWhiteSpace(line) && line.EndsWith("<eor>")).LastOrDefault();
                }

                AdifRecord latestRecord;

                if (lastValidLine == null)
                {
                    Console.WriteLine("No contacts in WSJT-X log yet");
                    latestRecord = new AdifRecord();
                }
                else
                {
                    if (!AdifRecord.TryParse(lastValidLine, out latestRecord, out string error))
                    {
                        Console.WriteLine($"Error parsing last adif record in WSJT-X logfile: {error}");
                        return;
                    }
                }

                var fsw = new FileSystemWatcher(Path.GetDirectoryName(targetFile), Path.GetFileName(targetFile));

                Console.WriteLine($"Cloudlog instance: {linePusher.InstanceUrl}");
                Console.WriteLine($"Watching {targetFile}, ctrl-c to quit...");

                while (true)
                {
                    WaitForChangedResult changed = fsw.WaitForChanged(WatcherChangeTypes.Changed);
                    IEnumerable<AdifRecord> records = GetRecords(targetFile).Where(r => r.QsoStart > latestRecord.QsoStart);

                    if (!records.Any())
                    {
                        continue;
                    }

                    var lines = records.Select(r => r.ToString());
                    PushLineResult[] results = linePusher.PushLines(lines.ToArray(), false, default).Result;
                    foreach (var result in results)
                    {
                        if (result.Success)
                        {
                            Console.WriteLine($"Uploaded QSO with {result.Record.Call}");
                        }
                        else
                        {
                            Console.WriteLine($"Error uploading: {result.ErrorContent}");
                        }
                    }

                    latestRecord = records.OrderByDescending(r => r.QsoStart).First();
                }
            }
            else
            {
                // one-off upload
                if (!args.Any(File.Exists))
                {
                    Console.WriteLine("No existing ADIF file specified");
                    return;
                }

                foreach (var file in args.Where(File.Exists))
                {
                    var lines = GetRecords(file)
                        .Select(ar => ar.ToString())
                        .ToArray();

                    PushLineResult[] results = linePusher.PushLines(lines, showProgress, default).Result;

                    Console.WriteLine($"{file}: {results.Count(r => r.Success)} successful, {results.Count(r => !r.Success)} failure(s)");

                    for (int i = 0; i < results.Length; i++)
                    {
                        if (results[i].Success)
                            continue;

                        Console.WriteLine($"  Line {i + 1}: {results[i].ErrorContent}");
                    }
                }
            }
        }

        private static IEnumerable<AdifRecord> GetRecords(string file)
        {
            string[] lines;

            while (true)
            {
                try
                {
                    lines = File.ReadAllLines(file);
                    break;
                }
                catch (Exception)
                {
                    //Console.WriteLine($"{ex.Message} - retrying...");
                    Thread.Sleep(1000);
                }
            }

            return lines.Select(line => (AdifRecord.TryParse(line, out AdifRecord r, out _) ? r : null))
                        .Where(ar => ar != null);
        }

        private static void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
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
