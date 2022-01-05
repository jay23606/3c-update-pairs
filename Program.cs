using System;
using System.Linq;
using XCommas.Net;
using XCommas.Net.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace update_pairs
{
    class Program
    {
        //update with your info
        static string key = "xxx";
        static string secret = "xxx";
        static string market = "kucoin";
        static string baseType = "USDT";
        static bool usePerp = false;
        static int botId = 1234567;
        static int accountId = 0;
        
        public static XCommasApi api;
        public static HttpClient hc = new HttpClient();

        public static int MAX_ALTRANK_PAIRS = 40;
        public static int MAX_GALAXY_PAIRS = 20;

        static Random r = new Random();

        static void Main() { MainAsync().GetAwaiter().GetResult(); }
        static async Task MainAsync()
        {
            api = new XCommasApi(key, secret, default, UserMode.Real);
            var accts = await api.GetAccountsAsync();
            if (accountId != 0) foreach (var acct in accts.Data) if (acct.MarketCode == market) { accountId = acct.Id; break; }
            var marketPairs = await api.GetMarketPairsAsync(market);
            HashSet<string> pairs = new HashSet<string>();
            foreach (string p in marketPairs.Data) if (p.StartsWith(baseType)) pairs.Add(p); //get available USDT pairs 

            while (true)
            {
                try
                {
                    LunarCrushRoot res = await GetJSON<LunarCrushRoot>("https://api.lunarcrush.com/v2?data=market&type=fast&sort=acr&limit=1000&key=asdf");
                    HashSet<string> pairsToUpdate = new HashSet<string>();

                    foreach(Datum d in res.data)
                    {
                        if (pairsToUpdate.Count >= MAX_ALTRANK_PAIRS) break;
                        string pair = $"{baseType}_{d.s}{((usePerp) ? "-PERP" : "")}";
                        if (pairs.Contains(pair) && !pairsToUpdate.Contains(pair)) pairsToUpdate.Add(pair);
                    }
                    int altRankPairsCount = pairsToUpdate.Count;
                    res = await GetJSON<LunarCrushRoot>("https://api.lunarcrush.com/v2?data=market&type=fast&sort=gs&limit=1000&key=asdf&desc=True");
                    foreach (Datum d in res.data)
                    {
                        if (pairsToUpdate.Count >= altRankPairsCount + MAX_GALAXY_PAIRS) break;
                        string pair = $"{baseType}_{d.s}{((usePerp) ? "-PERP" : "")}";
                        if (pairs.Contains(pair) && !pairsToUpdate.Contains(pair)) pairsToUpdate.Add(pair);
                    }

                    var sb = await api.ShowBotAsync(botId: botId);
                    Bot bot = sb.Data;
                    bot.Pairs = pairsToUpdate.ToArray();

                    var ub = await api.UpdateBotAsync(botId, new BotUpdateData(bot));
                    if (ub.IsSuccess) Console.WriteLine($"Successfully updated {bot.Name} with {pairsToUpdate.Count} new pairs..");
                    else Console.WriteLine($"ERROR: {ub.Error}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                    api = new XCommasApi(key, secret, default, UserMode.Real);
                    hc = new HttpClient();
                }
                //update every five minutes
                await Task.Delay(1000 * 60 * 5);
            }
        }

        static async Task<dynamic> GetJSON<T>(string endpoint)
        {
            var res = await hc.GetAsync(endpoint);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                string json = await res.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            return null;
        }
    }
}
