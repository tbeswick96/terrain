using System;
using UnityEngine;

namespace Assets.Scripts.Terrain.Pathing {

    /*
     * Heap item interface. Used by Node. Allows proper comparison implementation.
     */
    public interface IHeapItem<T>: IComparable<T> {
        int Index { get; set; }
    }

    /*
     * Heap object. Used for a-star pathfinding for rivers. Generic implementation based on multiple widely available variants of Heap data structures.
     */
    class Heap<T> where T: IHeapItem<T> {
        T[] items;
        int count = 0;

        //Create a new heap of given size.
        public Heap(int maxSize) {
            items = new T[maxSize];
        }

        //Add an item to the heap and sort.
        public void AddItem(T item) {
            item.Index = count;
            items[count] = item;
            SortUpwards(item);
            count++;
        }

        //Remove and return first item from heap and sort.
        public T RemoveFirstItem() {
            T firstItem = items[0];
            count--;
            items[0] = items[count];
            items[0].Index = 0;
            SortDownwards(items[0]);
            return firstItem;
        }

        //Remove and return item at given index from heap and sort.
        public T RemoveAt(int index) {
            T item = items[index];
            count--;
            items[index] = items[count];
            items[index].Index = index;
            SortDownwards(items[index]);
            return item;
        }

        //Overrides generic [] accessor with reference to index.
        public T this[int index] {
            get {
                return items[index];
            }
        }

        //Update the given item in the heap and sort.
        public void UpdateItem(T item) {
            SortUpwards(item);
        }

        //Return if the heap contains the given item.
        public bool Contains(T item) {
            return Equals(items[item.Index], item);
        }

        //Return current heap count.
        public int GetCount {
            get {
                return count;
            }
        }

        //Swap the given items in the heap.
        private void Swap(T item, T swapItem) {
            items[item.Index] = swapItem;
            items[swapItem.Index] = item;
            int index = item.Index;
            item.Index = swapItem.Index;
            swapItem.Index = index;
        }

        //Sort the heap downwards (parent to child).
        //Sorting is based on the item's compare implementation. For the Node implementation, this is the combined cost value.
        private void SortDownwards(T item) {
            while (true) {
                int childIndexLeft = item.Index * 2 + 1;
                int childIndexRight = item.Index * 2 + 2;
                int swapIndex = 0;
                if (childIndexLeft < count) {
                    swapIndex = childIndexLeft;
                    if (childIndexRight < count) {
                        if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0) {
                            swapIndex = childIndexRight;
                        }
                    }
                    if (item.CompareTo(items[swapIndex]) < 0) {
                        Swap(item, items[swapIndex]);
                    } else {
                        return;
                    }
                } else {
                    return;
                }
            }
        }

        //Sort the heap upwards (child to parent).
        //Sorting is based on the item's compare implementation. For the Node implementation, this is the combined cost value.
        private void SortUpwards(T item) {
            int parentIndex = Mathf.FloorToInt((item.Index - 1) / 2);
            while (true) {
                T parentItem = items[parentIndex];
                if (item.CompareTo(parentItem) > 0) {
                    Swap(item, parentItem);
                } else {
                    break;
                }
                parentIndex = Mathf.FloorToInt((item.Index - 1) / 2);
            }
        }
    }
}
