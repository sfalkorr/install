#region Using directives

using System;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

#endregion

[assembly: CLSCompliant(true)]

[assembly: InternalsVisibleTo("AvalonEdit.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100F1844BC8CBDC3779B0E5970A30D800668414128135F5D6CD274E726F7C84FBDBF74AD1AD0D9FBA9C0A6CC64C11D0F6A9EDBBE7B32B6F19D8F734E1C130814D40DF54FF9D063CE29BF7AF86B46A69F0E2B910991B52A2AE443648E199A09547E74663CBE1E72E89365034FF53B6A3CE281415CBE7E2DFB5E40E54667F35DC04CA")]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: ComVisible(false)]


[assembly: XmlnsPrefix("http://icsharpcode.net/sharpdevelop/avalonedit", "avalonedit")]

[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "AvalonEdit")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "AvalonEdit.Editing")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "AvalonEdit.Rendering")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "AvalonEdit.Highlighting")]


[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly", Justification = "AssemblyInformationalVersion does not need to be a parsable version")]