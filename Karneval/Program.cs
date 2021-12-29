using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Configuration;

namespace Karneval
{
    public class Program
    {
        private static Timer Timer;
        private static string PushOverToken;
        private static string PushOverUser;
        private static string[] Urls;

        private static void Main()
        {
            var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", false).Build();
            PushOverToken = configuration["PushOverToken"];
            PushOverUser = configuration["PushOverUser"];
            Urls = configuration["Urls"].Split(',').Select(x => x.Trim()).ToArray();

            Console.WriteLine("Start");
            SendToPushoverApi("Start");

            Timer = new Timer((int)TimeSpan.FromHours(5).TotalMilliseconds, false);
            Timer.Elapsed += CheckUrls;

            CheckUrls(null, null);

            Console.ReadLine();
            Timer.Dispose();
        }

        private static void CheckUrls(object sender, EventArgs e)
        {
            try
            {
                foreach (var url in Urls)
                {
                    var content = GetContent(url);
                    if (content.Contains("Restkarten") || content.Contains("kaufen"))
                        SendToPushoverApi(url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Timer.Start();
            }
        }

        private static string GetContent(string url)
        {
            using (var webClient = new HttpClient())
            {
                HttpResponseMessage response;
                do
                {
                    webClient.DefaultRequestHeaders.Clear();
                    response = webClient.GetAsync(url).GetAwaiter().GetResult();
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(response.IsSuccessStatusCode);
                }
                while (!response.IsSuccessStatusCode && ThreadSleep15Minutes());

                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return content;
            }
        }

        private static bool ThreadSleep15Minutes()
        {
            Thread.Sleep(TimeSpan.FromMinutes(15));

            return true;
        }

        private static void SendToPushoverApi(string url)
        {
            var client = new HttpClient();
            var toSend = $"token={PushOverToken}&user={PushOverUser}&message=🎶 There are tickets 🎶 {url} 🎶&title=Karneval";
            var now = DateTime.Now;
            if (now.Hour >= 22 || now.Hour < 7)
                toSend = $"{toSend}&sound=none";

            client.PostAsync("https://api.pushover.net/1/messages.json", new StringContent(toSend, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
        }
    }
}
