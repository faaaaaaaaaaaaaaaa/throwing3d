namespace PPSEngine_one
{
    public sealed class GetSetAttribute : UnityEngine.PropertyAttribute
    {
        public readonly string name;
        public bool dirty;

        public GetSetAttribute(string name)
        {
            this.name = name;
        }
    }
}
