using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Npgsql;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<Role> Roles { get; set; } = new();
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
}

class Program
{
    static void Main()
    {
        string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=pg123;Database=TestDatabase";

        using IDbConnection db = new NpgsqlConnection(connectionString);

        var sqlUsersWithRoles = @"
            SELECT u.id, u.username, u.email,
                   r.id, r.name
            FROM users u
            LEFT JOIN user_roles ur ON u.id = ur.user_id
            LEFT JOIN roles r ON ur.role_id = r.id
            ORDER BY u.id;
        ";

        var userDict = new Dictionary<int, User>();

        var users = db.Query<User, Role, User>(
            sqlUsersWithRoles,
            (user, role) =>
            {
                if (!userDict.TryGetValue(user.Id, out var currentUser))
                {
                    currentUser = user;
                    userDict[user.Id] = currentUser;
                }

                if (role != null)
                    currentUser.Roles.Add(role);

                return currentUser;
            },
            splitOn: "id"
        ).Distinct().ToList();

        Console.WriteLine("Пользователи и их роли:\n");
        foreach (var user in users)
        {
            Console.WriteLine($"Пользователь: {user.Username} ({user.Email})");
            foreach (var role in user.Roles)
            {
                Console.WriteLine($"  - Роль: {role.Name}");
            }
            Console.WriteLine();
        }

        var sqlRoleCounts = @"
            SELECT r.name, COUNT(ur.user_id) AS user_count
            FROM roles r
            LEFT JOIN user_roles ur ON r.id = ur.role_id
            GROUP BY r.name
            ORDER BY r.name;
        ";

        var roleCounts = db.Query<(string RoleName, int UserCount)>(sqlRoleCounts).ToList();

        Console.WriteLine("Количество пользователей по ролям:\n");
        foreach (var rc in roleCounts)
        {
            Console.WriteLine($"Роль: {rc.RoleName} — {rc.UserCount} пользователей");
        }
    }
}
