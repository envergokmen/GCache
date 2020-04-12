using Newtonsoft.Json;
using GCache.Nodes;
using GCache.PersistentProviders;
using GCache.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GCache
{
    public partial class SwCacheService
    {

        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;
        private static object lockObj = new object();
        //private string CacheFolder = "";
        private Dictionary<string, CacheVM> cache = new Dictionary<string, CacheVM>();
        private DateTime lastAllocationDate = DateTime.Now;
        IPersistentProvider persister = new PersistentFactory().Persister;
        //List<ISwNodeClient> nodes = new NodeClientFactory().Nodes;
        string Id = "1"; // ConfigurationManager.AppSettings["id"];
        INodeSyncronizer nodeSynconizer = new NodeSyncronizer();

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        //public G2CacheService(int port)
        //{
        //    this.Initialize(port);
        //}

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        //public G2CacheService()
        //{
        //    //get an empty port
        //    TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        //    l.Start();
        //    int port = ((IPEndPoint)l.LocalEndpoint).Port;
        //    l.Stop();
        // //   this.Initialize(port);
        //}

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        //public void Stop()
        //{
        //    _serverThread.Abort();
        //    _listener.Stop();
        //}

        //private void Listen()
        //{
        //    _listener = new HttpListener();
        //    _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
        //    _listener.Start();
        //    while (true)
        //    {
        //        try
        //        {
        //            HttpListenerContext context = _listener.GetContext();
        //            Process(context);
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //}


        /// <summary>
        /// Generates stream from string
        /// </summary>
        /// <param name="s">string for the stream</param>
        /// <returns>Stream</returns>
        private Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Read and convert to string POST request body
        /// </summary>
        /// <param name="context">Current HttpListener</param>
        /// <returns>RequestBody as string</returns>
        private string GetBodyRequestBodyAsString(HttpListenerContext context)
        {
            string requestBody = "";

            if (context.Request.HasEntityBody)
            {
                using (System.IO.Stream body = context.Request.InputStream) // here we have data
                {
                    using (System.IO.StreamReader reader = new System.IO.StreamReader(body, System.Text.Encoding.UTF8))
                    {
                        requestBody = reader.ReadToEnd();

                    }
                }
            }

            return requestBody;
        }


        // Displays the header information that accompanied a request.
        private Dictionary<string, string> GetHeaders(HttpListenerRequest request)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            System.Collections.Specialized.NameValueCollection headers = request.Headers;

            foreach (string Key in headers.AllKeys)
            {
                string[] values = headers.GetValues(Key);

                if (values.Length > 0)
                {
                    if (!result.ContainsKey(Key))
                    {
                        result.Add(Key, values[0]);

                    }

                }
            }

            return result;
        }

        /// <summary>
        /// Parse Request Params and Convert To CacheViewModel Object
        /// </summary>
        /// <param name="requestBody">request post body</param>
        /// <returns>CacheVM</returns>
        private CacheVM GetAsCacheRequest(string requestBody)
        {
            return JsonConvert.DeserializeObject<CacheVM>(requestBody);

        }

        /// <summary>
        /// Writes text to http result with headers
        /// </summary>
        /// <param name="responseBody">which text will be write</param>
        /// <param name="context">Current HttpListener Context</param>
        private void WriteStringToHttpResult(string responseBody, HttpListenerContext context, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                Stream input = GenerateStreamFromString(responseBody);

                context.Response.ContentEncoding = System.Text.Encoding.UTF8;
                context.Response.ContentType = "application/json"; // _mimeTypeMappings.TryGetValue(".html", out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(path).ToString("r"));
                context.Response.AddHeader("NodeId", this.Id);


                byte[] buffer = new byte[1024 * 16];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();

                context.Response.StatusCode = (int)statusCode;
                context.Response.OutputStream.Flush();


            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

        }

        /// <summary>
        /// Flush Removes all cache keys
        /// </summary>
        /// <param name="context">Current HttpListener Context</param>
        private void RemoveAllCache(HttpListenerContext context)
        {

            string requestBody = GetBodyRequestBodyAsString(context);
            CacheVM cacheForRemove = GetAsCacheRequest(requestBody);

            string source = GetSoureHeaderParam(context);
            if (source != null && source == this.Id) return;

            lock (lockObj)
            {
                cache = new Dictionary<string, CacheVM>();
            }

            Task.Run(() => GC.Collect());
            persister.DeleteCacheBulk();

            Task.Run(() => nodeSynconizer.DeleteFromAllNodes(cacheForRemove, source));


            WriteStringToHttpResult("{\"result\":\"OK\"}", context);


        }

        /// <summary>
        /// Moves all keys to new dictionary in order to free memory
        /// </summary>
        /// <param name="context">Current HttpListener Context</param>
        //private void AllocateMemory()
        //{
        //    if (lastAllocationDate > DateTime.Now.AddMinutes(-1))
        //    {
        //        return;
        //    }

        //    lock (lockObj)
        //    {
        //        var cache2 = new Dictionary<string, CacheVM>();

        //        foreach (var item in cache)
        //        {
        //            if (item.Value.ExpiresAt > DateTime.Now)
        //            {
        //                cache2.Add(item.Key, item.Value);
        //            }
        //        }

        //        cache = cache2;
        //        GC.Collect();
        //        lastAllocationDate = DateTime.Now;

        //    }


        //}

        /// <summary>
        /// Removes only matched cache items which starts with Key CacheKey
        /// </summary>
        /// <param name="context">Current HttpListener Context</param>
        private void RemoveCacheStartsWith(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath;

            if (context.Request.HasEntityBody)
            {
                string requestBody = GetBodyRequestBodyAsString(context);
                CacheVM cacheForRemove = GetAsCacheRequest(requestBody);

                string source = GetSoureHeaderParam(context);
                if (source != null && source == this.Id) return;

                if (cacheForRemove != null && !String.IsNullOrWhiteSpace(cacheForRemove.Key))
                {
                    lock (lockObj)
                    {

                        foreach (var item in cache.ToList())
                        {
                            if (item.Key.StartsWith(cacheForRemove.Key))
                            {
                                cache.Remove(item.Key);
                            }
                        }

                    }

                    Task.Run(() => AllocateMemory());
                    Task.Run(() => nodeSynconizer.DeleteFromNodesStartsWith(cacheForRemove, source));

                    persister.DeleteCacheBulk(cacheForRemove.Key);

                }
            }

            WriteStringToHttpResult("{\"result\":\"OK\"}", context);

        }


        /// <summary>
        /// Remove only item which Key's equal to CacheKey
        /// </summary> 
        /// <param name="context">Current HttpListener Context</param>
        private void RemoveCache(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath;

            if (context.Request.HasEntityBody)
            {
                string requestBody = GetBodyRequestBodyAsString(context);
                CacheVM cacheForRemove = GetAsCacheRequest(requestBody);

                string source = GetSoureHeaderParam(context);
                if (source != null && source == this.Id) return;


                if (cacheForRemove != null && !String.IsNullOrWhiteSpace(cacheForRemove.Key))
                {
                    cache.Remove(cacheForRemove.Key);
                    persister.DeleteCache(cacheForRemove.Key);
                    nodeSynconizer.DeleteFromNodes(cacheForRemove, source);

                }
            }

            WriteStringToHttpResult("{\"result\":\"OK\"}", context);
        }

        /// <summary>
        /// Set Requestbody to to cache (Request body contains CacheKey, CacheEndDate, CacheValue items)
        /// </summary> 
        /// <param name="context">Current HttpListener Context</param>
        private void SetCache(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath;

            if (context.Request.HasEntityBody)
            {
                try
                {
                    string source = GetSoureHeaderParam(context);

                    string requestBody = GetBodyRequestBodyAsString(context);
                    CacheVM cacheForTheSet = GetAsCacheRequest(requestBody);

                    if (source != null && source == this.Id) return;

                    if (cacheForTheSet != null && !String.IsNullOrWhiteSpace(cacheForTheSet.Key) && !String.IsNullOrWhiteSpace(cacheForTheSet.Key))
                    {
                        AddToMemoryCache(cacheForTheSet);

                        Task.Run(() => persister.AddToPersistentCache(cacheForTheSet));
                        Task.Run(() => nodeSynconizer.AddToNodes(cacheForTheSet, source));
                        Task.Run(() => AllocateMemory());

                        WriteStringToHttpResult("{\"result\":\"OK\"}", context);


                    }
                }
                catch (Exception)
                {
                    WriteStringToHttpResult("{\"result\":\"FAIL\"}", context, HttpStatusCode.InternalServerError);

                }
            }
        }

        private string GetSoureHeaderParam(HttpListenerContext context)
        {
            var headers = GetHeaders(context.Request);
            string source = null;
            headers.TryGetValue("source", out source);
            return source;
        }

        private void AddToMemoryCache(CacheVM cacheForTheSet)
        {
            lock (lockObj)
            {
                if (cache.ContainsKey(cacheForTheSet.Key))
                {
                    cache.Remove(cacheForTheSet.Key);
                };

                cache.Add(cacheForTheSet.Key, cacheForTheSet);
            }
        }





        /// <summary>
        /// Get Cached Item with CacheKey
        /// </summary> 
        /// <param name="context">Current HttpListener Context</param>
        private void GetCache(HttpListenerContext context)
        {
            string requestBody = GetBodyRequestBodyAsString(context);
            CacheVM cacheRequest = GetAsCacheRequest(requestBody);

            if (cacheRequest != null && !String.IsNullOrWhiteSpace(cacheRequest.Key))
            {

                CacheVM cachedContent = null;
                cache.TryGetValue(cacheRequest.Key, out cachedContent);

                if (cachedContent != null && cachedContent.ExpiresAt < DateTime.Now)
                {
                    lock (lockObj)
                    {
                        cache.Remove(cachedContent.Key);
                    }

                    Task.Run(() => AllocateMemory());
                }

                //for persistent mode
                if (cachedContent == null)
                {
                    cachedContent = persister.TryToGetFromPersistent(cacheRequest.Key);
                    if (cachedContent != null)
                    {
                        AddToMemoryCache(cachedContent);
                    }
                }

                if (cachedContent != null)
                {
                    WriteStringToHttpResult(JsonConvert.SerializeObject(cachedContent), context);
                }
            }
        }


        /// <summary>
        /// Process Http Request
        /// </summary>
        /// <param name="context"></param>
        private void Process(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath;

            switch (path)
            {
                case "/GetCache": GetCache(context); break;
                case "/SetCache": SetCache(context); break;
                case "/RemoveCache": RemoveCache(context); break;
                case "/RemoveAllCache": RemoveAllCache(context); break;
                case "/RemoveCacheStartsWith": RemoveCacheStartsWith(context); break;
                case "/Manage": Manage(context); break;
                case "/GetKeys": GetKeys(context); break;
                case "/GetAll": GetAll(context); break;
                case "/Flush": RemoveAllCache(context); break;
            }

            context.Response.OutputStream.Close();

        }

        private void GetKeys(HttpListenerContext context)
        {
            string requestBody = GetBodyRequestBodyAsString(context);
            List<string> keys = new List<string>();

            foreach (var item in cache)
            {
                keys.Add(item.Key);
            }

            WriteStringToHttpResult(JsonConvert.SerializeObject(keys), context);

            context.Response.OutputStream.Close();
        }

        private void GetAll(HttpListenerContext context)
        {
            string requestBody = GetBodyRequestBodyAsString(context);


            List<CacheVM> keys = new List<CacheVM>();
            foreach (var item in cache)
            {
                keys.Add(item.Value);
            }

            WriteStringToHttpResult(JsonConvert.SerializeObject(keys), context);

            context.Response.OutputStream.Close();
        }


        private void Manage(HttpListenerContext context)
        {

            string appLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            var directoryPath = Path.GetDirectoryName(appLocation);
            string filename = Path.Combine(directoryPath, "Manage", "index.html");

            if (File.Exists(filename))
            {
                try
                {
                    Stream input = new FileStream(filename, FileMode.Open);

                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = input.Length;

                    byte[] buffer = new byte[1024 * 16];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            context.Response.OutputStream.Close();
        }


        //private void Initialize(int port)
        //{
        //    this._port = port;
        //    this.cache = persister.GetAllCachedItems();

        //    _serverThread = new Thread(this.Listen);
        //    _serverThread.Start();
        //}

    }
}
