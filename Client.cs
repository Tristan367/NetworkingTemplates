using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SynchronousSocketClient
{

    public static string dataFromServer;

    public static IPHostEntry ipHostInfo = Dns.GetHostEntry("127.0.0.1");
    public static IPAddress ipAddress = ipHostInfo.AddressList[0];
    public static IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
    public static Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    public static void StartClient()
    {
        byte[] bytes = new byte[1024];

        try
        {
            sender.Connect(remoteEP);
            Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

            byte[] msg = Encoding.ASCII.GetBytes("Hello! <EOF>");
            int bytesSent = sender.Send(msg);

            int bytesRec = sender.Receive(bytes);
            Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));

            Thread sendThread = new Thread(SendThread);
            sendThread.Start();

            while (true) // and continue listening to the server
            {
                bytesRec = sender.Receive(bytes);
                Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
            }

            //sender.Shutdown(SocketShutdown.Both);
            //sender.Close();

        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
        }
        catch (SocketException se)
        {
            Console.WriteLine("SocketException : {0}", se.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception : {0}", e.ToString());
        }

    }

    public static void SendThread()
    {
        while (true)
        {
            sender.Send(Encoding.ASCII.GetBytes(Console.ReadLine() + "<EOF>"));
        }
    }

    public static int Main(String[] args)
    {
        Thread connectAndListenThread = new Thread(StartClient);
        connectAndListenThread.Start();

        return 0;
    }
}