
using Movies.Api.Sdk;
using Refit;
using System.Text.Json;

var moviesApi = RestService.For<IMoviesApi>("https://localhost:44318");

var movie = await moviesApi.GetMoviesAsync("d1b965ee-0a07-45f7-b809-5062b2097126");

Console.WriteLine(JsonSerializer.Serialize(movie));
