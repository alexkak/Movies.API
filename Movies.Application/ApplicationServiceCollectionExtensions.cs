﻿using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Database;
using Movies.Application.Repositories;
using Movies.Application.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<IRatingRepository, RatingRepository>();
            services.AddSingleton<IRatingService, RatingService>();
            services.AddSingleton<IMovieRepository, MovieRepository>();
            services.AddSingleton<IMoviesService, MoviesService>();
            services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services,
            string connectionString)
        {
            services.AddSingleton<IDbConnectionFactory>(_ => 
                new SqlserverConnectionFactory(connectionString));
            services.AddSingleton<DbInitializer>();
            return services;
        }
    }
}
