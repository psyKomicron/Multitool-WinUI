using System;
using System.Collections.Generic;

namespace Multitool.Sorting
{
    public static class QuickSort
    {
        public static void Sort<T>(T[] array, int low, int high) where T : IComparable<T>
        {
            CheckParameters(array, ref low, ref high);

            if (low < high)
            {
                int pIndex = Partition(array, low, high);

                Sort(array, low, pIndex - 1);
                Sort(array, pIndex + 1, high);
            }
        }

        public static void Sort<T>(T[] array, IComparer<T> comparer, int low, int high)
        {
            CheckParameters(array, ref low, ref high);

            if (low < high)
            {
                int pIndex = PartitionWithComparer(array, comparer, low, high);

                Sort(array, comparer, low, pIndex - 1);
                Sort(array, comparer, pIndex + 1, high);
            }
        }

        /// <summary>
        /// Sorts the array and returns a tuple of : old index, new index, object
        /// for each item in the <paramref name="array"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <returns></returns>
        public static List<Tuple<int, int>> SortIndexes<T>(T[] array, int low, int high) where T : class, IComparable<T>
        {
            CheckParameters(array, ref low, ref high);
            List<Tuple<int, int>> indexes = new(array.Length + 1);
            T[] copy = new T[array.Length];
            array.CopyTo(copy, 0);
            Sort(array, low, high);

            for (int i = 0; i < copy.Length; i++)
            {
                int indexOf = -1;
                for (int j = 0; j < array.Length; j++)
                {
                    if (copy[i] == array[j])
                    {
                        indexOf = j;
                        break;
                    }
                }
                indexes.Add(new(i, indexOf));
            }
            indexes.TrimExcess();
            return indexes;
        }

        private static void Swap<T>(T[] array, int i, int j)
        {
            (array[j], array[i]) = (array[i], array[j]);
        }

        private static int PartitionWithComparer<T>(T[] array, IComparer<T> comparer, int low, int high)
        {
            T pivot = array[high];
            int i = low - 1;
            for (int j = low; j <= high - 1; j++)
            {
                if (comparer.Compare(array[j], pivot) < 0)
                {
                    i++;
                    Swap(array, i, j);
                }
            }
            Swap(array, i + 1, high);
            return i + 1;
        }

        private static int Partition<T>(T[] array, int low, int high) where T : IComparable<T>
        {
            T pivot = array[high];
            int i = low - 1;
            for (int j = low; j <= high - 1; j++)
            {
                if (array[j].CompareTo(pivot) < 0)
                {
                    i++;
                    Swap(array, i, j);
                }
            }
            Swap(array, i + 1, high);
            return i + 1;
        }

        /// <summary>
        /// bounds checks && null array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        private static void CheckParameters<T>(T[] array, ref int low, ref int high)
        {
            if (high >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(high));
            }
            if (low < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(low));
            }
            if (array == null)
            {
                throw new ArgumentException("Array was null", nameof(array));
            }

            if (high == -1)
            {
                high = array.Length - 1;
            }
        }
    }
}
