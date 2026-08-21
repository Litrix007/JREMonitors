using System;
using System.Collections.Generic;

namespace JREMonitors.Core.Managers
{
    public class ExecutionGraph<T> where T : class
    {
        private readonly Dictionary<T, HashSet<T>> _adjacencyList = new Dictionary<T, HashSet<T>>();
        private readonly HashSet<T> _nodes = new HashSet<T>();

        public void Add(T item, IEnumerable<T> before = null, IEnumerable<T> after = null)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            EnsureNode(item);

            if (before != null)
                foreach (var b in before)
                {
                    if (b == null) continue;
                    EnsureNode(b);
                    _adjacencyList[item].Add(b);
                }

            if (after != null)
                foreach (var a in after)
                {
                    if (a == null) continue;
                    EnsureNode(a);
                    _adjacencyList[a].Add(item);
                }
        }

        private void EnsureNode(T node)
        {
            _nodes.Add(node);
            if (!_adjacencyList.ContainsKey(node)) _adjacencyList[node] = new HashSet<T>();
        }

        public List<T> Solve()
        {
            var inDegree = new Dictionary<T, int>();
            foreach (var node in _nodes) inDegree[node] = 0;

            foreach (var pair in _adjacencyList)
            foreach (var neighbor in pair.Value)
                inDegree[neighbor]++;

            var queue = new Queue<T>();
            foreach (var node in _nodes)
                if (inDegree[node] == 0)
                    queue.Enqueue(node);

            var result = new List<T>();
            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                result.Add(u);

                if (_adjacencyList.TryGetValue(u, out var neighbors))
                    foreach (var v in neighbors)
                    {
                        inDegree[v]--;
                        if (inDegree[v] == 0) queue.Enqueue(v);
                    }
            }

            if (result.Count < _nodes.Count)
                throw new InvalidOperationException(
                    $"Circular dependency detected in the {typeof(ExecutionGraph<T>).Name}.");

            return result;
        }

        public void Clear()
        {
            _adjacencyList.Clear();
            _nodes.Clear();
        }
    }
}