﻿using System.Reflection;
using mROA.Abstract;
using mROA.Implementation.Attributes;

namespace mROA.Implementation;

public class MethodRepository : IMethodRepository
{
    private readonly List<MethodInfo> _methods = [];

    public MethodInfo GetMethod(int id)
    {
        if (_methods.Count <= id)
            throw new Exception("Method such registered method");

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

    public void CollectForAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(i => i.IsInterface && i.GetCustomAttributes(typeof(SharedObjectInterfaceAttribute), true).Length > 0);
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods())
                RegisterMethod(method);
        }
    }
    public void Inject<T>(T dependency)
    {
        
    }
}