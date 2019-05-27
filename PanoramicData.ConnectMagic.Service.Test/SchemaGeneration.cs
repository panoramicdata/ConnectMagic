using Newtonsoft.Json.Schema.Generation;
using PanoramicData.ConnectMagic.Service.Config;
using System;
using System.IO;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class SchemaGeneration
	{
		[Fact]
		public async void GenerateDeviceGroupStructureSchemaAsync()
		{
			var generator = new JSchemaGenerator();
			var schema = generator.Generate(typeof(Configuration)).ToString();
			await File.WriteAllTextAsync("appsettings.ConnectMagic.schema.json", schema).ConfigureAwait(false);
		}
	}
}
