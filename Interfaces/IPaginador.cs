
namespace Tracker.Interfaces
{
    public interface IPaginador
    {

        int PageSize { get; set; }
        int Skip { get; set; }

    }
}
