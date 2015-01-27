using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    public class IDPool
    {
        object threadlock = new object();
        int maxIDs = 0;

        List<int> freeIDList = new List<int>();
        List<int> usedIDList = new List<int>();

        public IDPool(int max_IDs)
        {
            maxIDs = max_IDs;
            for (int x = 1; x <= maxIDs; x++)
            {
                freeIDList.Add(x);
            }
        }

        public int get_free_ID()
        {
            lock(threadlock)
            {
                int id = freeIDList[0];
                freeIDList.Remove(id);
                usedIDList.Add(id);
                return id;
            }
            
        }

        public int register_free_ID(int id)
        {
            lock (threadlock)
            {
                freeIDList.Remove(id);
                usedIDList.Add(id);
                return id;
            }

        }

        public void dispose_ID(int id)
        {
            lock (threadlock)
            {
                usedIDList.Remove(id);
                freeIDList.Add(id);
            }
        }
    }
}
