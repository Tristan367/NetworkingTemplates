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

    public static Socket senderUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    public static IPAddress broadcast = IPAddress.Parse("127.0.0.1"); // needs a separate ipAddress instance
    public static IPEndPoint ep = new IPEndPoint(broadcast, 11000);

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

            Thread UDPsenderThread = new Thread(UDPSenderThread);
            UDPsenderThread.Start();

            while (true) // and continue listening to the server
            {
                bytesRec = sender.Receive(bytes);
                Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
            }
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
            string str = Console.ReadLine();
            sender.Send(Encoding.ASCII.GetBytes(str + "<EOF>"));
        }
    }

    public static void DiconnectTCP()
    {
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();
    }


    public static void UDPSenderThread()
    {
        byte[] sendbuf = Encoding.ASCII.GetBytes("UDP TEST");
        while (true)
        {
            senderUdp.SendTo(sendbuf, ep);
            Thread.Sleep(1000);
        }
    }
    

    public static int Main(String[] args)
    {
        Thread connectAndListenThread = new Thread(StartClient);
        connectAndListenThread.Start();

        //Console.Read();
        //Console.WriteLine("press any key to close...");
        return 0;
    }
}