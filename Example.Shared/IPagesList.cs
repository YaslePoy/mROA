using mROA.Implementation.Attributes;

namespace Example.Shared
{
    [SharedObjectInterface]
    public partial interface IPagesList : IDataList<IPage>
    {
    }
}