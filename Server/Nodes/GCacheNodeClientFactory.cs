using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.Nodes
{
   
    public class GCacheNodeClientFactory
    {
        public List<IGCacheNodeClient> Nodes
        {
            get
            {
                //string persistentType = ConfigurationManager.AppSettings["persisterType"];

                List<IGCacheNodeClient> nodes = new List<IGCacheNodeClient>();

                //var Path = ConfigurationManager.AppSettings["node1path"];
                //var Port = ConfigurationManager.AppSettings["node1port"];

                //string[] nodeSettings = ConfigurationManager.AppSettings.AllKeys
                //             .Where(key => key.StartsWith("node"))
                //             .Select(key => key)
                //             .ToArray();

                //foreach (var item in nodeSettings.Where(c=>c.EndsWith("path")))
                //{
                //    var Path = ConfigurationManager.AppSettings[item];
                //    var Port = ConfigurationManager.AppSettings[item.Replace("path","port")];
                //    var Id = ConfigurationManager.AppSettings[item.Replace("path", "id")];

                //    var server = new GCacheServer(Path, Port, Id);
                //    var cacheClient = new SwNodeClient(server);

                //    nodes.Add(cacheClient);
                //}

                return nodes;

            }
        }
    }
}
