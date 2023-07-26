using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace CustomerManagement
{
    public class Customer
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Age { get; set; }
        public int ID { get; set; }
    }

    public class RestServer
    {
        private static List<Customer> customers = new List<Customer>();
        private const string dataFilePath = "customers.json";

        public static void Main(string[] args)
        {
            LoadData(); // Load existing data from the file on server start-up

            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8096/");
            listener.Start();
            Console.WriteLine("Listening on http://localhost:8096/");

            while (true)
            {
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "POST")
                {
                    HandlePostRequest(request, response); // Handle POST requests
                }
                else if (request.HttpMethod == "GET")
                {
                    HandleGetRequest(request, response); // Handle GET requests
                }

                response.Close();
            }
        }

        private static void LoadData()
        {
            // Check if the data file exists and deserialize it to the customers list
            if (File.Exists(dataFilePath))
            {
                var jsonData = File.ReadAllText(dataFilePath);
                customers = JsonSerializer.Deserialize<List<Customer>>(jsonData);
            }
        }

        private static void SaveData()
        {
            // Serialize the customers list and save it to the data file
            var jsonData = JsonSerializer.Serialize(customers);
            File.WriteAllText(dataFilePath, jsonData);
        }

        private static void HandlePostRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!request.HasEntityBody)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var requestBody = reader.ReadToEnd();

            var newCustomers = JsonSerializer.Deserialize<List<Customer>>(requestBody);

            if (newCustomers == null || newCustomers.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            foreach (var newCustomer in newCustomers)
            {
                // Check if any of the required fields are missing or if the ID is already used
                if (string.IsNullOrEmpty(newCustomer.FirstName) || string.IsNullOrEmpty(newCustomer.LastName)
                    || newCustomer.Age <= 18 || customers.Any(c => c.ID == newCustomer.ID))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                // Insert the customer at the correct position based on last name and first name
                int index = 0;
                while (index < customers.Count &&
                    string.Compare($"{customers[index].LastName} {customers[index].FirstName}", $"{newCustomer.LastName} {newCustomer.FirstName}", StringComparison.InvariantCultureIgnoreCase) < 0)
                {
                    index++;
                }
                customers.Insert(index, newCustomer);
            }

            SaveData(); // Save the data to the file after inserting new customers

            response.StatusCode = (int)HttpStatusCode.OK;
        }

        private static void HandleGetRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Serialize the customers list and send it as the response
            var jsonData = JsonSerializer.Serialize(customers);
            var buffer = System.Text.Encoding.UTF8.GetBytes(jsonData);

            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";

            using var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
        }
    }
}
