using Common.Resources;
using Common.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using Newtonsoft.Json;
using System;
using System.IO;
using log4net;
using log4net.Config;
using Newtonsoft.Json.Linq;

namespace ValidateWindowsServiceResults
{
    class Program
    {
        private static Repository repository;
        private static ILog _logger = LogManager.GetLogger("ValidateWindowsServiceResults");
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            string startFromArg = "";
            if(args.Count() > 0)
                startFromArg = args[0];
            int startFrom = 0;
            _logger.Info("ValidateWindowsServiceResults start");
            repository = new Repository(new SessionManager());
            List<EvaluationRequest> EvaluationRequests = new List<EvaluationRequest>();
            int.TryParse(startFromArg, out startFrom);
            EvaluationRequests = repository.GetAll<EvaluationRequest>().Where(x => x.RequestStatus == RequestStatusConstants.COMPLETED && x.Id >= startFrom).ToList();
            
            string getDiagnosticsUrl = ConfigurationManager.AppSettings["GetDiagnosticsUrl"].ToString();
            string filesFolderPath = ConfigurationManager.AppSettings["FilesFolder"].ToString();
            string securityUrl = ConfigurationManager.AppSettings["GoSystemsSecurityUrl"].ToString();

            _logger.Info("Set Configurations");

            if (Directory.Exists(filesFolderPath))
            {
                _logger.Info(String.Format("Folder exists {0}", filesFolderPath));

                string responseResult;
                bool match = false;
                string token = "";
                DateTime dt = DateTime.Now.AddMinutes(-5);

                foreach (EvaluationRequest evaluation in EvaluationRequests)
                {
                    _logger.Info(String.Format("Validate Evaluation {0}", evaluation.Id));

                    string documentName = evaluation.DocumentToEvaluate.Substring(37);
                    string extension = System.IO.Path.GetExtension(documentName);
                    documentName = documentName.Substring(0, documentName.Length - extension.Length);
                    if (!String.IsNullOrEmpty(documentName))
                    {
                        documentName = documentName + "-Return";
                    }

                    string pathFile = Directory.EnumerateFiles(filesFolderPath, documentName + "*", SearchOption.AllDirectories).Where(s => s.EndsWith(".txt")).FirstOrDefault(); ;

                    if (File.Exists(pathFile))
                    {
                        _logger.Info(String.Format("Validate Evaluation {0} PathExists {1}", evaluation.Id, pathFile));

                        TimeSpan span = DateTime.Now - dt;
                        if (string.IsNullOrEmpty(token) || span.Minutes > 1)
                        {
                            //generate a new token if the previous token generated 4 minute back
                            token = GenerateToken(securityUrl);
                            dt = DateTime.Now;
                            _logger.Info(String.Format("Validate Evaluation {0} GenerateToen {1} Span {2}", evaluation.Id, token, span.Minutes));
                        }

                        string url = getDiagnosticsUrl + evaluation.Id;
                        CallGet(url, token, out responseResult);
                        if (!String.IsNullOrEmpty(responseResult))
                        {
                            string fileText = File.ReadAllText(pathFile);
                            var jsonResponse = JsonConvert.SerializeObject(RemoveDates(responseResult));
                            var jsonDocument = JsonConvert.SerializeObject(RemoveDates(fileText));

                            if (Newtonsoft.Json.Linq.JToken.DeepEquals(jsonResponse, jsonDocument))
                            {
                                _logger.Info(String.Format("Validate Evaluation {0} {1} true - Results match", evaluation.Id,
                                    evaluation.Locator));
                            }
                            else
                            {
                                _logger.Info(String.Format("Validate Evaluation {0} {1} false - Results don't match", evaluation.Id,
                                    evaluation.Locator));
                            }
                        }
                        else
                        {
                            _logger.Info(String.Format("Validate Evaluation {0} No Resonpse {1}", evaluation.Id, responseResult));
                        }
                    }
                    else
                    {
                        _logger.Info(String.Format("Validate Evaluation {0} The file with path {1} does not exist", evaluation.Id, filesFolderPath));
                    }
                }
            }
            else { 
                _logger.Info(String.Format("The folder with the files to compare with path '{0}' does not exist", filesFolderPath));
            }
        }

        public static string RemoveDates(string json) {
            dynamic _json = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            try
            {
                _json[0]["StartDate"] = "";
                _json[0]["EndDate"] = "";
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format("RemoveDates unable to update JSON: {0}", json), ex);
            }
            return _json.ToString(); 
        }

        public static string GenerateToken(string securityUrl)
        {
            var securityService = new SecurityService(securityUrl);
            var clearToken = string.Format("UserId={0}", "ValidateWindowsServiceResults");
            return securityService.ContentForPost("Encrypt", clearToken);
        }

        static void CallGet(string url, string token, out string responseResult)
        {
            var client = new HttpClient();
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = new Uri(url);

            if (!String.IsNullOrEmpty(token))
                message.Headers.Add("token", token);

            var response = client.SendAsync(message).Result;

            responseResult = response.Content.ReadAsStringAsync().Result;

        }
    }
}
