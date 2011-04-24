﻿// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.BusConfigurators
{
	using System;
	using System.Collections.Generic;
	using MassTransit.Builders;
	using MassTransit.Configuration;
	using MassTransit.EndpointConfigurators;
	using MassTransit.Exceptions;
	using log4net;
	using MassTransit.Transports;

	public class ServiceBusConfiguratorImpl :
		ServiceBusConfigurator
	{
		static readonly ILog _log = LogManager.GetLogger(typeof (ServiceBusConfiguratorImpl));

		readonly IList<BusBuilderConfigurator> _configurators;
		readonly ServiceBusSettings _settings;
		Func<BusSettings, IEndpointCache, BusBuilder> _builderFactory;

		EndpointFactoryConfigurator _endpointFactoryConfigurator;

		public ServiceBusConfiguratorImpl(ServiceBusConfiguratorDefaultSettings defaultSettings)
		{
			_settings = new ServiceBusSettings(defaultSettings);

			_builderFactory = DefaultBuilderFactory;
			_configurators = new List<BusBuilderConfigurator>();

			_endpointFactoryConfigurator = new EndpointFactoryConfiguratorImpl();
		}

		public void Validate()
		{
			if (_builderFactory == null)
				throw new ConfigurationException("The builder factory can not be null.");
			if (_settings.InputAddress == null)
				throw new ConfigurationException("The input address can not be null.");

			_configurators.Each(configurator => configurator.Validate());

			_endpointFactoryConfigurator.Validate();
		}

		public void UseBusBuilder(Func<BusSettings, IEndpointCache, BusBuilder> builderFactory)
		{
			_builderFactory = builderFactory;
		}

		public void AddBusConfigurator(BusBuilderConfigurator configurator)
		{
			_configurators.Add(configurator);
		}

		public void ReceiveFrom(Uri uri)
		{
			_settings.InputAddress = uri;
		}

		public void BeforeConsumingMessage(Action beforeConsume)
		{
			if (_settings.BeforeConsume == null)
				_settings.BeforeConsume = beforeConsume;
			else
				_settings.BeforeConsume += beforeConsume;
		}

		public void AfterConsumingMessage(Action afterConsume)
		{
			if (_settings.AfterConsume == null)
				_settings.AfterConsume = afterConsume;
			else
				_settings.AfterConsume += afterConsume;
		}

		public void SetObjectBuilder(IObjectBuilder objectBuilder)
		{
			_settings.ObjectBuilder = objectBuilder;
		}

		public void UseEndpointFactoryBuilder(Func<EndpointFactoryBuilder> endpointFactoryBuilderFactory)
		{
			_endpointFactoryConfigurator.UseEndpointFactoryBuilder(endpointFactoryBuilderFactory);
		}

		public void AddEndpointFactoryConfigurator(EndpointFactoryBuilderConfigurator configurator)
		{
			_endpointFactoryConfigurator.AddEndpointFactoryConfigurator(configurator);
		}

		public IServiceBus CreateServiceBus()
		{
			_log.InfoFormat("MassTransit v{0}, .NET Framework v{1}", typeof (ServiceBusFactory).Assembly.GetName().Version,
				Environment.Version);

			IEndpointCache endpointCache = CreateEndpointCache();

			BusBuilder builder = _builderFactory(_settings, endpointCache);

			foreach (BusBuilderConfigurator configurator in _configurators)
			{
				builder = configurator.Configure(builder);
			}

			IServiceBus bus = builder.Build();

			return bus;
		}

		public IEndpointFactory CreateEndpointFactory()
		{
			return _endpointFactoryConfigurator.CreateEndpointFactory();
		}

		IEndpointCache CreateEndpointCache()
		{
			IEndpointFactory endpointFactory = _endpointFactoryConfigurator.CreateEndpointFactory();

			IEndpointCache endpointCache = new EndpointCache(endpointFactory);

			return endpointCache;
		}

		static BusBuilder DefaultBuilderFactory(BusSettings settings, IEndpointCache endpointCache)
		{
			return new ServiceBusBuilderImpl(settings, endpointCache);
		}
	}
}