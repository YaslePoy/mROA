namespace mROA.Abstract
{
    public interface IStorage<T>
    {
        T GetValue(int index);
        int GetIndex(T value);
        int Place(T value);
        void Free(int index);
    }
}