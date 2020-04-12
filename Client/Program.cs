using GCache;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace GClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new GCache.Caching.CachingClient(channel);

            //var setCacheResponse = await client.SetCacheAsync(new  CacheVM {  Key="vv", ExpiresAt= Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(5)), Value="bbb" });
            //Console.WriteLine("setCacheResponse: " + JsonConvert.SerializeObject(setCacheResponse));

            var getAllResponse = await client.GetCacheAsync(new CacheVM { Key="vv"});
            Console.WriteLine("setCacheResponse: " + JsonConvert.SerializeObject(getAllResponse));

            var getKeyResponse = await client.GetAllAsync(new CacheVM { Key="a" });
            Console.WriteLine("setCacheResponse: " + JsonConvert.SerializeObject(getKeyResponse));


            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
