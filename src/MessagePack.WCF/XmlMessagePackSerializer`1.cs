using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Xml;

namespace MessagePack.Wcf
{
    class XmlMessagePackSerializer<T> : XmlMessagePackSerializer
    {
        public XmlMessagePackSerializer()
            : base(typeof(T))
        { }

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
                var buffer = MessagePackSerializer.Serialize<T>((T)graph);
                writer.WriteBase64(buffer, 0, buffer.Length);
            }
        }

        private const int MaxBufferSize = 1024 * 1024;
        static readonly BufferManager s_bufferManager = BufferManager.CreateBufferManager(10 * 1024 * 1024, MaxBufferSize);

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
                return MessagePackSerializer.Deserialize<T>(Array.Empty<byte>());
            }


            if (reader.TryGetBase64ContentLength(out var len) && len <= MaxBufferSize)
            {
                var bytes = s_bufferManager.TakeBuffer(len);
                try
                {
                    int position = 0;
                    int read;

                    do
                    {
                        read = reader.ReadContentAsBase64(bytes, position, len - position);
                        position += read;
                    } while (read > 0);

                    // TODO: Assert position = len?
                    reader.ReadEndElement();

                    return MessagePackSerializer.Deserialize<T>(new ArraySegment<byte>(bytes, 0, position));
                }
                finally
                {
                    s_bufferManager.ReturnBuffer(bytes);
                }
            }
            else
            {
                var content = reader.ReadContentAsBase64();
                reader.ReadEndElement();

                return MessagePackSerializer.Deserialize<T>(content);
            }
        }
    }
}
