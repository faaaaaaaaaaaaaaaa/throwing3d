namespace PPSEngine_one
{
    public sealed class TrackballAttribute : UnityEngine.PropertyAttribute
    {
        public readonly string method;

        public TrackballAttribute(string method)
        {
            this.method = method;
        }
    }
}
