using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCache.PersistentProviders
{
    public class PersistentFactory
    {
        public IPersistentProvider Persister
        {
            get
            {
                string persistentType = "file"; // ConfigurationManager.AppSettings["persisterType"];
                IPersistentProvider persister = new NoPersistentProvider();

                switch (persistentType)
                {
                    case "file": persister = new FilePersister(); break;

                    default: persister = new NoPersistentProvider(); break;
                }

                return persister;
            }
        }
    }
}
