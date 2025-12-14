using Microsoft.Data.Sqlite; // Обратите внимание: используем Sqlite вместо SqlClient
using System.Collections.Generic;

namespace CallAll
{
    public class SubscriptionService
    {
        // База данных будет лежать в файле users.db рядом с exe-файлом
        private const string ConnectionString = "Data Source=users.db";

        public class Subscriber
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string? Username { get; set; }
        }

        // Конструктор: при запуске проверяем, есть ли таблица, и если нет — создаем
        public SubscriptionService()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                // Команда создания таблицы. SQLite типы: INTEGER (для long) и TEXT (для string)
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Subscribers (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Username TEXT
                    );";

                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Метод добавления подписчика
        public bool AddSubscriber(long id, string name, string? username)
        {
            if (UserExists(id)) return false;

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string query = "INSERT INTO Subscribers (Id, Name, Username) VALUES (@Id, @Name, @Username)";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Username", (object?)username ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
            return true;
        }

        // Метод получения всех подписчиков
        public IEnumerable<Subscriber> GetAllSubscribers()
        {
            var list = new List<Subscriber>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name, Username FROM Subscribers";

                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Subscriber
                            {
                                Id = reader.GetInt64(0),
                                Name = reader.GetString(1),
                                Username = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return list;
        }

        // Проверка существования
        private bool UserExists(long id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM Subscribers WHERE Id = @Id";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    // В SQLite Count возвращает long (Int64), поэтому приводим к long
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}