using System;
using System.Collections.Generic;

namespace SceneFlowTools.Runtime
{
    public class UnionFind<T>
    {
        private readonly Dictionary<T, T> parent = new();
        private readonly Dictionary<T, int> rank = new();

        /// <summary>

        /// </summary>
        public void Add(T item)
        {
            if (!parent.ContainsKey(item))
            {
                parent[item] = item;
                rank[item] = 0;
            }
        }

        /// <summary>

        /// </summary>
        public T Find(T item)
        {
            if (!parent.ContainsKey(item))
                throw new ArgumentException("Item not found in UnionFind.");

            if (!EqualityComparer<T>.Default.Equals(parent[item], item))
            {
                parent[item] = Find(parent[item]);
            }
            return parent[item];
        }

        /// <summary>

        /// </summary>
        public void Union(T a, T b)
        {
            Add(a);
            Add(b);

            T rootA = Find(a);
            T rootB = Find(b);

            if (EqualityComparer<T>.Default.Equals(rootA, rootB))
                return;

            // Union by rank
            if (rank[rootA] < rank[rootB])
            {
                parent[rootA] = rootB;
            }
            else if (rank[rootA] > rank[rootB])
            {
                parent[rootB] = rootA;
            }
            else
            {
                parent[rootB] = rootA;
                rank[rootA]++;
            }
        }

        /// <summary>

        /// </summary>
        public bool Connected(T a, T b)
        {
            if (!parent.ContainsKey(a) || !parent.ContainsKey(b))
                return false;

            return EqualityComparer<T>.Default.Equals(Find(a), Find(b));
        }
        
        public bool Contains(T item)
        {
            return parent.ContainsKey(item);
        }
    }
}