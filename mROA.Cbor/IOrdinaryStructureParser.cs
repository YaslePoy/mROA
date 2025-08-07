using System;
using System.Formats.Cbor;
using mROA.Abstract;
using mROA.Implementation;
using mROA.Implementation.CommandExecution;

namespace mROA.Cbor
{
    public interface IOrdinaryStructureParser
    {
        void Write(CborWriter writer, object value, IEndPointContext context, CborSerializationToolkit serialization);
        object Read(CborReader reader, IEndPointContext context, CborSerializationToolkit serialization);
    }

    public class CallRequestParser : IOrdinaryStructureParser
    {
        public void Write(CborWriter writer, object value, IEndPointContext context, CborSerializationToolkit serialization)
        {
            var v = (CallRequest)value;
            writer.WriteStartArray(4);
            v.Id.WriteToCborInline(writer);
            // writer.WriteByteString(v.Id.ToByteArray());
            writer.WriteInt32(v.CommandId);
            writer.WriteUInt64(v.ObjectId.Flat);
            serialization.WriteData(v.Parameters, writer, context);
            writer.WriteEndArray();
        }

        public object Read(CborReader reader, IEndPointContext context, CborSerializationToolkit serialization)
        {
            reader.ReadStartArray();
            var value = new CallRequest
            {
                Id = new RequestId(reader.ReadByteString()),
                CommandId = reader.ReadInt32(),
                ObjectId = (ComplexObjectIdentifier)ComplexObjectIdentifierParser.Instance.Read(reader, context, serialization),
                Parameters = serialization.ReadData(reader, typeof(object[]), context) as object[]
            };
            reader.ReadEndArray();
            return value;
        }
    }

    public class ComplexObjectIdentifierParser : IOrdinaryStructureParser
    {
        public static readonly ComplexObjectIdentifierParser Instance = new();
        public void Write(CborWriter writer, object value, IEndPointContext context, CborSerializationToolkit serialization)
        {
            writer.WriteUInt64(((ComplexObjectIdentifier)value).Flat);
        }

        public object Read(CborReader reader, IEndPointContext context, CborSerializationToolkit serialization)
        {
            var value = new ComplexObjectIdentifier { Flat = reader.ReadUInt64() };
            return value;
        }
    }

    public class FinalCommandExecutionParser : IOrdinaryStructureParser
    {
        public void Write(CborWriter writer, object value, IEndPointContext context, CborSerializationToolkit serialization)
        {
            var v = (FinalCommandExecution<object>)value;
            writer.WriteStartArray(2);
            // writer.WriteByteString(v.Id.ToByteArray());
            v.Id.WriteToCborInline(writer);
            serialization.WriteData(v.Result, writer, context);
            writer.WriteEndArray();
        }

        public object Read(CborReader reader, IEndPointContext context, CborSerializationToolkit serialization)
        {
            reader.ReadStartArray();
            var result = new FinalCommandExecution<object>
            {
                Id = new RequestId(reader.ReadByteString()),
                Result = serialization.ReadData(reader, typeof(object), context),
            };
             reader.ReadEndArray();
             return result;
        }
    }
    
    public class FinalCommandExecutionResultlessParser : IOrdinaryStructureParser
    {
        public void Write(CborWriter writer, object value, IEndPointContext context, CborSerializationToolkit serialization)
        {
            var v = (FinalCommandExecution)value;
            writer.WriteStartArray(1);
            // writer.WriteByteString(v.Id.ToByteArray());
            v.Id.WriteToCborInline(writer);
            writer.WriteEndArray();
        }

        public object Read(CborReader reader, IEndPointContext context, CborSerializationToolkit serialization)
        {
            reader.ReadStartArray();
            var result = new FinalCommandExecution
            {
                Id = new RequestId(reader.ReadByteString())
            };
            reader.ReadEndArray();
            return result;
        }
    }
}