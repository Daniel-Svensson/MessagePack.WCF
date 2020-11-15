using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace MessagePack.Wcf
{
    public class MessagePackSerializerContractBehaviorAttribute : Attribute, IContractBehavior
    {
        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            this.ReplaceSerializerOperationBehavior(contractDescription);
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            this.ReplaceSerializerOperationBehavior(contractDescription);
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
            foreach (OperationDescription operation in contractDescription.Operations)
            {
                foreach (MessageDescription message in operation.Messages)
                {
                    this.ValidateMessagePartDescription(message.Body.ReturnValue);
                    foreach (MessagePartDescription part in message.Body.Parts)
                    {
                        this.ValidateMessagePartDescription(part);
                    }

                    foreach (MessageHeaderDescription header in message.Headers)
                    {
                        this.ValidateCustomSerializableType(header.Type);
                    }
                }
            }
        }

        private void ValidateMessagePartDescription(MessagePartDescription part)
        {
            if (part != null)
            {
                this.ValidateCustomSerializableType(part.Type);
            }
        }

        private void ValidateCustomSerializableType(Type type)
        {
            // TODO: Throw if type is not compatible with messagepack?
        }

        /// <summary>
        /// Replaces the default DataContractSerializerOperationBehavior with 
        /// an instance of DataContractSerializerOperationBehavior
        /// </summary>
        /// <param name="contract"></param>
        private void ReplaceSerializerOperationBehavior(ContractDescription contract)
        {
            foreach (OperationDescription od in contract.Operations)
            {
                for (int i = 0; i < od.Behaviors.Count; i++)
                {

                    DataContractSerializerOperationBehavior dcsob = od.OperationBehaviors[i] as DataContractSerializerOperationBehavior;
                    if (dcsob != null)
                    {
                        // TODO: Copy settings from DataContractSerializerOperationBehavior
                        od.OperationBehaviors[i] = new MessagePackSerializerOperationBehavior(od, dcsob);
                    }
                }
            }
        }

        class MessagePackSerializerOperationBehavior : DataContractSerializerOperationBehavior
        {
            public MessagePackSerializerOperationBehavior(OperationDescription operation, DataContractSerializerOperationBehavior dataContractSerializerOperationBehavior)
                : base(operation, dataContractSerializerOperationBehavior.DataContractFormatAttribute)
            {
                // TODO: Look into copyging some setttings from DataContractSerializerOperationBehavior 
                // * DataContractResolver
                // * MaxItemsInObjectGraph
            }

            public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
            {
                // TODO: Create type of serialier based on setting (Lz4 compression, and generic type serializer)
                // TODO: Look into using knowntypes for whitelisting
                return new XmlMessagePackSerializer(type);
            }

            public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
            {
                return CreateSerializer(type, name.Value, ns.Value, knownTypes);
            }
        }
    }

}
