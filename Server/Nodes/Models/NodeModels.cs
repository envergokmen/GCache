using GCache.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.Nodes
{

    public class GCacheServer
    {
        public GCacheServer(string url, string port, string id)
        {
            this.ServerUrl = url;
            this.ServerPort = port;
            this.Id = id;
        }

        public string ServerUrl { get; set; }
        public string ServerPort { get; set; }
        public string Id { get; set; }

        public string CombineRequestPath(string ServerPath = "")
        {
            if (String.IsNullOrEmpty(ServerPath))
            {
                return String.Concat(ServerUrl, ":", ServerPort);
            }
            else
            {
                return String.Concat(ServerUrl, ":", ServerPort, "/", ServerPath.TrimStart('/'));
            }
        }
    }

    public enum HttpMethod
    {
        GET = 0,
        POST = 1
    }

    public class CacheResponseViewModel
    {
        public string key { get; set; }
        public string value { get; set; }
        public DateTime? expiresAt { get; set; }

    }

    public class SwParam
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }


    public class GCacheClientRequest
    {
        public GCacheClientRequest()
        {
            RequestHeaders = new List<SwParam>();
            RequestBody = null;
        }

        public string RequestUrl { get; set; }
        public HttpMethod RequestMethod { get; set; }
        public CacheVM RequestBody { get; set; }
        public List<SwParam> RequestHeaders { get; set; }

        public string ResponseContentBody { get; set; }
        public string ResponseContentType { get; set; }
        public int ResponseStatusCode { get; set; }
        public string ResponseStatDescription { get; set; }
    }

}
