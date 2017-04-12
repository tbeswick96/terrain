using Artngame.SKYMASTER;
using Assets.Scripts.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Terrain {

    /*
     * Master class. Generation is started and controlled from here. Runs via Unity calls (Start) upon program start.
     */
    public class Master: MonoBehaviour {

        //Store camera object so we can control whether the player can move.
        public new Camera camera;

        //Local variables for control of generation
        private bool firstRun = true;
        public static WorldGenerator worldGenerator;
        private FastNoise fastNoise;
        private World world;
        private GameObject[,] terrains;
        private int tileIndex;

        //UI elements
        private GameObject loadingPanel, cornerPanel, optionsPanel;
        private Text loadingText, loadingTextLeft, loadingTextRight;

        //Called once via Unity upon program start. Initialises static variables. 
        private void Start() {
            SetupUI();

            Info.MATERIAL = (Material) Resources.Load("TerrainMaterial");
            Info.TEXTURE_GRASS = (Texture2D) Resources.Load("Textures/Grass");
            Info.TEXTURE_EARTH = (Texture2D) Resources.Load("Textures/Earth");
            Info.TEXTURE_ROCK = (Texture2D) Resources.Load("Textures/Rock");
            Info.TEXTURE_SAND = (Texture2D) Resources.Load("Textures/Sand");
            Info.TEXTURE_DIRT = (Texture2D) Resources.Load("Textures/Dirt");
            Info.GRASS_1 = (Texture2D) Resources.Load("Grass/Grass1");
            Info.GRASS_2 = (Texture2D) Resources.Load("Grass/Grass2");
            Info.GRASS_3 = (Texture2D) Resources.Load("Grass/Grass3");
            Info.GRASS_4 = (Texture2D) Resources.Load("Grass/Grass4");
            Info.TREE_OAK = (GameObject) Resources.Load("Trees/Broadleaf_Desktop");
            Info.TREE_PINE = (GameObject) Resources.Load("Trees/Conifer_Desktop");
            Info.TEXTURE_PROTOTYPES = new SplatPrototype[] {
                new SplatPrototype() { texture = Info.TEXTURE_GRASS },
                new SplatPrototype() { texture = Info.TEXTURE_EARTH },
                new SplatPrototype() { texture = Info.TEXTURE_ROCK },
                new SplatPrototype() { texture = Info.TEXTURE_SAND },
                new SplatPrototype() { texture = Info.TEXTURE_DIRT }
            };
            Info.DETAIL_PROTOTYPES = new DetailPrototype[] {
                new DetailPrototype() {
                    prototypeTexture = Info.GRASS_1,
                    minHeight = 2, maxHeight = 5,
                    minWidth = 5, maxWidth = 10,
                    noiseSpread = 20,
                    healthyColor = new Color(0.35f, 0.35f, 0.22f)
                },
                new DetailPrototype() {
                    prototypeTexture = Info.GRASS_2,
                    minHeight = 3, maxHeight = 5,
                    minWidth = 2, maxWidth = 4,
                    noiseSpread = 20,
                    healthyColor = new Color(0.35f, 0.35f, 0.22f)
                },
                new DetailPrototype() {
                    prototypeTexture = Info.GRASS_3,
                    minHeight = 3, maxHeight = 5,
                    minWidth = 2, maxWidth = 4,
                    noiseSpread = 20,
                    healthyColor = new Color(0.35f, 0.35f, 0.22f)
                },
                new DetailPrototype() {
                    prototypeTexture = Info.GRASS_4,
                    minHeight = 2, maxHeight = 4,
                    minWidth = 2, maxWidth = 4,
                    noiseSpread = 30,
                    healthyColor = new Color(0.35f, 0.35f, 0.22f)
                }
            };
            Info.TREE_PROTOTYPES = new TreePrototype[] {
                new TreePrototype() {
                    prefab = Info.TREE_OAK
                },
                new TreePrototype() {
                    prefab = Info.TREE_PINE
                }
            };

            //Create and start the logging thread.
            Info.log = new ThreadedLogger();
            Info.log.Start();

            //Create a world.
            CreateWorld();
            firstRun = false;
        }

        //Creates a world. If this is not the first time running, settings are read from the UI.
        public void CreateWorld() {
            if (!firstRun) {
                for (int i = 0; i < Info.TILES; i++) {
                    for (int j = 0; j < Info.TILES; j++) {
                        Destroy(terrains[i, j]);
                        terrains[i, j] = null;
                    }
                }
                ReadSettings();
            }

            //If a random seed is to be chosen, choose it now and create a new random and noise function based on that seed.
            Info.TOTAL_TILES = Info.TILES * Info.TILES;
            Info.OFFSET = ((Info.HEIGHTMAP_SIZE - 1) * (Info.TILES / 2));
            Info.SEED = Info.SEED_RANDOMISE ? new System.Random().Next() : (GameObject.Find("seedManual").GetComponent<InputField>().text.Length > 0 ? Convert.ToInt32(GameObject.Find("seedManual").GetComponent<InputField>().text) : 0);
            GameObject.Find("seedManual").GetComponent<InputField>().text = Info.SEED.ToString();
            Info.RANDOM = new System.Random(Info.SEED);
            fastNoise = new FastNoise(Info.SEED);

            //Reset the time, disable the camera, and reset some variables.
            SkyMasterManager.instance.Current_Time = 5.245f;
            SkyMasterManager.instance.SPEED = 0.1f;
            camera.transform.position = new Vector3(500, 400, 0);
            camera.transform.eulerAngles = new Vector3(0, -97, 0);
            camera.GetComponent<CameraMovement>().enabled = false;
            camera.GetComponent<UnityStandardAssets.ImageEffects.BlurOptimized>().enabled = true;
            loadingPanel.SetActive(true);
            cornerPanel.SetActive(false);
            optionsPanel.SetActive(false);
            tileIndex = 0;

            //Create and start the world generation thread, giving it the chosen seed value and noise function reference.
            worldGenerator = new WorldGenerator() {
                fastNoise = fastNoise
            };
            worldGenerator.Start();
        }

        //Update is called via Unity once each frame. Here we check if the world generation thread is running, and attempt to retrieve any output it may have.
        //Once the thread is complete, we set the local world reference and start the terrain creation.
        private void Update() {
            if (worldGenerator != null) {
                List<object> output = worldGenerator.Recieve();
                if (output != null) {
                    SetLoadingText((int) output[0]);
                }
                if (worldGenerator.Update()) {
                    world = worldGenerator.World;
                    worldGenerator = null;
                    StartCoroutine(GenerateWorld());
                }
            }
        }

        //Creates the terrain objects visible to the player from the data created by the world generation thread.
        //IEnumerator used to not block the main thread when creating the terrain objects. (One terrain is created each frame)
        private IEnumerator GenerateWorld() {
            tileIndex = 1;
            terrains = new GameObject[Info.TILES, Info.TILES];
            GameObject terrainParent = GameObject.Find("Terrains");
            for (int i = 0; i < Info.TILES; i++) {
                for (int j = 0; j < Info.TILES; j++) {
                    SetLoadingText(6, new object[2] { tileIndex, Info.TOTAL_TILES });
                    terrains[i, j] = TerrainCreator.CreateTerrain(world, i, j);
                    terrains[i, j].transform.parent = terrainParent.transform;
                    terrains[i, j].GetComponent<UnityEngine.Terrain>().Flush();
                    tileIndex = tileIndex >= Info.TOTAL_TILES ? tileIndex : tileIndex + 1;
                    yield return new WaitForEndOfFrame();
                }
            }
            tileIndex = 1;
            for (int i = 0; i < Info.TILES; i++) {
                for (int j = 0; j < Info.TILES; j++) {
                    SetLoadingText(7, new object[2] { tileIndex, Info.TOTAL_TILES });
                    TerrainCreator.SetDetail(world, terrains[i, j], i, j);
                    terrains[i, j].GetComponent<UnityEngine.Terrain>().Flush();
                    tileIndex = tileIndex >= Info.TOTAL_TILES ? tileIndex : tileIndex + 1;
                    yield return new WaitForEndOfFrame();
                }
            }
            tileIndex = 1;
            for (int i = 0; i < Info.TILES; i++) {
                for (int j = 0; j < Info.TILES; j++) {
                    SetLoadingText(8, new object[2] { tileIndex, Info.TOTAL_TILES });
                    TerrainCreator.SetTrees(world, terrains[i, j], i, j);
                    terrains[i, j].GetComponent<UnityEngine.Terrain>().Flush();
                    tileIndex = tileIndex >= Info.TOTAL_TILES ? tileIndex : tileIndex + 1;
                    yield return new WaitForEndOfFrame();
                }
            }
            for (int i = 0; i < Info.TILES; i++) {
                for (int j = 0; j < Info.TILES; j++) {
                    TerrainCreator.UpdateNeighbours(terrains, i, j);
                    terrains[i, j].GetComponent<UnityEngine.Terrain>().Flush();
                }
            }

            StartCoroutine(Finish());
        }

        //Once all of the terrains have been created, finish up and give control of the camera back to the player.
        private IEnumerator Finish() {
            SetLoadingText(9);
            SkyMasterManager.instance.SPEED = 0.5f;
            camera.GetComponent<CameraMovement>().enabled = true;
            cornerPanel.SetActive(true);

            while (camera.GetComponent<UnityStandardAssets.ImageEffects.BlurOptimized>().blurSize > 0) {
                camera.GetComponent<UnityStandardAssets.ImageEffects.BlurOptimized>().blurSize -= 0.045f;
                yield return new WaitForEndOfFrame();
            }
            camera.GetComponent<UnityStandardAssets.ImageEffects.BlurOptimized>().enabled = false;
            camera.GetComponent<UnityStandardAssets.ImageEffects.BlurOptimized>().blurSize = 3;
            loadingPanel.SetActive(false);

            world = null;
        }

        //Sets the loading text displayed when a world is being generated.
        private void SetLoadingText(int index, params object[] parameters) {
            parameters = parameters.Length == 0 ? new object[2] { Info.TOTAL_TILES, Info.TOTAL_TILES } : parameters;
            string text = GetFormattedString(index, parameters);
            loadingText.text = text;
            
            loadingTextLeft.text = index > 0 ? GetFormattedString(index - 1, new object[2] { Info.TOTAL_TILES, Info.TOTAL_TILES }) : "";
            loadingTextRight.text = index < Info.LOADING_TEXTS.Length - 1 ? GetFormattedString(index + 1, new object[2] { 0, Info.TOTAL_TILES }) : "";

            Info.log.Send(text);
        }

        //Returns a formatted string based on the current text index and given parameters.
        private string GetFormattedString(int index, params object[] parameters) {
            string text = Info.LOADING_TEXTS[index][0];
            if (Info.SIMS_TEXTS) {
                text = Info.LOADING_TEXTS[index][1];
            }
            return string.Format(text, parameters);
        }

        //Finds and stores the UI objects from the game scene.
        private void SetupUI() {
            loadingPanel = GameObject.Find("LoadingPanel");
            cornerPanel = GameObject.Find("CornerPanel");
            optionsPanel = GameObject.Find("OptionsPanel");
            loadingText = GameObject.Find("LoadingText").GetComponent<Text>();
            loadingTextLeft = GameObject.Find("LoadingTextLeft").GetComponent<Text>();
            loadingTextRight = GameObject.Find("LoadingTextRight").GetComponent<Text>();

            GameObject.Find("tiles").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("tilesText").GetComponent<Text>().text = string.Format("Tiles: {0}", (int) GameObject.Find("tiles").GetComponent<Slider>().value); });
            GameObject.Find("noiseFrequency").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("noiseFrequencyText").GetComponent<Text>().text = string.Format("Noise Freqency Mutliplier: {0}", (int) GameObject.Find("noiseFrequency").GetComponent<Slider>().value); });
            GameObject.Find("noiseOctaves").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("noiseOctavesText").GetComponent<Text>().text = string.Format("Noise Octaves: {0}", (int) GameObject.Find("noiseOctaves").GetComponent<Slider>().value); });
            GameObject.Find("thermalStrength").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("thermalStrengthText").GetComponent<Text>().text = string.Format("Thermal Erosion Strength: {0}", (int) GameObject.Find("thermalStrength").GetComponent<Slider>().value); });
            GameObject.Find("thermalIterations").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("thermalIterationsText").GetComponent<Text>().text = string.Format("Thermal Erosion Iterations: {0}", (int) GameObject.Find("thermalIterations").GetComponent<Slider>().value); });
            GameObject.Find("hydraulicStrength").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("hydraulicStrengthText").GetComponent<Text>().text = string.Format("Hydraulic Erosion Strength: {0}", Math.Round(GameObject.Find("hydraulicStrength").GetComponent<Slider>().value, 1)); });
            GameObject.Find("hydraulicIterations").GetComponent<Slider>().onValueChanged.AddListener(delegate { GameObject.Find("hydraulicIterationsText").GetComponent<Text>().text = string.Format("Hydraulic Erosion Iterations: {0}", (int) GameObject.Find("hydraulicIterations").GetComponent<Slider>().value); });
            GameObject.Find("seedManual").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.Integer;
        }

        public void ValidateSeedInput() {
            string input = GameObject.Find("seedManual").GetComponent<InputField>().text;
            if (input.Length > 0) {
                long inputValue = 0;
                if (input.Contains("-")) {
                    if (input.Length == 1) {
                        return;
                    }
                    inputValue = -Convert.ToInt64(input.Substring(1, input.Length - 1));
                } else {
                    inputValue = Convert.ToInt64(input);
                }
                if (inputValue > Int32.MaxValue) {
                    GameObject.Find("seedManual").GetComponent<InputField>().text = Int32.MaxValue.ToString();
                } else if (inputValue < Int32.MinValue) {
                    GameObject.Find("seedManual").GetComponent<InputField>().text = Int32.MinValue.ToString();
                }
            }
        }

        //Read and stored the values from the UI elements for the settings.
        private void ReadSettings() {
            Info.TILES = (int) GameObject.Find("tiles").GetComponent<Slider>().value;
            Info.SEED_RANDOMISE = GameObject.Find("seedRandomise").GetComponent<Toggle>().isOn;
            Info.ISLAND_ROUND = GameObject.Find("islandRound").GetComponent<Toggle>().isOn;
            Info.NOISE_FREQUENCY_MULTIPLIER = (int) GameObject.Find("noiseFrequency").GetComponent<Slider>().value;
            Info.NOISE_OCTAVES = (int) GameObject.Find("noiseOctaves").GetComponent<Slider>().value;
            Info.EROSION_THERMAL_STRENGTH = (int) GameObject.Find("thermalStrength").GetComponent<Slider>().value;
            Info.EROSION_THERMAL_ITERATIONS = (int) GameObject.Find("thermalIterations").GetComponent<Slider>().value;
            Info.EROSION_HYDRAULIC_STRENGTH = Convert.ToSingle(Math.Round(GameObject.Find("hydraulicStrength").GetComponent<Slider>().value, 1));
            Info.EROSION_HYDRAULIC_ITERATIONS = (int) GameObject.Find("hydraulicIterations").GetComponent<Slider>().value;
            Info.SIMS_TEXTS = GameObject.Find("simsText").GetComponent<Toggle>().isOn;
        }

        public void Exit() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying) {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
            Application.Quit();
        }

        //Called via Unity when the program is ended. Tells threads to stop on their next check.
        private void OnDestroy() {
            if (worldGenerator != null) {
                worldGenerator.Abort();
            }
            if (Info.log != null) {
                Info.log.Abort();
            }
        }
    }
}