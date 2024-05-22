namespace Utils
{
    /// <summary>
    /// Used to store references to unmanaged types
    /// </summary>
    public class Box<T> where T : unmanaged
    {
        public T value;

        public Box(T value)
        {
            this.value = value;
        }
    }
}