using Microsoft.AspNetCore.Mvc;
using Movies.API.Mapping;
using Movies.Application.Models;
using Movies.Application.Repositories;
using Movies.Application.Services;
using Movies.Contracts.Requests;

namespace Movies.API.Controllers
{
    [ApiController]
    //[Route("api")]
    public class MoviesController : ControllerBase
    {
        private readonly IMoviesService _movieService;

        public MoviesController(IMoviesService movieService)
        {
            _movieService = movieService;
        }

        [HttpPost(ApiEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody]CreateMovieRequest request)
        {
            var movie = request.MapToMovie();
            await _movieService.CreateAsync(movie);
            return CreatedAtAction(nameof(Get), new { idOrSlug = movie.Id }, movie);
            //return Created($"/{ApiEndpoints.Movies.Create}/{movie.Id}", movie); 
            // I shouldn't return "movie" but rather map "movie" to a new MovieResponse object and return that. Only accept and return contracts
        }

        [HttpGet(ApiEndpoints.Movies.Get)]
        public async Task<IActionResult> Get([FromRoute] string idOrSlug)
        { 
            var movie = Guid.TryParse(idOrSlug, out var id)
                ? await _movieService.GetByIdAsync(id)
                : await _movieService.GetBySlugAsync(idOrSlug);
            if (movie is null)
            {
                return NotFound();
            }

            var response = movie.MapToResponse();
            return Ok(response);
        }

        [HttpGet(ApiEndpoints.Movies.GetAll)]
        public async Task<IActionResult> GetAll()
        { 
            var movies = await _movieService.GetAllAsync();

            var moviesResponse = movies.MapToResponse();
            return Ok(moviesResponse);
        }

        [HttpPut(ApiEndpoints.Movies.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid id,
            [FromBody] UpdateMovieRequest request)
        {
            var movie = request.MapToMovie(id);
            var updatedMovie = await _movieService.UpdateAsync(movie);

            if (updatedMovie is null)
            {
                return NotFound();
            }

            var response = updatedMovie.MapToResponse();
            return Ok(response);
        }

        [HttpDelete(ApiEndpoints.Movies.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        { 
            var deleted = await _movieService.DeleteByIdAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
