using Movies.Contracts.Requests;
using Movies.Contracts.Responses;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Api.Sdk
{
    public interface IMoviesApi
    {
        [Get(ApiEndpoints.Movies.Get)]
        Task<MovieResponse> GetMovieAsync(string idOrSlug);

        [Get(ApiEndpoints.Movies.GetAll)]
        Task<MoviesResponse> GetMoviesAsync(GetAllMoviesRequest request);
    }
}
