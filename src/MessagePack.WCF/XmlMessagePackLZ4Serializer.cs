using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace MessagePack.Wcf
{
    class XmlMessagePackLZ4Serializer : XmlMessagePackSerializer
    {
        /// <summary>
        /// Creates a new serializer for the given model and type
        /// </summary>
        /// <param name="type"></param>
        public XmlMessagePackLZ4Serializer(Type type)
            : base(type)
        {
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
                writer.WriteAttributeString("nil", "true");
            }
            else
            {
                var buffer = LZ4MessagePackSerializer.NonGeneric.Serialize(TargetType, graph);
                writer.WriteBase64(buffer, 0, buffer.Length);
            }
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

            bool isSelfClosed = reader.IsEmptyElement;
            bool isNil = "true".Equals(reader.GetAttribute("nil"), StringComparison.Ordinal);

            reader.ReadStartElement(MESSAGEPACK_ELEMENT);

            // explicitly null
            if (isNil)
            {
                if (!isSelfClosed)
                    reader.ReadEndElement();
                return null;
            }

            if (isSelfClosed) // no real content
            {
                return LZ4MessagePackSerializer.NonGeneric.Deserialize(TargetType, Array.Empty<byte>());
            }

            // TODO: read directly into reusable buffer instead by using other overload
            var content = reader.ReadContentAsBase64();
            reader.ReadEndElement();

            return LZ4MessagePackSerializer.NonGeneric.Deserialize(TargetType, content);
        }
    }
}
