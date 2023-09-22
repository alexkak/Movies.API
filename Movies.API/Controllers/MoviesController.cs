﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    //[Route("api")]
    public class MoviesController : ControllerBase
    {
        private readonly IMoviesService _movieService;

        public MoviesController(IMoviesService movieService)
        {
            _movieService = movieService;
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPost(ApiEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody]CreateMovieRequest request,
            CancellationToken token)
        {
            var movie = request.MapToMovie();
            await _movieService.CreateAsync(movie, token);
            return CreatedAtAction(nameof(Get), new { idOrSlug = movie.Id }, movie);
            //return Created($"/{ApiEndpoints.Movies.Create}/{movie.Id}", movie); 
            // I shouldn't return "movie" but rather map "movie" to a new MovieResponse object and return that. Only accept and return contracts
        }

        [HttpGet(ApiEndpoints.Movies.Get)]
        public async Task<IActionResult> Get([FromRoute] string idOrSlug,
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
                Href = linkGenerator.GetPathByAction(HttpContext, nameof(Get), values: new { idOrSlug = movie.Id}),
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

            var response = updatedMovie.MapToResponse();
            return Ok(response);
        }

        [Authorize(AuthConstants.AdminUserPolicyName)]
        [HttpDelete(ApiEndpoints.Movies.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id,
            CancellationToken token)
        { 
            var deleted = await _movieService.DeleteByIdAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
