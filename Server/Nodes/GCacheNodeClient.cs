using Newtonsoft.Json;
using GCache.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Google.Protobuf.WellKnownTypes;

namespace GCache.Nodes
{

    public class GCacheNodeClient : IGCacheNodeClient
    {
        public GCacheNodeClient(GCacheServer serverSettings)
        {
            this.SwServer = serverSettings;
        }

        public static string BltCachePrefix = "GC"; // ConfigurationManager.AppSettings["CacheKeyPrefix"];

        public string Id => SwServer != null ? SwServer.Id : "";

        public GCacheServer SwServer { get; set; }

        private GCacheClientRequest DoHttpRequest(GCacheClientRequest swRequest)
        {
            try
            {
                string url = SwServer.CombineRequestPath(swRequest.RequestUrl);

                if (!url.Contains("http://")) url = "http://" + url;

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                httpWebRequest.Method = swRequest.RequestMethod.ToString();
                httpWebRequest.KeepAlive = true;
                httpWebRequest.Credentials = CredentialCache.DefaultCredentials;
                httpWebRequest.AllowAutoRedirect = true;

                if(swRequest.RequestHeaders!=null && swRequest.RequestHeaders.Any())
                {
                    foreach (var item in swRequest.RequestHeaders)
                    {
                        httpWebRequest.Headers.Add(item.Name, item.Value);
                    }
                }

                if (swRequest.RequestMethod == HttpMethod.POST)
                {

                    httpWebRequest.ContentType = "application/json";
                    Stream memStream = new System.IO.MemoryStream();
                    string allParams = JsonConvert.SerializeObject(swRequest.RequestBody);

                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(allParams);
                    memStream.Write(formitembytes, 0, formitembytes.Length);

                    Stream requestStream = httpWebRequest.GetRequestStream();

                    memStream.Position = 0;
                    byte[] tempBuffer = new byte[memStream.Length];
                    memStream.Read(tempBuffer, 0, tempBuffer.Length);
                    memStream.Close();
                    requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                    requestStream.Close();

                }

                HttpWebResponse webResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                Stream responseStream = webResponse.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                swRequest.ResponseContentBody = reader.ReadToEnd();
                swRequest.ResponseContentType = webResponse.ContentType;
                swRequest.ResponseStatusCode = (int)webResponse.StatusCode;
                swRequest.ResponseStatDescription = webResponse.StatusDescription;

                if (webResponse.Headers != null && webResponse.Headers.Count > 0)
                {
                    foreach (var key in webResponse.Headers.AllKeys)
                    {

                    }
                }


                webResponse.Close();
                httpWebRequest = null;
                webResponse = null;

                return swRequest;

            }
            catch (WebException ex)
            {

                if (ex.Response != null)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        swRequest.ResponseContentBody = reader.ReadToEnd();
                        swRequest.ResponseStatusCode = Convert.ToInt32(((HttpWebResponse)ex.Response).StatusCode);
                        swRequest.ResponseStatDescription = ((HttpWebResponse)ex.Response).StatusDescription;
                    }
                }
                else
                {
                    swRequest.ResponseStatusCode = 500;
                    swRequest.ResponseStatDescription = ex.Message;
                }

            }
            catch (Exception ex)
            {
                swRequest.ResponseContentBody = String.Concat("Unknown Error on request", ex.Message);
                swRequest.ResponseStatusCode = 500;
                swRequest.ResponseStatDescription = swRequest.ResponseContentBody;
            }

            return swRequest;
        }


        public GCacheClientRequest GetRequestModel(string CacheKey, string CacheValue, DateTime? CacheEndDate = null, string fromNode = null)
        {
            GCacheClientRequest request = new GCacheClientRequest();

            string cacheDate = (CacheEndDate != null) ? Convert.ToDateTime(CacheEndDate).ToString() : DateTime.Now.AddYears(1).ToString();
            request.RequestMethod = HttpMethod.POST;
            request.RequestBody = new CacheVM { Key = CacheKey, Value = CacheValue, ExpiresAt = Timestamp.FromDateTime(CacheEndDate.Value)};

            if (fromNode != null)
            {
                request.RequestHeaders.Add(new SwParam { Name = "source", Value = fromNode });
            }

            return request;
        }

        public void Set<T>(string key, T value, string[] fileDependencies = null, string fromNode = null)
        {
            GCacheClientRequest request = GetRequestModel(key, value.ToString(), null, fromNode);
            DoHttpRequest(request);
        }

        public void Set<T>(string key, T value, DateTime expireDate, string sourceHeader=null)
        {
            GCacheClientRequest request = GetRequestModel(key, value.ToString(), expireDate, sourceHeader);
            request.RequestUrl = "/SetCache";
            DoHttpRequest(request);

        }

        public void Set<T>(string key, T value, string sourceHeader = null)
        {
            GCacheClientRequest request = GetRequestModel(key, value.ToString(), null, sourceHeader);
            request.RequestUrl = "/SetCache";
            DoHttpRequest(request);
        }

        public T Get<T>(string key) where T : class
        {

            GCacheClientRequest request = GetRequestModel(key, "");
            request.RequestUrl = "/GetCache";

            var response = DoHttpRequest(request);

            if (response != null && !String.IsNullOrWhiteSpace(response.ResponseContentBody))
            {

                var cacheResult = JsonConvert.DeserializeObject<CacheResponseViewModel>(response.ResponseContentBody);

                if (cacheResult != null && !String.IsNullOrWhiteSpace(cacheResult.value))
                {
                    return JsonConvert.DeserializeObject<T>(cacheResult.value);

                }

                return null;

            }
            else
            {
                return null;
            }
        }

        public void Remove(string key, string sourceHeader = null)
        {
            GCacheClientRequest request = GetRequestModel(key, "", null, sourceHeader);
            request.RequestUrl = "/RemoveCache";
            var response = DoHttpRequest(request);

        }

        public void RemoveKeyStartsWith(string StartWith, string sourceHeader = null)
        {
            GCacheClientRequest request = GetRequestModel(StartWith, "", null, sourceHeader);
            request.RequestUrl = "/RemoveCacheKeyStarsWith";
            DoHttpRequest(request);
        }

        public bool Exists(string key)
        {
            GCacheClientRequest request = GetRequestModel(key, "");
            request.RequestUrl = "/GetCache";
            var response = DoHttpRequest(request);

            if (response != null && !String.IsNullOrWhiteSpace(response.ResponseContentBody))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Set<T>(string key, T value, DateTime expireDate, string[] fileDependencies = null, string sourceHeader = null)
        {
            Set(key, value, expireDate, sourceHeader);
        }

        public void ClearAllCache(string sourceHeader = null)
        {
            GCacheClientRequest request = GetRequestModel("", "", null, sourceHeader);
            request.RequestUrl = "/Flush";
            DoHttpRequest(request);
        }

        public List<string> GetAllKeys()
        {
            GCacheClientRequest request = new GCacheClientRequest();
            request.RequestUrl = "/GetKeys";

            var response = DoHttpRequest(request);

            if (response != null && !String.IsNullOrWhiteSpace(response.ResponseContentBody))
            {
                return JsonConvert.DeserializeObject<List<string>>(response.ResponseContentBody);

            }
            else
            {
                return null;
            }
        }



    }

}