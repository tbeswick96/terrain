using Assets.Scripts.Terrain.Pathing;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Terrain {

    /*
     * World object. Contains all of the information about the world and some utility functions.
     */
    public class World {

        public int worldSize;
        public int islandSize;
        public Node[,] worldMap;
        public float maxWidth;
        public List<Node> rivers;

        //Set fields based on settings.
        public World() {
            worldSize = Info.HEIGHTMAP_SIZE * Info.TILES;
            worldMap = new Node[worldSize, worldSize];
            islandSize = Convert.ToInt32(worldSize * 0.9f);
            maxWidth = islandSize * 0.5f;
            rivers = new List<Node>();

            //Initialise the world with empty nodes, each containing a world point with given x,y coord.
            for (int y = 0; y < worldSize; y++) {
                for (int x = 0; x < worldSize; x++) {
                    worldMap[y, x] = new Node() {
                        worldPoint = new WorldPoint() {
                            x = x,
                            y = y
                        }
                    };
                }
            }
        }

        //Returns 2 nodes used for the start and end of a river.
        //Chooses a random start point and finds the highest point around it in a 50 unit radius.
        //Choose a random end point based on a random angle to ensure the river end is downslope of the start.
        public Node[] GetRiverStartAndEnd() {
            float angle = Convert.ToSingle(Info.RANDOM.NextDouble() * Math.PI * 2);
            int radius = Convert.ToInt32(Info.RANDOM.NextDouble() * (islandSize / 5));
            int outerRadius = UnityEngine.Mathf.Clamp(Convert.ToInt32(Info.RANDOM.NextDouble() * (islandSize / 2)), 0, worldSize);
            int yEnd = Convert.ToInt32((worldSize / 2) + outerRadius * Math.Sin(angle));
            int xEnd = Convert.ToInt32((worldSize / 2) + outerRadius * Math.Cos(angle));
            int searchRadius = 50;

            Node highestPoint = FindNodeInRadius(Convert.ToInt32((worldSize / 2) + radius * Math.Sin(angle)), Convert.ToInt32((worldSize / 2) + radius * Math.Cos(angle)), searchRadius, 0.3f, 1f, true);
            Node lowestPoint = FindNodeInRadius(yEnd, xEnd, 50, 0.05f, 0.06f, false);
            int iteration = 0;
            while (lowestPoint == worldMap[yEnd, xEnd] && !Master.worldGenerator.terminate.WaitOne(0)) {
                if (iteration > 5) break;
                searchRadius += 50;
                lowestPoint = FindNodeInRadius(yEnd, xEnd, searchRadius, 0.05f, 0.06f, false);
                iteration++;
            }

            return new Node[2] { highestPoint, lowestPoint };
        }

        //Returns a node from the given centre node in a radius based on given settings.
        //If we want to find the highest node in a radius, we look for nodes with greater z values than the other nodes.
        //If we want the lowest node in a radius, we look for nodes with the smaller z values than the other nodes.
        //A minimum and maximum height allow control over what z values are acceptable. For coastal points, we only want nodes with z values between 0.05 and 0.06 for example.
        private Node FindNodeInRadius(int yCentre, int xCentre, int radius, float minHeight, float maxHeight, bool higher) {
            Node bestPoint = worldMap[yCentre, xCentre];
            int bestDistance = int.MaxValue;
            for (int y = UnityEngine.Mathf.Clamp(yCentre - radius, 0, worldSize); y < UnityEngine.Mathf.Clamp(yCentre + radius, 0, worldSize); y++) {
                for (int x = UnityEngine.Mathf.Clamp(xCentre - radius, 0, worldSize); x < UnityEngine.Mathf.Clamp(xCentre + radius, 0, worldSize); x++) {
                    float height = worldMap[y, x].worldPoint.z;
                    int distance = GetDistance(worldMap[y, x], worldMap[yCentre, xCentre]);
                    if (higher) {
                        Info.log.Send(string.Format("Testing height {0} > {1} = {2}", height, bestPoint.worldPoint.z, height > bestPoint.worldPoint.z), 1);
                        if (height > minHeight && height < maxHeight && height > bestPoint.worldPoint.z && distance < bestDistance) {
                            bestPoint = worldMap[y, x];
                            bestDistance = distance;
                        }
                    } else {
                        Info.log.Send(string.Format("Testing height {0} < {1} = {2}", height, bestPoint.worldPoint.z, height < bestPoint.worldPoint.z), 1);
                        if (height > minHeight && height < maxHeight && height < bestPoint.worldPoint.z && distance < bestDistance) {
                            bestPoint = worldMap[y, x];
                            bestDistance = distance;
                        }
                    }
                }
            }
            return bestPoint;
        }

        //Returnt the cardinal distance between two nodes.
        public int GetDistance(Node start, Node end) {
            return UnityEngine.Mathf.Abs(start.worldPoint.y - end.worldPoint.y) + UnityEngine.Mathf.Abs(start.worldPoint.x - end.worldPoint.x);
        }

        //Find and return a list of the surrounding nodes of a given node.
        //If the node is an edge/corner node, we need to check if the surrounding node we are looking for is a valid array index. (0 - 1 = -1, which is not a valid array index)
        public List<Node> GetSurroundingNodes(Node node) {
            WorldPoint point = node.worldPoint;
            List<Node> nodes = new List<Node>();
            for (int y = -1; y < 2; y++) {
                for (int x = -1; x < 2; x++) {
                    if (point.y + y >= 0 && point.y + y < worldSize && point.x + x >= 0 && point.x + x < worldSize) {
                        nodes.Add(worldMap[point.y + y, point.x + x]);
                    }
                }
            }
            return nodes;
        }
    }
}
