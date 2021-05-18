using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog;

namespace SaprHttpClient
{
    class Program
    {
        private static HttpClient _client;
        //{
        //    Timeout = TimeSpan.FromSeconds(100)
        //};

        static async Task Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();
                var serverAdress = config["ServerAdress"];
                var pollTimeout = int.Parse(config["PollTimeout"]);
                var handler = new HttpClientHandler();
                _client = new HttpClient(handler);

                var dir = Directory.GetCurrentDirectory();
                var di = new DirectoryInfo(dir);
                var templates = di.GetFiles("*.bin");
                var faceTemplRaw = File.ReadAllBytes(templates[0].FullName);
                var palmTemplRaw = File.ReadAllBytes(templates[1].FullName);
                var faceTemplate = Convert.ToBase64String(faceTemplRaw);
                var palmTemplate = Convert.ToBase64String(palmTemplRaw);
                
                while (true)
                {
                    await Task.Run(async () =>
                    {
                        var msg = new SecurePackage()
                        {
                            FaceTemplate = faceTemplate,
                            PalmTemplate = palmTemplate,
                            Msg = "Crypto Test",
                            ClientSendingTime = DateTime.Now,
                        };
                        //var response = await _client.GetAsync("http://localhost:5000/123");
                        var response = await _client.PostAsJsonAsync(serverAdress, msg);
                        var answer = await response.Content.ReadAsStringAsync();
                        var package = JsonConvert.DeserializeObject<SecurePackage>(answer);
                        package.ClientReceivingTime = DateTime.Now;
                        logger.Debug($"Client Sending Time: {package.ClientSendingTime:hh.mm.ss.fff}");
                        logger.Debug($"Server Receiving Time: {package.ServerReceivingTime:hh.mm.ss.fff}");
                        logger.Debug($"Client Receiving Time: {package.ClientReceivingTime:hh.mm.ss.fff}");
                        logger.Debug($"Msg: {package.Msg}");
                    });
                    Thread.Sleep(pollTimeout);
                }
            }
            catch (Exception e)
            {
                logger.Error($"{e}");
                _client.Dispose();
            }
        }
    }
}
