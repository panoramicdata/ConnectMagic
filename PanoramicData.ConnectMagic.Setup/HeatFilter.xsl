<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
	xmlns="http://schemas.microsoft.com/wix/2006/wi"
	xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	xmlns:user="urn:my-scripts"

	version="1.0"
	exclude-result-prefixes="xsl wix"
>
	<msxsl:script language="C#" implements-prefix="user">
		<![CDATA[
      public bool endswith(string source, string value)
      {
          return source.EndsWith(value);
      }
    ]]>
	</msxsl:script>

	<xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />

	<xsl:strip-space elements="*" />

	<xsl:key
		name="FilesToRemove"
		match="wix:Component[user:endswith(wix:File/@Source, 'PanoramicData.ConnectMagic.Service.dll') or user:endswith(wix:File/@Source, 'PanoramicData.ConnectMagic.Service.exe') or user:endswith(wix:File/@Source, '.exe.config') or user:endswith(wix:File/@Source, '.dll.config')]"
		use="@Id"
	/>

	<!-- By default, copy all elements and nodes into the output... -->
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />
		</xsl:copy>
	</xsl:template>

	<!-- ...but if the element has the "FilesToRemove" key then don't render anything (i.e. removing it from the output) -->
	<xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'FilesToRemove', @Id ) ]" />

</xsl:stylesheet>