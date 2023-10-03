
using Microsoft.Extensions.DependencyInjection;
using Movies.Api.Sdk;
using Movies.Api.Sdk.Consumer;
using Movies.Contracts.Requests;
using Refit;
using System.Text.Json;

//var moviesApi = RestService.For<IMoviesApi>("https://localhost:44318");

var services = new ServiceCollection();

//services.AddRefitClient<IMoviesApi>()
//    .ConfigureHttpClient(x =>
//    x.BaseAddress = new Uri("https://localhost:44318"));
services
    .AddHttpClient()
    .AddSingleton<AuthTokenProvider>()
    .AddRefitClient<IMoviesApi>(s => new RefitSettings
    {
        AuthorizationHeaderValueGetter = async(request, cancellationToken) => await s.GetRequiredService<AuthTokenProvider>().GetTokenAsync()
    })
    .ConfigureHttpClient(x =>
    x.BaseAddress = new Uri("https://localhost:44318"));

var provider = services.BuildServiceProvider();

var moviesApi = provider.GetRequiredService<IMoviesApi>();

var movie = await moviesApi.GetMovieAsync("d1b965ee-0a07-45f7-b809-5062b2097126");

var newMovie = await moviesApi.CreateMovieAsync(new CreateMovieRequest
{
    Title = "Spider-Man 2",
    YearOfRelease = 2002,
    Genres = new[] { "Action", "Adventure", "Sci-Fi" }
});

await moviesApi.UpdateMovieAsync(newMovie.Id, new UpdateMovieRequest
{
    Title = "Spider-Man 2",
    YearOfRelease = 2004,
    Genres = new[] { "Action", "Adventure", "Sci-Fi" }
});

await moviesApi.DeleteMovieAsync(newMovie.Id);

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
