using System.Collections.Generic;

namespace Assets.Scripts.Terrain.Pathing {

    /*
     * A* pathfinding for rivers.
     */
    public class PathFinder {

        //Find the best path from the given starting node to the given end node. 
        //This uses A* with a heuristic based on the distance to the end node and the difference in z value (height). 
        public static Node FindAStarPath(World world, Node[] points) {
            Node startNode = points[0];
            Node endNode = points[1];
            Heap<Node> openSet = new Heap<Node>(world.islandSize * world.islandSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.AddItem(startNode);
            
            while (openSet.GetCount > 0 && !Master.worldGenerator.terminate.WaitOne(0)) {
                Node currentNode = openSet.RemoveFirstItem();
                closedSet.Add(currentNode);
                if (currentNode == endNode) { break; }
                foreach (Node neighbor in world.GetSurroundingNodes(currentNode)) {
                    if (closedSet.Contains(neighbor)) continue;
                    //Heuristic cost based on the current node's stored cost, the distance to the neighbour node (from the current node), and the inverted difference in height between the current node and the neighbour node.
                    float newCostToNeighbor = currentNode.gCost + world.GetDistance(currentNode, neighbor) + (1f - (currentNode.worldPoint.z - neighbor.worldPoint.z));
                    if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor)) {
                        Info.log.Send(string.Format("New lowest cost: {0}", newCostToNeighbor), 1);
                        neighbor.gCost = newCostToNeighbor;
                        //Cost based on distance from neighbour node to end node, with a scaled multiplier based on a random value and the inverted z value (height) of the neighbour node.
                        //This results in an increasing displacement of the path the lower the z value is. The creates meanders at lower altitudes.
                        //Increasing the range of the random value increases the size of meanders, but also increases the calculation time.
                        neighbor.hCost = world.GetDistance(neighbor, endNode) - ((1f - neighbor.worldPoint.z) * (Info.RANDOM.Next(0, 250) - 250));
                        neighbor.parent = currentNode;
                        currentNode.child = neighbor;
                        if (!openSet.Contains(neighbor)) {
                            Info.log.Send(string.Format("Adding node [x: {0}, y: {1}] to open set. Parent node [x: {2}, y: {3}]", neighbor.worldPoint.x, neighbor.worldPoint.y, neighbor.parent.worldPoint.x, neighbor.parent.worldPoint.y), 1);
                            openSet.AddItem(neighbor);
                        } else {
                            openSet.UpdateItem(neighbor);
                        }
                    }
                }
            }

            return CompilePathFromNodes(startNode, endNode);
        }

        //Return the start node of the river with each child node set to the node under it, and each parent node set to the node above it. This ensures the path is complete and correct.
        private static Node CompilePathFromNodes(Node start, Node end) {
            //Info.log.Send(string.Format("Tracing from [x: {0}, y: {1}] to [x: {2}, y: {3}]", start.worldPoint.x, start.worldPoint.y, end.worldPoint.x, end.worldPoint.y), 1);
            Node currentNode = end;
            Node currentParent;
            while (currentNode != start && !Master.worldGenerator.terminate.WaitOne(0)) {
                //Info.log.Send(string.Format("Tracing through node [x: {0}, y: {1}]. Parent? {2}", currentNode.worldPoint.x, currentNode.worldPoint.y, currentNode.parent != null), 2);
                currentParent = currentNode.parent;
                currentParent.child = currentNode;
                currentNode = currentParent;
            }
            return start;
        }
    }
}
