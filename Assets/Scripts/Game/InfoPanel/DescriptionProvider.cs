namespace Game.InfoPanel
{
    public abstract class DescriptionProvider
    {
        string? lastDescription_;

        public bool HasDescriptionChanged(out string description)
        {
            description = GenerateDescription();
            if (lastDescription_ == description)
                return false;

            lastDescription_ = description;
            return true;
        }

        protected abstract string GenerateDescription();
    }
}
