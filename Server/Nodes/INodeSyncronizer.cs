using GCache.ViewModels;

namespace GCache.Nodes
{
    public interface INodeSyncronizer
    {
        void AddToNodes(CacheVM cacheForTheSet, string fromNode);
        void DeleteFromAllNodes(CacheVM cacheSource, string fromNode);
        void DeleteFromNodesStartsWith(CacheVM cacheToRemove, string fromNode);
        void DeleteFromNodes(CacheVM cacheToRemove, string fromNode);
    }
}