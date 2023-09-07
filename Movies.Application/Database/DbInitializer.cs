using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Database
{
    public class DbInitializer
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DbInitializer(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task InitializeAsync()
        { 
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();

            await connection.ExecuteAsync("""
            IF OBJECT_ID(N'dbo.movies', N'U') IS NULL
            create table dbo.movies(
            id uniqueidentifier primary key,
            slug VARCHAR(max) not null,
            title VARCHAR(max) not null,
            yearofrelease integer not null);
            """);

            await connection.ExecuteAsync("""
            IF OBJECT_ID(N'dbo.movies', N'U') IS NULL
            create unique index movies_slug_idx
            on dbo.movies (id)
            """);

            await connection.ExecuteAsync("""
                IF OBJECT_ID(N'dbo.genres', N'U') IS NULL
                CREATE TABLE dbo.genres (
                movieId uniqueidentifier FOREIGN KEY REFERENCES movies(id),
                name varchar(max) NOT NULL
            );
            """);
        }
    }
}
