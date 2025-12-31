using System;
using System.Collections;
using System.Collections.Generic;
using kekchpek.Auxiliary.Pools;
using NodeIndex = System.Int32; 

namespace kekchpek.Auxiliary.Collections
{
    public class SortedMultiMap<TKey, TValue> : IDictionary<TKey, TValue>
    {

        public delegate ref Node GetNodeDelegate(NodeIndex nodeIndex);

        public enum NodeColor : byte
        {
            Red = 0,
            Black = 1
        }

        public struct Node
        {
            public TKey Key;
            public List<TValue> Values;
            public NodeIndex Parent;
            public NodeIndex Left;
            public NodeIndex Right;
            public NodeColor Color;
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {

            private readonly SortedMultiMap<TKey, TValue> _map;
            private NodeIndex _currentNode;
            private int _valueIndex;
            private bool _started;

            public KeyValuePair<TKey, TValue> Current => 
                new KeyValuePair<TKey, TValue>(_getNode(_currentNode).Key, _getNode(_currentNode).Values[_valueIndex]);

            object IEnumerator.Current => Current;
            
            private GetNodeDelegate _getNode;

            public Enumerator(
                GetNodeDelegate getNode,
                SortedMultiMap<TKey, TValue> map)
            {
                _getNode = getNode;
                _map = map;
                _currentNode = -1;
                _valueIndex = -1;
                _started = false;
            }

            public bool MoveNext()
            {
                if (_map._root == -1)
                {
                    return false;
                }

                if (!_started)
                {
                    _started = true;
                    _currentNode = _map.GetMinimumNode(_map._root);
                    _valueIndex = 0;
                    return _currentNode != -1 && _getNode(_currentNode).Values.Count > 0;
                }

                _valueIndex++;
                if (_valueIndex < _getNode(_currentNode).Values.Count)
                {
                    return true;
                }

                _currentNode = _map.GetSuccessor(_currentNode);
                if (_currentNode == -1)
                {
                    return false;
                }

                _valueIndex = 0;
                return _getNode(_currentNode).Values.Count > 0;
            }

            public void Reset()
            {
                _currentNode = -1;
                _valueIndex = -1;
                _started = false;
            }

            public void Dispose()
            {
                _currentNode = -1;
            }
        }

        private const int NodesPoolInitialCapacity = 128;

        private Node[] _nodesPool = new Node[NodesPoolInitialCapacity];
        private readonly Stack<int> _freeNodes = new Stack<NodeIndex>(NodesPoolInitialCapacity);

        private readonly AdvancedListPool<TValue> _listPool = new(NodesPoolInitialCapacity, 3);
        private readonly IComparer<TKey> _comparer;
        private NodeIndex _root = -1;
        private NodeIndex _firstNodeIndex = -1;
        private int _keyCount;
        private int _totalCount;

        public KeyValuePair<TKey, TValue> First
        {
            get
            {
                if (_firstNodeIndex == -1)
                {
                    return default;
                }
                ref var node = ref GetNode(_firstNodeIndex);
                return new KeyValuePair<TKey, TValue>(node.Key, node.Values[0]);
            }
        }

        public int Capacity => _nodesPool.Length;

        public SortedMultiMap() : this(null) { }

        public SortedMultiMap(IComparer<TKey> comparer)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
            for (int i = 0; i < NodesPoolInitialCapacity; i++)
            {
                _freeNodes.Push(i);
            }
        }

        public void Add(TKey key, TValue value)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex != -1)
            {
                GetNode(nodeIndex).Values.Add(value);
                _totalCount++;
                return;
            }

            var list = _listPool.Get();
            list.Add(value);
            InsertNode(key, list);
            _keyCount++;
            _totalCount++;
        }

        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex != -1)
            {
                ref var node = ref GetNode(nodeIndex);
                int countBefore = node.Values.Count;
                node.Values.AddRange(values);
                _totalCount += node.Values.Count - countBefore;
                return;
            }

            var list = _listPool.Get();
            list.AddRange(values);
            if (list.Count > 0)
            {
                InsertNode(key, list);
                _keyCount++;
                _totalCount += list.Count;
            }
            else
            {
                _listPool.Release(list);
            }
        }

        public bool TryGetValues(TKey key, out IEnumerable<TValue> values)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex != -1)
            {
                values = GetNode(nodeIndex).Values;
                return true;
            }
            values = Array.Empty<TValue>();
            return false;
        }

        public bool Remove(TKey key, out TValue value) {
            var nodeIndex = FindNode(key);
            if (nodeIndex == -1)
            {
                value = default;
                return false;
            }

            var list = GetNode(nodeIndex).Values;
            value = list[^1];
            list.RemoveAt(list.Count - 1);
            _totalCount--;
            UpdateNodeState(key, nodeIndex, list);
            return true;
        }

        public bool Remove(TKey key, TValue value)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex == -1)
            {
                return false;
            }

            var list = GetNode(nodeIndex).Values;
            var index = FindValueIndex(list, value);
            if (index < 0)
            {
                return false;
            }

            list.RemoveAt(index);
            _totalCount--;
            UpdateNodeState(key, nodeIndex, list);
            return true;
        }

        private void UpdateNodeState(TKey key, NodeIndex nodeIndex, List<TValue> list) {
            if (list.Count == 0)
            {
                bool wasFirst = _firstNodeIndex != -1 && _comparer.Compare(key, GetNode(_firstNodeIndex).Key) == 0;
                DeleteNode(nodeIndex);
                _listPool.Release(list);
                _keyCount--;
                if (wasFirst)
                {
                    _firstNodeIndex = _root == -1 ? -1 : GetMinimumNode(_root);
                }
            }
        }

        private static int FindValueIndex(List<TValue> list, TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(list[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> AsPairs()
        {
            foreach (var node in TraverseInOrder())
            {
                foreach (var value in GetNode(node).Values)
                {
                    yield return new KeyValuePair<TKey, TValue>(GetNode(node).Key, value);
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            return FindNode(key) != -1;
        }

        public bool Remove(TKey key)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex == -1)
            {
                return false;
            }
            bool wasFirst = _firstNodeIndex != -1 && _comparer.Compare(key, GetNode(_firstNodeIndex).Key) == 0;
            ref var node = ref GetNode(nodeIndex);
            _totalCount -= node.Values.Count;
            _listPool.Release(node.Values);
            DeleteNode(nodeIndex);
            _keyCount--;
            if (wasFirst)
            {
                _firstNodeIndex = _root == -1 ? -1 : GetMinimumNode(_root);
            }
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var nodeIndex = FindNode(key);
            if (nodeIndex != -1)
            {
                value = GetNode(nodeIndex).Values[0];
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetClosestLargerKey(TKey key, out TKey outcomeKey)
        {
            var current = _root;
            NodeIndex candidateIndex = -1;
            
            while (current != -1)
            {
                ref var node = ref GetNode(current);
                int cmp = _comparer.Compare(key, node.Key);
                
                if (cmp == 0)
                {
                    outcomeKey = node.Key;
                    return true;
                }
                
                if (cmp < 0)
                {
                    candidateIndex = current;
                    current = node.Left;
                }
                else
                {
                    current = node.Right;
                }
            }
            
            if (candidateIndex != -1)
            {
                outcomeKey = GetNode(candidateIndex).Key;
                return true;
            }
            
            outcomeKey = default;
            return false;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            ClearSubtree(_root);
            _root = -1;
            _firstNodeIndex = -1;
            _keyCount = 0;
            _totalCount = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            var nodeIndex = FindNode(item.Key);
            if (nodeIndex != -1)
            {
                return GetNode(nodeIndex).Values.Contains(item.Value);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var node in TraverseInOrder())
            {
                foreach (var value in GetNode(node).Values)
                {
                    array[arrayIndex++] = new KeyValuePair<TKey, TValue>(GetNode(node).Key, value);
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key, item.Value);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(GetNode, this);
        }

        public IReadOnlyCollection<TKey> Keys {
            get {
                var list = new List<TKey>();
                foreach(var kvp in this) {
                    list.Add(kvp.Key);
                }
                return list;
            }
        }
        public int CountKeys => _keyCount;

        public ICollection<TValue> Values {
            get {
                var list = new List<TValue>();
                foreach(var kvp in this) {
                    list.Add(kvp.Value);
                }
                return list;
            }
        }

        public int Count => _totalCount;

        public bool IsReadOnly => false;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys {
            get {
                var list = new List<TKey>();
                foreach(var kvp in this) {
                    list.Add(kvp.Key);
                }
                return list;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                var nodeIndex = FindNode(key);
                if (nodeIndex == -1)
                {
                    throw new KeyNotFoundException();
                }
                return GetNode(nodeIndex).Values[0];
            }
            set
            {
                var nodeIndex = FindNode(key);
                if (nodeIndex == -1)
                {
                    throw new KeyNotFoundException();
                }
                ref var node = ref GetNode(nodeIndex);
                if (node.Values.Count == 0)
                {
                    node.Values.Add(value);
                    return;
                }
                else if (node.Values.Count == 1)
                {
                    node.Values[0] = value;
                    return;
                }
                else
                {
                    throw new InvalidOperationException("Cannot set value of a node with multiple values");
                }
            }
        }

        private NodeIndex FindNode(TKey key)
        {
            var current = _root;
            while (current != -1)
            {
                ref var node = ref GetNode(current);
                int cmp = _comparer.Compare(key, node.Key);
                if (cmp == 0)
                {
                    return current;
                }
                current = cmp < 0 ? node.Left : node.Right;
            }
            return -1;
        }

        private void InsertNode(TKey key, List<TValue> values)
        {
            var newNodeIndex = GetFreeNodeIndex();
            ref var newNode = ref GetNode(newNodeIndex);
            newNode.Key = key;
            newNode.Values = values;

            if (_root == -1)
            {
                _root = newNodeIndex;
                _firstNodeIndex = newNodeIndex;
                newNode.Color = NodeColor.Black;
                return;
            }
            newNode.Color = NodeColor.Red;

            if (_comparer.Compare(key, GetNode(_firstNodeIndex).Key) < 0)
            {
                _firstNodeIndex = newNodeIndex;
            }

            var current = _root;
            NodeIndex parent = -1;
            int cmp = 0;

            while (current != -1)
            {
                parent = current;
                cmp = _comparer.Compare(key, GetNode(current).Key);
                current = cmp < 0 ? GetNode(current).Left : GetNode(current).Right;
            }

            newNode.Parent = parent;
            if (cmp < 0)
            {
                GetNode(parent).Left = newNodeIndex;
            }
            else
            {
                GetNode(parent).Right = newNodeIndex;
            }

            InsertFixup(newNodeIndex);
        }

        private void InsertFixup(NodeIndex nodeIndex)
        {
            while (nodeIndex != _root)
            {
                var parentIndex = GetNode(nodeIndex).Parent;
                if (parentIndex == -1 || GetNode(parentIndex).Color == NodeColor.Black)
                {
                    break;
                }

                var grandparentIndex = GetNode(parentIndex).Parent;
                if (grandparentIndex == -1)
                {
                    break;
                }

                if (parentIndex == GetNode(grandparentIndex).Left)
                {
                    var uncleIndex = GetNode(grandparentIndex).Right;
                    if (uncleIndex != -1 && GetNode(uncleIndex).Color == NodeColor.Red)
                    {
                        GetNode(parentIndex).Color = NodeColor.Black;
                        GetNode(uncleIndex).Color = NodeColor.Black;
                        GetNode(grandparentIndex).Color = NodeColor.Red;
                        nodeIndex = grandparentIndex;
                    }
                    else
                    {
                        if (nodeIndex == GetNode(parentIndex).Right)
                        {
                            nodeIndex = parentIndex;
                            RotateLeft(nodeIndex);
                            parentIndex = GetNode(nodeIndex).Parent;
                            grandparentIndex = GetNode(parentIndex).Parent;
                        }
                        GetNode(parentIndex).Color = NodeColor.Black;
                        GetNode(grandparentIndex).Color = NodeColor.Red;
                        RotateRight(grandparentIndex);
                    }
                }
                else
                {
                    var uncleIndex = GetNode(grandparentIndex).Left;
                    if (uncleIndex != -1 && GetNode(uncleIndex).Color == NodeColor.Red)
                    {
                        GetNode(parentIndex).Color = NodeColor.Black;
                        GetNode(uncleIndex).Color = NodeColor.Black;
                        GetNode(grandparentIndex).Color = NodeColor.Red;
                        nodeIndex = grandparentIndex;
                    }
                    else
                    {
                        if (nodeIndex == GetNode(parentIndex).Left)
                        {
                            nodeIndex = parentIndex;
                            RotateRight(nodeIndex);
                            parentIndex = GetNode(nodeIndex).Parent;
                            grandparentIndex = GetNode(parentIndex).Parent;
                        }
                        GetNode(parentIndex).Color = NodeColor.Black;
                        GetNode(grandparentIndex).Color = NodeColor.Red;
                        RotateLeft(grandparentIndex);
                    }
                }
            }
            GetNode(_root).Color = NodeColor.Black;
        }

        private void DeleteNode(NodeIndex nodeIndex)
        {
            ref var node = ref GetNode(nodeIndex);
            NodeIndex replacement;
            NodeIndex fixupNodeIndex;
            NodeIndex fixupParentIndex;

            if (node.Left == -1 || node.Right == -1)
            {
                replacement = nodeIndex;
            }
            else
            {
                replacement = GetSuccessor(nodeIndex);
            }

            ref var replacementNode = ref GetNode(replacement);
            fixupNodeIndex = replacementNode.Left != -1 ? replacementNode.Left : replacementNode.Right;
            fixupParentIndex = replacementNode.Parent;
            var replacementColor = replacementNode.Color;

            if (fixupNodeIndex != -1)
            {
                GetNode(fixupNodeIndex).Parent = replacementNode.Parent;
            }

            if (replacementNode.Parent == -1)
            {
                _root = fixupNodeIndex;
            }
            else if (replacement == GetNode(replacementNode.Parent).Left)
            {
                GetNode(replacementNode.Parent).Left = fixupNodeIndex;
            }
            else
            {
                GetNode(replacementNode.Parent).Right = fixupNodeIndex;
            }

            if (replacement != nodeIndex)
            {
                node.Key = GetNode(replacement).Key;
                node.Values = GetNode(replacement).Values;
            }

            ReleaseNode(replacement);

            if (replacementColor == NodeColor.Black)
            {
                DeleteFixup(fixupNodeIndex, fixupParentIndex);
            }
        }

        private void DeleteFixup(NodeIndex nodeIndex, NodeIndex parentIndex)
        {
            while (nodeIndex != _root && (nodeIndex == -1 || GetNode(nodeIndex).Color == NodeColor.Black))
            {
                if (parentIndex == -1)
                {
                    break;
                }
                ref var parentNode = ref GetNode(parentIndex);

                if (nodeIndex == parentNode.Left)
                {
                    var sibling = parentNode.Right;
                    if (sibling != -1 && GetNode(sibling).Color == NodeColor.Red)
                    {
                        GetNode(sibling).Color = NodeColor.Black;
                        parentNode.Color = NodeColor.Red;
                        RotateLeft(parentIndex);
                        sibling = parentNode.Right;
                    }

                    if (sibling == -1 ||
                        ((GetNode(sibling).Left == -1 || GetNode(GetNode(sibling).Left).Color == NodeColor.Black) &&
                         (GetNode(sibling).Right == -1 || GetNode(GetNode(sibling).Right).Color == NodeColor.Black)))
                    {
                        if (sibling != -1)
                        {
                            GetNode(sibling).Color = NodeColor.Red;
                        }
                        nodeIndex = parentIndex;
                        parentIndex = GetNode(nodeIndex).Parent;
                    }
                    else
                    {
                        if (GetNode(sibling).Right == -1 || GetNode(GetNode(sibling).Right).Color == NodeColor.Black)
                        {
                            if (GetNode(sibling).Left != -1)
                            {
                                GetNode(GetNode(sibling).Left).Color = NodeColor.Black;
                            }
                            GetNode(sibling).Color = NodeColor.Red;
                            RotateRight(sibling);
                            sibling = GetNode(parentIndex).Right != -1 ? GetNode(parentIndex).Right : -1;
                        }

                        GetNode(sibling).Color = GetNode(parentIndex).Color;
                        GetNode(parentIndex).Color = NodeColor.Black;
                        if (GetNode(sibling).Right != -1)
                        {
                            GetNode(GetNode(sibling).Right).Color = NodeColor.Black;
                        }
                        RotateLeft(parentIndex);
                        nodeIndex = _root;
                        break;
                    }
                }
                else
                {
                    var sibling = parentIndex != -1 ? GetNode(parentIndex).Left : -1;
                    if (sibling != -1 && GetNode(sibling).Color == NodeColor.Red)
                    {
                        GetNode(sibling).Color = NodeColor.Black;
                        GetNode(parentIndex).Color = NodeColor.Red;
                        RotateRight(parentIndex);
                        sibling = GetNode(parentIndex).Left != -1 ? GetNode(parentIndex).Left : -1;
                    }

                    if (sibling == -1 ||
                        ((GetNode(sibling).Right == -1 || GetNode(GetNode(sibling).Right).Color == NodeColor.Black) &&
                         (GetNode(sibling).Left == -1 || GetNode(GetNode(sibling).Left).Color == NodeColor.Black)))
                    {
                        if (sibling != -1)
                        {
                            GetNode(sibling).Color = NodeColor.Red;
                        }
                        nodeIndex = parentIndex;
                        parentIndex = GetNode(nodeIndex).Parent;
                    }
                    else
                    {
                        if (sibling != -1 && (GetNode(sibling).Left == -1 || GetNode(GetNode(sibling).Left).Color == NodeColor.Black))
                        {
                            if (sibling != -1 && GetNode(sibling).Right != -1)
                            {
                                GetNode(GetNode(sibling).Right).Color = NodeColor.Black;
                            }
                            GetNode(sibling).Color = NodeColor.Red;
                            RotateLeft(sibling);
                            sibling = GetNode(parentIndex).Left != -1 ? GetNode(parentIndex).Left : -1;
                        }

                        GetNode(sibling).Color = GetNode(parentIndex).Color;
                        GetNode(parentIndex).Color = NodeColor.Black;
                        if (GetNode(sibling).Left != -1)
                        {
                            GetNode(GetNode(sibling).Left).Color = NodeColor.Black;
                        }
                        RotateRight(parentIndex);
                        nodeIndex = _root;
                        break;
                    }
                }
            }

            if (nodeIndex != -1)
            {
                GetNode(nodeIndex).Color = NodeColor.Black;
            }
        }

        private void RotateLeft(NodeIndex nodeIndex)
        {
            ref var node = ref GetNode(nodeIndex);
            var rightChildIndex = node.Right;
            ref var rightChild = ref GetNode(rightChildIndex);
            
            node.Right = rightChild.Left;
            if (rightChild.Left != -1)
            {
                GetNode(rightChild.Left).Parent = nodeIndex;
            }

            rightChild.Parent = node.Parent;
            if (node.Parent == -1)
            {
                _root = rightChildIndex;
            }
            else if (nodeIndex == GetNode(node.Parent).Left)
            {
                GetNode(node.Parent).Left = rightChildIndex;
            }
            else
            {
                GetNode(node.Parent).Right = rightChildIndex;
            }

            rightChild.Left = nodeIndex;
            node.Parent = rightChildIndex;
        }

        private void RotateRight(NodeIndex nodeIndex)
        {
            ref var node = ref GetNode(nodeIndex);
            var leftChildIndex = node.Left;
            ref var leftChild = ref GetNode(leftChildIndex);
            
            node.Left = leftChild.Right;
            if (leftChild.Right != -1)
            {
                GetNode(leftChild.Right).Parent = nodeIndex;
            }

            leftChild.Parent = node.Parent;
            if (node.Parent == -1)
            {
                _root = leftChildIndex;
            }
            else if (nodeIndex == GetNode(node.Parent).Right)
            {
                GetNode(node.Parent).Right = leftChildIndex;
            }
            else
            {
                GetNode(node.Parent).Left = leftChildIndex;
            }

            leftChild.Right = nodeIndex;
            node.Parent = leftChildIndex;
        }

        private NodeIndex GetMinimumNode(NodeIndex nodeIndex)
        {
            ref var node = ref GetNode(nodeIndex);
            while (node.Left != -1)
            {
                nodeIndex = node.Left;
                node = ref GetNode(node.Left);
            }
            return nodeIndex;
        }

        private NodeIndex GetSuccessor(NodeIndex nodeIndex)
        {
            ref var node = ref GetNode(nodeIndex);
            if (node.Right != -1)
            {
                return GetMinimumNode(node.Right);
            }

            var parent = node.Parent;
            while (parent != -1 && nodeIndex == GetNode(parent).Right)
            {
                nodeIndex = parent;
                parent = GetNode(nodeIndex).Parent;
            }
            return parent;
        }

        private IEnumerable<NodeIndex> TraverseInOrder()
        {
            if (_root == -1)
            {
                yield break;
            }

            var current = GetMinimumNode(_root);
            while (current != -1)
            {
                yield return current;
                current = GetSuccessor(current);
            }
        }

        private void ClearSubtree(NodeIndex nodeIndex)
        {
            if (nodeIndex == -1)
            {
                return;
            }

            ref var node = ref GetNode(nodeIndex);
            ClearSubtree(node.Left);
            ClearSubtree(node.Right);
            _listPool.Release(node.Values);
            ReleaseNode(nodeIndex);
        }

        private int GetFreeNodeIndex()
        {
            if (_freeNodes.Count == 0)
            {
                var newPool = new Node[_nodesPool.Length * 2];
                for (int i = _nodesPool.Length; i < newPool.Length; i++)
                {
                    _freeNodes.Push(i);
                }
                _nodesPool.CopyTo(newPool, 0);
                _nodesPool = newPool;
            }
            var index = _freeNodes.Pop();
            ref var newNode = ref GetNode(index);
            newNode.Key = default;
            newNode.Values = null;
            newNode.Parent = -1;
            newNode.Left = -1;
            newNode.Right = -1;
            newNode.Color = NodeColor.Red;
            return index;
        }

        private ref Node GetNode(NodeIndex index)
        {
            return ref _nodesPool[index];
        }

        private void ReleaseNode(NodeIndex index)
        {
            _freeNodes.Push(index);
        }
    }
}
