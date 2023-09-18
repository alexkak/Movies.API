using Dapper;
using Movies.Application.Database;
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
    }
}
