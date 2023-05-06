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
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json;

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
                        int link1 = int.Parse(commands[1]);
                        int link2 = int.Parse(commands[2]);
                        string newCost = commands[3];

                        if (link1 == link2)
                        {
                            Console.WriteLine("Error: Same Line");
                            break;
                        }
                        else if (link2 == serverID)
                        {
                            SendCost(link2, link1, newCost);
                            break;
                        }
                        else
                        {
                            SendCost(link1, link2, newCost);
                            break;
                        }
                    case "step":
                        DoStep(nodes);
                        break;

                    case "packets":
                        DisplayPackets(nodes);
                        break;

                    case "disable":
                        // Send this to all servers, not just neighbors
                        int serverToDisable = int.Parse(commands[1]);
                        if (serverToDisable == serverID) // serverID is equivalent to myServerId
                        {
                            Console.WriteLine("Cannot disable yourself");
                            break;
                        }
                        SendDisable(serverToDisable);
                        numOfDisabled++;
                        break;
                }



            }


        }
        
        
            private static void DoStep(List<Node> nodes)
            {
                foreach (var server in nodes)
                {
                    if (server.id == serverID)
                    {
                        //Console.WriteLine("my servers id = " + server.Id);

                        foreach (var neighbor in server.neighborsIdAndCost)
                        {
                            string ipAddressOfNeighbor = "";
                            int portOfNeighbor = 0;

                            // find ip of neighbor and send routing table to that neighbor
                            foreach (var s in nodes)
                            {
                                if (s.id == neighbor.Key)
                                {
                                    ipAddressOfNeighbor = s.ipAddress;
                                    portOfNeighbor = s.port;
                                    break;
                                }
                            }

                            //Console.WriteLine("ipaddress of neighbor = " + ipAddressOfNeighbor);
                            //Console.WriteLine("port of neighbor = " + portOfNeighbor);
                            try
                            {
                                //Console.WriteLine("send message");
                                SendRTtoNeighbor(ipAddressOfNeighbor, portOfNeighbor);
                                //Console.WriteLine("message sent");
                            }
                            catch (Exception e)
                            {
                                // handle exception
                            }
                        }
                        break;
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
                Console.WriteLine("Sending JSON: " + obj.ToString());
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
        

            // Initialize original and new routing tables
            int[,] myOriginalRoutingTable = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];
            int[,] myNewRoutingTable = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];

            // Find the index of the server with matching id
            int i;
            for (i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == serverID)
                {
                    // Copy the routing table of the matching server
                    for (int a = 0; a < nodes[i].routingTable.GetLength(0); a++)
                    {
                        for (int b = 0; b < nodes[i].routingTable.GetLength(1); b++)
                        {
                            myOriginalRoutingTable[a, b] = nodes[i].routingTable[a, b];
                            myNewRoutingTable[a, b] = nodes[i].routingTable[a, b];
                        }
                    }
                    break;
                }
            }

            // Get the ids of neighbors of the server
            int[] neighbors = new int[nodes[i].neighborsIdAndCost.Count];
            int x = 0;
            foreach (var entry in nodes[i].neighborsIdAndCost)
            {
                neighbors[x] = entry.Key;
                x++;
            }

            // Update new routing table using the received routing table
            for (int j = 0; j < myNewRoutingTable.GetLength(0); j++)
            {
                if (j + 1 == serverID)
                {
                    continue;
                }
                for (int k = 0; k < myNewRoutingTable.GetLength(1); k++)
                {
                    if (j == k)
                    {
                        continue;
                    }
                    if (myNewRoutingTable[j, k] < newTable[j,k])
                    {
                        continue;
                    }
                    else
                    {
                        myNewRoutingTable[j, k] = newTable[j,k];
                    }
                }
            }

            // Update new routing table using distance vector algorithm
            for (int j = 0; j < myNewRoutingTable.GetLength(0); j++)
            {
                if (j + 1 == serverID)
                {
                    for (int k = 0; k < myNewRoutingTable.GetLength(1); k++)
                    {
                        if (j == k)
                        {
                            continue;
                        }
                        int[] newCosts = new int[nodes[i].neighborsIdAndCost.Count];
                        for (int a = 0; a < neighbors.Length; a++)
                        {
                            newCosts[a] = myNewRoutingTable[j, neighbors[a] - 1] + myNewRoutingTable[neighbors[a] - 1, k];
                        }
                        int minCost = 9999;
                        for (int a = 0; a < newCosts.Length; a++)
                        {
                            if (minCost > newCosts[a])
                            {
                                minCost = newCosts[a];
                            }
                        }
                        myNewRoutingTable[j, k] = minCost;
                    }
                }
            }
            bool didRoutingTableChange = false;
            for (int s = 0; s < nodes[i].routingTable.GetLength(0); s++)
            {
                for (int t = 0; t < nodes[i].routingTable.GetLength(1); t++)
                {
                    if (myNewRoutingTable[s, t] != myOriginalRoutingTable[s, t])
                    {
                        didRoutingTableChange = true;
                        break;
                    }
                }
            }

            if (didRoutingTableChange)
            {
                nodes[i].routingTable = myNewRoutingTable;
                // send routing table to neighbors
                //DoStep(nodes);
            }

            return nodes;
        }
        static List<Node> ReadTF(string file, List<Node> nodes)
        {
            int numOfServers = 0;
            int numofNeighbors = 0;

            Console.Write("Enter the server ID: ");
            string input = Console.ReadLine();
            
            if (int.TryParse(input, out serverID))
            {
                Console.WriteLine($"Server ID set to {serverID}");
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid integer.");
            }


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
            if (newCost.Equals("inf", StringComparison.OrdinalIgnoreCase))
            {
                routingTableTF[link1 - 1, link2 - 1] = 9999;
            }
            else
            {
                routingTableTF[link1 - 1, link2 - 1] = int.Parse(newCost);
            }

            for (int x = 0; x < nodes.Count; x++)
            {
                if (nodes[x].id == serverID)
                {
                    for (int i = 0; i < routingTableTF.GetLength(0); i++)
                    {
                        for (int j = 0; j < routingTableTF.GetLength(1); j++)
                        {
                            nodes[x].routingTable[i, j] = routingTableTF[i, j];
                        }
                    }
                    break;
                }
            }

            JObject obj = new JObject
    {
        { "operation", "update" },
        { "update_server_id_1", link1 },
        { "update_server_id_2", link2 },
        { "cost", newCost }
    };

            try
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].id == link1 || nodes[i].id == link2)
                    {
                        IPAddress ip = IPAddress.Parse(nodes[i].ipAddress);
                        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            socket.Connect(ip, nodes[i].port);
                            using (NetworkStream networkStream = new NetworkStream(socket))
                            using (StreamWriter writer = new StreamWriter(networkStream, Encoding.UTF8))
                            {
                                writer.Write(obj.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error sending update to neighbors");
                Console.WriteLine(e.StackTrace);
            }

           // DoStep(nodes);
        }
        private static void DisplayPackets(List<Node> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == serverID)
                {
                    Console.WriteLine($"Number of packets received: {nodes[i].numOfPackets}");
                    nodes[i].numOfPackets = 0;
                    break;
                }
            }
            
        }
        private static void SendDisable(int dsid)
        {
            // Iterate through the routingTableTF (converted from routingTableReadFromTopologyFile) 2D array
            for (int i = 0; i < routingTableTF.GetLength(0); i++)
            {
                for (int j = 0; j < routingTableTF.GetLength(1); j++)
                {
                    if (j == (dsid - 1))
                    {
                        continue;
                    }
                    routingTableTF[j, dsid - 1] = 9999;
                    routingTableTF[dsid - 1, j] = 9999;
                }
            }

            // Iterate through the nodes (converted from allServers) list
            for (int x = 0; x < nodes.Count; x++)
            {
                if (nodes[x].id == serverID) // serverID is equivalent to myServerId
                {
                    nodes[x].neighborsIdAndCost.Remove(dsid);

                    for (int i = 0; i < routingTableTF.GetLength(0); i++)
                    {
                        for (int j = 0; j < routingTableTF.GetLength(1); j++)
                        {
                            nodes[x].routingTable[i, j] = routingTableTF[i, j];
                        }
                    }
                    break;
                }
            }

            // Create and populate a JObject (equivalent to JSONObject in Java)
            JObject obj = new JObject
    {
        { "operation", "disable" },
        { "disable_server_id", dsid }
    };

            // Iterate through the nodes list and send the JObject to each server
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].id == serverID)
                {
                    continue;
                }

                IPAddress ip = IPAddress.Parse(nodes[i].ipAddress);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(ip, nodes[i].port);
                    using (NetworkStream networkStream = new NetworkStream(socket))
                    using (StreamWriter writer = new StreamWriter(networkStream, Encoding.UTF8))
                    {
                        writer.Write(obj.ToString());
                    }
                }
            }

            // Remove the server with ID dsid from the nodes list
            nodes.RemoveAt(dsid - 1);

            // Call the DoStep(nodes) method to update the routing tables in the network
            DoStep(nodes);
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
        private TcpListener server;
        private int port;

        public Listener(int port)
        {
            this.port = port;
        }

        public void Listen()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                

                while (true)
                {
                    Console.WriteLine("Waiting for client connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected.");

                    NetworkStream stream = client.GetStream();
                    StreamReader reader = new StreamReader(stream);

                    string line = "";
                    StringBuilder sb = new StringBuilder();
                    while ((line = reader.ReadLine()) != null)
                    {
                        sb.Append(line);
                    }

                    string json = sb.ToString();
                  //  Console.WriteLine($"Received: {json}");

                    JObject receivedJson = JObject.Parse(json);

                    //Console.WriteLine($"Received: {line}");

                        // parse the received JSON
                        

                        switch (receivedJson["operation"].ToString())
                        {
                            case "step":
                                Console.WriteLine("Received a Message From Server " + receivedJson["id_of_sender"]);

                                int[,] newTable = new int[nodes.Count + numOfDisabled, nodes.Count + numOfDisabled];
                                JArray jsonArray = (JArray)receivedJson["rt"];
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

                        case "update":
                            string newCost = receivedJson["cost"].ToString();
                            int server1 = int.Parse(receivedJson["update_server_id_1"].ToString());
                            int server2 = int.Parse(receivedJson["update_server_id_2"].ToString());

                            if (newCost.Equals("inf", StringComparison.OrdinalIgnoreCase))
                            {
                                routingTableTF[server2 - 1, server1 - 1] = 9999;
                            }
                            else
                            {
                                routingTableTF[server2 - 1, server1 - 1] = int.Parse(newCost);
                            }

                            for (int x = 0; x < nodes.Count; x++)
                            {
                                if (nodes[x].id == serverID)
                                {
                                    for (int i = 0; i < routingTableTF.GetLength(0); i++)
                                    {
                                        for (int j = 0; j < routingTableTF.GetLength(1); j++)
                                        {
                                            nodes[x].routingTable[i, j] = routingTableTF[i, j];
                                        }
                                    }
                                    break;
                                }
                            }

                            break;

                        case "disable":
                            int disableServerId = int.Parse(receivedJson["disable_server_id"].ToString());

                            if (disableServerId == serverID) // serverID is equivalent to myServerId
                            {
                                Environment.Exit(1);
                            }

                            for (int i = 0; i < routingTableTF.GetLength(0); i++)
                            {
                                for (int j = 0; j < routingTableTF.GetLength(1); j++)
                                {
                                    if (j == (disableServerId - 1))
                                    {
                                        continue;
                                    }
                                    routingTableTF[j, disableServerId - 1] = 9999;
                                    routingTableTF[disableServerId - 1, j] = 9999;
                                }
                            }

                            for (int x = 0; x < nodes.Count; x++)
                            {
                                if (nodes[x].id == serverID)
                                {
                                    nodes[x].neighborsIdAndCost.Remove(disableServerId);

                                    for (int i = 0; i < routingTableTF.GetLength(0); i++)
                                    {
                                        for (int j = 0; j < routingTableTF.GetLength(1); j++)
                                        {
                                            nodes[x].routingTable[i, j] = routingTableTF[i, j];
                                        }
                                    }
                                    break;
                                }
                            }

                            nodes.RemoveAt(disableServerId - 1);
                            numOfDisabled++;
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



