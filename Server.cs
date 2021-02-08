using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ThreadedSocketServer
{
    public static IPHostEntry ipHostInfo;
    public static IPAddress ipAddress;
    public static IPEndPoint localEndPoint;
    public static Socket listener;

    public static UdpClient listenerUDP;

    public static int clientMax;
    public static Thread[] listeningThreads;
    public static CLIENT[] clientsArr;

    public static void InitializeNetwork(int maxClients)
    {
        try
        {
            ipHostInfo = Dns.GetHostEntry("127.0.0.1");
            ipAddress = ipHostInfo.AddressList[0];
            localEndPoint = new IPEndPoint(ipAddress, 11000);
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            listenerUDP = new UdpClient(12000);

            clientMax = maxClients;
            listeningThreads = new Thread[clientMax];
            clientsArr = new CLIENT[clientMax];
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            Console.Read();
        }

    }

    public class CLIENT
    {
        public Socket socketTCP;
        public bool active;
        public CLIENT()
        {
            socketTCP = null;
            active = false;
        }
    }

    public class ThreadSafeClientParameters
    {
        public string MSG {get; set;}
        public int Index { get; set; }
        public ThreadSafeClientParameters(string msg, int index)
        {
            MSG = msg;
            Index = index;
        }
    }

    public static void StartListening()
    {
        string data = null;
        byte[] bytes = new Byte[1024];

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
            Console.WriteLine("Client connected : {0}", data);

            int clientIndex = -1;
            for (int i = 0; i < clientMax; i++)
            {
                if (!clientsArr[i].active)
                {
                    clientsArr[i].socketTCP = handler;
                    clientsArr[i].active = true;
                    clientIndex = i;
                    break;
                }
            }

            if (clientIndex == -1)
            {
                handler.Send(Encoding.ASCII.GetBytes("Sorry, the server is full right now."));
                break;
            }
            else
            {
                handler.Send(Encoding.ASCII.GetBytes("You're connected!"));
            }

            while (true) // continue to listen for client input
            {
                data = null;
                while (true)
                {
                    int bytesRec = 0;
                    try
                    {
                        bytesRec = handler.Receive(bytes);
                    }
                    catch 
                    {
                        Console.WriteLine("Client disconnected.");
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        clientsArr[clientIndex].socketTCP = null;
                        clientsArr[clientIndex].active = false;
                        goto DISCONNECTED_CLIENT;
                    }

                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                for (int i = 0; i < clientMax; i++)
                {
                    if (i != clientIndex && clientsArr[i].active)
                    {
                        ThreadSafeClientParameters tscp = new ThreadSafeClientParameters(data, i);
                        Thread t = new Thread(() => SendThread(tscp));
                        t.Start();
                    }
                }
            }

        DISCONNECTED_CLIENT:;
            
        }
    }

    public static void SendThread(ThreadSafeClientParameters tscp)
    {
        try
        {
            clientsArr[tscp.Index].socketTCP.Send(Encoding.ASCII.GetBytes(tscp.MSG));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static void ListenUDPThread()
    {
        byte[] bytes;
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 12000);
        while (true)
        {
            bytes = listenerUDP.Receive(ref groupEP);
            Console.WriteLine("Recieved UDP: " + Encoding.ASCII.GetString(bytes, 0, bytes.Length));
        }
    }

    public static int Main(String[] args)
    {
        InitializeNetwork(10); // initializing the network

        for (int i = 0; i < clientMax; i++) // initializing client array
        {
            clientsArr[i] = new CLIENT();
        }

        for (int i = 0; i < clientMax; i++)
        {
            listeningThreads[i] = new Thread(StartListening); // a tcp thread for each client
            listeningThreads[i].Start();
        }

        Thread UDPThread = new Thread(ListenUDPThread); // a single UDP thread for everyone
        UDPThread.Start();

        Console.Read();
        Console.WriteLine("press any key to close...");

        return 0;
    }
}
