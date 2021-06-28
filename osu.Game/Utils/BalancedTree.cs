using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace osu.Game.Utils {

    /// <summary>
    /// Self-Balancing (Weight Balanced) Tree
    /// </summary>
    /// This implementation is optimized for frequent lookup/traversal and infrequent insertion/deletion.
    public sealed class BalancedTree<T> : ICollection<T>, IEnumerable<T>, ICollection /*, IDeserializationCallback, ISerializable */ {

        private sealed class Node : IEnumerable<T> {
            public int NumDescendants = 0;
            public Node Left;
            public Node Right;
            public T Value;

            public Node(T value)
            {
                Value = value;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (Left != null)
                {
                    foreach(var v in Left)
                        yield return v;
                }

                yield return Value;

                if (Right != null)
                {
                    foreach (var v in Right)
                        yield return v;
                }
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                string result = "";
                result += "([";
                result += NumDescendants.ToString();
                result += "] ";
                result += Value.ToString();
                if (Left != null)
                {
                    result += " left=";
                    result += Left.ToString();
                }
                if (Right != null)
                {
                    result += " right=";
                    result += Right.ToString();
                }
                result += ")";
                return result;
            }

            /// <summary>
            /// Manually count the number of descendants of this Node. Used for validation.
            /// </summary>
            public int CountDescendants()
            {
                int sum = 0;
                if (Left != null)
                    sum += Left.CountDescendants() + 1;
                if (Right != null)
                    sum += Right.CountDescendants() + 1;
                return sum;
            }

            /// <summary>
            /// Validate that this Node and all it's subnodes are in a consistent state.
            /// </summary>
            public bool Validate()
            {
                if (!(Left?.Validate() ?? true))
                    return false;
                if (!(Right?.Validate() ?? true))
                    return false;
                return NumDescendants == CountDescendants();
            }

            /// <summary>
            /// Validate that this Node and all it's subnodes are sorted.
            /// </summary>
            public bool ValidateSorted(IComparer<T> comparer)
            {
                if (Left != null)
                {
                    if (comparer.Compare(Left.Value, Value) >= 0 || !Left.ValidateSorted(comparer))
                        return false;
                }
                if (Right != null)
                {
                    if (comparer.Compare(Right.Value, Value) <= 0 || !Right.ValidateSorted(comparer))
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// A cached ancestor of some other node
        /// </summary>
        private readonly struct AncestorNode {
            /// <summary>
            /// The node itself
            /// </summary>
            public readonly Node Node;
            /// <summary>
            /// The parent of this node, or null if this is the Root node
            /// </summary>
            public readonly Node Parent;
            /// <summary>
            /// The position of the node in the parent. -1 for Left, +1 for Right.
            /// </summary>
            public readonly int Ord;

            public AncestorNode(int ord, Node node, Node parent)
            {
                Ord = ord;
                Node = node;
                Parent = parent;
            }
        }

        /// <summary>
        /// Rotate the Left node of the given node to the Right, and return its new value
        /// </summary>
        private static Node RotateRight(Node OldParent)
        {
            Node NewParent = OldParent.Left;
            Node NewChild = NewParent.Right;
            OldParent.Left = NewChild;
            NewParent.Right = OldParent;
            return NewParent;
        }
        /// <summary>
        /// Rotate the Right node of the given node to the Left, and return its new value
        /// </summary>
        private static Node RotateLeft(Node OldParent)
        {
            Node NewParent = OldParent.Right;
            Node NewChild = NewParent.Left;
            OldParent.Right = NewChild;
            NewParent.Left = OldParent;
            return NewParent;
        }

        private Node Root;
        public IComparer<T> Comparer { get; private set; }

        // ICollection variables

        public int Count { get; private set; }
        public bool IsReadOnly { get { return true; } }
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return null; } }

        public BalancedTree() : this(Comparer<T>.Default) {}
        public BalancedTree(IComparer<T> comparer)
        {
            Comparer = comparer;
        }
        public BalancedTree(IEnumerable<T> elements) : this(elements, Comparer<T>.Default) {}
        public BalancedTree(IEnumerable<T> elements, IComparer<T> comparer)
        {
            Comparer = comparer;
            List<AncestorNode> ancestors = new List<AncestorNode>();
            foreach (var element in elements)
                AddWith(element, ancestors);
        }

        // ICollection methods

        public bool Add(T value)
        {
            return AddWith(value, new List<AncestorNode>());
        }
        void ICollection<T>.Add(T value)
        {
            Add(value);
        }
        public void Clear()
        {
            Root = null;
        }
        public bool Contains(T value)
        {
            return FindNode(value) != null;
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo((Array)array, arrayIndex);
        }
        public void CopyTo(Array array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (array.Rank != 1) throw new ArgumentException();
            if (array.Length < arrayIndex + Count) throw new ArgumentException();
            foreach (var item in this)
                array.SetValue(item, arrayIndex++);
        }
        public bool Remove(T value)
        {
            return RemoveWith(value, new List<AncestorNode>());
        }

        // IEnumerable methods

        public IEnumerator<T> GetEnumerator()
        {
            if (Root == null)
                yield break;
            else
            {
                foreach (var element in Root)
                    yield return element;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // private methods

        private bool AddWith(T value, List<AncestorNode> ancestors)
        {
            ancestors.Clear();

            Node parentNode = null;
            Node node = Root;
            int ord = 0;

            while (node != null)
            {
                ord = Math.Sign(Comparer.Compare(value, node.Value));
                if (ord == 0)
                    return false;
                ancestors.Add(new AncestorNode(ord, node, parentNode));
                parentNode = node;
                node = ord < 0 ? node.Left : node.Right;
            }

            Node newNode = new Node(value);

            if (parentNode == null)
                Root = newNode;
            else if (ord < 0)
                parentNode.Left = newNode;
            else /* if (ord > 0) */
                parentNode.Right = newNode;

            Count++;

            Rebalance(ancestors, +1);

            return true;
        }
        private bool RemoveWith(T value, List<AncestorNode> ancestors)
        {
            ancestors.Clear();

            Node node = Root;
            Node parentNode = null; // The parent of our node
            int parentOrd = 0; // The order of our node in the parent

            while (node != null) {
                int ord = Math.Sign(Comparer.Compare(value, node.Value));
                if (ord == 0)
                    break;
                ancestors.Add(new AncestorNode(ord, node, parentNode));
                parentNode = node;
                parentOrd = ord;
                node = ord < 0 ? node.Left : node.Right;
            }

            if (node == null) // We didn't find the value, nothing to remove
                return false;

Again:
            if (node.Left != null && node.Right != null)
            {
                // Complex case: both sides have a node
                AncestorNode ancestorNode = new AncestorNode(parentOrd, node, parentNode);
                AncestorNode successor = FindSuccessor(ancestorNode, ancestors);
                node.Value = successor.Node.Value; // Move the successor value into the current node...
                parentNode = successor.Parent;
                parentOrd = successor.Ord;
                node = successor.Node; // Continue with removing the successor node (which is now likely further down the subtree)
                goto Again;
            }
            else
            {
                // Simple case: At most one side has a node
                Node newNode = null;
                if (node.Left != null)
                {
                    // The node has one child on the left
                    // Replace the node with it
                    newNode = node.Left;
                }
                else if (node.Right != null)
                {
                    // The node has one child on the right
                    // Replace the node with it
                    newNode = node.Right;
                }
                // Otherwise, the node has no children at all, and we just remove the node
                if (parentNode == null)
                    Root = newNode;
                else if (parentOrd > 0)
                    parentNode.Right = newNode;
                else /* if (parentOrd < 0) */
                    parentNode.Left = newNode;
            }

            Count--;

            Rebalance(ancestors, -1);

            return true;
        }

        /// <summary>
        /// Rebalance the tree.
        /// </summary>
        private void Rebalance(List<AncestorNode> ancestors, int direction)
        {
            foreach (var ancestor in ancestors)
            {
                int ancestorOrd = ancestor.Ord;
                Node ancestorParentNode = ancestor.Parent;
                Node ancestorNode = ancestor.Node;
                ancestorNode.NumDescendants += direction;
                // TODO: Actually rebalance
            }
        }

        /// <summary>
        /// Find the node with the given value, or null if not found.
        /// </summary>
        private Node FindNode(T value)
        {
            Node node = Root;

            while (node != null)
            {
                int ord = Comparer.Compare(value, node.Value);
                if (ord == 0)
                    break;
                node = (ord < 0) ? node.Left : node.Right;
            }

            return node;
        }

        /// <summary>
        /// Find the next node bigger than the given node. Used for deletion.
        /// </summary>
        private AncestorNode FindSuccessor(AncestorNode node, List<AncestorNode> ancestors)
        {
            if (node.Node.Right != null)
            {
                ancestors.Add(node);
                node = new AncestorNode(1, node.Node.Right, node.Node);
                // Simple case: the successor of the given node can be found in its descendants
                while (node.Node != null)
                {
                    if (node.Node.Left == null) // No value between the given node and the node of the current iteration
                        break;
                    ancestors.Add(node);
                    node = new AncestorNode(-1, node.Node.Left, node.Node);
                }
                return node;
            }
            // The successor of the given node can be found in the ancestors
            AncestorNode parent;
            do {
                int lastIdx = ancestors.Count - 1;
                parent = ancestors[lastIdx];
                ancestors.RemoveAt(lastIdx);
                if (node.Node != parent.Node.Right) // No value between the given node and the node of the current iteration
                    break;
                node = parent;
            } while (ancestors.Count > 0);
            return parent;
        }

        /// <summary>
        /// Find the nearest value to the given value
        /// </summary>
        public bool FindNearest(T value, out T nearest)
        {
            Node node = Root;
            Node lastNode = null;
            T nearestValue;

            if (node == null)
            {
                nearest = default(T);
                return false;
            }

            do {
                int ord = Comparer.Compare(value, node.Value);
                if (ord == 0)
                {
                    nearest = node.Value;
                    return true;
                }
                nearestValue = node.Value;
                Node nextNode = (ord < 0) ? node.Left : node.Right;
                if (nextNode != null)
                    lastNode = node;
                node = nextNode;
            } while (node != null);

            if (lastNode != null)
            {
                T lastValue = lastNode.Value;
                if (Math.Abs(Comparer.Compare(value, lastValue)) < Math.Abs(Comparer.Compare(value, nearestValue)))
                    nearestValue = lastValue;
            }

            nearest = nearestValue;
            return true;
        }

        /// <summary>
        /// Validate that the tree is in a consistent state
        /// </summary>
        public bool Validate()
        {
            if (Root != null)
            {
                if (!Root.Validate())
                    return false;
                return Root.NumDescendants + 1 == Count;
            }
            return true;
        }

        /// <summary>
        /// Validate that the tree is sorted
        /// </summary>
        public bool ValidateSorted()
        {
            if (Root != null)
                return Root.ValidateSorted(Comparer);
            return true;
        }

    }

}
