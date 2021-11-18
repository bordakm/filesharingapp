using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileHostingAppDesktopClient.Services
{
    public class AuthService
    {
        HttpClient _httpClient;
        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(Settings1.Default.cloudAddress);
        }

        public async Task<LoginResult> Login(string email, string password)
        {
            var payload = new Credentials
            {
                Email = email,
                Password = password
            };
            var stringPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
            string jwt = null;
            bool success = false;
            ((MainWindow)Application.Current.MainWindow).LogMessage("Logging in..");
            try
            {
                var response = await _httpClient.PostAsync("Auth/Login", httpContent);
                if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ((MainWindow)Application.Current.MainWindow).LogMessage("Invalid credentials, try again!");
                } else if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ((MainWindow)Application.Current.MainWindow).LogMessage("Succesful login!");
                    var responseJsonBody = await response.Content.ReadAsStringAsync();
                    try
                    {
                        jwt = JsonConvert.DeserializeObject<TokenResult>(responseJsonBody).Token;
                    } catch(Exception e)
                    {
                        ((MainWindow)Application.Current.MainWindow).LogMessage("Server error!");
                    }
                    success = true;
                }
            }
            catch (Exception e)
            {
                ((MainWindow)Application.Current.MainWindow).LogMessage("Server unavailable");
            }
            
            return new LoginResult
            {
                Success = success,
                Jwt = jwt
            };
        }

        public class LoginResult
        {
            public bool Success { get; set; }
            public string Jwt { get; set; }
        }

        public class TokenResult
        {
            public string Token { get; set; }
        }

        internal class Credentials
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
