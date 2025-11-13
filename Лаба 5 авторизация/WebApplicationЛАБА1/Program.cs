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
                // получаем печеньки
                context.Request.Cookies.TryGetValue("key", out string? key);
                List<User> objs = new List<User>();
                objs = Db.users.ToList();
                if (keys.isAuth(key))
                {
                    objs = Db.users.ToList();
                }                
                return objs;
            }
            );
            app.MapGet("/getRoles", (HttpContext context) =>
            {
                context.Request.Cookies.TryGetValue("key", out string? key);
                List<Role> objs = new List<Role>();
                if (keys.isAuth(key))
                {
                    objs = Db.roles.ToList();
                }
                return objs;
            }
            );
            app.MapPost("/postRole", (Role obj, HttpContext context) =>
            {
                context.Request.Cookies.TryGetValue("key", out string? key);
                if (keys.isAuth(key))
                {
                    Db.AddAsync(obj);
                    Db.SaveChanges();
                }
                //Role newRole = new Role { name = obj.name };
                //Db.AddAsync(newRole);
                //Db.SaveChanges();

            }
            );
            app.MapPost("/postUser", (User obj, HttpContext context) =>
            {
                //User newUser = new User { login = obj.login, password = obj.password, roleId = obj.roleId };
                //Db.AddAsync(newUser);
                //Db.SaveChanges();
                context.Request.Cookies.TryGetValue("key", out string? key);
                if (keys.isAuth(key))
                {
                    Db.AddAsync(obj);
                    Db.SaveChanges();
                }
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