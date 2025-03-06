using System;
using Example.Shared;

namespace Example.Backend
{
    public class CsTest
    {
        public T FinalCasted<T>(IDataList<T> list, int index)
        {
            return list.Get(index);
        }

        public object NonCasted(object list, int index)
        {
            return FinalCasted(list as IDataList<object>, index);
        }
    }
}