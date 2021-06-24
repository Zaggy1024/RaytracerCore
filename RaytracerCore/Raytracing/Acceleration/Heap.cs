//#define VERIFY

using System;
using System.Collections.Generic;
using System.Text;

namespace RaytracerCore.Raytracing.Acceleration
{
	public class Heap<T>
	{
		protected List<T> Contents;
		protected Comparer<T> Comparer;

#if DEBUG
		private void Verify()
		{
			for (int i = 0; i < Contents.Count; i++)
			{
				T child = Contents[i];
				T parent = Contents[GetParent(i)];
				Util.Assert(Comparer.Compare(parent, child) <= 0, "BuildHeap resulted in an incorrect child/parent relationship.");
			}
		}
#endif

		private void BuildHeap()
		{
			for (int i = Contents.Count / 2 - 1; i >= 0; i--)
				HeapifyDown(i);

#if DEBUG
			Verify();
#endif
		}

		public Heap(List<T> contents, Comparer<T> comparer)
		{
			Contents = new List<T>(contents.Count);
			Comparer = comparer;
			BuildHeap();
		}

		public Heap(T[] contents, Comparer<T> comparer)
		{
			Contents = new List<T>(contents);
			Comparer = comparer;
			BuildHeap();
		}

		public int Count => Contents.Count;

		public T Current => Contents[0];

		protected int GetParent(int index)
		{
			return (index - 1) / 2;
		}

		private void HeapifyUp(int index)
		{
			T item = Contents[index];
			int parent = GetParent(index);

			// Shift parents down until our item is less than its children.
			while (index != 0 && Comparer.Compare(item, Contents[parent]) <= 0)
			{
				Contents[index] = Contents[parent];

				index = parent;
				parent = GetParent(index);
			}

			// Place the item in the place of the last parent we swapped.
			Contents[index] = item;
		}

		public void Add(T item)
		{
			Contents.Add(item);
			HeapifyUp(Contents.Count - 1);

#if DEBUG && VERIFY
			Verify();
#endif
		}

		protected int GetChildren(int index)
		{
			return index * 2 + 1;
		}

		private void HeapifyDown(int index)
		{
			int left;
			int right;
			int child;

			while (true)
			{
				left = GetChildren(index);
				right = left + 1;
				child = index;

				if (left < Contents.Count && Comparer.Compare(Contents[child], Contents[left]) > 0)
					child = left;

				if (right < Contents.Count && Comparer.Compare(Contents[child], Contents[right]) > 0)
					child = right;

				if (child == index)
					break;

				T temp = Contents[index];
				Contents[index] = Contents[child];
				Contents[child] = temp;
				index = child;
			}
		}

		public T Extract()
		{
			T min = Current;
			Contents[0] = Contents[^1];
			Contents.RemoveAt(Contents.Count - 1);
			HeapifyDown(0);

#if DEBUG && VERIFY
			Verify();
#endif

			return min;
		}

		public void Replace(T item)
		{
			Contents[0] = item;
			HeapifyDown(0);

#if DEBUG && VERIFY
			Verify();
#endif
		}
	}
}
