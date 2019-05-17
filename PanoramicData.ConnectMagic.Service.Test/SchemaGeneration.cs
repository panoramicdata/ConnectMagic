using System;
using Xunit;

namespace PanoramicData.ConnectMagic.Service.Test
{
	public class SchemaGeneration
	{
		[Fact]
		public async void GenerateDeviceGroupStructureSchemaAsync(string destSchemaPath)
		{
			var generator = new JSchemaGenerator();
			var schema = generator.Generate(typeof(Configuration)).ToString();
			await File.WriteAllTextAsync(destSchemaPath, schema).ConfigureAwait(false);
		}
	}
}
