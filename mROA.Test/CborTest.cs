using System;
using System.Collections.Generic;
using System.Linq;
using mROA.Cbor;

namespace mROA.Test;

public class CborTest
{
    private ComplexTestObject _complexTestObject;
    private IContextualSerializationToolKit _serializationToolKit;
    private BasicCollectionElement _basicCollectionElement;

    [SetUp]
    public void Setup()
    {
        _basicCollectionElement = new() { A = 567565, B = "test text", C = 2.781f };

        _complexTestObject = new ComplexTestObject
        {
            IntValue = 123,
            DoubleValue = 3.14159,
            StringValue = "abc",
            EnumValue = TestEnum.X,
            CollectionElements =
            [
                _basicCollectionElement,
                new BasicCollectionElement { A = 8_000_000, B = "Fi number", C = 1.618f }
            ],
            IntArray = [1, 4, 8, 16, 87]
        };
        _serializationToolKit = new CborSerializationToolkit();
    }

    [Test]
    public void BasicOnly()
    {
        var value = 123;
        var data = _serializationToolKit.Serialize(value, null);

        var deserialize = _serializationToolKit.Deserialize<int>(data, null);
        Assert.That(value, Is.EqualTo(deserialize));
    }

    [Test]
    public void ComplexFlat()
    {
        var value = _basicCollectionElement;
        var data = _serializationToolKit.Serialize(value, null);
        var deserialize = _serializationToolKit.Deserialize<BasicCollectionElement>(data, null);
        Assert.That(value, Is.EqualTo(deserialize));
    }

    [Test]
    public void ComplexFull()
    {
        var value = _complexTestObject;
        var data = _serializationToolKit.Serialize(value, null);
        var deserialize = _serializationToolKit.Deserialize<ComplexTestObject>(data, null);
        Assert.That(value, Is.EqualTo(deserialize));
    }

    public void SharedObject()
    {
    }


    private class ComplexTestObject
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public string StringValue { get; set; }
        public TestEnum EnumValue { get; set; }
        public int[] IntArray { get; set; }
        public List<BasicCollectionElement> CollectionElements { get; set; }

        protected bool Equals(ComplexTestObject other)
        {
            return IntValue == other.IntValue && DoubleValue.Equals(other.DoubleValue) && StringValue == other.StringValue && IntArray.SequenceEqual(other.IntArray) && CollectionElements.SequenceEqual(other.CollectionElements);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ComplexTestObject)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IntValue, DoubleValue, StringValue, IntArray, CollectionElements);
        }
    }

    private class BasicCollectionElement
    {
        public int A { get; set; }
        public string B { get; set; }
        public float C { get; set; }

        protected bool Equals(BasicCollectionElement other)
        {
            return A == other.A && B == other.B && C.Equals(other.C);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BasicCollectionElement)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B, C);
        }
    }
    
    public enum TestEnum
    {
        X = -5, Y, Z
    }
}