using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.ServiceModel.Channels;

namespace P2P
{
    
    [ServiceContract]
    public interface IP2PService
    {
        [OperationContract]
        string GetName();

        [OperationContract(IsOneWay = true)]
        void SendMessage(string message, string from);
        [OperationContract(IsOneWay = true)]
        void SendMessageByte(byte[] message, string from);
    }
}