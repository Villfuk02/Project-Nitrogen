using System.Threading.Tasks;

namespace Data.Loader
{
    public interface ILoader
    {
        Task LoadAllAsync();
    }
}
