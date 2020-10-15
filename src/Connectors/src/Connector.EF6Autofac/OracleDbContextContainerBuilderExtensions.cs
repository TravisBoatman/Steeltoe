﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Oracle;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac
{
    public static class OracleDbContextContainerBuilderExtensions
    {
        /// <summary>
        /// Add your Oracle-based DbContext to the ContainerBuilder
        /// </summary>
        /// <typeparam name="TContext">Your DbContext</typeparam>
        /// <param name="container">Autofac <see cref="ContainerBuilder" /></param>
        /// <param name="config">Your app config</param>
        /// <param name="serviceName">Name of service instance</param>
        /// <returns><see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/></returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterDbContext<TContext>(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var info = serviceName == null
                ? config.GetSingletonServiceInfo<OracleServiceInfo>()
                : config.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

            var oracleConfig = new OracleProviderConnectorOptions(config);
            var factory = new OracleProviderConnectorFactory(info, oracleConfig, typeof(TContext));
            return container.Register(c => factory.Create(null)).As<TContext>();
        }
    }
}