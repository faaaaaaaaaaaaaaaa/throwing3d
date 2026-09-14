namespace PPSEngine_one
{
    public sealed class MinAttribute : UnityEngine.PropertyAttribute
    {
        public readonly float min;

        public MinAttribute(float min)
        {
            this.min = min;
        }
    }
}
