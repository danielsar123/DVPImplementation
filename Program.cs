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
using static DVPImplementation.Program;


namespace DVPImplementation
{
    public class Program
    {

        public static List<Node> nodes = new List<Node>();
        public static int[,] routingTableTF;
        public static int updateInterval = 1000;
        public static int serverID = 1;
        public static int numOfDisabled = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, begin with following command: \"server -t <topology-file-name> -i <routing-update-interval>\"");

            while (true)
            {
                Console.Write("->");

                string line = Console.ReadLine();
                string[] commands = line.Split(' '); // seperate the commands, store them in String array

                if (commands.Length < 1 || string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine("Nothing inputted, try again!");
                    //skip to next iteration if no user input
                    continue;
                }

                switch (commands[0])
                {
                    case "server":
                        try
                        {
                            updateInterval = int.Parse(commands[4]); // update interval is 4th position in array 

                        }
                        catch
                        {
                            Console.WriteLine("Incorrect command! Try again with correct format!");
                            continue;
                        }
                        string fileName = commands[2];
                        nodes = ReadTF(fileName, nodes);
                        nodes = CreateTable(nodes);

                        routingTableTF = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];

                        for (int i = 0; i < nodes.Count; i++)
                        {
                            if (nodes[i].id == serverID)
                            {
                                for (int s = 0; s < nodes[i].routingTable.GetLength(0); s++)
                                {
                                    for (int t = 0; t < nodes[i].routingTable.GetLength(1); t++)
                                    {
                                        routingTableTF[s, t] = nodes[i].routingTable[s, t];
                                    }
                                }
                                break;
                            }
                        }

                        Console.WriteLine(commands[0] + " Success!");
                        break;

                    case "display":
                        DisplayRT(nodes);
                        Console.WriteLine(commands[0] + " Success!");
                        break;
                    default:
                        break;

                    case "update":
                        int linkServer1 = int.Parse(commands[1]);
                        int linkServer2 = int.Parse(commands[2]);
                        string newCostOfLink = commands[3];

                        if (linkServer1 == linkServer2)
                        {
                            Console.WriteLine("Error: Same Line");
                            break;
                        }
                        else if (linkServer2 == serverID)
                        {
                            SendCost(linkServer2, linkServer1, newCostOfLink);
                            break;
                        }
                        else
                        {
                            SendCost(linkServer1, linkServer2, newCostOfLink);
                            break;
                        }


                }



            }


        }

        static void DisplayRT(List<Node> nodes) {
            Console.WriteLine("Routing Table is:");
           
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == serverID)
                {
                    Console.Write("\t");
                    for (int t = 0; t < nodes.Count; t++)
                    {
                        Console.Write(nodes[t].id + "\t");
                    }
                    Console.WriteLine();
                    for (int j = 0; j < nodes[i].routingTable.GetLength(0); j++)
                    {
                        Console.Write((j + 1) + "\t");
                        for (int k = 0; k < nodes[i].routingTable.GetLength(1); k++)
                        {
                            Console.Write(nodes[i].routingTable[j, k] + "\t");
                        }
                        Console.WriteLine();
                    }
                    break;
                }
            }

        }
        static void SendRTtoNeighbor(string neighborIP, int neighborPort)
        {
            JObject obj = new JObject();
            try
            {
                obj.Add("operation", "step");
                obj.Add("id_of_sender", serverID);
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].id == serverID)
                    {
                        obj.Add("rt", JToken.FromObject(nodes[i].routingTable));
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("JSON Object Error");
                Console.WriteLine(e.StackTrace);
            }
            try
            {
                IPAddress ip = IPAddress.Parse(neighborIP);
                TcpClient client = new TcpClient();
                client.Connect(ip, neighborPort);

                NetworkStream stream = client.GetStream();
                StreamWriter writer = new StreamWriter(stream);
                StreamReader reader = new StreamReader(stream);

                writer.Write(obj.ToString());
                writer.Flush();

                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }
        



        public static List<Node> UpdateRT(List<Node> nodes, int[,] newTable)
        {
            return nodes;
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
                    throw new Exception("Incorrect format1!");
                }

                if((line = reader.ReadLine()) != null)
                {
                    numofNeighbors = int.Parse(line);
                }
                else
                {
                    throw new Exception("Incorrect format2!");
                }

                for (int i=0; i < numOfServers; i++)
                {
                    if((line = reader.ReadLine()) != null)
                    {
                        string[] splitLine = line.Split(' ');

                        if (splitLine.Length != 3)
                        {
                            throw new Exception("Incorrect format3!");
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
                        throw new Exception("Incorrect format4!");
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

        public static void SendCost(int link1, int link2, String newCost)
        {

        }
        

        static List<Node> CreateTable(List<Node> nodes)
        {
            // Iterate through all the servers
            for (int i = 0; i < nodes.Count; i++)
            {
                // Create a new routing table for the current server
                nodes[i].routingTable = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];

                // If the current server is the one we need
                if (nodes[i].id == serverID)
                {
                    // Initialize the routing table for the current server
                    for (int j = 0; j < nodes.Count + numOfDisabled; j++)
                    {
                        for (int k = 0; k < nodes.Count + numOfDisabled; k++)
                        {
                            // Set the distance to itself as 0
                            if (j == k)
                            {
                                nodes[i].routingTable[j, k] = 0;
                            }
                            else
                            {
                                // Set the distance to other servers as 9999 (a large value)
                                nodes[i].routingTable[j, k] = 9999;
                            }
                        }
                    }
                }
                else
                {
                    // Initialize the routing table for the other servers with 9999 (a large value)
                    for (int j = 0; j < nodes.Count + numOfDisabled; j++)
                    {
                        for (int k = 0; k < nodes.Count + numOfDisabled; k++)
                        {
                            nodes[i].routingTable[j, k] = 9999;
                        }
                    }
                }
            }

            // Iterate through all the servers again
            for (int i = 0; i < nodes.Count; i++)
            {
                // If the current server is the one we need
                if (nodes[i].id == serverID)
                {
                    // Iterate through the routing table of the current server
                    for (int j = 0; j < nodes.Count + numOfDisabled; j++)
                    {
                        // If the current index + 1 is equal to the server ID we need
                        if (j + 1 == serverID)
                        {
                            // Iterate through the neighbors of the current server and update the routing table
                            foreach (KeyValuePair<int, int> entry in nodes[i].neighborsIdAndCost)
                            {
                                nodes[i].routingTable[j, entry.Key - 1] = entry.Value;
                            }
                            break;
                        }
                    }
                    break;
                }
            }
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

                    switch (receivedJson["operation"].ToString())
                    {
                        case "step":
                            Console.WriteLine("Received a Message From Server " + receivedJson["id_of_sender"]);

                            int[,] newTable = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];
                            JArray jsonArray = JArray.Parse(receivedJson["rt"].ToString());
                            for (int a = 0; a < jsonArray.Count; a++)
                            {
                                JArray innerJsonArray = (JArray)jsonArray[a];
                                for (int b = 0; b < innerJsonArray.Count; b++)
                                {
                                    newTable[a, b] = int.Parse(innerJsonArray[b].ToString());
                                }
                            }

                            for (int i = 0; i < nodes.Count; i++)
                            {
                                if (nodes[i].id == Program.serverID)
                                {
                                    nodes[i].numOfPackets++;
                                    break;
                                }
                            }

                            nodes = UpdateRT(nodes, newTable);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}


