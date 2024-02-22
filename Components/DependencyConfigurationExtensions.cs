using System.Linq.Expressions;
using MassTransit.MongoDbIntegration.Saga;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HashCrack.Components;

public static class DependencyConfigurationExtensions
{
    public static IServiceCollection AddMongoDbCollection<T>(this IServiceCollection services,
        Expression<Func<T, Guid>> idPropertyExpression)
        where T : class
    {
        IMongoCollection<T> MongoDbCollectionFactory(IServiceProvider provider)
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            var collectionNameFormatter = DotCaseCollectionNameFormatter.Instance;

            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap(new BsonClassMap<T>(cfg =>
                {
                    cfg.AutoMap();
                    cfg.MapIdProperty(idPropertyExpression);
                }));
            }

            return database.GetCollection<T>(collectionNameFormatter.Collection<T>());
        }

        services.TryAddSingleton(MongoDbCollectionFactory);


        return services;
    }
}