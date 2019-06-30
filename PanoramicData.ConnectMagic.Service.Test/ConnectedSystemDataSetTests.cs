using FluentAssertions;
using PanoramicData.ConnectMagic.Service.Exceptions;
using PanoramicData.ConnectMagic.Service.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class ConnectedSystemDataSetTests
	{
		[Fact]
		public void Validate_NameIsNullOrEmpty_ExceptionThrown()
		{
			Action act = () => new ConnectedSystemDataSet().Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)}'s {nameof(ConnectedSystemDataSet.Name)} must not be null or empty.");

			act = () =>
				new ConnectedSystemDataSet
				{
					Name = ""
				}
				.Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)}'s {nameof(ConnectedSystemDataSet.Name)} must not be null or empty.");
		}

		[Fact]
		public void Validate_StateDataSetNameIsNullOrEmpty_ExceptionThrown()
		{
			Action act = () =>
				new ConnectedSystemDataSet
				{
					Name = "ValidName"
				}
				.Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)} ValidName's StateDataSetName must not be null or empty.");

			act = () =>
				new ConnectedSystemDataSet
				{
					Name = "ValidName",
					StateDataSetName = ""
				}
				.Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)} ValidName's {nameof(ConnectedSystemDataSet.StateDataSetName)} must not be null or empty.");
		}

		[Fact]
		public void Validate_MappingsIsNullOrEmpty_ExceptionThrown()
		{
			Action act = () =>
				new ConnectedSystemDataSet
				{
					Name = "ValidName",
					StateDataSetName = "Valid StateDataSetName"
				}
				.Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)} ValidName's Mappings must not be null.");

			act = () =>
				new ConnectedSystemDataSet
				{
					Name = "ValidName",
					StateDataSetName = "Valid StateDataSetName",
					Mappings = new List<Mapping>()
				}
				.Validate();
			act
				.Should()
				.Throw<ConfigurationException>()
				.Where(e => e.Message == $"{nameof(ConnectedSystemDataSet)} ValidName's Mappings must not be empty.");
		}
	}
}
