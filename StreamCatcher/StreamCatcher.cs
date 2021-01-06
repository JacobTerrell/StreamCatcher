using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp;
using LibVLCSharp.Shared;
using Newtonsoft.Json;

namespace StreamCatcher
{
    public class StreamCatcher
    {
        private string _channelName;
        private string _destination;

        private string ChannelInfoPath => $"https://api.picarto.tv/v1/channel/name/{_channelName}";
        private string BalancingInfoPath => $"https://picarto.tv/process/channel";

        private HttpClient _httpClient;

        public StreamCatcher(string channelName, string destination)
        {
            _channelName = channelName;
            _destination = destination;

            _httpClient = new HttpClient();
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (await IsStreamOnline())
                    {
                        Console.WriteLine("Stream Online");
                        
                        var endpoint = await GetStreamEndpoint();

                        Console.WriteLine("Using endpoint " + endpoint);

                        await Record(endpoint, _destination);
                    }
                    else
                    {
                        Console.WriteLine("Stream Offline");
                    }
                }
                catch (Exception e)
                {

                }

                Console.WriteLine("Waiting...");
                await Task.Delay(1000 * 60 * 5, cancellationToken);
            }
        }

        private async Task<bool> IsStreamOnline()
        {
            var channelInfoResponse = await _httpClient.GetAsync(ChannelInfoPath);
            var channelInfo =
                JsonConvert.DeserializeObject<ChannelInfo>(await channelInfoResponse.Content.ReadAsStringAsync());
            return channelInfo.Online;
        }

        private async Task<string> GetStreamEndpoint()
        {
            var parameters = new Dictionary<string, string>()
            {
                ["loadbalancinginfo"] = _channelName
            };
            var balancingInfoResponse =
                await _httpClient.PostAsync(BalancingInfoPath, new FormUrlEncodedContent(parameters));
            var balancingInfo =
                JsonConvert.DeserializeObject<BalancingInfo>(await balancingInfoResponse.Content.ReadAsStringAsync());
            return balancingInfo.Edges.First(e => e.ID == balancingInfo.PreferredEdge).Endpoint;
        }

        public async Task Record(string endpoint, string destination)
        {
            //var currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            destination = Path.Combine(destination, DateTime.Now.ToString("yyyyMMddHHmmss_") + "record.ts");

            // Load native libvlc library
            Core.Initialize();

            using (var libvlc = new LibVLC())
            using (var mediaPlayer = new MediaPlayer(libvlc))
            {
                // Redirect log output to the console
                libvlc.Log += (sender, e)
                    => Console.WriteLine($"[{e.Level}] {e.Module}:{e.Message}");

                // Create new media with HLS link
                var media = new Media(libvlc,
                    $"https://1-{endpoint}/hls/{_channelName}/index.m3u8", FromType.FromLocation);
                
                // Define stream output options. 
                // In this case stream to a file with the given path
                // and play locally the stream while streaming it.
                media.AddOption(":sout=#file{dst=" + destination + "}");
                media.AddOption(":sout-keep");

                var semaphore = new SemaphoreSlim(0, 1);

                mediaPlayer.EndReached += delegate (object o, EventArgs a)
                {
                    semaphore.Release();
                };

                // Start recording
                mediaPlayer.Play(media);
                
                Console.WriteLine($"Recording in {destination}");

                await semaphore.WaitAsync();

                mediaPlayer.Stop();
            }
        }
    }
}
