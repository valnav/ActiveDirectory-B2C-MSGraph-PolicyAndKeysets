using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AADB2C.PolicyAndKeys.Lib;
using System.Configuration;
namespace AADB2C.PolicyAndKeys.Client
{
    public class Program
    {


        static CommandType cmdType = CommandType.EXIT;
        static ResourceType resType = ResourceType.POLICIES;
        static void Main(string[] args)
        {
            // validate parameters
            if (!CheckConfiguration(args))
                return;

            var appSettings = ConfigurationManager.AppSettings;
            Constants.ClientIdForUserAuthn = appSettings["ida:ClientId"];
            Constants.Tenant = appSettings["ida:Tenant"];
            HttpRequestMessage request = null;
            var authHelper = new AuthenticationHelper(Constants.ClientIdForUserAuthn, Constants.Tenant);
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            
            try
            {
                // Login as global admin of the Azure AD B2C tenant
                authHelper.LoginAsAdmin();
                // Graph client does not yet support trustFrameworkPolicy, so using HttpClient to make rest calls
                var userMode = new UserMode(authHelper.TokenForUser);

                do
                {
                    resType = ProcessResourceInput(true);
                    userMode.SetResouce(resType);

                    cmdType = ProcessCommandInput(true);
                    switch (cmdType)
                    {
                        case CommandType.LIST:

                            // List all polcies using "GET /trustFrameworkPolicies"
                            PrintInfo("");
                            request = userMode.HttpGet();
                            break;
                        case CommandType.GET:
                            // Get a specific policy using "GET /trustFrameworkPolicies/{id}"
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            PrintInfo("", args[0]);
                            request = userMode.HttpGetID(args[1]);
                            break;
                        case CommandType.CREATE:
                            // Create a policy using "POST /trustFrameworkPolicies" with XML in the body
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            string cont = System.IO.File.ReadAllText(args[0]);
                            PrintInfo("", args[0]);
                            request = userMode.HttpPost(cont);
                            break;
                        case CommandType.UPDATE:
                            // Update using "PUT /trustFrameworkPolicies/{id}" with XML in the body
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            cont = System.IO.File.ReadAllText(args[1]);
                            PrintInfo("", args[0], args[1]);
                            request = userMode.HttpPutID(args[0], cont);
                            break;
                        case CommandType.DELETE:
                            // Delete using "DELETE /trustFrameworkPolicies/{id}"
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            PrintInfo("", args[0]);
                            request = userMode.HttpDeleteID(args[0]);
                            break;

                        case CommandType.BACKUPKEYSETS:
                        case CommandType.GETACTIVEKEY:
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            PrintInfo("", args[0]);
                            request = userMode.HttpGetByCommandType(cmdType, args[0]);
                            break;

                        case CommandType.GENERATEKEY:
                        case CommandType.UPLOADCERTIFICATE:
                        case CommandType.UPLOADPKCS:
                        case CommandType.UPLOADSECRET:
                            args = ProcessParametersInput(true);
                            if (cmdType == CommandType.EXIT) goto Finish;
                            PrintInfo("", args[0]);
                            cont = args[1];
                            if (cont.Contains(Path.DirectorySeparatorChar))
                                cont = File.ReadAllText(args[1]);
                            request = userMode.HttpPostByCommandType(cmdType, args[0], cont);
                            break;
                        case CommandType.EXIT:
                            goto Finish;

                    }

                    Print(request);

                    HttpClient httpClient = new HttpClient();
                    Task<HttpResponseMessage> response = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                    Print(response);

                Finish: Console.WriteLine("Finished execution or received exit command");

                } while (cmdType != CommandType.EXIT);

            }
            catch (Exception e)
            {
                Print(request);
                Console.WriteLine("\nError {0} {1}", e.Message, e.InnerException != null ? e.InnerException.Message : "");
            }
        }

        private static string[] ProcessParametersInput(bool first)
        {
            List<string> parameters = new List<string>();
            Console.WriteLine($"For Resource {resType.ToString()} and Command {cmdType.ToString()} ");
            
            switch (cmdType)
            {
                case CommandType.DELETE:
                // Delete using "DELETE /trustFrameworkPolicies/{id}"
                case CommandType.GET:
                    // Get a specific policy using "GET /trustFrameworkPolicies/{id}"
                    Console.WriteLine($"For Command: {cmdType.ToString()} Enter Id of {resType.ToString()} ");
                    break;
                case CommandType.CREATE:
                    // Create a policy using "POST /trustFrameworkPolicies" with XML in the body
                    Console.WriteLine($"For Command: {cmdType.ToString()} specify path of {resType.ToString()} ");
                    break;
                case CommandType.UPDATE:
                    // Update using "PUT /trustFrameworkPolicies/{id}" with XML in the body
                    Console.WriteLine($"For Command: {cmdType.ToString()} (space separated) specify Id and path of {resType.ToString()} ");
                    break;
              
            }
            Console.Write(":> ");
            var pars = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(pars) )
            {
                if (first) ProcessParametersInput(false);
                else cmdType = CommandType.EXIT;
            }


            var parsArray = pars.Split(' ');
            parameters = new List<string>(parsArray);
            if ((cmdType == CommandType.UPDATE && parameters.Count != 2) || parameters.Any(string.Empty.Contains))
            {
                if (!first) cmdType = CommandType.EXIT;
                else
                ProcessParametersInput(false);
            }
            
            return parameters.ToArray();
        }

        private static CommandType ProcessCommandInput(bool first)
        {
            var commands = Enum.GetNames(typeof(CommandType));
            Console.WriteLine("Which command do you want to execute on {0} ", string.Join(",", commands));
            Console.Write(":> ");
            var command = Console.ReadLine().ToUpper();
            if (!commands.Any(command.Contains) && command != CommandType.EXIT.ToString())
            {
                cmdType = !first ?  CommandType.EXIT : ProcessCommandInput(false);
            } else
            {
                cmdType = (CommandType) Enum.Parse(typeof(CommandType), command);
            }

            return cmdType;
        }

        private static ResourceType ProcessResourceInput(bool first)
        {
            
            var resources = Enum.GetNames(typeof(ResourceType));
            Console.WriteLine("Policy and Keyset Client (type exit at any time)");
            Console.WriteLine("Which resource do you want to execute on {0} or {1}", resources[0], resources[1]);
            Console.Write(":> ");
            var resource = Console.ReadLine().ToUpper();
            if (!resources.Any(resource.Contains) && resource != CommandType.EXIT.ToString())
            {
                if (!first) cmdType = CommandType.EXIT; 
                else ProcessResourceInput(false);
            }
            else
            {
                resType = (ResourceType)Enum.Parse(typeof(ResourceType), resource);
            }

            return resType;
        }

        public static bool CheckConfiguration(string[] args)
        {
            if (Constants.ClientIdForUserAuthn.Equals("ENTER_YOUR_CLIENT_ID") ||
                Constants.Tenant.Equals("ENTER_YOUR_TENANT_NAME"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("1. Open 'app.config'");
                Console.WriteLine("2. Update 'ida:ClientId'");
                Console.WriteLine("3. Update 'ida:Tenant'");
                Console.WriteLine("");
                Console.WriteLine("See README.md for detailed instructions.");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[press any key to exit]");
                Console.ReadKey();
                return false;
            }

            return true;
        }

        public static void PrintInfo(string print, params string[] args)
        {
            args = args ?? (new List<string>()).ToArray<string>();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(print + $" {resType.ToString()}  {cmdType.ToString()}");
            Console.WriteLine("{0}", string.Join(" ", args) );
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Print(Task<HttpResponseMessage> responseTask)
        {
            responseTask.Wait();
            HttpResponseMessage response = responseTask.Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error Calling the Graph API HTTP Status={0}", response.StatusCode);
            }

            Console.WriteLine(response.Headers);
            Task<string> taskContentString = response.Content.ReadAsStringAsync();
            taskContentString.Wait();
            Console.WriteLine(taskContentString.Result);
        }

        public static void Print(HttpRequestMessage request)
        {
            if (request != null)
            {
                Console.Write(request.Method + " ");
                Console.WriteLine(request.RequestUri);
                Console.WriteLine("");
            }
        }


    }
}
