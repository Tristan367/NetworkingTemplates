using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class ThreadedSocketListener
{
    public static IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
    public static IPAddress ipAddress = ipHostInfo.AddressList[0];
    public static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
    public static Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    public static int clientMax = 10;
    public static List<Socket> clients = new List<Socket>();
    public static List<Thread> listeningThreads = new List<Thread>();

    public static void StartListening()
    {
        string data = null;
        byte[] bytes = new Byte[1024];
        Thread[] sendThreads = new Thread[clientMax];

        while (true)
        {
            Console.WriteLine("Waiting for a connection...");
            Socket handler = listener.Accept();
            data = null;

            while (true)
            {
                int bytesRec = handler.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (data.IndexOf("<EOF>") > -1)
                {
                    break;
                }
            }

            Console.WriteLine("Text received : {0}", data);
            byte[] msg = Encoding.ASCII.GetBytes("You're connected!");
            handler.Send(msg);

            clients.Add(handler);

            
            while (true) // continue to listen for client input
            {
                data = null;
                while (true)
                {
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                for (int i = 0; i < clients.Count; i++)
                {

                    clients[i].Send(Encoding.ASCII.GetBytes(data));

                    /*
                    if (sendThreads[i] != null)
                    {
                        sendThreads[i].Join();
                    }

                    sendThreads[i] = new Thread(() => SendThread(i, data));
                    sendThreads[i].Start();
                    sendThreads[i].Join();
                    */
                }
            }
            //handler.Shutdown(SocketShutdown.Both);
            //handler.Close();
        }
    }

    public static void SendThread(int c, string message)
    {
        clients[c].Send(Encoding.ASCII.GetBytes(message));
    }

    public static int Main(String[] args)
    {
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        
        for (int i = 0; i < clientMax; i++)
        {
            listeningThreads.Add(new Thread(StartListening));
            listeningThreads[i].Start();
        }

        Console.Read();
        Console.WriteLine("press any key to close...");

        return 0;
    }
}
