﻿using System.Reflection;

namespace mROA.Implementation;

public class MethodRepository : IMethodRepository
{
    private List<MethodInfo> _methods;
    
    public MethodInfo GetMethod(int id)
    {
        if (_methods.Count >= id)
            return null;
        
        return _methods[id];
    }

    public int RegisterMethod(MethodInfo method)
    {
        _methods.Add(method);
        return _methods.Count - 1;
    }

    public IEnumerable<MethodInfo> GetMethods()
    {
        return _methods;
    }
}