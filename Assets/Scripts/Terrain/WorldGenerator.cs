using Assets.Scripts.Terrain.Pathing;
using Assets.Scripts.Utility;
using System;

namespace Assets.Scripts.Terrain {

    /*
     * World generator thread. Contains all functions used to generate the world information.
     */
    public class WorldGenerator: ThreadedWorker {

        private World world;
        public FastNoise fastNoise;
        private int textIndex;

        //Get/Set the world object. Thread-safe for copying.
        public World World {
            get {
                World localWorld;
                lock (handle) {
                    localWorld = world;
                }
                return localWorld;
            }
            set {
                lock (handle) {
                    world = value;
                }
            }
        }

        //Thread function called on thread start. This creates a world object, runs all world calculations to define the heightmap, and sends the world object as output.
        protected override void ThreadFunction() {
            textIndex = 0;
            world = new World();

            CheckAndExecute(SetHeights);
            CheckAndExecute(ErosionThermal);
            CheckAndExecute(ErosionHydraulic);
            CheckAndExecute(FindRivers);
            CheckAndExecute(CarveRivers);
            CheckAndExecute(DetailPass);

            World = world;
        }

        //Checks if the thread is supposed to stop, and if not, executes the given method. Also tells the thread to send the next loading screen message.
        private void CheckAndExecute(Action method) {
            if (!terminate.WaitOne(0)) {
                Send(textIndex);
                method();
                textIndex++;
            }
        }

        //Sets the basic height values of the world. This is based on the noise function stored in the world object.
        //This also stores the minimum and maximum heights, and then normalises the range to 0 - 1.
        //This is because the noise function can return negative numbers, which are not valid values for Unity terrain heightmaps. They only accept values of 0 - 1.
        //During the normalisation of the height values, the height is also multiplied by a factor based on the distance to the centre of the world.
        //This creates a drop off of height from the centre of the world to the edge of the world, creating an island. This can be set to be round or square.
        private void SetHeights() {
            float minimum = 0f;
            float maximum = 0f;

            fastNoise.SetFrequency(Info.NOISE_FREQUENCY_BASE * Info.NOISE_FREQUENCY_MULTIPLIER);
            fastNoise.SetFractalOctaves(Info.NOISE_OCTAVES);
            for (int y = 0; y < world.worldSize; y++) {
                for (int x = 0; x < world.worldSize; x++) {
                    float height = fastNoise.GetSimplexFractal(y, x);
                    world.worldMap[y, x].worldPoint.z += height;
                    minimum = height < minimum ? height : minimum;
                    maximum = height > maximum ? height : maximum;
                }
            }

            float scale = maximum - minimum;
            for (int y = 0; y < world.worldSize; y++) {
                for (int x = 0; x < world.worldSize; x++) {
                    float distanceX = Math.Abs(x - world.worldSize * 0.5f);
                    float distanceY = Math.Abs(y - world.worldSize * 0.5f);
                    float distance = Math.Max(distanceX, distanceY);
                    if (Info.ISLAND_ROUND) {
                        distance = (float) Math.Sqrt((distanceX * distanceX) + (distanceY * distanceY));
                    }
                    float delta = distance / world.maxWidth;
                    world.worldMap[y, x].worldPoint.z = ((world.worldMap[y, x].worldPoint.z - minimum) / scale) * Math.Max(0f, (1f - (delta * delta)));
                }
            }
        }

        //Find a random number of rivers (2 - 5) using the A* pathing function.
        private void FindRivers() {
            for (int i = 0; i < Info.RANDOM.Next(2, 5); i++) {
                try {
                    Node river = PathFinder.FindAStarPath(world, world.GetRiverStartAndEnd());
                    if (river != null) {
                        world.rivers.Add(river);
                    }
                } catch (Exception e) {
                    Info.log.Send(string.Format("Caught exception in thread: {0}", e.ToString()), LogLevel.ERROR, 1);
                }
            }
        }

        //For each of the found river paths, carve the river into the heightmap. The width and depth of the river is based on the z value (height) of the current node.
        //To ensure no point is carved twice, each node is set to be a river once carved.
        //If the node is below sea level (z = 0.05), don't carve the river.
        private void CarveRivers() {
            foreach (Node river in world.rivers) {
                Node currentNode = river;
                Node previousNode = river;
                river.worldPoint.isRiver = false;
                float currentMinHeight = currentNode.worldPoint.z;
                int carved = 0;
                while (currentNode != null && !Master.worldGenerator.terminate.WaitOne(0) && carved < river.children) {
                    WorldPoint point = currentNode.worldPoint;
                    int radius = (int) ((1 - point.z) * 10) / 2;
                    float depth = (point.z / 50f);
                    Info.log.Send(string.Format("[x:{0}, y:{1}] radius = {2}, depth = {3}", point.x, point.y, radius, depth), 1);
                    for (int y = -radius; y < radius; y++) {
                        for (int x = -radius; x < radius; x++) {
                            if (point.y + y >= 0 && point.y + y < world.worldSize && point.x + x >= 0 && point.x + x < world.worldSize) {
                                if (!world.worldMap[point.y + y, point.x + x].worldPoint.isRiver) {
                                    Info.log.Send(string.Format("Carving [x:{0}, y:{1}].worldPoint.z {2} by {3}", x, y, world.worldMap[point.y + y, point.x + x].worldPoint.z, depth), 1);
                                    if (world.worldMap[point.y + y, point.x + x].worldPoint.z > 0.05f) {
                                        if (world.worldMap[point.y + y, point.x + x].worldPoint.z >= currentMinHeight) {
                                            world.worldMap[point.y + y, point.x + x].worldPoint.z = currentMinHeight;
                                        } else if (world.worldMap[point.y + y, point.x + x].worldPoint.z < currentMinHeight) {
                                            currentMinHeight = world.worldMap[point.y + y, point.x + x].worldPoint.z;
                                        }
                                        world.worldMap[point.y + y, point.x + x].worldPoint.z -= depth;
                                    }
                                    world.worldMap[point.y + y, point.x + x].worldPoint.isRiver = true;
                                }
                            }
                        }
                    }
                    carved++;
                    if (currentNode.child == previousNode) {
                        break;
                    } else {
                        previousNode = currentNode;
                        currentNode = currentNode.child;
                    }
                }
            }
        }

        //Inverted thermal erosion. Creates cliffs, plateaus, and steps in an otehrwise smooth terrain.
        private void ErosionThermal() {
            float talus = (31 - Info.EROSION_THERMAL_STRENGTH) / world.worldSize;
            for (int iteration = 0; iteration < Info.EROSION_THERMAL_ITERATIONS; iteration++) {
                for (int y = 1; y < world.worldSize - 1; y++) {
                    for (int x = 1; x < world.worldSize - 1; x++) {
                        float height = world.worldMap[y, x].worldPoint.z;
                        float maxDifference = -float.MaxValue;
                        int lowestY = 0, lowestX = 0;
                        for (int i = -1; i < 2; i += 2) {
                            for (int j = -1; j < 2; j += 2) {
                                float difference = height - world.worldMap[y + i, x + j].worldPoint.z;
                                if (difference > maxDifference) {
                                    maxDifference = difference;
                                    lowestY = i;
                                    lowestX = j;
                                }
                            }
                        }
                        if (maxDifference > 0.0f && maxDifference <= talus) {
                            float newHeight = height - maxDifference / 2.0f;
                            world.worldMap[y, x].worldPoint.z = newHeight;
                            world.worldMap[y + lowestY, x + lowestX].worldPoint.z = newHeight;
                        }
                    }
                }
            }
        }

        //Hydraulic erosion. This replicates the process of rain dropping water, that water picking up material, flowing downhill, and depositing the material.
        //The amount of water dropped is based on a rain factor. The amount of material picked up by the water is based on a solubility factor. The rate at which water evapourates is based on an evapouration factor.
        //This process is very slow, and the time taken increases with he number of iterations. A higher number of iterations results in a smoother and more realistic terrain however.
        private void ErosionHydraulic() {
            float rainAmount = 0.03f * (Info.EROSION_HYDRAULIC_STRENGTH / 2);
            float solubility = 0.01f * (Info.EROSION_HYDRAULIC_STRENGTH / 2);
            float evaporation = 0.5f;
            int lowestZ = 0, lowestX = 0;
            float waterLost, currentHeight, currentDifference, maxDifference;
            for (int iteration = 0; iteration < Info.EROSION_HYDRAULIC_ITERATIONS; iteration++) {
                for (int y = 0; y < world.worldSize; y++) {
                    for (int x = 0; x < world.worldSize; x++) {
                        world.worldMap[y, x].worldPoint.waterAmount += rainAmount;
                        world.worldMap[y, x].worldPoint.z -= world.worldMap[y, x].worldPoint.waterAmount * solubility;
                    }
                }
                for (int y = 1; y < world.worldSize - 1; y++) {
                    for (int x = 1; x < world.worldSize - 1; x++) {
                        currentHeight = world.worldMap[y, x].worldPoint.z + world.worldMap[y, x].worldPoint.waterAmount;
                        maxDifference = -float.MaxValue;
                        for (int i = -1; i < 2; i += 2) {
                            for (int j = -1; j < 2; j += 2) {
                                currentDifference = currentHeight - world.worldMap[y + i, x + j].worldPoint.z - world.worldMap[y + i, x + j].worldPoint.waterAmount;
                                if (currentDifference > maxDifference) {
                                    maxDifference = currentDifference;
                                    lowestZ = i;
                                    lowestX = j;
                                }
                            }
                        }
                        if (maxDifference > 0) {
                            if (world.worldMap[y, x].worldPoint.waterAmount < maxDifference) {
                                world.worldMap[y + lowestZ, x + lowestX].worldPoint.waterAmount += world.worldMap[y, x].worldPoint.waterAmount;
                                world.worldMap[y, x].worldPoint.waterAmount = 0f;
                            } else {
                                world.worldMap[y + lowestZ, x + lowestX].worldPoint.waterAmount += maxDifference / 2.0f;
                                world.worldMap[y, x].worldPoint.waterAmount -= maxDifference / 2.0f;
                            }
                        }
                    }
                }
                for (int y = 0; y < world.worldSize; y++) {
                    for (int x = 0; x < world.worldSize; x++) {
                        waterLost = world.worldMap[y, x].worldPoint.waterAmount * evaporation;
                        world.worldMap[y, x].worldPoint.waterAmount -= waterLost;
                        world.worldMap[y, x].worldPoint.z += waterLost * solubility;
                    }
                }
            }
        }

        //Final detail pass over the top of all other calculations to add a small level of detail to the terrain.
        private void DetailPass() {
            fastNoise.SetFrequency(Info.NOISE_FREQUENCY_BASE * 10 * Info.NOISE_FREQUENCY_MULTIPLIER);
            fastNoise.SetFractalOctaves(Convert.ToInt32(Math.Round(Info.NOISE_OCTAVES / 4f)));
            for (int y = 0; y < world.worldSize; y++) {
                for (int x = 0; x < world.worldSize; x++) {
                    world.worldMap[y, x].worldPoint.z += (fastNoise.GetSimplexFractal(y, x) * 0.05f);
                }
            }
        }
    }
}
