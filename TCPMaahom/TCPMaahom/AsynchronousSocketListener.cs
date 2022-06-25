using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace TCPMaahom
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Client socket.
        public Socket workSocket = null;
    }

    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static int Port = 11005;

        static bool debug = true;

        public static List<string> _log = new List<string>();

        public static Socket listener;

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

            // Create a TCP/IP socket.  
            listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1000);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  

                try
                {
                    byte[] array2 = new byte[bytesRead];
                    Array.Copy(state.buffer, array2, bytesRead);

                    if (debug)
                    {
                        var str = BitConverter.ToString(array2).Replace("-", "");
                        if (_log.Count > 10)
                        {
                            _log.Clear();
                        }
                        _log.Add(str);
                    }

                    var firstByte = state.buffer[0];
                    var lastByte = state.buffer[bytesRead - 1];

                    // Check for end-of-file tag. If it is not there, read
                    // more data.

                    if (firstByte == 0x7e && lastByte == 0x7e)
                    {
                        // All the data has been read from the
                        // client. Display it on the console.  
                        // Echo the data back to the client.  

                        try
                        {
                            var response = JimiV001.ParseDate(array2);
                            Send(handler, response);
                        }
                        catch
                        {

                        }

                    }
                }
                catch
                {

                }
                
                //else
                //{
                    // Not all data received. Get more.
                try
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
new AsyncCallback(ReadCallback), state);
                }
                catch
                {

                }

                //}
            }
        }

        private static void Send(Socket handler, byte[] byteData)
        {
            if (debug)
            {
                try
                {
                    var str = BitConverter.ToString(byteData).Replace("-", "");
                    _log.Add(str);
                }
                catch
                {

                }

            }
            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


    }
}
