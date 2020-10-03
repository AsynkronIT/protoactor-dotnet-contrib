﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using Proto.Remote;
using Wire;
using Wire.SerializerFactories;
using Wire.ValueSerializers;

namespace Proto.Serialization.Wire
{
    public class PidValueSerializer : ValueSerializerFactory
    {
        public override ValueSerializer BuildSerializer(
            Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping
        )
        {
            var x = new ObjectSerializer(type);
            typeMapping.TryAdd(type, x);

            static object Reader(Stream stream, DeserializerSession session)
            {
                var address = StringSerializer.ReadValueImpl(stream, session);
                var id      = StringSerializer.ReadValueImpl(stream, session);
                var pid     = new PID(address, id);
                session.TrackDeserializedObject(pid);

                return pid;
            }

            static void Writer(Stream stream, object o, SerializerSession session)
            {
                session.TrackSerializedObject(o);
                var pid = (PID) o;
                StringSerializer.WriteValueImpl(stream, pid.Address, session);
                StringSerializer.WriteValueImpl(stream, pid.Id, session);
            }

            x.Initialize(Reader, Writer);

            return x;
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => type == typeof(PID);

        public override bool CanSerialize(Serializer serializer, Type type) => type == typeof(PID);
    }

    public class WireSerializer : ISerializer
    {
        private readonly Serializer _serializer;

        public WireSerializer() : this(new Type[] { }) { }

        public WireSerializer(IEnumerable<Type> knownTypes)
            => _serializer = new Serializer(
                new SerializerOptions(
                    false, true, null,
                    new ValueSerializerFactory[] {new PidValueSerializer()}, knownTypes
                )
            );

        public ByteString Serialize(object obj)
        {

            var ms = new MemoryStream();
            _serializer.Serialize(obj, ms);
            var arr = ms.ToArray();
            ms.Dispose();
            return ByteString.CopyFrom(arr);
        }

        public object Deserialize(ByteString bytes, string typeName)
        {
            var arr = bytes.ToByteArray();
            var ms  = new MemoryStream(arr) {Position = 0};
            var obj = _serializer.Deserialize(ms);
            ms.Dispose();
            return obj;
        }

        public string GetTypeName(object message) => "";
    }
}
