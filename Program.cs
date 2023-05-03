using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace DVPImplementation
{
    public class Program
    { 

        static List<Node> nodes = new List<Node>();
        static int[,] routingTableTF;
        static int updateInterval = 1000;
        static int serverID = 1;
        static int numOfDisabled = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, begin with following command: \"server -t <topology-file-name> -i <routing-update-interval>\"");

            while (true)
            {
                Console.Write("->");

                string line = Console.ReadLine();
                string[] commands = line.Split(' '); // seperate the commands, store them in String array

                if(commands.Length < 1)
                {
                    Console.Write("Nothing inputted, try again!");
                    //skip to next iteration if no user input
                    continue;
                }

                switch (commands[0])
                {
                    case "server":
                        try
                        {
                            updateInterval = int.Parse(commands[4]); // because update inteval is 4th position in array 

                        }
                        catch
                        {
                            Console.WriteLine("Incorrect command! Try again with correct format!");
                            continue;
                        }
                        string fileName = commands[2];
                        nodes = ReadTF(fileName, nodes);
                        nodes = CreateTable(nodes);
                        
                }



            }


        }

        static List<Node> ReadTF(string file, List<Node> nodes)
        {
            int numOfServers = 0;
            int numofNeighbors = 0;

            Dictionary<int, int> newIdCost = new Dictionary<int, int>();

            try
            {
                
                StreamReader reader = new StreamReader(file);
                string line;

                if ((line = reader.ReadLine()) != null)
                {
                    numOfServers = int.Parse(line);
                }
                else
                {
                    throw new Exception("Incorrect format!");
                }

                if((line = reader.ReadLine()) != null)
                {
                    numofNeighbors = int.Parse(line);
                }
                else
                {
                    throw new Exception("Incorrect format!");
                }

                for (int i=0; i < numOfServers; i++)
                {
                    if((line = reader.ReadLine()) != null)
                    {
                        string[] splitLine = line.Split(' ');

                        if (splitLine.Length != 3)
                        {
                            throw new Exception("Incorrect format!");
                        }
                        else
                        {
                            Node newServer = new Node();
                            newServer.SetId(int.Parse(splitLine[0]));
                            newServer.SetIpAddress((splitLine[1]));
                            newServer.SetPort(int.Parse(splitLine[2]));
                            newServer.SetNoOfPacketsReceived(0);
                            nodes.Add(newServer);

                            if (newServer.GetId() == serverID)
                            {
                                Listener listener = new Listener(newServer.GetPort());
                                Thread listenerThread = new Thread(listener.Listen);
                                listenerThread.Start();
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Incorrect format!");
                    }
                }

                for (int i = 0; i < numofNeighbors; i++)
                {
                    if ((line = reader.ReadLine()) != null)
                    {
                        string[] splitLine = line.Split(' ');

                        if (splitLine.Length != 3)
                        {
                            throw new Exception("Topology File Not Correctly Formatted!");
                        }
                        else
                        {
                            serverID = int.Parse(splitLine[0]);
                            newIdCost[int.Parse(splitLine[1])] = int.Parse(splitLine[2]);
                        }
                    }
                    else
                    {
                        throw new Exception("Topology File Not Correctly Formatted!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            foreach (Node server in nodes)
            {
                if (server.GetId() == serverID)
                {
                    server.SetNeighborsIdAndCost(newIdCost); 
                }
                else
                {
                    Dictionary<int, int> emptyDictionary = new Dictionary<int, int>();
                    emptyDictionary[0] = 0;
                    server.SetNeighborsIdAndCost(emptyDictionary);
                }
            }
            return nodes;
        }

        

        static List<Node> CreateTable(List<Node> nodes)
        {
            return nodes;
        }
    }
    

    public class Node

    /* This class represents details of a server in a network.
     * It has fields to store the server's ID, IP address, port number, number of packets received, and a dictionary of neighbor server IDs and their associated costs.
     * It also has a 2D array to store the server's routing table.
    */
    {
        public int id;

        public string ipAddress;

        public int port;

        public int numOfPackets;

        public Dictionary<int, int> neighborsIdAndCost;

        public int[,] routingTable;

        //create new dictionary of neighbors' IDs and costs, as well as initialize number of packets to 0 each time a server is made (instance of class created)
        public Node()
        {
            this.neighborsIdAndCost = new Dictionary<int, int>();
            this.numOfPackets = 0;
        }

        public int GetId()
        {
            return id;
        }

        public void SetId(int id)
        {
            this.id = id;
        }

        public string GetIpAddress()
        {
            return ipAddress;
        }

        public void SetIpAddress(string ipAddress)
        {
            this.ipAddress = ipAddress;
        }

        public int GetPort()
        {
            return port;
        }

        public void SetPort(int port)
        {
            this.port = port;
        }

        public Dictionary<int, int> GetNeighborsIdAndCost()
        {
            return neighborsIdAndCost;
        }

        public void SetNeighborsIdAndCost(Dictionary<int, int> neighborsIdAndCost)
        {
            this.neighborsIdAndCost = neighborsIdAndCost;
        }

        public int GetNumOfPackets()
        {
            return numOfPackets;
        }

        public void SetNoOfPacketsReceived(int numOfPackets)
        {
            this.numOfPackets = numOfPackets;
        }

        public int[,] GetRoutingTable()
        {
            return routingTable;
        }

        public void SetRoutingTable(int[,] routingTable)
        {
            this.routingTable = routingTable;
        }
    }
    public class Listener
    {
        private Socket socket = null;
        private TcpListener server = null;
        private BinaryReader inStream = null;
        private int port = 0;
        public Listener(int port)
        {
            this.port = port;
        }
        public void Listen()
        {
            try
            {
                // create TcpListener object and start listening for incoming connections
                server = new TcpListener(IPAddress.Any, port);
                server.Start();

                Console.WriteLine("Server started, waiting for a client...");

                while (true)
                {
                    // accept client connection
                    socket = server.AcceptSocket();
                    Console.WriteLine("Client connected.");

                    // read data from the client socket
                    inStream = new BinaryReader(new BufferedStream(new NetworkStream(socket)));

                    string line = inStream.ReadString();
                    Console.WriteLine(line);

                    // parse the received JSON
                    JObject receivedJson = JObject.Parse(line);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}


