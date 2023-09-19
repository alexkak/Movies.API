using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Repositories
{
    public class RatingRepository : IRatingRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public RatingRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<bool> RatingMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO ratings (userid, movieid, rating)
                VALUES (@userId, @movieId, @rating);

                IF @@ROWCOUNT = 0 
                BEGIN
                    UPDATE ratings
                    SET rating = @rating
                    WHERE userid = @userId AND movieid = @movieId;
                END
                """, new { userId, movieId, rating}, cancellationToken: token));

            return result > 0;
        }
        public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
                select CONVERT(DECIMAL(5, 2), AVG(CONVERT(DECIMAL(5, 2), r.rating))) from ratings r
                where movieid = @movieId
                """, new { movieId }, cancellationToken: token));
        }

        public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(new CommandDefinition("""
                 select
                         CONVERT(DECIMAL(5, 2), AVG(CONVERT(DECIMAL(5, 2), rating))),
                         (select top 1 rating
                		  from ratings
                		  where movieid = @movieId
                			and userid = @userId)
                 from ratings
                 where movieid = @movieId
                """, new { movieId, userId }, cancellationToken: token));
        }

        public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                delete from ratings
                where movieid = @movieId
                and userid = @userId
                """, new { userId, movieId }, cancellationToken: token));

            return result > 0;
        }

        public async Task<IEnumerable<MovieRating>> GetRatingForUserAsync(Guid userId, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.QueryAsync<MovieRating>(new CommandDefinition("""
                 select r.rating, r.movieid, m.slug
                 from ratings r
                 inner join movies m on r.movieid = m.id
                 where userid = @userId
                """, new { userId }, cancellationToken: token));
        }
    }
}
