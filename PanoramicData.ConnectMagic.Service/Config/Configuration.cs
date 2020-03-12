using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace PanoramicData.ConnectMagic.Service.Config
{
	/// <summary>
	/// System configuration
	/// </summary>
	[DataContract]
	public class Configuration
	{
		/// <summary>
		/// The configuration name
		/// </summary>
		[Required]
		[MinLength(1)]
		public string Name { get; set; }

		/// <summary>
		/// The configuration description
		/// </summary>
		[Required]
		[MinLength(1)]
		public string Description { get; set; }

		/// <summary>
		/// The configuration version - format is free but suggest either increasing version number or date/time based versioning
		/// </summary>
		public string Version { get; set; } = "v1";

		/// <summary>
		/// Systems
		/// </summary>
		[DataMember(Name = "ConnectedSystems")]
		public List<ConnectedSystem> ConnectedSystems { get; set; }

		[DataMember(Name = "State")]
		public State State { get; set; }

		[DataMember(Name = "MaxFileAgeHours")]
		public double MaxFileAgeHours { get; set; } = 72;

		/// <summary>
		/// Validates the configuration and throws an exception if an issue is found
		/// </summary>
		internal void Validate()
		{
			ThereShouldBeAtLeastOneEnabledConnectedSystem();
			AllConnectedSystemsShouldHaveCredentials();
			AllConnectedSystemsShouldHaveAtLeastOneDataset();
			AllConnectedSystemsDataSetsShouldHaveAtLeastOneMapping();
			AllConnectedSystemsDataSetsShouldHaveOneOrMoreJoinMappingAndAllShouldBeValid();
		}

		private void ThereShouldBeAtLeastOneEnabledConnectedSystem()
		{
			if (ConnectedSystems == null)
			{
				throw new ConfigurationException($"{nameof(ConnectedSystem)} should be defined");
			}

			if (!ConnectedSystems.Any(cs => cs.IsEnabled))
			{
				throw new ConfigurationException($"There should be at least 1 enabled {nameof(ConnectedSystem)}");
			}
		}

		private void AllConnectedSystemsShouldHaveCredentials()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				if (connectedSystem.Credentials == null)
				{
					throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no Credentials set");
				}

				// Enforce PublicText & PrivateText
				switch (connectedSystem.Type)
				{
					case SystemType.AutoTask:
					case SystemType.SalesForce:
					case SystemType.Certify:
					case SystemType.LogicMonitor:
					case SystemType.ServiceNow:
					case SystemType.SolarWinds:
						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.PublicText))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PublicText)} set");
						}

						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.PrivateText))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PrivateText)} set");
						}
						break;
				}

				// Enforce Account
				switch (connectedSystem.Type)
				{
					case SystemType.LogicMonitor:
					case SystemType.ServiceNow:
						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.Account))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.Account)} set");
						}
						break;
				}

				// Enforce ConnectionString
				switch (connectedSystem.Type)
				{
					case SystemType.MsSqlServer:
						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.ConnectionString))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.ConnectionString)} set");
						}
						break;
				}
			}
		}

		private void AllConnectedSystemsShouldHaveAtLeastOneDataset()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				if (connectedSystem.Datasets == null || connectedSystem.Datasets.Count == 0)
				{
					throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Datasets)} set");
				}
			}
		}

		private void AllConnectedSystemsDataSetsShouldHaveAtLeastOneMapping()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				foreach (var dataSet in connectedSystem.Datasets)
				{
					if (dataSet.Mappings == null || dataSet.Mappings.Count == 0)
					{
						throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(ConnectedSystemDataSet.Mappings)} set in {dataSet.Name}");
					}
				}
			}
		}

		private void AllConnectedSystemsDataSetsShouldHaveOneOrMoreJoinMappingAndAllShouldBeValid()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				foreach (var dataSet in connectedSystem.Datasets)
				{
					var joinMappings = dataSet.Mappings?.Where(m => m.Direction == MappingType.Join).ToList() ?? new List<Mapping>();
					if (joinMappings.Count == 0)
					{
						throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(ConnectedSystemDataSet.Mappings)} set in {dataSet.Name}");
					}

					// Ensure each JoinMapping is valid
					foreach (var joinMapping in joinMappings)
					{
						if (string.IsNullOrWhiteSpace(joinMapping.StateExpression))
						{
							throw new ConfigurationException($"DataSet {dataSet.Name} has a Join mapping without a {nameof(joinMapping.StateExpression)} defined.");
						}

						if (string.IsNullOrWhiteSpace(joinMapping.SystemExpression))
						{
							throw new ConfigurationException($"DataSet {dataSet.Name} has a Join mapping without a {nameof(joinMapping.SystemExpression)} defined.");
						}
					}
				}
			}
		}
	}
}
