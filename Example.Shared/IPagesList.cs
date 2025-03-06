using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public interface IPagesList : IDataList<IPage>
    {
    }
}