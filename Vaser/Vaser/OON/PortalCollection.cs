using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaser.OON
{
    class PortalCollection
    {
        private object _ListLock = new object();
        private List<Portal> PortalList = new List<Portal>();




        public void Add(Portal portal)
        {
            lock(_ListLock)
            {
                PortalList.Add(portal);
            }
        }
    }
}
