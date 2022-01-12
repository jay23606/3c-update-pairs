using XCommas.Net;
using XCommas.Net.Objects;
using System.Net;

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

        public static int MAX_BUBBLE_PAIRS = 50;

        static void Main() { MainAsync().GetAwaiter().GetResult(); }
        static async Task MainAsync()
        {
            string perp = ((usePerp) ? "-PERP" : "");
            api = new XCommasApi(key, secret, default, UserMode.Real);
            var accts = await api.GetAccountsAsync();
            if (accountId != 0) foreach (var acct in accts.Data) if (acct.MarketCode == market) { accountId = acct.Id; break; }

            HashSet<string> pairs = new HashSet<string>();
            var marketPairs = await api.GetMarketPairsAsync(market);
            foreach (string p in marketPairs.Data) if (p.StartsWith(baseType)) pairs.Add(p + perp);

            while (true)
            {
                try
                {
                    Console.WriteLine("Adding cryptobubble pairs...");
                    List<Bubble500Root> bubbles = await GetJSON<List<Bubble500Root>>("https://cryptobubbles.net/backend/data/currentBubbles500.json");

                    bubbles = bubbles.OrderByDescending(x => x.data.usd.performance.min5).ToList();

                    HashSet<string> pairsToUpdate = new HashSet<string>();
                    int idx = 1;
                    foreach (Bubble500Root bubble in bubbles)
                    {
                        if (pairsToUpdate.Count >= MAX_BUBBLE_PAIRS) break;
                        string pair = $"{baseType}_{bubble.symbol}{perp}";
                        if (!pairsToUpdate.Contains(pair))
                        {
                            if (pairs.Contains(pair))
                            {
                                pairsToUpdate.Add(pair);
                                Console.WriteLine($"{idx}) Added {pair} on {market}, performance: {bubble.data.usd.performance.min5}");
                            }
                        }
                        idx++;
                    }

                    //return;

                    var sb = await api.ShowBotAsync(botId: botId);
                    Bot bot = sb.Data;
                    //pairsToUpdate.Add("USDT_TESTTEST");
                    bot.Pairs = pairsToUpdate.ToArray();
                    var ub = await api.UpdateBotAsync(botId, new BotUpdateData(bot));
                    if (ub.IsSuccess) Console.WriteLine($"\nSuccessfully updated {bot.Name} with {pairsToUpdate.Count} new pairs..");
                    else
                    {
                        //I couldn't find the market code for ftx futures although it probably exists
                        if (ub.Error.Contains("No market data for this pair"))
                        {
                            string[] badPairs = ub.Error.Split(": ").Select(p => p.Substring(0, p.IndexOf('"'))).ToArray();
                            foreach (string badPair in badPairs)
                                if (pairsToUpdate.Contains(badPair))
                                {
                                    Console.WriteLine($"Removed {badPair} on {market} because it only exists on spot");
                                    pairsToUpdate.Remove(badPair);
                                }
                                else if (badPair.Contains(baseType)) Console.WriteLine(badPair + " malformed?");
                            bot.Pairs = pairsToUpdate.ToArray();
                            ub = await api.UpdateBotAsync(botId, new BotUpdateData(bot));
                            if (ub.IsSuccess) Console.WriteLine($"\nSuccessfully updated {bot.Name} with {pairsToUpdate.Count} new pairs..");
                            else Console.WriteLine($"ERROR: {ub.Error}");
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: {ub.Error}");
                        }
                    }
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
