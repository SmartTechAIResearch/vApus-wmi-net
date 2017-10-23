/*
 * Copyright 2017 (c) Sizing Servers Lab
 * University College of West-Flanders, Department GKG
 * 
 * Author(s):
 *    Dieter Vandroemme
 */
using RandomUtils;
using System.Net.Sockets;

namespace vApus_wmi_net {
    /// <summary>
    /// Class that helps sending and receiving '\n' delimited text messages.
    /// </summary>
    internal static class SocketHelper {
        /// <summary>
        /// Reads the specified socket if Server is running and the socket is connected.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        public static string Read(Socket socket) {
            string read = "";
            while (Server.Running && socket.Connected && !read.EndsWith("\n")) {
                var buffer = new byte[socket.ReceiveBufferSize];
                socket.Receive(buffer);
                read += SerializationHelper.Decode(buffer, SerializationHelper.TextEncoding.UTF8);
                if (read.Length == 0) read = "\n"; //Connection error.
            }

            //The last char is a \n
            if (read.Length != 0)
                read = read.Substring(0, read.Length - 1);

            return read;
        }
        /// <summary>
        /// Writes the specified socket if Server is running and the socket is connected.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="write">The write.</param>
        public static void Write(Socket socket, string write) {
            if (Server.Running && socket.Connected) {
                if (!write.EndsWith("\n")) write += "\n";
                socket.Send(SerializationHelper.Encode(write, SerializationHelper.TextEncoding.UTF8));
            }
        }
    }
}
