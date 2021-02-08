using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ThreadedSocketListener
{
    public static IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
    public static IPAddress ipAddress = ipHostInfo.AddressList[0];
    public static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
    public static Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

    public static UdpClient listenerUDP = new UdpClient(11000);

    public static int clientMax = 10;
    public static Thread[] listeningThreads = new Thread[clientMax];
    public static CLIENT[] clientsArr = new CLIENT[clientMax];

    public class CLIENT
    {
        public Socket socketTCP;
        public bool active = false;
    }

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
                    catch // handling the client unexpectedly disconnecting
                    {
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
                        clientsArr[i].socketTCP.Send(Encoding.ASCII.GetBytes(data));
                    }

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

        DISCONNECTED_CLIENT:;
            
        }
    }

    public static void SendThread(int c, string message)
    {
        clientsArr[c].socketTCP.Send(Encoding.ASCII.GetBytes(message));
    }

    public static void ListenUDPThread()
    {
        byte[] bytes;
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 11000);
        while (true)
        {
            bytes = listenerUDP.Receive(ref groupEP);
            Console.WriteLine($"Received broadcast from {groupEP} :");
            Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
        }
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

        for (int i = 0; i < clientMax; i++) // initializing client array
        {
            clientsArr[i] = new CLIENT();
        }

        for (int i = 0; i < clientMax; i++)
        {
            listeningThreads[i] = new Thread(StartListening);
            listeningThreads[i].Start();
        }

        Thread UDPtestThread = new Thread(ListenUDPThread);
        UDPtestThread.Start();

        Console.Read();
        Console.WriteLine("press any key to close...");

        return 0;
    }
}
