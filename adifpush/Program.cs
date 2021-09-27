using M0LTE.AdifLib;
using M0LTE.WsjtxUdpLib.Client;
using M0LTE.WsjtxUdpLib.Messages;
using M0LTE.WsjtxUdpLib.Messages.Out;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
                using var client = new WsjtxClient(RecordReceived, IPAddress.Parse("239.1.2.3"), multicast: true, debug: true);

                Console.WriteLine($"Cloudlog instance: {linePusher.InstanceUrl}");
                Console.WriteLine($"Listening for WSJT-X, ctrl-c to quit...");

                Thread.CurrentThread.Join();

                void RecordReceived(WsjtxMessage message, IPEndPoint _) 
                {
                    if (!(message is LoggedAdifMessage loggedAdifMessage))
                    {
                        return;
                    }

                    if (!AdifFile.TryParse(loggedAdifMessage.AdifText, out AdifFile adifFile))
                    {
                        return;
                    }

                    string adifRecord = adifFile.Records.Single().ToString();

                    PushLineResult[] results = linePusher.PushLines(new[] { adifRecord }, false, default).Result;

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

            return lines.Select(line => (AdifContactRecord.TryParse(line, out AdifContactRecord r, out _) ? r : null))
                        .Where(ar => ar != null);
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
