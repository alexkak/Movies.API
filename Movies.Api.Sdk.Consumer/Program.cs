
using Movies.Api.Sdk;
using Movies.Contracts.Requests;
using Refit;
using System.Text.Json;

var moviesApi = RestService.For<IMoviesApi>("https://localhost:44318");

//var movie = await moviesApi.GetMovieAsync("d1b965ee-0a07-45f7-b809-5062b2097126");

var request = new GetAllMoviesRequest
{
    Title = null,
    Year = null,
    SortBy = null,
    Page = 1,
    PageSize = 3
};

var movies = await moviesApi.GetMoviesAsync(request);

foreach (var movieResponse in movies.Items)
{
    Console.WriteLine(JsonSerializer.Serialize(movieResponse));
}

Console.ReadLine();
