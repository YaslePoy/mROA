﻿using mROA.Abstract;
using mROA.Implementation;

namespace mROA;

public interface IContextRepository : IInjectableModule
{
    int ResisterObject(object o);
    void ClearObject(int id);
    object? GetObject(int id);
    T? GetObject<T>(int id);
    object GetSingleObject(Type type);
    int GetObjectIndex(object o);
}