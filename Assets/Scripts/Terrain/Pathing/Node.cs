using System;

namespace Assets.Scripts.Terrain.Pathing {

    /*
     * Node object. World node, containing a worldpoint, references to the parent and child nodes (for rivers), and heuristic values (for river pathfinding).
     */
    public class Node: IHeapItem<Node> {

        public WorldPoint worldPoint;
        public Node parent, child;
        public int children;

        private int index;
        public float gCost, hCost;

        //Get/Set the index value of the node in the pathfinding heap.
        public int Index {
            get {
                return index;
            }

            set {
                index = value;
            }
        }

        //Return the combined cost value of the node's heuristic values.
        public float FCost {
            get {
                return gCost + hCost;
            }
        }

        //Compare this node to given node, based on each node's combined cost value.
        public int CompareTo(Node toCompare) {
            int compare = FCost.CompareTo(toCompare.FCost);
            if (compare == 0) {
                compare = hCost.CompareTo(toCompare.hCost);
            }
            return -compare;
        }
    }
}
