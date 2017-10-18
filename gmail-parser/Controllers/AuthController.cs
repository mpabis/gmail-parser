using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace gmail_parser.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        static string[] Scopes = { GmailService.Scope.GmailReadonly };
        static string ApplicationName = "gmail to moneylover watchdog";
        public AuthController(ILogger<AuthController> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;

        [HttpGet("callback")]
        [HttpPost("callback")]
        public Task<IActionResult> Callback()
        {
            _logger.LogInformation("query string:" + Request.QueryString.ToString());
            var bodyContent = new StreamReader(Request.Body).ReadToEnd();
            _logger.LogInformation("body:" + bodyContent);
            return Task.FromResult<IActionResult>(Ok());
        }


        [HttpGet("setup")]
        public IActionResult Setup()
        {
            UserCredential credential = GetCredentials();

            _logger.LogInformation($"Got credentials {credential?.Token?.AccessToken}");

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            UsersResource.LabelsResource.ListRequest request = service.Users.Labels.List("mpabis@gmail.com");
            _logger.LogInformation($"Got request {request?.MethodName}");

            // List labels.
            IList<Label> labels = request.Execute().Labels;
            _logger.LogInformation("Labels:");
            if (labels != null && labels.Count > 0)
            {
                foreach (var labelItem in labels)
                {
                    _logger.LogInformation("{0}", labelItem.Name);
                }
            }
            else
            {
                _logger.LogInformation("No labels found.");
            }
            return Ok();
        }

        private UserCredential GetCredentials()
        {
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                _logger.LogInformation("Calling GoogleAuthorizationCodeFlow");
                var credential = new UserCredential(
                    new GoogleAuthorizationCodeFlow(
                        new GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = GoogleClientSecrets.Load(stream).Secrets,
                            Scopes = Scopes,
                        }),
                    "mpabis@gmail.com",
                    null);
                _logger.LogInformation($"Credential {credential}");
                return credential;
            }
        }

        private async Task<UserCredential> GetCredentials2()
        {
            UserCredential credential;
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/gmail-dotnet-quickstart.json");

                _logger.LogInformation("Calling AuthorizeAsync");
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    /*new FileDataStore(credPath, true)*/
                    new NullDataStore()
                );
                _logger.LogInformation($"Credential {credential}");
            }
            return credential;
        }
    }
}
