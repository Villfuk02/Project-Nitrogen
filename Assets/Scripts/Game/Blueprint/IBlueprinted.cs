namespace Game.Blueprint
{
    public interface IBlueprinted
    {
        public Blueprint Blueprint { get; }
        public Blueprint OriginalBlueprint { get; }
        public void InitBlueprint(Blueprint blueprint);
        public void Placed();
    }
}
