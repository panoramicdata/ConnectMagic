using NCalc;
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
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The configuration description
		/// </summary>
		[Required]
		[MinLength(1)]
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// The configuration version - format is free but suggest either increasing version number or date/time based versioning
		/// </summary>
		public string Version { get; set; } = "v1";

		/// <summary>
		/// Systems
		/// </summary>
		[DataMember(Name = "ConnectedSystems")]
		public List<ConnectedSystem> ConnectedSystems { get; set; } = new List<ConnectedSystem>();

		public IEnumerable<ConnectedSystem> EnabledConnectedSystems => ConnectedSystems.Where(cs => cs.IsEnabled);

		[DataMember(Name = "State")]
		public State State { get; set; } = new State();

		[DataMember(Name = "MaxFileAgeHours")]
		public double MaxFileAgeHours { get; set; } = 72;

		/// <summary>
		/// Validates the configuration and throws an exception if an issue is found
		/// </summary>
		internal void Validate()
		{
			NameShouldNotBeNullOrWhiteSpace();
			ThereShouldBeAtLeastOneEnabledConnectedSystem();
			AllConnectedSystemsShouldHaveCredentials();
			AllConnectedSystemsShouldHaveConfiguration();
			AllConnectedSystemsShouldHaveAtLeastOneDataset();
			AllConnectedSystemsDataSetsShouldHaveAtLeastOneMapping();
			AllConnectedSystemsDataSetsShouldHaveQueryConfigSet();
			AllConnectedSystemsDataSetsShouldHaveOneOrMoreJoinMappingAndAllShouldBeValid();
			AllNcalcExpressionsShouldBeValid();
		}

		private void AllNcalcExpressionsShouldBeValid()
		{
			var errors = new List<string>();
			foreach (var connectedSystem in EnabledConnectedSystems)
			{
				foreach (var csDataSet in connectedSystem.EnabledDatasets)
				{
					for (var index = 0; index < csDataSet.Mappings.Count; index++)
					{
						var mapping = csDataSet.Mappings[index];
						var systemExpression = new Expression(mapping.SystemExpression);
						if (systemExpression.HasErrors())
						{
							errors.Add($"{connectedSystem.Name}:{csDataSet.Name} mapping index {index + 1} has an invalid SystemExpression: {systemExpression.Error}");
						}

						var stateExpression = new Expression(mapping.StateExpression);
						if (stateExpression.HasErrors())
						{
							errors.Add($"{connectedSystem.Name}:{csDataSet.Name} mapping index {index + 1} has an invalid StateExpression: {stateExpression.Error}");
						}
					}
				}
			}
			if (errors.Count > 0)
			{
				throw new ConfigurationException($"Configuration contains mapping expression errors:\n{string.Join("\n", errors)}\n");
			}
		}

		private void NameShouldNotBeNullOrWhiteSpace()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				throw new ConfigurationException($"{nameof(Name)} should be defined.");
			}
		}

		private void ThereShouldBeAtLeastOneEnabledConnectedSystem()
		{
			if (ConnectedSystems == null)
			{
				throw new ConfigurationException($"{nameof(ConnectedSystem)} should be defined.");
			}

			if (!ConnectedSystems.Any(cs => cs.IsEnabled))
			{
				throw new ConfigurationException($"There should be at least 1 enabled {nameof(ConnectedSystem)}");
			}
		}

		private void AllConnectedSystemsShouldHaveConfiguration()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				if (connectedSystem.Configuration is null)
				{
					throw new ConfigurationException($"ConnectedSystem {connectedSystem.Configuration} must not be null");
				}
			}
		}

		private void AllConnectedSystemsShouldHaveCredentials()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				if (connectedSystem.Credentials is null)
				{
					throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no Credentials set");
				}

				// Enforce PublicText & PrivateText
				switch (connectedSystem.Type)
				{
					case SystemType.AutoTask:
						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.PublicText))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PublicText)} set");
						}

						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.PrivateText))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.PrivateText)} set");
						}

						if (string.IsNullOrWhiteSpace(connectedSystem.Credentials.ClientSecret))
						{
							throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(connectedSystem.Credentials)} {nameof(connectedSystem.Credentials.ClientSecret)} set with the Integration Code");
						}
						break;
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
				if (connectedSystem.Datasets is null || connectedSystem.Datasets.Count == 0)
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
					if (dataSet.Mappings is null || dataSet.Mappings.Count == 0)
					{
						throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name} has no {nameof(ConnectedSystemDataSet.Mappings)} set in {dataSet.Name}");
					}
				}
			}
		}

		private void AllConnectedSystemsDataSetsShouldHaveQueryConfigSet()
		{
			foreach (var connectedSystem in ConnectedSystems)
			{
				foreach (var dataSet in connectedSystem.Datasets)
				{
					if (dataSet.QueryConfig is null)
					{
						throw new ConfigurationException($"ConnectedSystem {connectedSystem.Name}'s {nameof(ConnectedSystemDataSet.QueryConfig)} must not be null");
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
