﻿using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public MovieRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                insert into movies (id, slug, title, yearofrelease)
                values (@Id, @Slug, @Title, @YearOfRelease)
                """, movie, transaction, cancellationToken: token));

            if (result > 0)
            {
                foreach (var genre in movie.Genres)
                {
                    await connection.ExecuteAsync(new CommandDefinition("""
                        insert into genres (movieId, name)
                        values (@MovieId, @Name)
                        """, new { MovieId = movie.Id, Name = genre }, transaction, cancellationToken: token));
                }
            }

            transaction.Commit();

            return result > 0;
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
                new CommandDefinition("""
                    select m.*, CONVERT(decimal(5,2),AVG(CONVERT(decimal(5,2),r.rating))) as rating, myr.rating as userrating  
                    from movies m
                    left join ratings r on m.id = r.movieid
                    left join ratings myr on m.id = myr.movieid
                        and myr.userid = @userId
                    where id = @id
                    group by m.id, m.slug, m.title, m.yearofrelease,myr.rating
                    """, new { id, userId }, cancellationToken: token));

            if (movie is null)
            {
                return null;
            }

            var genres = await connection.QueryAsync<string>(
                new CommandDefinition("""
                    select name from genres where movieid = @id
                    """, new { id }, cancellationToken: token));

            foreach (var genre in genres)
            { 
                movie.Genres.Add(genre);
            }

            return movie;
        }

        public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
                new CommandDefinition("""
                    select m.*, CONVERT(decimal(5,2),AVG(CONVERT(decimal(5,2),r.rating))) as rating, myr.rating as userrating  
                    from movies m
                    left join ratings r on m.id = r.movieid
                    left join ratings myr on m.id = myr.movieid
                        and myr.userid = @userId
                    where slug = @slug
                    group by m.id, m.slug, m.title, m.yearofrelease,myr.rating
                    """, new { slug, userId }, cancellationToken: token));

            if (movie is null)
            {
                return null;
            }

            var genres = await connection.QueryAsync<string>(
                new CommandDefinition("""
                    select name from genres where movieid = @id
                    """, new { id = movie.Id }, cancellationToken: token));

            foreach (var genre in genres)
            {
                movie.Genres.Add(genre);
            }

            return movie;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

            var orderClause = "order by m.id"; //string.Empty;
            if (options.SortField is not null)
            {
                orderClause = $"""
                    order by m.{options.SortField} {(options.SortOrder == SortOrder.Ascending ? "asc" : "desc")}
                    """;
            }

            var result = await connection.QueryAsync(new CommandDefinition($"""
                select m.*, 
                        (
                            SELECT STRING_AGG( g.name, ',') 
                            FROM genres g 
                            WHERE g.movieId = m.id
                        ) AS genres,
                        CONVERT(DECIMAL(5, 2), AVG(CONVERT(DECIMAL(5, 2), r.rating))) AS rating,
                        myr.rating AS userrating
                from movies m
                left join ratings r on m.id = r.movieid
                left join ratings myr on m.id = myr.movieid
                    and myr.userid = @userId
                where (@title is null or m.title like @title)
                and (@yearofrelease is null or m.yearofrelease = @yearofrelease)
                group by m.id,m.slug,m.title,m.yearofrelease,myr.rating {orderClause}
                OFFSET @pageOffset ROWS 
                FETCH NEXT @pageSize ROWS ONLY
                """, new 
            { 
                userId = options.UserId,
                title = '%' + options.Title + '%',
                yearofrelease = options.YearOfRelease,
                pageSize = options.PageSize,
                pageOffset = (options.Page - 1) * options.PageSize
            }, cancellationToken: token));

            return result.Select(x => new Movie
            {
                Id = x.id,
                Title = x.title,
                YearOfRelease = x.yearofrelease,
                Rating = (float?)x.rating,
                UserRating = (int?)x.userrating,
                Genres = Enumerable.ToList(x.genres.Split(','))
            });
        }

        public async Task<bool> UpdateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition("""
                delete from genres where movieid = @id
                """, new { id = movie.Id }, transaction, cancellationToken: token));

            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into genres (movieId, name)
                    values (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre}, transaction, cancellationToken: token));
            }

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                update movies set slug = @Slug, title = @Title, yearofrelease = @YearOfRelease
                where id = @Id
                """, movie, transaction, cancellationToken: token));

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition("""
                delete from genres where movieid = @id
                """, new { id }, transaction, cancellationToken: token));

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                delete from movies where id = @id
                """, new { id }, transaction, cancellationToken: token));

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                select count(*) from movies where id = @id
                """, new { id }, cancellationToken: token));

        }

        public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken token)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.QuerySingleAsync<int>(new CommandDefinition("""
                select count(id) from movies
                where (@title is null or title like @title)
                and (@yearofrelease is null or yearofrelease = @yearofrelease) 
                """, new
            { 
                title = '%' + title + '%',
                yearOfRelease,
            }, cancellationToken: token));
        }
    }
}
