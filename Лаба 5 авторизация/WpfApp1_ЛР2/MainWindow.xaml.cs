using Db;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace WpfApp1_ЛР2
{
    
    public partial class MainWindow : Window
    {
        
        HttpClient httpClient;
        Uri uri;
        CookieContainer cookies;

        public MainWindow()
        {
            InitializeComponent();            
            uri = new Uri("http://localhost:5018");
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultNetworkCredentials;
            httpClient = new HttpClient(handler);
            cookies = new CookieContainer();
            // устанавливаем куки-ключ
            cookies.Add(uri, new Cookie("key", "0"));
            // устанавливаем заголовок cookie
            httpClient.DefaultRequestHeaders.Add("cookie", cookies.GetCookieHeader(uri));
        }        
        
        private async void btnSetPort_Click(object sender, RoutedEventArgs e)
        {
            uri = new Uri(TB_URI.Text);
        }

        private async void GetUserRoleB(object sender, RoutedEventArgs e)
        {
            List<User>? objs = await httpClient.GetFromJsonAsync<List<User>>(uri + "getUsers");

            foreach (var obj in objs)
            {
                textBox_db_user.Text += "\n" + obj.role.id + " " + obj.login + " " + obj.password;
            }
        }
        private async Task setDbUsers()
        {
            var obj = new User 
            { login = textbox_login.Text, 
               password = int.Parse(textbox_password.Text), 
               roleId = int.Parse(TB_RoleID.Text) 
            };
            var response = await httpClient.PostAsJsonAsync(uri + "postUser", obj);
        }

        private async void setDBuserB(object sender, RoutedEventArgs e)
        {
            await setDbUsers();
        }

        private async Task GetDbRoles()
        {
            //сделать по аналогии с async Task getDbUsers()
            var objs = await httpClient.GetFromJsonAsync<List<Role>>(uri + "getRoles");
            foreach (var obj in objs)
            {
                textBox_db_role.Text += "\n" + obj.id + " " + obj.name;
            }
        }

        private async Task SetDbRoles()
        {
            //сделать по аналогии с async Task setDbUsers()
            var obj = new Role
            {
                name = TB_NewRole.Text
            };
            var response = await httpClient.PostAsJsonAsync(uri + "postRole", obj);
        }

        private async void GetRolesB_Click(object sender, RoutedEventArgs e)
        {
            await GetDbRoles();
        }

        private async void AddRoleB_Click(object sender, RoutedEventArgs e)
        {
            await SetDbRoles();
        }

        private async void ReCreateDB_Click(object sender, RoutedEventArgs e)
        {
            int a = 0;
            await httpClient.PostAsJsonAsync(uri + "recreate", a);
        }

        private async void BtnAuth_Click(object sender, RoutedEventArgs e)
        {
            var obj = new User
            {
                login = TB_LOGIN_AUTH.Text,
                password = int.Parse(TB_PASSWORD_AUTH.Text),
                //roleId = int.Parse(TB_RoleID_AUTH.Text)
            };
            var responce = await httpClient.PutAsJsonAsync(uri + "auth", obj);
            if (responce.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // десериализация Content в ответе responce в Key
                string responceText = await responce.Content.ReadAsStringAsync();
                TB_KEYTOKEN.Text = responceText;
                // устанавливаем новый обновленный куки-ключ
                cookies = new CookieContainer();
                cookies.Add(uri, new Cookie("key", responceText));
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("cookie", cookies.GetCookieHeader(uri));
            }
        }

        private async void btnNgrokSkipBrowserWarning_Click(object sender, RoutedEventArgs e)
        {
            await ngrokSkipBrowserWarning();
        }
        public async Task ngrokSkipBrowserWarning()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            // устанавливаем оба заголовка
            request.Headers.Add("ngrok-skip-browser-warning", "000000");
            using var responce = await httpClient.SendAsync(request);
            var responceText = await responce.Content.ReadAsStringAsync();
        }                
    }   
}

namespace Db
{    
    public class User
    {
        public int id { get; set; }
        public string login { get; set; }
        public int password { get; set; }
        public int roleId { get; set; }
        public Role role { get; set; }
    }
    public class Role
    {
        public int id { get; set; }
        public string name { get; set; }        
    }
}
