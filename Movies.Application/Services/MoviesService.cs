using Movies.Application.Models;
using Movies.Application.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Services
{
    public class MoviesService : IMoviesService
    {
        private readonly IMovieRepository _movieRepository;

        public MoviesService(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        public Task<bool> CreateAsync(Movie movie)
        {
            return _movieRepository.CreateAsync(movie);
        }
        public Task<Movie?> GetByIdAsync(Guid id)
        {
            return _movieRepository.GetByIdAsync(id);
        }

        public Task<Movie?> GetBySlugAsync(string slug)
        {
            return _movieRepository.GetBySlugAsync(slug);
        }
        public Task<IEnumerable<Movie>> GetAllAsync()
        {
            return _movieRepository.GetAllAsync();
        }
        public async Task<Movie?> UpdateAsync(Movie movie)
        {
            var movieExists = await _movieRepository.ExistsByIdAsync(movie.Id);
            if (!movieExists) 
            { 
                return null;
            }

            await _movieRepository.UpdateAsync(movie);
            return movie;
        }
        public Task<bool> DeleteByIdAsync(Guid id)
        {
            return _movieRepository.DeleteByIdAsync(id);
        }
    }
}
