namespace mutant_server
{
    public struct MyVector3
    {
        public float x, y, z;
        public MyVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public void reset()
        {
            x = y = z = 0;
        }
    }
}