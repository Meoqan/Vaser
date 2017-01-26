using System;
using System.Collections.Generic;

namespace Vaser
{
    /// <summary>
    /// This class is a simple ID pool holder.
    /// </summary>
    public class IDPool
    {
        private object threadlock = new object();
        private uint _MaxIDs = 0;

        private List<uint> freeIDList = new List<uint>();
        private List<uint> usedIDList = new List<uint>();


        /// <summary>
        /// Creates a new pool of IDs
        /// </summary>
        /// <param name="MaxIDs"></param>
        public IDPool(uint MaxIDs)
        {
            //if (MaxIDs < 0) throw new Exception("MaxIDs must be an positive integer");
            lock (threadlock)
            {
                _MaxIDs = MaxIDs;
                for (uint x = 1; x <= _MaxIDs; x++)
                {
                    freeIDList.Add(x);
                }
            }
        }

        /// <summary>
        /// Returns a free ID from the pool
        /// </summary>
        /// <returns>ID</returns>
        public uint GetFreeID()
        {
            lock (threadlock)
            {
                if (freeIDList.Count == 0) throw new Exception("free pool is empty");

                uint id = freeIDList[0];
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
        public uint RegisterFreeID(uint id)
        {
            //if (id < 0) throw new Exception("ID must be an positive integer");

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
        public void DisposeID(uint id)
        {
            //if (id < 0) throw new Exception("ID must be an positive integer");

            lock (threadlock)
            {
                if (usedIDList.Contains(id)) usedIDList.Remove(id);
                freeIDList.Add(id);
            }
        }

        /// <summary>
        /// How many IDs are available in the free pool.
        /// </summary>
        /// <returns>Free IDs</returns>
        public int CountFreeIDs()
        {
            lock (threadlock)
            {
                return freeIDList.Count;
            }
        }

        /// <summary>
        /// Adds an specific amount to the free ID pool.
        /// </summary>
        /// <param name="Quantity">quantity of new IDs</param>
        /// <returns>new maximum IDs</returns>
        public uint AddFreeIDs(uint Quantity)
        {
            //if (Quantity < 0) throw new Exception("Quantity must be an positive integer");
            lock (threadlock)
            {
                uint _OldMaxIDs = _MaxIDs;
                _MaxIDs += Quantity;
                for (uint x = _OldMaxIDs + 1; x <= Quantity; x++)
                {
                    freeIDList.Add(x);
                }
                return _MaxIDs;
            }
        }
    }
}
