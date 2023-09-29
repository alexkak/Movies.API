using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.API.Auth;
using Movies.API.Mapping;
using Movies.Application.Models;
using Movies.Application.Repositories;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.API.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    //[Route("api")]
    public class MoviesController : ControllerBase
    {
        private readonly IMoviesService _movieService;
        private readonly IOutputCacheStore _outputCacheStore;

        public MoviesController(IMoviesService movieService, IOutputCacheStore outputCacheStore)
        {
            _movieService = movieService;
            _outputCacheStore = outputCacheStore;
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPost(ApiEndpoints.Movies.Create)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMovieRequest request,
            CancellationToken token)
        {
            var movie = request.MapToMovie();
            await _movieService.CreateAsync(movie, token);
            await _outputCacheStore.EvictByTagAsync("movies", token);
            return CreatedAtAction(nameof(GetV1), new { idOrSlug = movie.Id }, movie);
            //return Created($"/{ApiEndpoints.Movies.Create}/{movie.Id}", movie); 
            // I shouldn't return "movie" but rather map "movie" to a new MovieResponse object and return that. Only accept and return contracts
        }

        [HttpGet(ApiEndpoints.Movies.Get)]
        [OutputCache(PolicyName = "MovieCache")]
        //[ResponseCache(Duration = 30, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetV1([FromRoute] string idOrSlug,
            [FromServices] LinkGenerator linkGenerator,
            CancellationToken token)
        {
            var userId = HttpContext.GetUserId();

            var movie = Guid.TryParse(idOrSlug, out var id)
                ? await _movieService.GetByIdAsync(id, userId, token)
                : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
            if (movie is null)
            {
                return NotFound();
            }

            var response = movie.MapToResponse();

            var movieObj = new { id = movie.Id };
            response.Links.Add(new Link
            {
                Href = linkGenerator.GetPathByAction(HttpContext, nameof(GetV1), values: new { idOrSlug = movie.Id}),
                Rel = "self",
                Type = "GET"
            });

            response.Links.Add(new Link
            {
                Href = linkGenerator.GetPathByAction(HttpContext, nameof(Update), values: new { movieObj.id }),
                Rel = "self",
                Type = "PUT"
            });

            response.Links.Add(new Link
            {
                Href = linkGenerator.GetPathByAction(HttpContext, nameof(Delete), values: new { movieObj.id }),
                Rel = "self",
                Type = "DELETE"
            });

            return Ok(response);
        }

        [HttpGet(ApiEndpoints.Movies.GetAll)]
        [OutputCache(PolicyName = "MovieCache")]
        //[ResponseCache(Duration = 30, VaryByQueryKeys = new[] {"title", "year", "sortBy", "page", "pageSize"}, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request,
            CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var options = request.MapToOptions()
                .WithUser(userId);
            var movies = await _movieService.GetAllAsync(options, token);
            var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
            var moviesResponse = movies.MapToResponse(request.Page, request.PageSize, movieCount);
            return Ok(moviesResponse);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPut(ApiEndpoints.Movies.Update)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([FromRoute] Guid id,
            [FromBody] UpdateMovieRequest request,
            CancellationToken token)
        {
            var movie = request.MapToMovie(id);
            var userId = HttpContext.GetUserId();
            var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);

            if (updatedMovie is null)
            {
                return NotFound();
            }

            await _outputCacheStore.EvictByTagAsync("movies", token);
            var response = updatedMovie.MapToResponse();
            return Ok(response);
        }

        [Authorize(AuthConstants.AdminUserPolicyName)]
        [HttpDelete(ApiEndpoints.Movies.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id,
            CancellationToken token)
        { 
            var deleted = await _movieService.DeleteByIdAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }

            await _outputCacheStore.EvictByTagAsync("movies", token);
            return Ok();
        }
    }
}
