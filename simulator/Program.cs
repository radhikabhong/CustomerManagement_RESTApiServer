using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace CustomerSimulator
{
    public class Customer
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Age { get; set; }
        public int ID { get; set; }
    }

    public class Program
    {
        private const string serverUrl = "http://localhost:8096/customer";

        private static Customer GenerateRandomCustomer(Random random, List<string> firstNames, List<string> lastNames, int customerId)
        {
            var lastName = lastNames[random.Next(lastNames.Count)];
            var firstName = firstNames[random.Next(firstNames.Count)];
            var age = random.Next(10, 91);

            var newCustomer = new Customer
            {
                LastName = lastName,
                FirstName = firstName,
                Age = age,
                ID = customerId
            };

            return newCustomer;
        }

        private static async Task<int?> GetLastCustomerIdAsync(HttpClient client)
        {
            var getResponse = await client.GetAsync(serverUrl);
            if (getResponse.IsSuccessStatusCode)
            {
                var responseData = await getResponse.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<Customer>>(responseData);
                if (customers.Any())
                {
                    return customers.Max(customer => customer.ID);
                }
            }
            return null; // Return null if the list of customers is empty
        }

        private static async Task<bool> PostCustomersAsync(HttpClient client, string url, List<Customer> customers)
        {
            var jsonData = JsonSerializer.Serialize(customers);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            return response.IsSuccessStatusCode;
        }

        public static async Task Main(string[] args)
        {
            var random = new Random();
            var client = new HttpClient();

            var lastNames = new List<string>
            {
                "Liberty", "Ray", "Harrison", "Ronan", "Drew", "Powell", "Larsen", "Chan", "Anderson", "Lane"
            };

            var firstNames = new List<string>
            {
                "Leia", "Sadie", "Jose", "Sara", "Frank", "Dewey", "Tomas", "Joel", "Lukas", "Carlos"
            };

            int? lastCustomerId = await GetLastCustomerIdAsync(client);
            int customerId = lastCustomerId.HasValue ? lastCustomerId.Value + 1 : 1;

            for (int i = 0; i < 3; i++)
            {
                var newCustomers = new List<Customer>();

                for (int j = 0; j < 2; j++)
                {
                    var newCustomer = GenerateRandomCustomer(random, firstNames, lastNames, customerId);
                    newCustomers.Add(newCustomer);
                    customerId++; // Increment the customerId for the next customer
                }

                // Send the POST request with newCustomers
                var postResponse = await PostCustomersAsync(client, serverUrl, newCustomers);

                if (postResponse)
                {
                    Console.WriteLine($"POST Request {i + 1} - Success");
                }
                else
                {
                    Console.WriteLine($"POST Request {i + 1} - Failed");
                }
            }

            // Send a GET request to retrieve and display all customers from the server
            var getResponse = await client.GetAsync(serverUrl);
            if (getResponse.IsSuccessStatusCode)
            {
                var responseData = await getResponse.Content.ReadAsStringAsync();
                var customers = JsonSerializer.Deserialize<List<Customer>>(responseData);
                foreach (var customer in customers)
                {
                    Console.WriteLine($"Last Name: {customer.LastName}, First Name: {customer.FirstName}, Age: {customer.Age}, ID: {customer.ID}");
                }
            }
            else
            {
                Console.WriteLine("GET Request Failed");
            }
        }
    }
}
