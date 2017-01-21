using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using System.Fabric;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Alphabet.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri alphabetServiceUri = new Uri(@"fabric:/AlphabetPartitions/Processing");
            string connectionEndpoint1 = ConfigurationManager.AppSettings["ClusterConnection1"];
            string connectionEndpoint2 = ConfigurationManager.AppSettings["ClusterConnection2"];

            HttpClient httpClient = new HttpClient();

            //ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
            ServicePartitionResolver servicePartitionResolver = new ServicePartitionResolver(connectionEndpoint1, connectionEndpoint2);

            string lastname = string.Empty;
            string output = string.Empty;

            CancellationToken cancelRequest = new CancellationToken();

            while (string.Compare(lastname, "quit") != 0)
            {
                Console.WriteLine("Please enter a last name");
                lastname = Console.ReadLine();

                char firstLetterOfLastName = lastname.First();
                ServicePartitionKey partitionKey = new ServicePartitionKey(Char.ToUpper(firstLetterOfLastName) - 'A');

                // This contacts the Service Fabric Naming Services to get the addresses of the replicas of the processing service 
                // for the partition with the partition key generated above. 
                // Note that this gets the most current addresses of the partition's replicas,
                // however it is possible that the replicas have moved between the time this call is made and the time that the address is actually used
                // a few lines below.
                // For a complete solution, a retry mechanism is required.
                // For more information, see http://aka.ms/servicefabricservicecommunication
                ResolvedServicePartition partition = servicePartitionResolver.ResolveAsync(alphabetServiceUri, partitionKey, cancelRequest).Result;
                ResolvedServiceEndpoint ep = partition.GetEndpoint();

                JObject addresses = JObject.Parse(ep.Address);
                string primaryReplicaAddress = (string)addresses["Endpoints"].First();

                UriBuilder primaryReplicaUriBuilder = new UriBuilder(primaryReplicaAddress);
                primaryReplicaUriBuilder.Query = "lastname=" + lastname;

                string result = httpClient.GetStringAsync(primaryReplicaUriBuilder.Uri).Result;
                output = String.Format(
                    "Result: {0}. <p>Partition key: '{1}' generated from the first letter '{2}' of input value '{3}'. <br>Processing service partition ID: {4}. <br>Processing service replica address: {5}",
                    result,
                    partitionKey,
                    firstLetterOfLastName,
                    lastname,
                    partition.Info.Id,
                    primaryReplicaAddress);

                Console.WriteLine(output);
            }
        }
    }
}
