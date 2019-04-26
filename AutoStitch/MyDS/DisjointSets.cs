using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch.MyDS
{
    class DisjointSets
    {
        List<int> boss;
        public DisjointSets(int n)
        {
            boss = new List<int>();
            for (int i = 0; i < n; i++) boss.Add(i);
        }
        public int get_gid(int i)
        {
            return boss[i] == i ? i : (boss[i] = get_gid(boss[i]));
        }
        public bool merge(int a,int b)
        {
            if ((a = get_gid(a)) == (b = get_gid(b))) return false;
            boss[a] = b;
            return true;
        }
    }
}
