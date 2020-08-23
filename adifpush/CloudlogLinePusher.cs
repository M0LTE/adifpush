using M0LTE.AdifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace adifpush
{
    public class CloudlogLinePusher : ILinePusher
    {
        readonly Uri url;
        readonly string apikey;
        readonly HttpClient httpClient;

        public CloudlogLinePusher()
        {
            string[] lines = File.ReadAllLines(Program.ConfigFile);
            url = new Uri(lines.Single(l => l.StartsWith("url=", StringComparison.Ordinal)).Split('=')[1]);
            apikey = lines.Single(l=>l.StartsWith("apikey=", StringComparison.Ordinal)).Split('=')[1];
            httpClient = new HttpClient();
        }

        public string InstanceUrl => url.ToString();

        public async Task<PushLineResult[]> PushLines(string[] lines, bool showProgress, DateTime notBefore)
        {
            Uri uri = new Uri(url, "index.php/api/qso");

            if (showProgress)
            {
                Console.WriteLine($"POSTing to {uri}");
            }

            var results = new List<PushLineResult>();

            foreach (string line in lines)
            {
                if (!AdifContactRecord.TryParse(line, out AdifContactRecord contactRecord, out string error))
                {
                    results.Add(new PushLineResult { ErrorContent = "Invalid ADIF: " + error});
                    continue;
                }

                if (contactRecord.QsoStart < notBefore)
                {
                    continue;
                }

                if (contactRecord.Fields.ContainsKey("tx_pwr"))
                {
                    contactRecord.Fields["tx_pwr"] = contactRecord.Fields["tx_pwr"].Replace("W", "");
                }

                string newline = contactRecord.ToString();

                if (showProgress)
                {
                    Console.Write($"{contactRecord.QsoStart.ToString("yyyy-MM-dd HH:mm:ss")} {contactRecord.Call ?? ""}... ");
                }

                HttpResponseMessage responseMessage;
                try
                {
                    responseMessage = await httpClient.PostAsync(uri,
                       new JsonContent(new AdifLineModel { Key = apikey, String = newline }));
                }
                catch (Exception ex)
                {
                    string message = $"{ex.GetType().Name}: {ex.Message}";
                    if (showProgress)
                    {
                        Console.WriteLine(message);
                    }
                    results.Add(new PushLineResult { ErrorContent = message });
                    continue;
                }

                if (showProgress)
                {
                    Console.WriteLine(responseMessage.StatusCode);
                }

                var result = new PushLineResult { Record = contactRecord };

                if (!responseMessage.IsSuccessStatusCode)
                {
                    result.ErrorContent = await responseMessage.Content.ReadAsStringAsync();
                }

                result.Success = responseMessage.IsSuccessStatusCode;

                results.Add(result);
            }

            return results.ToArray();
        }
    }

    public class PushLineResult
    {
        public bool Success { get; set; }
        public string ErrorContent { get; set; }
        public AdifContactRecord Record { get; set; }
    }
}
