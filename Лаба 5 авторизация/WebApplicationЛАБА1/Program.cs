using Db;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using static Db.ApplicationContext;
using System.Collections.Generic;

namespace WebApplicationЛАБА1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Keys keys = new Keys();
            ApplicationContext Db = new ApplicationContext();
         
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.MapGet("/getUsers", (HttpContext context) =>
            {
                return get<User>(context, Db, keys);
            }
            );
            app.MapGet("/getRoles", (HttpContext context) =>
            {
                return get<Role>(context, Db, keys);
            }
            );
            app.MapPost("/postRole", (Role obj, HttpContext context) =>
            {
                post<Role>(context, Db, keys, obj);
            }
            );
            app.MapPost("/postUser", (User obj, HttpContext context) =>
            {
                post<User>(context, Db, keys, obj);
            }
            );
            app.MapPost("/recreate", (HttpContext context) =>
            {
                context.Request.Cookies.TryGetValue("key", out string? key);
                if (keys.isAuth(key))
                {
                    Db.Database.EnsureDeleted();
                    Db.Database.EnsureCreated();
                    Db = new ApplicationContext();
                    Db.DefauldData();
                }                
            }
            );
            app.MapPut("auth", (User objClient) =>
            {
                var usersFind = Db.users
                .Where(obj => obj.login == objClient.login)
                .Where(obj => obj.password == objClient.password)
                //.Where(obj => obj.roleId == objClient.roleId)
                .ToList();
                Key outKey = new Key(0, "0");
                // костыль, тк string не десеализируется, тут без записи userId
                if (usersFind.Count != 0)
                    if (keys.isUser(usersFind.First().id))
                    {
                        outKey.key = keys.getKey(usersFind.First().id);
                    }
                    else
                    {
                        outKey.key = keys.add(usersFind.First().id);
                    }
                return outKey.key;
            }
            );

            app.Run();
        }
        static void post<T>(HttpContext httpContext, ApplicationContext Db, Keys tokenManager, T obj)
        {
            //get cookies 
            httpContext.Request.Cookies.TryGetValue("key", out string? key);

            if (key != null)
            {
                if (tokenManager.isAuth(key))
                {
                    Db.AddAsync(obj);
                    Db.SaveChanges();
                }
                else
                {
                    Console.WriteLine("ERROR post<T>(): tokenManager.isAuth(key) == false");
                }
            }
            else
            {
                Console.WriteLine("ERROR post<T>(): (key != null) == false");
            }
        }
        static List<T> get<T>(HttpContext htttpContext, ApplicationContext Db, Keys tokenManager) where T : class
        {
            //get cookies
            htttpContext.Request.Cookies.TryGetValue("key", out string? key);

            List<T> objs = new List<T>();
            if (key != null)
            {
                if (tokenManager.isAuth(key))
                {
                    objs = Db.Set<T>().ToList();
                    return objs;
                }
                else 
                {
                    Console.WriteLine("ERROR get<T>(): tokenManager.isAuth(key) == false");
                    return objs;
                }
            }
            else 
            {
                Console.WriteLine("ERROR get<T>(): (key != null) == false");
                Console.WriteLine("count objs - " + objs.Count.ToString());
                return objs;
            }
            
        }
    }
    public class Key
    // токен, связанный с user
    {
        public int userId { get; set; }
        public string key { get; set; }
        public Key(int userId, string key)
        {
            this.userId = userId;
            this.key = key;
        }
        public Key(int userId)
        // создание нового токена
        {
            this.userId = userId;
            Random rnd = new Random();
            key = rnd.Next(1, 100000000).ToString();
        }
    }
    public class Keys
    // container of tokens
    {
        private List<Key> keys;
        public Keys()
        {
            keys = new List<Key>();
        }
        
        public string getKey(int userIdIn)
        // получаем токен по userId
        {
            string findKey = "0";
            foreach (var key in keys)
            {
                if (userIdIn == key.userId)
                    findKey = key.key;
            }
            return findKey;
        }
        public bool isAuth(string keyIn)
        // проверка существования токена
        {
            bool isFindKey = false;
            foreach (var key in keys)
            {
                if (keyIn == key.key)
                    isFindKey = true;
            }
            return isFindKey;
        }
        public bool isUser(int userIdIn)
        // проверка наличия уже авторизованного пользователя
        {
            bool isFindKey = false;
            foreach (var key in keys)
            {
                if (userIdIn == key.userId)
                    isFindKey = true;
            }
            return isFindKey;
        }
        public string add(int userId)
        // добавление нового авторизованного пользователя с возвратом токена
        {
            Key newKey = new Key(userId);
            keys.Add(newKey);
            return newKey.key;
        }
    }
}

namespace Db
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> users { get; set; }
        public DbSet<Role> roles { get; set; }

        public ApplicationContext()
        {   
            //Database.EnsureDeleted();
            if (Database.EnsureCreated())
            {
                DefauldData();
            }
        }
        public void DefauldData()
        {
            Role role = new Role { name = "God" };
            roles.Add(role);
            SaveChanges();

            User user = new User { login = "Admin", password = 7410, roleId = 1 };
            users.Add(user);
            SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("DataSource=halloapp.db");
        }
    
        public class  User
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
}