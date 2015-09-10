using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser
{
    public class IDPool
    {
        private object threadlock = new object();
        private int _MaxIDs = 0;

        private List<int> freeIDList = new List<int>();
        private List<int> usedIDList = new List<int>();


        /// <summary>
        /// Creates a new pool of IDs
        /// </summary>
        /// <param name="MaxIDs"></param>
        public IDPool(int MaxIDs)
        {
            if (MaxIDs < 0) throw new Exception("MaxIDs must be positive");
            lock (threadlock)
            {
                _MaxIDs = MaxIDs;
                for (int x = 1; x <= _MaxIDs; x++)
                {
                    freeIDList.Add(x);
                }
            }
        }

        /// <summary>
        /// Returns a free ID from the pool
        /// </summary>
        /// <returns>ID</returns>
        public int GetFreeID()
        {
            lock(threadlock)
            {
                if(freeIDList.Count == 0) throw new Exception("free pool is empty");

                int id = freeIDList[0];
                freeIDList.Remove(id);
                usedIDList.Add(id);
                return id;
            }
            
        }

        /// <summary>
        /// Removes the ID from the pool and marks as used
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>ID</returns>
        public int RegisterFreeID(int id)
        {
            if (id < 0) throw new Exception("ID must be positive");
            
            lock (threadlock)
            {
                if (freeIDList.Contains(id)) freeIDList.Remove(id);
                usedIDList.Add(id);
                return id;
            }

        }

        /// <summary>
        /// Frees the used id back to the pool
        /// </summary>
        /// <param name="id">ID</param>
        public void DisposeID(int id)
        {
            if (id < 0) throw new Exception("ID must be positive");

            lock (threadlock)
            {
                if(usedIDList.Contains(id)) usedIDList.Remove(id);
                freeIDList.Add(id);
            }
        }
    }
}
