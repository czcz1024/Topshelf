﻿// Copyright 2007-2011 The Apache Software Foundation.
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
namespace Topshelf.Builders
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using Extensions;
	using Hosts;
	using log4net;
	using Magnum.Extensions;
	using Model;
	using Stact;


	public class RunBuilder :
		HostBuilder
	{
		static readonly ILog _log = LogManager.GetLogger("Topshelf.Builders.RunBuilder");

		readonly IList<Action<IServiceCoordinator>> _postStartActions = new List<Action<IServiceCoordinator>>();
		readonly IList<Action<IServiceCoordinator>> _postStopActions = new List<Action<IServiceCoordinator>>();
		readonly IList<Action<IServiceCoordinator>> _preStartActions = new List<Action<IServiceCoordinator>>();
		readonly IList<Action<IServiceCoordinator>> _preStopActions = new List<Action<IServiceCoordinator>>();
		readonly IList<ServiceBuilder> _serviceBuilders = new List<ServiceBuilder>();
		IServiceCoordinator _coordinator;
		readonly ServiceDescription _description;

		public ServiceDescription Description
		{
			get { return _description; }
		}

		TimeSpan _timeout = 1.Minutes();

		public RunBuilder(ServiceDescription description)
		{
			_description = description;
		}

		public IList<ServiceBuilder> ServiceBuilders
		{
			get { return _serviceBuilders; }
		}

		public virtual Host Build()
		{
			_coordinator = new ServiceCoordinator(new PoolFiber(),
			                                      ExecutePreStartActions,
			                                      ExecutePostStartActions,
			                                      ExecutePostStopActions,
			                                      _timeout);

			_serviceBuilders.Each(x => { _coordinator.CreateService(x.Name, x.Build); });

			return CreateHost(_coordinator);
		}

		public void Match<T>(Action<T> callback)
			where T : class, HostBuilder
		{
			if (typeof(T).IsAssignableFrom(GetType()))
				callback(this as T);
		}

		Host CreateHost(IServiceCoordinator coordinator)
		{
			if (Process.GetCurrentProcess().GetParent().ProcessName == "services")
			{
				_log.Debug("Running as a Windows service, using the service host");

				return new WinServiceHost(_description, coordinator);
			}

			_log.Debug("Running as a console application, using the console host");
			return new ConsoleRunHost(_description, coordinator);
		}

		public void AddServiceBuilder(ServiceBuilder serviceBuilder)
		{
			_serviceBuilders.Add(serviceBuilder);
		}

		public void BeforeStartingServices(Action<IServiceCoordinator> callback)
		{
			_preStartActions.Add(callback);
		}

		public void AfterStartingServices(Action<IServiceCoordinator> callback)
		{
			_postStartActions.Add(callback);
		}

		public void BeforeStoppingServices(Action<IServiceCoordinator> callback)
		{
			_preStopActions.Add(callback);
		}

		public void AfterStoppingServices(Action<IServiceCoordinator> callback)
		{
			_postStopActions.Add(callback);
		}


		void ExecutePreStartActions(IServiceCoordinator coordinator)
		{
			_preStartActions.Each(x => x(coordinator));
		}

		void ExecutePostStartActions(IServiceCoordinator coordinator)
		{
			_postStartActions.Each(x => x(coordinator));
		}

		void ExecutePreStopActions(IServiceCoordinator coordinator)
		{
			_preStopActions.Each(x => x(coordinator));
		}

		void ExecutePostStopActions(IServiceCoordinator coordinator)
		{
			_postStopActions.Each(x => x(coordinator));
		}
	}
}