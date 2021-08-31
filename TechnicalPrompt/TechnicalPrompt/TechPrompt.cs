using System;

// recommended package
using Newtonsoft.Json;

// reference: docs microsoft dotnet
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text;

// Combined list of References: 
// Docs Microsoft DotNet, JohanDorper blog, StackOverflow  

namespace TechnicalPrompt
{
    class ToDoDetailed
    {
        public DateTime DueDate { get; set; }
        public string Description { get; set; }
        public string AssigneeFirstName { get; set; }
        public string AssigneeLastName { get; set; }
    }

    class TechPrompt
    {
        /*
        Your application should perform the above operations in parallel.
        Make as few or as many requests to the API endpoints as you find necessary to accomplish each operation.
        The outputs from the operations should each be written to the same output file (e.g. a single file called report.txt).
        The order in which the application writes each section of the output file is unimportant, but they should not be mixed. 
        In other words, the application should avoid simultaneously writing multiple sections to the file.
         */

        const string outputFile = "report.txt";
        static readonly HttpClient client = new HttpClient();
        
        // a counter to keep track of how many threads have written to the file
        // program does not exit until all threads have written their data
        static int numThreadsWrite = 0;

        // determined by requirements
        const int numResponses = 3;

        // reference https://johandorper.com/log/thread-safe-file-writing-csharp
        static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        static void Main(string[] args)
        {
            FileStream file = File.Create(outputFile);
            // close the stream so that the file writing can be controlled with a lock
            file.Close();
            
            // async function call from main
            RunTechPrompt();
        }

        static void RunTechPrompt()
        {
            // Start your threading! 
            Thread employeesThread = new Thread(CalculateEmployeesWithoutBadges);
            employeesThread.Start();

            Thread deptsThread = new Thread(CalculateEmployeesByDept);
            deptsThread.Start();

            Thread todosThread = new Thread(CalculateSortedTodos);
            todosThread.Start();

            // wait on writing data from all threads to file to finish
            while (numThreadsWrite < numResponses)
            {
                Thread.Sleep(10);
            }
        }

        /***
         * MULTI THREAD FUNCTIONALITY
         ***/

        /*
         * Retrieve the collection of employees from the API. Find all the employees who do not 
         * have a badge number and output this collection to a file with the following format:
         */
        async static void CalculateEmployeesWithoutBadges()
        {
            string employeesStr = await GetEmployeesResponse();
            // employees deserialized to string. This gets rid of the enclosing double quotes
            string emplDes = JsonConvert.DeserializeObject<string>(employeesStr);

            // the actual List of employees
            List<Models.Employee> listEmps = JsonConvert.DeserializeObject<List<Models.Employee>>(emplDes);

            // mutable string FTW
            StringBuilder empsStr = new StringBuilder();
            empsStr.AppendLine("## Employees without Badges");

            foreach (Models.Employee e in listEmps)
            {
                if(e.BadgeNumber == null)
                {
                    empsStr.Append(e.Id);
                    empsStr.Append(" - ");
                    empsStr.Append(e.LastName);
                    empsStr.Append(", ");
                    empsStr.Append(e.FirstName);
                    empsStr.Append(Environment.NewLine);
                }
            }

            empsStr.AppendLine();
            WriteToFileThreadSafe(empsStr.ToString());
        }

        /*
         * Retrieve the collection of employees and departments from the API. Map the employees 
         * by department. Output this mapping (by department Id then employee Id order) to a file 
         * with the following format:
         */
        async static void CalculateEmployeesByDept()
        {
            string departmentsStr = await GetDeptsResponse();
            string deptDes = JsonConvert.DeserializeObject<string>(departmentsStr);

            // the actual List of Departments
            List<Models.Department> listDepts = JsonConvert.DeserializeObject<List<Models.Department>>(deptDes);

            string employeesStr = await GetEmployeesResponse();
            // employees deserialized to string. This gets rid of the enclosing double quotes
            string emplDes = JsonConvert.DeserializeObject<string>(employeesStr);

            // the actual List of employees
            List<Models.Employee> listEmps = JsonConvert.DeserializeObject<List<Models.Employee>>(emplDes);

            // each Dept ID is a key. Each Value is a List of Employee IDs for that Dept
            Dictionary<int, List<Models.Employee>> listsEmployeesByDept = new Dictionary<int, List<Models.Employee>>();
            
            // constructors for all of the new lists
            foreach(Models.Department d in listDepts)
            {
                listsEmployeesByDept[d.Id] = new List<Models.Employee>();
            }

            // add each Employee to the List that has the corresponding Dept ID Key for the Employee
            foreach(Models.Employee e in listEmps)
            {
                listsEmployeesByDept[e.DepartmentId].Add(e);
            }

            // at this point, listsEmployeesByDept should be a Dictionary of all the Dept Ids
            // and each Dept ID has a list of Employees for that Dept 

            StringBuilder deptsStr = new StringBuilder();
            deptsStr.AppendLine("## Employees by Department");

            foreach (Models.Department d in listDepts)
            {
                deptsStr.Append(d.Name);
                deptsStr.Append(Environment.NewLine);

                foreach(Models.Employee e in listsEmployeesByDept[d.Id])
                {
                    deptsStr.Append("\t");
                    deptsStr.Append(e.LastName);
                    deptsStr.Append(", ");
                    deptsStr.Append(e.FirstName);
                    deptsStr.Append(Environment.NewLine);
                }
            }

            deptsStr.AppendLine();
            WriteToFileThreadSafe(deptsStr.ToString());
        }

        /*
         * Retrieve the collection of employees and todos from the API. Iterate through the todos 
         * and generate a new object type which contains the due date and description of the todo and 
         * the first and last name of the employee who is assigned to the todo (Todo.AssigneeId). Sort 
         * by the due date of the todo item in descending order, and group the new objects by due date 
         * omitting the time of day that the task is due. Output this mapping to a file with the 
         * following format:
         */
        async static void CalculateSortedTodos()
        {
            string employeesStr = await GetEmployeesResponse();
            string empsDes = JsonConvert.DeserializeObject<string>(employeesStr);
            List<Models.Employee> listEmp = JsonConvert.DeserializeObject<List<Models.Employee>>(empsDes);

            // follow the pattern: API call, deserialize to string, deserialize to list
            string todosStr = await GetTodosResponse();
            string todosDes = JsonConvert.DeserializeObject<string>(todosStr);
            List<Models.Todo> listTodos = JsonConvert.DeserializeObject<List<Models.Todo>>(todosDes);

            // stack overflow Linq sorting
            // in-line sorting of list ToDos 
            listTodos.Sort((x, y) => x.DueDate.CompareTo(y.DueDate));

            Dictionary<int, Models.Employee> sortedEmps = new Dictionary<int, Models.Employee>();

            foreach(Models.Employee e in listEmp)
            {
                sortedEmps.Add(e.Id, e);
            }

            List<ToDoDetailed> listTodoDetails = new List<ToDoDetailed>();
            
            // take listTodos in opposite order
            for(int l = listTodos.Count -1; l >=0; --l)
            {
                Models.Todo t = listTodos[l];

                ToDoDetailed tdd = new ToDoDetailed()
                {
                    DueDate = t.DueDate.Date, 
                    Description = t.Description, 
                    AssigneeFirstName = sortedEmps[t.AssigneeId].FirstName,
                    AssigneeLastName = sortedEmps[t.AssigneeId].LastName
                };

                listTodoDetails.Add(tdd);
            }

            StringBuilder tdStr = new StringBuilder();

            DateTime currentDate = listTodoDetails[0].DueDate.Date;
            // .ToString("yyyy-MM-dd")
            tdStr.AppendLine(currentDate.ToString("yyyy/MM/dd"));

            foreach(ToDoDetailed t in listTodoDetails)
            {
                if (t.DueDate != currentDate)
                {
                    currentDate = t.DueDate;
                    tdStr.AppendLine(currentDate.ToString("yyyy/MM/dd"));
                }

                tdStr.Append("\t");
                tdStr.Append(t.AssigneeLastName);
                tdStr.Append(", ");
                tdStr.Append(t.AssigneeFirstName);
                tdStr.Append(" - \"");
                tdStr.Append(t.Description + "\"");
                tdStr.Append(Environment.NewLine);
            }

            tdStr.AppendLine();
            WriteToFileThreadSafe(tdStr.ToString());
        }

        /***
         * MULTI THREAD FILE WRITING
         * source: jonandorper link above
         ***/
        public static void WriteToFileThreadSafe(string text)
        {
            // Set Status to Locked
            _readWriteLock.EnterWriteLock();
            try
            {
                // Append text to the file
                StreamWriter sw = File.AppendText(outputFile);
                sw.WriteLine(text);
                sw.Close();
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();

                // increment that the result got written
                ++numThreadsWrite;
            }
        }

        /***
         * NETWORK CONNECTION
         * ***/
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

        async static Task<string> GetEmployeesResponse()
        {
            const string employeeEndpt = "https://mhealthtechinterview.azurewebsites.net/api/employees";

            return await getApiResponse(employeeEndpt);
        }

        async static Task<string> GetDeptsResponse()
        {
            const string deptsEndpt = "https://mhealthtechinterview.azurewebsites.net/api/departments";

            return await getApiResponse(deptsEndpt);
        }

        async static Task<string> GetTodosResponse()
        {
            const string todoEndpt = "https://mhealthtechinterview.azurewebsites.net/api/todos";

            return await getApiResponse(todoEndpt);
        }
    }
}
