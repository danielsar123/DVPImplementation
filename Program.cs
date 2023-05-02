using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Newtonsoft.Json.Linq;




namespace DVPImplementation
{
    public class Program
    {
        static void Main(string[] args)
        {


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
}


