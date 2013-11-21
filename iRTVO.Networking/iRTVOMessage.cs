using NetworkCommsDotNet;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace iRTVO.Networking
{
    public class iRTVORemoteEvent
    {
        public bool Cancel = false;
        public bool Handled = false;
        public bool Forward = false;
        public iRTVOMessage Message { get; private set; }

        public iRTVORemoteEvent(iRTVOMessage m)
        {
            Cancel = false;
            Message = m;
        }
    }

    /// <summary>
    /// A wrapper class for the messages that we intend to send and recieve.
    /// The [ProtoContract] attribute informs NetworkComms .Net that we intend to
    /// serialise (turn into bytes) this object. At the base level the
    /// serialisation is performed by protobuf.net.
    /// </summary>
    [ProtoContract]
    public class iRTVOMessage 
    {
        /// <summary>
        /// The source identifier of this ChatMessage.
        /// We use this variable as the constructor for the ShortGuid.
        /// The [ProtoMember(1)] attribute informs the serialiser that when
        /// an object of type ChatMessage is serialised we want to include this variable
        /// </summary>
        [ProtoMember(1)]
        public string Source { get; set; }        

        /// <summary>
        /// The name of the source of this ChatMessage.
        /// We use shorthand declaration, get and set.
        /// The [ProtoMember(2)] attribute informs the serialiser that when
        /// an object of type ChatMessage is serialised we want to include this variable
        /// </summary>
        [ProtoMember(2)]
        public string Command { get; private set; }

        /// <summary>
        /// The actual message.
        /// </summary>
        [ProtoMember(3,DynamicType=true)]
        public string[] Arguments { get; private set; }

       

        /// <summary>
        /// We must include a private constructor to be used by the deserialisation step.
        /// </summary>
        private iRTVOMessage() { }

        /// <summary>
        /// Create a new ChatMessage
        /// </summary>
        /// <param name="sourceIdentifier">The source identifier</param>
        /// <param name="sourceName">The source name</param>
        /// <param name="message">The message to be sent</param>
        /// <param name="messageIndex">The index of this message</param>
        public iRTVOMessage(string Source,string Command, params object[] Arguments)
        {
            this.Source = Source;
            this.Command = Command;

            string[] args = new string[Arguments.Length];
            for (int i = 0; i < Arguments.Length; i++)
                args[i] = Convert.ToString(Arguments[i]);
            this.Arguments = args;            
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendFormat("iRTVOMessage(\"{0}\",\"{1}\"", Source, Command);
            if (  ( Arguments == null ) || ( Arguments.Count() == 0))
                b.Append(");");
            else
            {
                b.Append(", { ");
                foreach( var arg in Arguments )
                    b.AppendFormat("\"{0}\",",arg);
                b.Append(" });");
            }
            return b.ToString();
        }


        
    }
}
