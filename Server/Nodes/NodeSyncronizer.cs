using GCache.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.Nodes
{
    public class NodeSyncronizer : INodeSyncronizer
    {
        string currentNodeId = "1"; //ConfigurationManager.AppSettings["id"];
        List<IGCacheNodeClient> nodes = new GCacheNodeClientFactory().Nodes.Where(c => c.Id != "1").ToList();

        public void DeleteFromNodes(CacheVM cacheToRemove, string fromNode)
        {
            if (fromNode == null)//check if it sub request for infinite loop
            {
                foreach (var node in this.nodes)
                {
                    node.Remove(cacheToRemove.Key, currentNodeId);

                }
            }

        }

        public void AddToNodes(CacheVM cacheForTheSet, string fromNode)
        {
            if (fromNode == null)//check if it sub request for infinite loop
            {
                foreach (var node in this.nodes.Where(c => c.Id != this.currentNodeId))
                {
                   
                        node.Set<string>(cacheForTheSet.Key, cacheForTheSet.Value, cacheForTheSet.ExpiresAt.ToDateTime(), fromNode: currentNodeId);
                   
                }
            }
        }

        public void DeleteFromNodesStartsWith(CacheVM cacheToRemove, string fromNode)
        {
            if (fromNode == null)
            {
                foreach (var node in this.nodes)
                {
                    node.RemoveKeyStartsWith(cacheToRemove.Key, fromNode);
                }
            }

        }



        public void DeleteFromAllNodes(CacheVM cacheSource, string fromNode)
        {
            if (fromNode == null)
            {
                foreach (var node in this.nodes)
                {
                    node.ClearAllCache(currentNodeId);
                }
            }
        }



    }
}
