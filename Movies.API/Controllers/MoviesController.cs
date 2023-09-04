using Microsoft.AspNetCore.Mvc;
using Movies.API.Mapping;
using Movies.Application.Models;
using Movies.Application.Repositories;
using Movies.Contracts.Requests;

namespace Movies.API.Controllers
{
    [ApiController]
    //[Route("api")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieRepository _movieRepository;

        public MoviesController(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        [HttpPost(ApiEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody]CreateMovieRequest request)
        {
            var movie = request.MapToMovie();
            await _movieRepository.CreateAsync(movie);
            return Created($"/{ApiEndpoints.Movies.Create}/{movie.Id}", movie); // I shouldn't return "movie" but rather map "movie" to a new MovieResponse object and return that. Only accept and return contracts
        }
    }
}
