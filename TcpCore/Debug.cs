using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TcpCore
{
    public enum DebugMessageType { General, Error }

    public static class Debug
    {
        public static event EventHandler<MessageWrittenEventArgs> MessageWritten;

        public static void Message(string message)
        {
            MessageWritten?.Invoke(null, new MessageWrittenEventArgs(message, DebugMessageType.General));
        }

        public static void Error(string message)
        {
            MessageWritten?.Invoke(null, new MessageWrittenEventArgs(message, DebugMessageType.Error));
        }
    }

    public class MessageWrittenEventArgs : EventArgs
    {
        public string Message { get; set; }
        public DebugMessageType Type { get; set; }

        public MessageWrittenEventArgs(string message, DebugMessageType type)
        {
            Message = message;
            Type = type;
        }
    }
}
