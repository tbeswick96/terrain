using Assets.Scripts.Utility;
using UnityEngine;

namespace Assets.Scripts.Terrain {

    /*
     * Contains global variables and constants.
     */
    public class Info {

        //Logging
        public static ThreadedLogger log;

        //Generation
        public static System.Random RANDOM;
        public static int TOTAL_TILES;
        public const int HEIGHTMAP_SIZE = 129;
        public const int SIZE_MULTIPLIER = 4;
        public static int OFFSET;
        public const float NOISE_FREQUENCY_BASE = 0.001f;

        //Terrain assets
        public static Material MATERIAL;
        public static Texture2D TEXTURE_GRASS;
        public static Texture2D TEXTURE_GRASS_NORMAL;
        public static Texture2D TEXTURE_EARTH;
        public static Texture2D TEXTURE_EARTH_NORMAL;
        public static Texture2D TEXTURE_ROCK;
        public static Texture2D TEXTURE_ROCK_NORMAL;
        public static Texture2D TEXTURE_SAND;
        public static Texture2D TEXTURE_SAND_NORMAL;
        public static Texture2D TEXTURE_DIRT;
        public static Texture2D TEXTURE_DIRT_NORMAL;
        public static Texture2D GRASS_1;
        public static Texture2D GRASS_2;
        public static Texture2D GRASS_3;
        public static Texture2D GRASS_4;
        public static GameObject TREE_OAK;
        public static GameObject TREE_PINE;
        public static SplatPrototype[] TEXTURE_PROTOTYPES;
        public static DetailPrototype[] DETAIL_PROTOTYPES;
        public static TreePrototype[] TREE_PROTOTYPES;

        //Loading screen messages
        public static string[][] LOADING_TEXTS = new string[10][] {
            new string[2]{ "Calculating Heights", "How High Can We Go?" }, //0
            new string[2]{ "Eroding (Thermal)", "Expanding Ice Crystals" }, //1
            new string[2]{ "Eroding (Hydraulic)", "Waiting Thousands Of years" }, //2
            new string[2]{ "Finding River Paths", "Streaming River Nodes" }, //3
            new string[2]{ "Carving Rivers", "Cascading Water Channels" }, //4
            new string[2]{ "Performing Detail Pass", "Thinking A Little" }, //5
            new string[2]{ "Applying World ({0}/{1})", "Adding Height Differentials ({0}/{1})" }, //6
            new string[2]{ "Growing Grass ({0}/{1})", "Photosynthesising Turf ({0}/{1})" }, //7
            new string[2]{ "Planting Trees ({0}/{1})", "Gathering The Ents ({0}/{1})" }, //8
            new string[2]{ "Generation Complete", "Let There Be Light" } //9
        };

        //Settings
        public static int TILES = 5; //Controls number of terrain tiles.
        public static int SEED = 0; //Controls seed value used for random and noise calculations.
        public static bool SEED_RANDOMISE = true; //Controls whether the seed is randomised each time generation is run.
        public static bool ISLAND_ROUND = true; //Controls whether the island created is round or square.
        public static int NOISE_FREQUENCY_MULTIPLIER = 1; //Controls the frequency of the noise set.
        public static int NOISE_OCTAVES = 8; //Controls how many octaves are used in the noise.
        public static int EROSION_THERMAL_STRENGTH = 18; //Controls how strong the inverse thermal erosion is.
        public static int EROSION_THERMAL_ITERATIONS = 50; //Controls how many iterations of inverse thermal erosion are performed.
        public static float EROSION_HYDRAULIC_STRENGTH = 1; //Controls how strong the hydraulic erosion is.
        public static int EROSION_HYDRAULIC_ITERATIONS = 25; //Controls how many iterations of hydraulic erosion are performed.
        public static bool SIMS_TEXTS = false; //Controls whether Sims-like loading texts are used. (Sims games by EA Maxis use comedic loading texts. See 'loading messages' https://www.gamefaqs.com/pc/561176-simcity-4/faqs/22135 )
        public static LogLevel LOG_LEVEL = LogLevel.ERROR;
        public static int LOG_VERBOSITY = 0;
    }
}
