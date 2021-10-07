// Copyright 2021 Anton Andryushchenko
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using AInq.Background;
using Microsoft.Extensions.DependencyInjection;

namespace AInq.Bitrix24;

/// <summary> Bitrix24 client dependency injection </summary>
public static class Bitrix24Injection
{
    /// <summary> Add Bitrix24 client as background service </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="client"> Bitrix24 client instance </param>
    /// <param name="timeout"> Request timeout </param>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="client" /> is NULL </exception>
    public static IServiceCollection AddBitrix24Service(this IServiceCollection services, IBitrix24Client client, TimeSpan timeout)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = client ?? throw new ArgumentNullException(nameof(client));
        return services.AddSingleton<IBitrix24Client>(new Bitrix24Service(services.CreateConveyor(new Bitrix24ConveyorMachine(client, timeout), 1)));
    }

    /// <summary> Add Bitrix24 client as background service </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="timeout"> Request timeout </param>
    /// <typeparam name="TClient"> Bitrix24 client type </typeparam>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    public static IServiceCollection AddBitrix24Service<TClient>(this IServiceCollection services, TimeSpan timeout)
        where TClient : IBitrix24Client
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        return services.AddSingleton<IBitrix24Client>(new Bitrix24Service(services.CreateConveyor(
            provider => new Bitrix24ConveyorMachine(provider.GetRequiredService<TClient>(), timeout),
            ReuseStrategy.Static,
            maxAttempts: 1)));
    }

    /// <summary> Add Bitrix24 client as background service </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="clientFactory"> Bitrix24 client factory </param>
    /// <param name="timeout"> Request timeout </param>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="clientFactory" /> is NULL </exception>
    public static IServiceCollection AddBitrix24Service(this IServiceCollection services, Func<IServiceProvider, IBitrix24Client> clientFactory,
        TimeSpan timeout)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        return services.AddSingleton<IBitrix24Client>(new Bitrix24Service(services.CreateConveyor(
            provider => new Bitrix24ConveyorMachine(clientFactory.Invoke(provider), timeout),
            ReuseStrategy.Static,
            maxAttempts: 1)));
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="client"> Bitrix24 client instance </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="client" /> is NULL </exception>
    public static IServiceCollection AddBitrix24PriorityService(this IServiceCollection services, IBitrix24Client client, TimeSpan timeout,
        int maxPriority = 100)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = client ?? throw new ArgumentNullException(nameof(client));
        return services.AddSingleton(new Bitrix24PriorityService(
                           services.CreatePriorityConveyor(new Bitrix24ConveyorMachine(client, timeout), maxPriority, 1)))
                       .AddSingleton<IBitrix24Client>(provider => provider.GetRequiredService<Bitrix24PriorityService>());
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <typeparam name="TClient"> Bitrix24 client type </typeparam>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    public static IServiceCollection AddBitrix24PriorityService<TClient>(this IServiceCollection services, TimeSpan timeout, int maxPriority = 100)
        where TClient : IBitrix24Client
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        return services.AddSingleton(new Bitrix24PriorityService(services.CreatePriorityConveyor(
                           provider => new Bitrix24ConveyorMachine(provider.GetRequiredService<TClient>(), timeout),
                           ReuseStrategy.Static,
                           maxPriority: maxPriority,
                           maxAttempts: 1)))
                       .AddSingleton<IBitrix24Client>(provider => provider.GetRequiredService<Bitrix24PriorityService>());
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="clientFactory"> Bitrix24 client factory </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <remarks> DO NOT use client implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="clientFactory" /> is NULL </exception>
    public static IServiceCollection AddBitrix24PriorityService(this IServiceCollection services,
        Func<IServiceProvider, IBitrix24Client> clientFactory,
        TimeSpan timeout, int maxPriority = 100)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        return services.AddSingleton(new Bitrix24PriorityService(services.CreatePriorityConveyor(
                           provider => new Bitrix24ConveyorMachine(clientFactory.Invoke(provider), timeout),
                           ReuseStrategy.Static,
                           maxPriority: maxPriority,
                           maxAttempts: 1)))
                       .AddSingleton<IBitrix24Client>(provider => provider.GetRequiredService<Bitrix24PriorityService>());
    }
}
