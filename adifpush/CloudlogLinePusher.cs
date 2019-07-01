﻿using System;
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
            this.url = new Uri(lines.Single(l => l.StartsWith("url=", StringComparison.Ordinal)).Split('=')[1]);
            this.apikey = lines.Single(l=>l.StartsWith("apikey=", StringComparison.Ordinal)).Split('=')[1];

            this.httpClient = new HttpClient();
        }

        public async Task<PushLineResult[]> PushLines(string[] lines)
        {
            Uri uri = new Uri(url, "/index.php/api/qso");

            var results = new List<PushLineResult>();

            foreach (string line in lines)
            {
                if (!AdifRecord.TryParse(line, out AdifRecord adifRecord, out string error))
                {
                    results.Add(new PushLineResult { ErrorContent = "Invalid ADIF: " + error });
                    continue;
                }

                if (adifRecord.Fields.ContainsKey("tx_pwr"))
                {
                    adifRecord.Fields["tx_pwr"] = adifRecord.Fields["tx_pwr"].Replace("W", "");
                }

                string newline = adifRecord.ToString();

                HttpResponseMessage responseMessage = await httpClient.PostAsync(uri,
                    new JsonContent(new AdifLineModel { Key = apikey, String = newline }));

                var result = new PushLineResult();

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
    }
}
