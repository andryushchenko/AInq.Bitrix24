// Copyright 2021-2023 Anton Andryushchenko
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
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="client" /> is NULL </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24Service(this IServiceCollection services, IBitrix24Client client, TimeSpan timeout)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = client ?? throw new ArgumentNullException(nameof(client));
        var proxy = new Bitrix24ConveyorProxy
        {
            Conveyor = services.CreateConveyor(new Bitrix24ConveyorMachine(client, timeout), 1), Portal = client.Portal
        };
        return services.AddSingleton<IBitrix24Client>(proxy);
    }

    /// <summary> Add Bitrix24 client as background service </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="timeout"> Request timeout </param>
    /// <typeparam name="TClient"> Bitrix24 client type </typeparam>
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24Service<TClient>(this IServiceCollection services, TimeSpan timeout)
        where TClient : IBitrix24Client
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        var proxy = new Bitrix24ConveyorProxy();
        proxy.Conveyor = services.CreateConveyor(provider =>
            {
                var client = provider.GetRequiredService<TClient>();
                proxy.Portal = client.Portal;
                return new Bitrix24ConveyorMachine(client, timeout);
            },
            ReuseStrategy.Static,
            maxAttempts: 1);
        return services.AddSingleton<IBitrix24Client>(proxy);
    }

    /// <summary> Add Bitrix24 client as background service </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="clientFactory"> Bitrix24 client factory </param>
    /// <param name="timeout"> Request timeout </param>
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="clientFactory" /> is NULL </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24Service(this IServiceCollection services, Func<IServiceProvider, IBitrix24Client> clientFactory,
        TimeSpan timeout)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        var proxy = new Bitrix24ConveyorProxy();
        proxy.Conveyor = services.CreateConveyor(provider =>
            {
                var client = clientFactory.Invoke(provider);
                proxy.Portal = client.Portal;
                return new Bitrix24ConveyorMachine(client, timeout);
            },
            ReuseStrategy.Static,
            maxAttempts: 1);
        return services.AddSingleton<IBitrix24Client>(proxy);
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="client"> Bitrix24 client instance </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="client" /> is NULL </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24PriorityService(this IServiceCollection services, IBitrix24Client client, TimeSpan timeout,
        int maxPriority = 100)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = client ?? throw new ArgumentNullException(nameof(client));
        var proxy = new Bitrix24PriorityConveyorProxy
        {
            Conveyor = services.CreatePriorityConveyor(new Bitrix24ConveyorMachine(client, timeout), maxPriority, 1), Portal = client.Portal
        };
        return services.AddSingleton<IBitrix24PriorityService>(proxy).AddSingleton<IBitrix24Client>(proxy);
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <typeparam name="TClient"> Bitrix24 client type </typeparam>
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24PriorityService<TClient>(this IServiceCollection services, TimeSpan timeout, int maxPriority = 100)
        where TClient : IBitrix24Client
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        var proxy = new Bitrix24PriorityConveyorProxy();
        proxy.Conveyor = services.CreatePriorityConveyor(provider =>
            {
                var client = provider.GetRequiredService<TClient>();
                proxy.Portal = client.Portal;
                return new Bitrix24ConveyorMachine(client, timeout);
            },
            ReuseStrategy.Static,
            maxPriority: maxPriority,
            maxAttempts: 1);
        return services.AddSingleton<IBitrix24PriorityService>(proxy).AddSingleton<IBitrix24Client>(proxy);
    }

    /// <summary> Add Bitrix24 client as background service with prioritization </summary>
    /// <param name="services"> Service collection </param>
    /// <param name="clientFactory"> Bitrix24 client factory </param>
    /// <param name="timeout"> Request timeout </param>
    /// <param name="maxPriority"> Max allowed operation priority </param>
    /// <remarks> DO NOT use <see cref="IBitrix24Client" /> implementations with internal timeout control </remarks>
    /// <exception cref="InvalidOperationException"> Thrown if service already exists </exception>
    /// <exception cref="ArgumentOutOfRangeException"> Thrown if <paramref name="timeout" /> value is invalid </exception>
    /// <exception cref="ArgumentNullException"> Thrown if <paramref name="clientFactory" /> is NULL </exception>
    [PublicAPI]
    public static IServiceCollection AddBitrix24PriorityService(this IServiceCollection services,
        Func<IServiceProvider, IBitrix24Client> clientFactory, TimeSpan timeout, int maxPriority = 100)
    {
        if (services.Any(service => service.ImplementationType == typeof(IBitrix24Client)))
            throw new InvalidOperationException("Service already exists");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _ = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        var proxy = new Bitrix24PriorityConveyorProxy();
        proxy.Conveyor = services.CreatePriorityConveyor(provider =>
            {
                var client = clientFactory.Invoke(provider);
                proxy.Portal = client.Portal;
                return new Bitrix24ConveyorMachine(client, timeout);
            },
            ReuseStrategy.Static,
            maxPriority: maxPriority,
            maxAttempts: 1);
        return services.AddSingleton<IBitrix24PriorityService>(proxy).AddSingleton<IBitrix24Client>(proxy);
    }
}
