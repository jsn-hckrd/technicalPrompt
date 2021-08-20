using System;

// recommended package
using Newtonsoft.Json;

// reference: docs microsoft dotnet
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

namespace TechnicalPrompt
{
    class TechPrompt
    {
        /*
        Your application should perform the above operations in parallel.
        Make as few or as many requests to the API endpoints as you find necessary to accomplish each operation.
        The outputs from the operations should each be written to the same output file (e.g. a single file called report.txt).
        The order in which the application writes each section of the output file is unimportant, but they should not be mixed. 
        In other words, the application should avoid simultaneously writing multiple sections to the file.
         */

        const string employeeEndpt = "https://mhealthtechinterview.azurewebsites.net/api/employees";
        const string deptsEndpt = "https://mhealthtechinterview.azurewebsites.net/api/departments";
        const string todoEndpt = "https://mhealthtechinterview.azurewebsites.net/api/todos";

        const string outputFile = "report.txt";

        static readonly HttpClient client = new HttpClient();
        

        static void Main(string[] args)
        {
            using StreamWriter file = new StreamWriter(outputFile);
            MainAsync(file).Wait();
        }

        // reference:Stack Overflow
        static async Task MainAsync(StreamWriter file)
        {
            await runTechPrompt(file);
        }

        async static Task runTechPrompt(StreamWriter file)
        {
            string employeesStr = await getApiResponse(employeeEndpt);
            // employees deserialized
            string emplDes = JsonConvert.DeserializeObject<string>(employeesStr);

            // the actual List of employees
            List<Models.Employee> listEmps = new List<Models.Employee>();
            listEmps = JsonConvert.DeserializeObject<List<Models.Employee>>(emplDes);

            file.WriteLine(employeesStr + Environment.NewLine);

            string departmentsStr = await getApiResponse(deptsEndpt);
            string deptDes = JsonConvert.DeserializeObject<string>(departmentsStr);

            // the actual List of Departments
            List<Models.Department> listDepts = new List<Models.Department>();
            listDepts = JsonConvert.DeserializeObject<List<Models.Department>>(emplDes);

            file.WriteLine(departmentsStr + Environment.NewLine);

            string todosStr = await getApiResponse(todoEndpt);
            file.WriteLine(todosStr + Environment.NewLine);

            // Console.WriteLine(employeesStr + Environment.NewLine);
            // Console.WriteLine(departmentsStr + Environment.NewLine);
            // Console.WriteLine(todosStr + Environment.NewLine);
        }

        async static Task<string> getApiResponse(string endpoint)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().Result;

                return responseBody;
            }
            catch(Exception e)
            {
                string exc = "Exception Caught! Message: " + e.Message;
                return exc;
            }
        }
    }
}
