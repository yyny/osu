// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Linq;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.NonVisual
{
    public class BalancedTreeTest
    {
        /// <summary>
        /// Determines if int array is sorted
        /// </summary>
        private static bool IsArraySorted(int[] arr)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i - 1] > arr[i])
                {
                    return false;
                }
            }
            return true;
        }
        private static int GetNodeCount<T>(BalancedTree<T> tree)
        {
            int count = 0;
            foreach (var v in tree)
                count++;
            return count;
        }

        [Test, TimeoutAttribute(250)]
        public void TestEmptyBalancedTreeIsEmpty()
        {
            var tree = new BalancedTree<int>();
            Assert.AreEqual(0, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void AddingToBalancedTreeContainsOneElement()
        {
            var tree = new BalancedTree<int>();
            tree.Add(0);
            Assert.AreEqual(1, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void AddingToBalancedTreeContainsTwoElements()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(2));
            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void AddingToBalancedTreeContainsThreeElements()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(3));
            Assert.AreEqual(3, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void AddingSameToBalancedTreeDoesNotChangeCount()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(3));
            Assert.AreEqual(3, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());

            Assert.IsFalse(tree.Add(1));
            Assert.IsFalse(tree.Add(2));
            Assert.IsFalse(tree.Add(3));
            Assert.AreEqual(3, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void BalancedTreeElementsAreSorted()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(6));
            Assert.IsTrue(tree.Add(3));
            Assert.IsTrue(tree.Add(5));
            Assert.IsTrue(tree.Add(7));
            Assert.IsTrue(tree.Add(9));
            Assert.IsTrue(tree.Add(4));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(8));
            Assert.AreEqual(9, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());

            int[] arr = tree.ToArray();
            Assert.AreEqual(tree.Count, arr.Length);
            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(2, arr[1]);
            Assert.AreEqual(3, arr[2]);
            Assert.AreEqual(4, arr[3]);
            Assert.AreEqual(5, arr[4]);
            Assert.AreEqual(6, arr[5]);
            Assert.AreEqual(7, arr[6]);
            Assert.AreEqual(8, arr[7]);
            Assert.AreEqual(9, arr[8]);
        }

        [Test, TimeoutAttribute(250)]
        public void RemovingFromBalancedTreeWorks()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(3));
            Assert.AreEqual(3, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(tree.Remove(1));
            Assert.IsFalse(tree.Remove(1));
            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
            {
                int[] arr = tree.ToArray();
                Assert.AreEqual(tree.Count, arr.Length);
                Assert.AreEqual(2, arr[0]);
                Assert.AreEqual(3, arr[1]);
            }

            Assert.IsTrue(tree.Remove(3));
            Assert.IsFalse(tree.Remove(3));
            Assert.AreEqual(1, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
            {
                int[] arr = tree.ToArray();
                Assert.AreEqual(tree.Count, arr.Length);
                Assert.AreEqual(2, arr[0]);
            }
        }

        [Test, TimeoutAttribute(250)]
        public void RemovingElementsStressTest()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(3));
            Assert.IsTrue(tree.Remove(2));
            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
            Assert.IsTrue(tree.ValidateSorted());
        }

        [Test, TimeoutAttribute(250)]
        public void RemovingManyElementsFromBalancedTreeWorks()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(6));
            Assert.IsTrue(tree.Add(3));
            Assert.IsTrue(tree.Add(5));
            Assert.IsTrue(tree.Add(7));
            Assert.IsTrue(tree.Add(9));
            Assert.IsTrue(tree.Add(4));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(8));
            Assert.AreEqual(9, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(tree.Remove(1));
            Assert.IsTrue(tree.Remove(2));
            Assert.IsTrue(tree.Remove(3));
            Assert.IsTrue(tree.Remove(4));
            Assert.IsTrue(tree.Remove(5));
            Assert.IsTrue(tree.Remove(6));
            Assert.IsTrue(tree.Remove(7));
            Assert.IsTrue(tree.Remove(8));
            Assert.IsTrue(tree.Remove(9));
            Assert.AreEqual(0, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }

        [Test, TimeoutAttribute(250)]
        public void RemovingManyElementsFromBalancedTreeInRandomOrderWorks()
        {
            var tree = new BalancedTree<int>();
            Assert.IsTrue(tree.Add(6));
            Assert.IsTrue(tree.Add(3));
            Assert.IsTrue(tree.Add(5));
            Assert.IsTrue(tree.Add(7));
            Assert.IsTrue(tree.Add(9));
            Assert.IsTrue(tree.Add(4));
            Assert.IsTrue(tree.Add(2));
            Assert.IsTrue(tree.Add(1));
            Assert.IsTrue(tree.Add(8));
            Assert.AreEqual(9, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());

            Assert.IsTrue(tree.Remove(5));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(3));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(7));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(9));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(6));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(2));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(4));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(8));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.IsTrue(tree.Remove(1));
            Assert.IsTrue(tree.ValidateSorted());
            Assert.AreEqual(0, tree.Count);
            Assert.AreEqual(tree.Count, GetNodeCount(tree));
            Assert.IsTrue(tree.Validate());
        }
    }
}
