namespace mROA;

public interface IContextRepository
{
    int ResisterObject(object o);
    void ClearObject(int id);
    object GetObject(int id);
}