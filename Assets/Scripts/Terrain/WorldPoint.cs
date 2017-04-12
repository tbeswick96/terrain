namespace Assets.Scripts.Terrain {

    /*
     * Worldpoint object. Contains the x,y coord of the point, its z (height) value, water value, and whether the point is a river point.
     */
    public class WorldPoint {
        public int x, y;
        public float z = 0;
        public float waterAmount = 0;
        public bool isRiver = false;
    }
}
