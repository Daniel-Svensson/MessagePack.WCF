// #define MSGPACK_COMPATIBILITY
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace MessagePack.Wcf
{
    class XmlMessagePackSerializer : XmlObjectSerializer
    {
        private const string NULL_ATTRIBUTE_TRUE = "true";
        private const string NULL_ATTRIBUTE = "nil";
        // Element with base64 encoded data, this is the same name as for msgpack.wcf
        // in order to enable interoperarability
        protected const string MESSAGEPACK_ELEMENT = "msgpack";
        static readonly Dictionary<Type, XmlObjectSerializer> s_SerialierCache = new Dictionary<Type, XmlObjectSerializer>();
        protected readonly Type TargetType;

        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlObjectSerializer CreateGeneric(Type type)
        {
            lock (s_SerialierCache)
            {
                if (!s_SerialierCache.TryGetValue(type, out var xmlObjectSerializer))
                {
                    xmlObjectSerializer = (XmlObjectSerializer)Activator.CreateInstance(typeof(XmlMessagePackSerializer<>).MakeGenericType(type));
                    s_SerialierCache.Add(type, xmlObjectSerializer);
                }

                return xmlObjectSerializer;
            }
        }

        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlObjectSerializer Create(Type type)
        {
            return new XmlMessagePackSerializer(type);
        }

        /// <summary>
        /// Creates a new serializer for the given model and type
        /// </summary>
        /// <param name="type"></param>
        public XmlMessagePackSerializer(Type type)
        {
            TargetType = type ?? throw new ArgumentOutOfRangeException(nameof(type));
        }

        /// <summary>
        /// Ends an object in the output
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.WriteEndElement();
        }
        /// <summary>
        /// Begins an object in the output
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="graph"></param>
        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.WriteStartElement(MESSAGEPACK_ELEMENT);
        }

        /// <summary>
        /// Writes the body of an object in the output
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="graph"></param>
        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (graph == null)
            {
#if MSGPACK_COMPATIBILITY
                writer.WriteAttributeString(NULL_ATTRIBUTE, NULL_ATTRIBUTE_TRUE);
#endif
            }
            else
            {
                var buffer = MessagePackSerializer.Serialize(TargetType, graph);
                writer.WriteBase64(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Indicates whether this is the start of an object we are prepared to handle
        /// </summary>
        /// <param name="reader"></param>
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            reader.MoveToContent();

            return reader.NodeType == XmlNodeType.Element
                && string.Equals(MESSAGEPACK_ELEMENT, reader.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Reads the body of an object
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="verifyObjectName"></param>
        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            reader.MoveToContent();

            // True if element is self closing <Element />
            bool isEmptyElement = reader.IsEmptyElement;
#if MSGPACK_COMPATIBILITY
            bool isNull = string.Equals(reader.GetAttribute(NULL_ATTRIBUTE), NULL_ATTRIBUTE_TRUE, StringComparison.Ordinal);
#else
            bool isNull = isEmptyElement;
#endif

            if (verifyObjectName)
                reader.ReadStartElement(MESSAGEPACK_ELEMENT);
            else
                reader.ReadStartElement();

            if (isNull)
            {
                if (!isEmptyElement)
                    reader.ReadEndElement();
                return null;
            }

            // TODO: read directly into reusable buffer instead by using other overload
            var content = reader.ReadContentAsBase64();
            reader.ReadEndElement();

            return MessagePackSerializer.Deserialize(TargetType, content);
        }
    }
}
