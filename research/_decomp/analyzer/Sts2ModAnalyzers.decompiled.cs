using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using ModAnalyzers.Json;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName = ".NET Standard 2.0")]
[assembly: AssemblyCompany("Alchyr")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("Analyzers to help with the creation of Slay the Spire 2 mods.")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+46c6a91ff24d47062d6b28cb734a8f855e1da0b6")]
[assembly: AssemblyProduct("Sts2ModAnalyzers")]
[assembly: AssemblyTitle("Sts2ModAnalyzers")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/Alchyr/StS2ModAnalyzers/tree/master/ModAnalyzers/ModAnalyzers")]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableAttribute : Attribute
	{
		public readonly byte[] NullableFlags;

		public NullableAttribute(byte P_0)
		{
			NullableFlags = new byte[1] { P_0 };
		}

		public NullableAttribute(byte[] P_0)
		{
			NullableFlags = P_0;
		}
	}
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;

		public NullableContextAttribute(byte P_0)
		{
			Flag = P_0;
		}
	}
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public readonly int Version;

		public RefSafetyRulesAttribute(int P_0)
		{
			Version = P_0;
		}
	}
}
namespace ModAnalyzers
{
	internal static class Extensions
	{
		private static readonly Regex CamelCaseRegex = new Regex("([A-Za-z0-9]|\\G(?!^))([A-Z])", RegexOptions.Compiled);

		private static readonly Regex SnakeCaseRegex = new Regex("(.*?)_([a-zA-Z0-9])", RegexOptions.Compiled);

		private static readonly Regex WhitespaceRegex = new Regex("\\s+", RegexOptions.Compiled);

		private static readonly Regex SpecialCharRegex = new Regex("[^A-Za-z0-9_]", RegexOptions.Compiled);

		public const char PREFIX_SPLIT_CHAR = '-';

		public static string FullName(this INamedTypeSymbol symbol)
		{
			SymbolDisplayFormat val = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle((SymbolDisplayGlobalNamespaceStyle)0);
			INamespaceSymbol containingNamespace = ((ISymbol)symbol).ContainingNamespace;
			string text = ((containingNamespace != null) ? ((ISymbol)containingNamespace).ToDisplayString(val) : null) ?? string.Empty;
			if (text.Length > 0)
			{
				text += ".";
			}
			return text + ((ISymbol)symbol).Name;
		}

		public static bool ImplementsInterface(this INamedTypeSymbol typeSymbol, INamedTypeSymbol? interfaceSymbol)
		{
			if (interfaceSymbol != null)
			{
				return ((ITypeSymbol)typeSymbol).AllInterfaces.Contains(interfaceSymbol);
			}
			return false;
		}

		public static bool ImplementsInterfaceOrBaseClass(this INamedTypeSymbol typeSymbol, Type typeToCheck)
		{
			return typeSymbol.ImplementsInterfaceOrBaseClass(typeToCheck.Name);
		}

		public static bool ImplementsInterfaceOrBaseClass(this INamedTypeSymbol typeSymbol, string name)
		{
			if (typeSymbol.FullName() == name)
			{
				return true;
			}
			ImmutableArray<INamedTypeSymbol>.Enumerator enumerator = ((ITypeSymbol)typeSymbol).AllInterfaces.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.FullName() == name)
				{
					return true;
				}
			}
			for (INamedTypeSymbol baseType = ((ITypeSymbol)typeSymbol).BaseType; baseType != null; baseType = ((ITypeSymbol)baseType).BaseType)
			{
				if (baseType.FullName() == name)
				{
					return true;
				}
			}
			return false;
		}

		public static bool OverridesMethodOrProperty(this INamedTypeSymbol typeSymbol, string baseTypeName, string baseName)
		{
			if (typeSymbol.FullName() == baseTypeName)
			{
				return false;
			}
			ImmutableArray<ISymbol>.Enumerator enumerator = ((INamespaceOrTypeSymbol)typeSymbol).GetMembers().GetEnumerator();
			while (enumerator.MoveNext())
			{
				ISymbol current = enumerator.Current;
				if (current.IsOverride && current.Name == baseName)
				{
					return true;
				}
			}
			return ((ITypeSymbol)typeSymbol).BaseType?.OverridesMethodOrProperty(baseTypeName, baseName) ?? false;
		}

		public static string? AttributeArgumentString(this AttributeData attr, int argIndex)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			ImmutableArray<TypedConstant> constructorArguments = attr.ConstructorArguments;
			if (argIndex < constructorArguments.Length)
			{
				TypedConstant val = constructorArguments[argIndex];
				return ((TypedConstant)(ref val)).Value?.ToString();
			}
			return null;
		}

		public static SyntaxNode? FindPropertyGetter(this SyntaxNode propertySyntax, SymbolAnalysisContext context)
		{
			SyntaxNode val = (SyntaxNode)(object)((SyntaxNode)(object)propertySyntax.FindChild<AccessorListSyntax>((Predicate<AccessorListSyntax>?)null))?.FindChild<AccessorDeclarationSyntax>((Predicate<AccessorDeclarationSyntax>?)((AccessorDeclarationSyntax syntax) => CSharpExtensions.IsKind((SyntaxNode)(object)syntax, (SyntaxKind)8896)));
			SyntaxNode val2 = (SyntaxNode)(object)(val ?? propertySyntax).FindChild<ArrowExpressionClauseSyntax>((Predicate<ArrowExpressionClauseSyntax>?)((ArrowExpressionClauseSyntax syntax) => CSharpExtensions.IsKind(syntax.ArrowToken, (SyntaxKind)8269)));
			if (val2 == null)
			{
				if (val == null)
				{
					return null;
				}
				val2 = (SyntaxNode)(object)((SyntaxNode)(object)val.FindChild<BlockSyntax>((Predicate<BlockSyntax>?)null))?.FindChild<ReturnStatementSyntax>((Predicate<ReturnStatementSyntax>?)null);
			}
			return val2;
		}

		public static string CreationTypeName(this ExpressionSyntax expression)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			if (!CSharpExtensions.IsKind((SyntaxNode)(object)expression, (SyntaxKind)8649))
			{
				return "WRONG EXPRESSION KIND";
			}
			IdentifierNameSyntax? obj = ((SyntaxNode)(((object)((SyntaxNode)(object)expression).FindChild<QualifiedNameSyntax>((Predicate<QualifiedNameSyntax>?)null)) ?? ((object)expression))).FindChild<IdentifierNameSyntax>((Predicate<IdentifierNameSyntax>?)null);
			IdentifierNameSyntax val = ((obj is IdentifierNameSyntax) ? obj : null);
			if (val != null)
			{
				SyntaxToken firstToken = ((CSharpSyntaxNode)val).GetFirstToken(false, false, false, false);
				return ((SyntaxToken)(ref firstToken)).ValueText;
			}
			return "";
		}

		public static T? FindChild<T>(this SyntaxNode syntax, Predicate<T>? condition = null) where T : SyntaxNode
		{
			foreach (SyntaxNode item in syntax.ChildNodes())
			{
				T val = (T)(object)((item is T) ? item : null);
				if (val != null && (condition == null || condition(val)))
				{
					return val;
				}
			}
			return default(T);
		}

		public static string Slugify(this string txt)
		{
			string text = CamelCaseRegex.Replace(txt.Trim(), "$1_$2");
			string input = WhitespaceRegex.Replace(text.ToUpper(), "_");
			return SpecialCharRegex.Replace(input, "");
		}

		public static string GetPrefix(this string fullName)
		{
			return $"{fullName.GetRootNamespace().ToUpperInvariant()}{'-'}";
		}

		public static string GetRootNamespace(this string fullName)
		{
			int num = fullName.IndexOf('.');
			if (num >= 0)
			{
				return fullName.Substring(0, num);
			}
			return "";
		}
	}
	[DiagnosticAnalyzer("C#", new string[] { })]
	public class LocalizationAnalyzer : DiagnosticAnalyzer
	{
		private class RequiredLocalization(string filename)
		{
			public readonly string Filename = filename;

			public readonly Dictionary<string, string> RequiredKeys = new Dictionary<string, string>();

			public RequiredLocalization Add(string key, string defaultValue = "")
			{
				RequiredKeys.Add(key, defaultValue);
				return this;
			}
		}

		public const string DiagnosticId = "STS001";

		public const string NoLocId = "STS002";

		public const string CustomModelRuleId = "STS003";

		private const string BaseLibAbstracts = "BaseLib.Abstracts.Custom";

		private const string CustomModelInterface = "BaseLib.Abstracts.ICustomModel";

		private const string ModelLocInterface = "BaseLib.Abstracts.ILocalizationProvider";

		private const string CustomIdAttribute = "BaseLib.Utils.Attributes.CustomIDAttribute";

		private static readonly Dictionary<string, RequiredLocalization[]> NamedTypeLocData = new Dictionary<string, RequiredLocalization[]>
		{
			{
				"MegaCrit.Sts2.Core.Models.CardModel",
				new RequiredLocalization[1] { new RequiredLocalization("cards").Add("SYMBOLID.title", "SYMBOLNAME").Add("SYMBOLID.description") }
			},
			{
				"MegaCrit.Sts2.Core.Models.CharacterModel",
				new RequiredLocalization[2]
				{
					new RequiredLocalization("characters").Add("SYMBOLID.title", "The SYMBOLNAME").Add("SYMBOLID.titleObject", "The SYMBOLNAME").Add("SYMBOLID.description", "Character Selection\\nScreen Description")
						.Add("SYMBOLID.pronounObject", "him/her/it")
						.Add("SYMBOLID.possessiveAdjective", "his/her/its")
						.Add("SYMBOLID.pronounPossessive", "his/hers/its")
						.Add("SYMBOLID.pronounSubject", "he/she/it")
						.Add("SYMBOLID.goldMonologue", "Line spoken when obtaining a large amount of gold")
						.Add("SYMBOLID.eventDeathPrevention", "Co-op survival line")
						.Add("SYMBOLID.aromaPrinciple", "Lore")
						.Add("SYMBOLID.cardsModifierTitle", "__ Cards")
						.Add("SYMBOLID.cardsModifierDescription", "__ cards will now appear in rewards and shops.")
						.Add("SYMBOLID.banter.alive.endTurnPing", "Co-op hurry up end turn ping message")
						.Add("SYMBOLID.banter.dead.endTurnPing", "..."),
					new RequiredLocalization("ancients").Add("THE_ARCHITECT.talk.SYMBOLID.0-0r.char", "I am angry at the architect").Add("THE_ARCHITECT.talk.SYMBOLID.0-0r.next", "Continue").Add("THE_ARCHITECT.talk.SYMBOLID.0-1r.ancient", "You die")
						.Add("THE_ARCHITECT.talk.SYMBOLID.0-attack", "Both")
				}
			},
			{
				"MegaCrit.Sts2.Core.Models.PotionModel",
				new RequiredLocalization[1] { new RequiredLocalization("potions").Add("SYMBOLID.title", "SYMBOLNAME").Add("SYMBOLID.description") }
			},
			{
				"MegaCrit.Sts2.Core.Models.PowerModel",
				new RequiredLocalization[1] { new RequiredLocalization("powers").Add("SYMBOLID.title", "SYMBOLNAME").Add("SYMBOLID.description").Add("SYMBOLID.smartDescription") }
			},
			{
				"MegaCrit.Sts2.Core.Models.RelicModel",
				new RequiredLocalization[1] { new RequiredLocalization("relics").Add("SYMBOLID.title", "SYMBOLNAME").Add("SYMBOLID.description").Add("SYMBOLID.flavor") }
			},
			{
				"MegaCrit.Sts2.Core.Models.AncientEventModel",
				new RequiredLocalization[1] { new RequiredLocalization("ancients").Add("SYMBOLID.title", "SYMBOLNAME").Add("SYMBOLID.epithet").Add("SYMBOLID.talk.firstVisitEver.0-0.ancient", "First time greeting.")
					.Add("SYMBOLID.talk.ANY.0-0r.ancient", "Reusable generic greeting.") }
			}
		};

		private static readonly Dictionary<string, RequiredLocalization[]> EnumLocData = new Dictionary<string, RequiredLocalization[]> { 
		{
			"CardKeyword",
			new RequiredLocalization[1] { new RequiredLocalization("card_keywords").Add("SYMBOLID.title", "NAME").Add("SYMBOLID.description", "Tooltip") }
		} };

		private static readonly Dictionary<string, string[]> CodeLocalizationData = new Dictionary<string, string[]>
		{
			{
				"ActLoc",
				new string[1] { "title" }
			},
			{
				"CardModifierLoc",
				new string[2] { "title", "description" }
			},
			{
				"CardLoc",
				new string[2] { "title", "description" }
			},
			{
				"CharacterLoc",
				Array.Empty<string>()
			},
			{
				"EncounterLoc",
				new string[2] { "title", "loss" }
			},
			{
				"ModifierLoc",
				new string[2] { "title", "description" }
			},
			{
				"MonsterLoc",
				new string[1] { "name" }
			},
			{
				"OrbLoc",
				new string[3] { "title", "description", "smartDescription" }
			},
			{
				"PotionLoc",
				new string[2] { "title", "description" }
			},
			{
				"PowerLoc",
				new string[3] { "title", "description", "smartDescription" }
			},
			{
				"RelicLoc",
				new string[3] { "title", "description", "flavor" }
			}
		};

		private static readonly Dictionary<string, KeyValuePair<string, string>[]> OverrideIgnores = new Dictionary<string, KeyValuePair<string, string>[]> { 
		{
			"MegaCrit.Sts2.Core.Models.PowerModel",
			new KeyValuePair<string, string>[3]
			{
				new KeyValuePair<string, string>("Title", "SYMBOLID.title"),
				new KeyValuePair<string, string>("Description", "SYMBOLID.description"),
				new KeyValuePair<string, string>("SmartDescriptionLocKey", "SYMBOLID.smartDescription")
			}
		} };

		private static readonly LocalizableString Title = (LocalizableString)new LocalizableResourceString("STS001Title", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString NoLocTitle = (LocalizableString)new LocalizableResourceString("STS002Title", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString CustomModelTitle = (LocalizableString)new LocalizableResourceString("STS003Title", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat = (LocalizableString)new LocalizableResourceString("STS001MessageFormat", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString CustomModelFormat = (LocalizableString)new LocalizableResourceString("STS003MessageFormat", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description = (LocalizableString)new LocalizableResourceString("STS001Description", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString NoLocDescription = (LocalizableString)new LocalizableResourceString("STS002Description", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString CustomModelDescription = (LocalizableString)new LocalizableResourceString("STS003Description", Resources.ResourceManager, typeof(Resources));

		private const string Category = "Localization";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor("STS001", Title, MessageFormat, "Localization", (DiagnosticSeverity)3, true, Description, (string)null, Array.Empty<string>());

		private static readonly DiagnosticDescriptor NoLoc = new DiagnosticDescriptor("STS002", NoLocTitle, NoLocDescription, "Localization", (DiagnosticSeverity)2, true, (LocalizableString)null, (string)null, new string[1] { "CompilationEnd" });

		private static readonly DiagnosticDescriptor CustomModelRule = new DiagnosticDescriptor("STS003", CustomModelTitle, CustomModelFormat, "Localization", (DiagnosticSeverity)2, true, CustomModelDescription, (string)null, Array.Empty<string>());

		private HashSet<string>? _currentLocKeys;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create<DiagnosticDescriptor>(Rule, NoLoc, CustomModelRule, LoggingDiagnostic.Fake);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis((GeneratedCodeAnalysisFlags)0);
			context.EnableConcurrentExecution();
			context.RegisterCompilationStartAction((Action<CompilationStartAnalysisContext>)LoadLocOnce);
		}

		private void LoadLocOnce(CompilationStartAnalysisContext context)
		{
			ImmutableArray<AdditionalText> additionalFiles = context.Options.AdditionalFiles;
			_currentLocKeys = new HashSet<string>();
			bool receivedJson = false;
			ImmutableArray<AdditionalText>.Enumerator enumerator = additionalFiles.GetEnumerator();
			while (enumerator.MoveNext())
			{
				AdditionalText current = enumerator.Current;
				if (current == null)
				{
					continue;
				}
				string path = current.Path;
				if (!path.EndsWith(".json") || !path.Contains("localization"))
				{
					continue;
				}
				receivedJson = true;
				string text = ((object)current.GetText(default(CancellationToken)))?.ToString();
				if (text == null)
				{
					continue;
				}
				try
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
					if (!(JsonValue.Parse(text) is JsonObject jsonObject))
					{
						continue;
					}
					foreach (string key in jsonObject.Keys)
					{
						_currentLocKeys.Add(fileNameWithoutExtension + "." + key);
					}
				}
				catch (Exception)
				{
				}
			}
			INamedTypeSymbol customModelInterface = context.Compilation.GetTypeByMetadataName("BaseLib.Abstracts.ICustomModel");
			INamedTypeSymbol customLocInterface = context.Compilation.GetTypeByMetadataName("BaseLib.Abstracts.ILocalizationProvider");
			INamedTypeSymbol idAttribute = context.Compilation.GetTypeByMetadataName("BaseLib.Utils.Attributes.CustomIDAttribute");
			context.RegisterSymbolAction((Action<SymbolAnalysisContext>)delegate(SymbolAnalysisContext context2)
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				CheckSymbol(context2, customModelInterface, customLocInterface, idAttribute);
			}, (SymbolKind[])(object)new SymbolKind[1] { (SymbolKind)11 });
			context.RegisterSymbolAction((Action<SymbolAnalysisContext>)CheckField, (SymbolKind[])(object)new SymbolKind[1] { (SymbolKind)6 });
			context.RegisterCompilationEndAction((Action<CompilationAnalysisContext>)delegate(CompilationAnalysisContext endContext)
			{
				if (!receivedJson)
				{
					Diagnostic val = Diagnostic.Create(NoLoc, (Location)null, Array.Empty<object>());
					((CompilationAnalysisContext)(ref endContext)).ReportDiagnostic(val);
				}
			});
		}

		private void CheckSymbol(SymbolAnalysisContext context, INamedTypeSymbol? customModel, INamedTypeSymbol? locProvider, INamedTypeSymbol? idAttribute)
		{
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			if (_currentLocKeys == null)
			{
				return;
			}
			ISymbol symbol = ((SymbolAnalysisContext)(ref context)).Symbol;
			INamedTypeSymbol val = (INamedTypeSymbol)(object)((symbol is INamedTypeSymbol) ? symbol : null);
			if (val == null || ((ISymbol)val).IsAbstract || ((ISymbol)val).IsStatic)
			{
				return;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (KeyValuePair<string, RequiredLocalization[]> namedTypeLocDatum in NamedTypeLocData)
			{
				if (!val.ImplementsInterfaceOrBaseClass(namedTypeLocDatum.Key))
				{
					continue;
				}
				bool flag = val.ImplementsInterface(customModel);
				List<string> list = new List<string>();
				if (OverrideIgnores.TryGetValue(namedTypeLocDatum.Key, out KeyValuePair<string, string>[] value))
				{
					KeyValuePair<string, string>[] array = value;
					for (int i = 0; i < array.Length; i++)
					{
						KeyValuePair<string, string> keyValuePair = array[i];
						if (val.OverridesMethodOrProperty(namedTypeLocDatum.Key, keyValuePair.Key))
						{
							list.Add(keyValuePair.Value);
						}
					}
				}
				ISet<string> set = null;
				if (val.ImplementsInterface(locProvider))
				{
					set = FindAndGetLocalizationDeclaration(val, "SYMBOLID", context);
					context.Log("ProvidedLoc: " + ((set == null) ? "null" : string.Join(",", set)), ((ISymbol)val).Locations[0]);
				}
				if (!flag)
				{
					string key = namedTypeLocDatum.Key;
					int num = key.LastIndexOf('.');
					key = "BaseLib.Abstracts.Custom" + key.Substring(num + 1);
					Diagnostic val2 = Diagnostic.Create(CustomModelRule, ((ISymbol)val).Locations[0], new object[1] { key });
					((SymbolAnalysisContext)(ref context)).ReportDiagnostic(val2);
				}
				AttributeData obj = ((ISymbol)val).GetAttributes().FirstOrDefault((AttributeData attr) => SymbolEqualityComparer.Default.Equals((ISymbol)(object)idAttribute, (ISymbol)(object)attr.AttributeClass));
				string text = val.FullName();
				string prefix = text.GetPrefix();
				string id = obj?.AttributeArgumentString(0) ?? ((flag ? prefix : "") + ((ISymbol)val).Name.Slugify());
				RequiredLocalization[] value2 = namedTypeLocDatum.Value;
				foreach (RequiredLocalization requiredLocalization in value2)
				{
					dictionary.Clear();
					foreach (KeyValuePair<string, string> requiredKey in requiredLocalization.RequiredKeys)
					{
						if (!list.Contains(requiredKey.Key) && (set == null || (set.Count != 0 && !set.Contains(requiredKey.Key))))
						{
							string text2 = ReplaceSpecial(requiredKey.Key, id, ((ISymbol)val).Name);
							if (!_currentLocKeys.Contains(requiredLocalization.filename + "." + text2))
							{
								string value3 = ReplaceSpecial(requiredKey.Value, id, ((ISymbol)val).Name);
								dictionary.Add(text2, value3);
							}
						}
					}
					set = null;
					if (dictionary.Count == 0)
					{
						continue;
					}
					ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>();
					builder.Add("LOCFILES", requiredLocalization.filename + ".json");
					foreach (KeyValuePair<string, string> item in dictionary)
					{
						builder.Add(item.Key, item.Value);
					}
					Diagnostic val3 = Diagnostic.Create(Rule, ((ISymbol)val).Locations[0], builder.ToImmutable(), new object[2]
					{
						JoinKeys(dictionary),
						text
					});
					((SymbolAnalysisContext)(ref context)).ReportDiagnostic(val3);
				}
				break;
			}
		}

		private ISet<string>? FindAndGetLocalizationDeclaration(INamedTypeSymbol? symbol, string symbolId, SymbolAnalysisContext context)
		{
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			while (symbol != null)
			{
				ImmutableArray<ISymbol>.Enumerator enumerator = ((INamespaceOrTypeSymbol)symbol).GetMembers().GetEnumerator();
				while (enumerator.MoveNext())
				{
					ISymbol current = enumerator.Current;
					if (current is IPropertySymbol && current.IsOverride && current.Name.Equals("Localization"))
					{
						ImmutableArray<SyntaxReference> declaringSyntaxReferences = current.DeclaringSyntaxReferences;
						if (declaringSyntaxReferences.Length == 0)
						{
							return null;
						}
						SyntaxNode syntax = declaringSyntaxReferences[0].GetSyntax(default(CancellationToken));
						syntax = syntax.FindPropertyGetter(context);
						if (syntax == null)
						{
							return null;
						}
						return GetLocalizationKeys(syntax, symbolId, context);
					}
				}
				symbol = ((ITypeSymbol)symbol).BaseType;
			}
			return null;
		}

		private ISet<string>? GetLocalizationKeys(SyntaxNode syntax, string symbolId, SymbolAnalysisContext context)
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			if (syntax.FindChild<LiteralExpressionSyntax>((Predicate<LiteralExpressionSyntax>?)((LiteralExpressionSyntax test) => CSharpExtensions.IsKind((SyntaxNode)(object)test, (SyntaxKind)8754))) != null)
			{
				return null;
			}
			SyntaxNode val = (SyntaxNode)(object)syntax.FindChild<ObjectCreationExpressionSyntax>((Predicate<ObjectCreationExpressionSyntax>?)null);
			ObjectCreationExpressionSyntax val2 = (ObjectCreationExpressionSyntax)(object)((val is ObjectCreationExpressionSyntax) ? val : null);
			IEnumerable<SyntaxNode> enumerable;
			if (val2 != null)
			{
				string text = ((ExpressionSyntax)(object)val2).CreationTypeName();
				context.Log(text, syntax.GetLocation());
				if (CodeLocalizationData.TryGetValue(text, out string[] value))
				{
					return value.Select((string name) => symbolId + "." + name).ToImmutableHashSet();
				}
				val = (SyntaxNode)(object)val.FindChild<ExpressionSyntax>((Predicate<ExpressionSyntax>?)((ExpressionSyntax test) => CSharpExtensions.IsKind((SyntaxNode)(object)test, (SyntaxKind)8645)));
				enumerable = (IEnumerable<SyntaxNode>)(((val != null) ? val.ChildNodes().OfType<TupleExpressionSyntax>() : null) ?? Array.Empty<TupleExpressionSyntax>());
			}
			else
			{
				CollectionExpressionSyntax val3 = syntax.FindChild<CollectionExpressionSyntax>((Predicate<CollectionExpressionSyntax>?)null);
				if (val3 == null)
				{
					return ImmutableHashSet<string>.Empty;
				}
				enumerable = (IEnumerable<SyntaxNode>)(from element in ((SyntaxNode)val3).ChildNodes().OfType<CollectionElementSyntax>()
					select ((SyntaxNode)(object)element).FindChild<TupleExpressionSyntax>((Predicate<TupleExpressionSyntax>?)null)).OfType<TupleExpressionSyntax>();
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (SyntaxNode item in enumerable)
			{
				LiteralExpressionSyntax val4 = ((SyntaxNode)(object)item.FindChild<ArgumentSyntax>((Predicate<ArgumentSyntax>?)null))?.FindChild<LiteralExpressionSyntax>((Predicate<LiteralExpressionSyntax>?)((LiteralExpressionSyntax test) => CSharpExtensions.IsKind((SyntaxNode)(object)test, (SyntaxKind)8750)));
				if (val4 == null)
				{
					return ImmutableHashSet<string>.Empty;
				}
				string text2 = symbolId;
				SyntaxToken token = val4.Token;
				hashSet.Add(text2 + "." + ((SyntaxToken)(ref token)).ValueText);
			}
			return hashSet;
		}

		private void CheckField(SymbolAnalysisContext context)
		{
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			if (_currentLocKeys == null)
			{
				return;
			}
			ISymbol symbol = ((SymbolAnalysisContext)(ref context)).Symbol;
			IFieldSymbol val = (IFieldSymbol)(object)((symbol is IFieldSymbol) ? symbol : null);
			if (val == null || !((ISymbol)val).IsStatic || val.IsReadOnly)
			{
				return;
			}
			ImmutableArray<AttributeData> attributes = ((ISymbol)val).GetAttributes();
			AttributeData val2 = null;
			ImmutableArray<AttributeData>.Enumerator enumerator = attributes.GetEnumerator();
			while (enumerator.MoveNext())
			{
				AttributeData current = enumerator.Current;
				INamedTypeSymbol attributeClass = current.AttributeClass;
				if ("CustomEnumAttribute".Equals((attributeClass != null) ? ((ISymbol)attributeClass).Name : null))
				{
					val2 = current;
					continue;
				}
				INamedTypeSymbol attributeClass2 = current.AttributeClass;
				"KeywordPropertiesAttribute".Equals((attributeClass2 != null) ? ((ISymbol)attributeClass2).Name : null);
			}
			if (val2 == null)
			{
				return;
			}
			string text = ((ISymbol)val).Name;
			INamedTypeSymbol containingType = ((ISymbol)val).ContainingType;
			if (containingType == null)
			{
				return;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (KeyValuePair<string, RequiredLocalization[]> enumLocDatum in EnumLocData)
			{
				if (!((ISymbol)val.Type).Name.Contains(enumLocDatum.Key))
				{
					continue;
				}
				if (val2.ConstructorArguments.Length > 0)
				{
					TypedConstant val3 = val2.ConstructorArguments[0];
					object value = ((TypedConstant)(ref val3)).Value;
					if (value != null)
					{
						text = value.ToString();
					}
				}
				string id = containingType.FullName().GetPrefix() + text.ToUpperInvariant();
				RequiredLocalization[] value2 = enumLocDatum.Value;
				foreach (RequiredLocalization requiredLocalization in value2)
				{
					dictionary.Clear();
					foreach (KeyValuePair<string, string> requiredKey in requiredLocalization.RequiredKeys)
					{
						string text2 = ReplaceSpecial(requiredKey.Key, id, text);
						if (!_currentLocKeys.Contains(requiredLocalization.filename + "." + text2))
						{
							string value3 = ReplaceSpecial(requiredKey.Value, id, text);
							dictionary.Add(text2, value3);
						}
					}
					if (dictionary.Count == 0)
					{
						continue;
					}
					ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>();
					builder.Add("LOCFILES", requiredLocalization.filename + ".json");
					foreach (KeyValuePair<string, string> item in dictionary)
					{
						builder.Add(item.Key, item.Value);
					}
					Diagnostic val4 = Diagnostic.Create(Rule, ((ISymbol)val).Locations[0], builder.ToImmutable(), new object[2]
					{
						JoinKeys(dictionary),
						text
					});
					((SymbolAnalysisContext)(ref context)).ReportDiagnostic(val4);
				}
			}
		}

		private static string ReplaceSpecial(string orig, string id, string name)
		{
			return orig.Replace("SYMBOLID", id).Replace("SYMBOLNAME", name);
		}

		private static string JoinKeys<T, U>(IDictionary<T, U> dict)
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			foreach (KeyValuePair<T, U> item in dict)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(item.Key);
			}
			return stringBuilder.ToString();
		}
	}
	[ExportCodeFixProvider("C#", new string[] { }, Name = "LocalizationFixProvider")]
	[Shared]
	public class LocalizationFixProvider : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("STS001");

		public override FixAllProvider? GetFixAllProvider()
		{
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			Dictionary<string, string?> missingKeys = new Dictionary<string, string>();
			string locFiles = null;
			ImmutableArray<Diagnostic>.Enumerator enumerator = ((CodeFixContext)(ref context)).Diagnostics.GetEnumerator();
			while (enumerator.MoveNext())
			{
				foreach (KeyValuePair<string, string> property in enumerator.Current.Properties)
				{
					if (property.Key == "LOCFILES")
					{
						locFiles = property.Value;
					}
					else
					{
						missingKeys.Add(property.Key, property.Value);
					}
				}
			}
			if (locFiles == null || missingKeys.Count == 0)
			{
				return;
			}
			SyntaxNode val = await ((CodeFixContext)(ref context)).Document.GetSyntaxRootAsync(((CodeFixContext)(ref context)).CancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (val == null)
			{
				return;
			}
			TextSpan sourceSpan = ((CodeFixContext)(ref context)).Diagnostics.First().Location.SourceSpan;
			SyntaxToken val2 = val.FindToken(((TextSpan)(ref sourceSpan)).Start, false);
			SyntaxNode declaration = ((SyntaxToken)(ref val2)).Parent;
			while (declaration != null && (int)CSharpExtensions.Kind(declaration) != 8855)
			{
				declaration = declaration.Parent;
			}
			if (declaration != null)
			{
				((CodeFixContext)(ref context)).RegisterCodeFix(CodeAction.Create(string.Format(Resources.STS001CodeFixTitle, locFiles), (Func<CancellationToken, Task<Document>>)((CancellationToken c) => GeneratingMissingKeyComment(((CodeFixContext)(ref context)).Document, declaration, missingKeys, c)), "STS001CodeFixTitle"), ((CodeFixContext)(ref context)).Diagnostics);
			}
		}

		private async Task<Document> GeneratingMissingKeyComment(Document document, SyntaxNode declaration, Dictionary<string, string?> missingKeys, CancellationToken cancellationToken)
		{
			StringBuilder commentBuilder = new StringBuilder();
			bool flag = true;
			foreach (KeyValuePair<string, string> item in missingKeys.ToImmutableSortedDictionary())
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					commentBuilder.AppendLine(",");
				}
				commentBuilder.Append("  \"" + item.Key + "\": \"" + item.Value + "\"");
			}
			commentBuilder.AppendLine();
			DocumentEditor val = await DocumentEditor.CreateAsync(document, cancellationToken);
			SyntaxTrivia comment = SyntaxFactory.Comment(commentBuilder.ToString());
			((SyntaxEditor)val).ReplaceNode(declaration, (Func<SyntaxNode, SyntaxGenerator, SyntaxNode>)delegate(SyntaxNode node, SyntaxGenerator generator)
			{
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0034: Unknown result type (might be due to invalid IL or missing references)
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_001c: Unknown result type (might be due to invalid IL or missing references)
				//IL_001e: Unknown result type (might be due to invalid IL or missing references)
				if (node.HasLeadingTrivia)
				{
					SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();
					SyntaxTriviaList val2 = ((SyntaxTriviaList)(ref leadingTrivia)).Add(comment);
					return SyntaxNodeExtensions.WithLeadingTrivia<SyntaxNode>(node, val2);
				}
				return SyntaxNodeExtensions.WithLeadingTrivia<SyntaxNode>(node, (SyntaxTrivia[])(object)new SyntaxTrivia[1] { comment });
			});
			return document.WithSyntaxRoot(((SyntaxEditor)val).GetChangedRoot());
		}
	}
	public static class LoggingDiagnostic
	{
		private const bool ENABLED = false;

		public static readonly DiagnosticDescriptor Fake = new DiagnosticDescriptor("STS999", "Log", "{0}", "Logging", (DiagnosticSeverity)2, true, "Logged info.", (string)null, Array.Empty<string>());

		public static void Log(this SymbolAnalysisContext context, string msg, Location? location = null)
		{
		}
	}
	[DiagnosticAnalyzer("C#", new string[] { })]
	public class ModelRequiresPool : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "STS004";

		private static readonly string[] ModelAbstracts = new string[3] { "BaseLib.Abstracts.CustomCardModel", "BaseLib.Abstracts.CustomPotionModel", "BaseLib.Abstracts.CustomRelicModel" };

		private static readonly LocalizableString Title = (LocalizableString)new LocalizableResourceString("STS004Title", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString MessageFormat = (LocalizableString)new LocalizableResourceString("STS004MessageFormat", Resources.ResourceManager, typeof(Resources));

		private static readonly LocalizableString Description = (LocalizableString)new LocalizableResourceString("STS004Description", Resources.ResourceManager, typeof(Resources));

		private const string Category = "Usage";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor("STS004", Title, MessageFormat, "Usage", (DiagnosticSeverity)2, true, Description, (string)null, Array.Empty<string>());

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create<DiagnosticDescriptor>(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis((GeneratedCodeAnalysisFlags)0);
			context.EnableConcurrentExecution();
			context.RegisterSymbolAction((Action<SymbolAnalysisContext>)CheckForPool, (SymbolKind[])(object)new SymbolKind[1] { (SymbolKind)11 });
		}

		private void CheckForPool(SymbolAnalysisContext context)
		{
			ISymbol symbol = ((SymbolAnalysisContext)(ref context)).Symbol;
			INamedTypeSymbol val = (INamedTypeSymbol)(object)((symbol is INamedTypeSymbol) ? symbol : null);
			if (val == null || ((ISymbol)val).IsAbstract || ((ISymbol)val).IsStatic)
			{
				return;
			}
			string[] modelAbstracts = ModelAbstracts;
			foreach (string name in modelAbstracts)
			{
				if (val.ImplementsInterfaceOrBaseClass(name))
				{
					if (!HasPoolAttribute(val))
					{
						Diagnostic val2 = Diagnostic.Create(Rule, ((ISymbol)val).Locations[0], new object[1] { val.FullName() });
						((SymbolAnalysisContext)(ref context)).ReportDiagnostic(val2);
					}
					break;
				}
			}
		}

		private static bool HasPoolAttribute(INamedTypeSymbol namedTypeSymbol)
		{
			ImmutableArray<AttributeData>.Enumerator enumerator = ((ISymbol)namedTypeSymbol).GetAttributes().GetEnumerator();
			while (enumerator.MoveNext())
			{
				AttributeData current = enumerator.Current;
				INamedTypeSymbol attributeClass = current.AttributeClass;
				if ("PoolAttribute".Equals((attributeClass != null) ? ((ISymbol)attributeClass).Name : null))
				{
					return true;
				}
			}
			if (((ITypeSymbol)namedTypeSymbol).BaseType != null)
			{
				return HasPoolAttribute(((ITypeSymbol)namedTypeSymbol).BaseType);
			}
			return false;
		}
	}
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("ModAnalyzers.Resources", typeof(Resources).Assembly);
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string STS001CodeFixTitle => ResourceManager.GetString("STS001CodeFixTitle", resourceCulture);

		internal static string STS001Description => ResourceManager.GetString("STS001Description", resourceCulture);

		internal static string STS001MessageFormat => ResourceManager.GetString("STS001MessageFormat", resourceCulture);

		internal static string STS001Title => ResourceManager.GetString("STS001Title", resourceCulture);

		internal static string STS002Description => ResourceManager.GetString("STS002Description", resourceCulture);

		internal static string STS002Title => ResourceManager.GetString("STS002Title", resourceCulture);

		internal static string STS003Description => ResourceManager.GetString("STS003Description", resourceCulture);

		internal static string STS003MessageFormat => ResourceManager.GetString("STS003MessageFormat", resourceCulture);

		internal static string STS003Title => ResourceManager.GetString("STS003Title", resourceCulture);

		internal static string STS004CodeFixTitle => ResourceManager.GetString("STS004CodeFixTitle", resourceCulture);

		internal static string STS004Description => ResourceManager.GetString("STS004Description", resourceCulture);

		internal static string STS004MessageFormat => ResourceManager.GetString("STS004MessageFormat", resourceCulture);

		internal static string STS004Title => ResourceManager.GetString("STS004Title", resourceCulture);

		internal Resources()
		{
		}
	}
}
namespace ModAnalyzers.Json
{
	public class JsonArray : JsonValue, IList<JsonValue>, ICollection<JsonValue>, IEnumerable<JsonValue>, IEnumerable
	{
		private List<JsonValue> list;

		public override int Count => list.Count;

		public bool IsReadOnly => false;

		public sealed override JsonValue this[int index]
		{
			get
			{
				return list[index];
			}
			set
			{
				list[index] = value;
			}
		}

		public override JsonType JsonType => JsonType.Array;

		public JsonArray(params JsonValue[] items)
		{
			list = new List<JsonValue>();
			AddRange(items);
		}

		public JsonArray(IEnumerable<JsonValue> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			list = new List<JsonValue>(items);
		}

		public void Add(JsonValue item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}
			list.Add(item);
		}

		public void AddRange(IEnumerable<JsonValue> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			list.AddRange(items);
		}

		public void AddRange(params JsonValue[] items)
		{
			if (items != null)
			{
				list.AddRange(items);
			}
		}

		public void Clear()
		{
			list.Clear();
		}

		public bool Contains(JsonValue item)
		{
			return list.Contains(item);
		}

		public void CopyTo(JsonValue[] array, int arrayIndex)
		{
			list.CopyTo(array, arrayIndex);
		}

		public int IndexOf(JsonValue item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, JsonValue item)
		{
			list.Insert(index, item);
		}

		public bool Remove(JsonValue item)
		{
			return list.Remove(item);
		}

		public void RemoveAt(int index)
		{
			list.RemoveAt(index);
		}

		public override void Save(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			stream.WriteByte(91);
			for (int i = 0; i < list.Count; i++)
			{
				JsonValue jsonValue = list[i];
				if (jsonValue != null)
				{
					jsonValue.Save(stream);
				}
				else
				{
					stream.WriteByte(110);
					stream.WriteByte(117);
					stream.WriteByte(108);
					stream.WriteByte(108);
				}
				if (i < Count - 1)
				{
					stream.WriteByte(44);
					stream.WriteByte(32);
				}
			}
			stream.WriteByte(93);
		}

		IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
		{
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
	public class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<KeyValuePair<string, JsonValue>>, IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable
	{
		private SortedDictionary<string, JsonValue> map;

		public override int Count => map.Count;

		public sealed override JsonValue this[string key]
		{
			get
			{
				return map[key];
			}
			set
			{
				map[key] = value;
			}
		}

		public override JsonType JsonType => JsonType.Object;

		public ICollection<string> Keys => map.Keys;

		public ICollection<JsonValue> Values => map.Values;

		bool ICollection<KeyValuePair<string, JsonValue>>.IsReadOnly => false;

		public JsonObject(params KeyValuePair<string, JsonValue>[] items)
		{
			map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
			if (items != null)
			{
				AddRange(items);
			}
		}

		public JsonObject(IEnumerable<KeyValuePair<string, JsonValue>> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			map = new SortedDictionary<string, JsonValue>(StringComparer.Ordinal);
			AddRange(items);
		}

		public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
		{
			return map.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return map.GetEnumerator();
		}

		public void Add(string key, JsonValue value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			map.Add(key, value);
		}

		public void Add(KeyValuePair<string, JsonValue> pair)
		{
			Add(pair.Key, pair.Value);
		}

		public void AddRange(IEnumerable<KeyValuePair<string, JsonValue>> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			foreach (KeyValuePair<string, JsonValue> item in items)
			{
				map.Add(item.Key, item.Value);
			}
		}

		public void AddRange(params KeyValuePair<string, JsonValue>[] items)
		{
			AddRange((IEnumerable<KeyValuePair<string, JsonValue>>)items);
		}

		public void Clear()
		{
			map.Clear();
		}

		bool ICollection<KeyValuePair<string, JsonValue>>.Contains(KeyValuePair<string, JsonValue> item)
		{
			return ((ICollection<KeyValuePair<string, JsonValue>>)map).Contains(item);
		}

		bool ICollection<KeyValuePair<string, JsonValue>>.Remove(KeyValuePair<string, JsonValue> item)
		{
			return ((ICollection<KeyValuePair<string, JsonValue>>)map).Remove(item);
		}

		public override bool ContainsKey(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return map.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, JsonValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, JsonValue>>)map).CopyTo(array, arrayIndex);
		}

		public bool Remove(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			return map.Remove(key);
		}

		public override void Save(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			stream.WriteByte(123);
			foreach (KeyValuePair<string, JsonValue> item in map)
			{
				stream.WriteByte(34);
				byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(item.Key));
				stream.Write(bytes, 0, bytes.Length);
				stream.WriteByte(34);
				stream.WriteByte(44);
				stream.WriteByte(32);
				if (item.Value == null)
				{
					stream.WriteByte(110);
					stream.WriteByte(117);
					stream.WriteByte(108);
					stream.WriteByte(108);
				}
				else
				{
					item.Value.Save(stream);
				}
			}
			stream.WriteByte(125);
		}

		public bool TryGetValue(string key, out JsonValue value)
		{
			return map.TryGetValue(key, out value);
		}
	}
	public class JsonPrimitive : JsonValue
	{
		private object value;

		private static readonly byte[] true_bytes = Encoding.UTF8.GetBytes("true");

		private static readonly byte[] false_bytes = Encoding.UTF8.GetBytes("false");

		internal object Value => value;

		public override JsonType JsonType
		{
			get
			{
				if (value == null)
				{
					return JsonType.String;
				}
				switch (Type.GetTypeCode(value.GetType()))
				{
				case TypeCode.Boolean:
					return JsonType.Boolean;
				case TypeCode.Object:
				case TypeCode.Char:
				case TypeCode.DateTime:
				case TypeCode.String:
					return JsonType.String;
				default:
					return JsonType.Number;
				}
			}
		}

		public JsonPrimitive(bool value)
		{
			this.value = value;
		}

		public JsonPrimitive(byte value)
		{
			this.value = value;
		}

		public JsonPrimitive(char value)
		{
			this.value = value;
		}

		public JsonPrimitive(decimal value)
		{
			this.value = value;
		}

		public JsonPrimitive(double value)
		{
			this.value = value;
		}

		public JsonPrimitive(float value)
		{
			this.value = value;
		}

		public JsonPrimitive(int value)
		{
			this.value = value;
		}

		public JsonPrimitive(long value)
		{
			this.value = value;
		}

		public JsonPrimitive(sbyte value)
		{
			this.value = value;
		}

		public JsonPrimitive(short value)
		{
			this.value = value;
		}

		public JsonPrimitive(string value)
		{
			this.value = value;
		}

		public JsonPrimitive(DateTime value)
		{
			this.value = value;
		}

		public JsonPrimitive(uint value)
		{
			this.value = value;
		}

		public JsonPrimitive(ulong value)
		{
			this.value = value;
		}

		public JsonPrimitive(ushort value)
		{
			this.value = value;
		}

		public JsonPrimitive(DateTimeOffset value)
		{
			this.value = value;
		}

		public JsonPrimitive(Guid value)
		{
			this.value = value;
		}

		public JsonPrimitive(TimeSpan value)
		{
			this.value = value;
		}

		public JsonPrimitive(Uri value)
		{
			this.value = value;
		}

		public override void Save(Stream stream)
		{
			switch (JsonType)
			{
			case JsonType.Boolean:
				if ((bool)value)
				{
					stream.Write(true_bytes, 0, 4);
				}
				else
				{
					stream.Write(false_bytes, 0, 5);
				}
				break;
			case JsonType.String:
			{
				stream.WriteByte(34);
				byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(value.ToString()));
				stream.Write(bytes, 0, bytes.Length);
				stream.WriteByte(34);
				break;
			}
			default:
			{
				byte[] bytes = Encoding.UTF8.GetBytes(GetFormattedString());
				stream.Write(bytes, 0, bytes.Length);
				break;
			}
			}
		}

		internal string GetFormattedString()
		{
			switch (JsonType)
			{
			case JsonType.String:
				if (value is string result)
				{
					return result;
				}
				if (value is char)
				{
					return value.ToString();
				}
				throw new NotImplementedException("GetFormattedString from value type " + value.GetType());
			case JsonType.Number:
			{
				string text = ((!(value is float) && !(value is double)) ? ((IFormattable)value).ToString("G", NumberFormatInfo.InvariantInfo) : ((IFormattable)value).ToString("R", NumberFormatInfo.InvariantInfo));
				switch (text)
				{
				case "NaN":
				case "Infinity":
				case "-Infinity":
					return "\"" + text + "\"";
				default:
					return text;
				}
			}
			default:
				throw new InvalidOperationException();
			}
		}
	}
	internal class JsonReader
	{
		private class TextReaderCharEnumerator(TextReader text) : IEnumerator<char>, IEnumerator, IDisposable
		{
			public char Current { get; private set; }

			object? IEnumerator.Current => Current;

			public bool MoveNext()
			{
				int num = text.Read();
				if (num >= 0)
				{
					Current = (char)num;
					return true;
				}
				Current = '\0';
				return false;
			}

			public void Reset()
			{
				throw new InvalidOperationException();
			}

			public void Dispose()
			{
			}
		}

		private int line = 1;

		private int column;

		private readonly IEnumerator<char> reader;

		private int peek;

		private bool has_peek;

		private bool prev_lf;

		private StringBuilder vb = new StringBuilder();

		public static object Read(string text)
		{
			JsonReader jsonReader = new JsonReader(text);
			object result = jsonReader.ReadCore();
			jsonReader.SkipSpaces();
			if (jsonReader.ReadChar() >= 0)
			{
				throw JsonError("extra characters in JSON input", jsonReader.line, jsonReader.column);
			}
			return result;
		}

		public static object Read(TextReader text)
		{
			JsonReader jsonReader = new JsonReader(new TextReaderCharEnumerator(text));
			object result = jsonReader.ReadCore();
			jsonReader.SkipSpaces();
			if (jsonReader.ReadChar() >= 0)
			{
				throw JsonError("extra characters in JSON input", jsonReader.line, jsonReader.column);
			}
			return result;
		}

		public static object? ReadIgnoreError(string text)
		{
			try
			{
				return new JsonReader(text).ReadCore();
			}
			catch (ArgumentException)
			{
			}
			return null;
		}

		private JsonReader(string text)
			: this(text.GetEnumerator())
		{
		}

		private JsonReader(IEnumerator<char> reader)
		{
			this.reader = reader;
		}

		~JsonReader()
		{
			reader?.Dispose();
		}

		private object ReadCore()
		{
			SkipSpaces();
			int num = PeekChar();
			if (num < 0)
			{
				throw JsonError("Incomplete JSON input", line, column);
			}
			switch (num)
			{
			case 91:
			{
				ReadChar();
				List<object> list = new List<object>();
				SkipSpaces();
				if (PeekChar() == 93)
				{
					ReadChar();
					return list;
				}
				while (true)
				{
					list.Add(ReadCore());
					SkipSpaces();
					num = PeekChar();
					if (num != 44)
					{
						break;
					}
					ReadChar();
				}
				if (ReadChar() != 93)
				{
					throw JsonError("JSON array must end with ']'", line, column);
				}
				return list.ToArray();
			}
			case 123:
			{
				ReadChar();
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				SkipSpaces();
				if (PeekChar() == 125)
				{
					ReadChar();
					return dictionary;
				}
				do
				{
					SkipSpaces();
					if (PeekChar() == 125)
					{
						ReadChar();
						break;
					}
					string key = ReadStringLiteral();
					SkipSpaces();
					Expect(':');
					SkipSpaces();
					dictionary[key] = ReadCore();
					SkipSpaces();
					num = ReadChar();
				}
				while (num == 44 || num != 125);
				return dictionary.ToArray();
			}
			case 116:
				Expect("true");
				return true;
			case 102:
				Expect("false");
				return false;
			case 110:
				Expect("null");
				return "NULL";
			case 34:
				return ReadStringLiteral();
			default:
				if ((48 <= num && num <= 57) || num == 45)
				{
					return ReadNumericLiteral();
				}
				throw JsonError($"Unexpected character '{(char)num}'", line, column);
			}
		}

		private int PeekChar()
		{
			if (!has_peek)
			{
				if (reader.MoveNext())
				{
					peek = reader.Current;
				}
				else
				{
					peek = -1;
				}
				has_peek = true;
			}
			return peek;
		}

		private int ReadChar()
		{
			int num = PeekChar();
			has_peek = false;
			if (prev_lf)
			{
				line++;
				column = 0;
				prev_lf = false;
			}
			if (num == 10)
			{
				prev_lf = true;
			}
			column++;
			return num;
		}

		private void SkipSpaces()
		{
			while (true)
			{
				int num = PeekChar();
				if ((uint)(num - 9) <= 1u || num == 13 || num == 32)
				{
					ReadChar();
					continue;
				}
				break;
			}
		}

		private object ReadNumericLiteral()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (PeekChar() == 45)
			{
				stringBuilder.Append((char)ReadChar());
			}
			int num = 0;
			bool flag = PeekChar() == 48;
			int num2;
			while (true)
			{
				num2 = PeekChar();
				if (num2 < 48 || 57 < num2)
				{
					break;
				}
				stringBuilder.Append((char)ReadChar());
				if (flag && num == 1)
				{
					throw JsonError("leading zeros are not allowed", line, column);
				}
				num++;
			}
			if (num == 0)
			{
				throw JsonError("Invalid JSON numeric literal; no digit found", line, column);
			}
			bool flag2 = false;
			int num3 = 0;
			if (PeekChar() == 46)
			{
				flag2 = true;
				stringBuilder.Append((char)ReadChar());
				if (PeekChar() < 0)
				{
					throw JsonError("Invalid JSON numeric literal; extra dot", line, column);
				}
				while (true)
				{
					num2 = PeekChar();
					if (num2 < 48 || 57 < num2)
					{
						break;
					}
					stringBuilder.Append((char)ReadChar());
					num3++;
				}
				if (num3 == 0)
				{
					throw JsonError("Invalid JSON numeric literal; extra dot", line, column);
				}
			}
			num2 = PeekChar();
			if (num2 != 101 && num2 != 69)
			{
				if (!flag2)
				{
					if (int.TryParse(stringBuilder.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
					{
						return result;
					}
					if (long.TryParse(stringBuilder.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
					{
						return result2;
					}
					if (ulong.TryParse(stringBuilder.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
					{
						return result3;
					}
				}
				if (decimal.TryParse(stringBuilder.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result4) && result4 != 0m)
				{
					return result4;
				}
			}
			else
			{
				stringBuilder.Append((char)ReadChar());
				if (PeekChar() < 0)
				{
					throw JsonError("Invalid JSON numeric literal; incomplete exponent", line, column);
				}
				switch (PeekChar())
				{
				case 45:
					stringBuilder.Append((char)ReadChar());
					break;
				case 43:
					stringBuilder.Append((char)ReadChar());
					break;
				}
				if (PeekChar() < 0)
				{
					throw JsonError("Invalid JSON numeric literal; incomplete exponent", line, column);
				}
				while (true)
				{
					num2 = PeekChar();
					if (num2 < 48 || 57 < num2)
					{
						break;
					}
					stringBuilder.Append((char)ReadChar());
				}
			}
			return double.Parse(stringBuilder.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		private string ReadStringLiteral()
		{
			if (PeekChar() != 34)
			{
				throw JsonError("Invalid JSON string literal format", line, column);
			}
			ReadChar();
			vb.Length = 0;
			while (true)
			{
				int num = ReadChar();
				if (num < 0)
				{
					break;
				}
				switch (num)
				{
				case 34:
					return vb.ToString();
				default:
					vb.Append((char)num);
					break;
				case 92:
					num = ReadChar();
					if (num < 0)
					{
						throw JsonError("Invalid JSON string literal; incomplete escape sequence", line, column);
					}
					switch (num)
					{
					case 34:
					case 47:
					case 92:
						vb.Append((char)num);
						break;
					case 98:
						vb.Append('\b');
						break;
					case 102:
						vb.Append('\f');
						break;
					case 110:
						vb.Append('\n');
						break;
					case 114:
						vb.Append('\r');
						break;
					case 116:
						vb.Append('\t');
						break;
					case 117:
					{
						ushort num2 = 0;
						for (int i = 0; i < 4; i++)
						{
							num2 <<= 4;
							if ((num = ReadChar()) < 0)
							{
								throw JsonError("Incomplete unicode character escape literal", line, column);
							}
							if (48 <= num && num <= 57)
							{
								num2 += (ushort)(num - 48);
							}
							if (65 <= num && num <= 70)
							{
								num2 += (ushort)(num - 65 + 10);
							}
							if (97 <= num && num <= 102)
							{
								num2 += (ushort)(num - 97 + 10);
							}
						}
						vb.Append((char)num2);
						break;
					}
					default:
						throw JsonError("Invalid JSON string literal; unexpected escape character", line, column);
					}
					break;
				}
			}
			throw JsonError("JSON string is not closed", line, column);
		}

		private void Expect(char expected)
		{
			int num;
			if ((num = ReadChar()) != expected)
			{
				throw JsonError($"Expected '{expected}', got '{(char)num}'", line, column);
			}
		}

		private void Expect(string expected)
		{
			for (int i = 0; i < expected.Length; i++)
			{
				if (ReadChar() != expected[i])
				{
					throw JsonError($"Expected '{expected}', differed at {i}", line, column);
				}
			}
		}

		private static Exception JsonError(string msg, int line, int column)
		{
			return new ArgumentException($"{msg}. At line {line}, column {column}");
		}
	}
	public enum JsonType
	{
		String,
		Number,
		Object,
		Array,
		Boolean
	}
	public abstract class JsonValue : IEnumerable
	{
		public virtual int Count
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		public abstract JsonType JsonType { get; }

		public virtual JsonValue this[int index]
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public virtual JsonValue this[string key]
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public static JsonValue? Load(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			return Load(new StreamReader(stream, detectEncodingFromByteOrderMarks: true));
		}

		public static JsonValue? Load(TextReader textReader)
		{
			if (textReader == null)
			{
				throw new ArgumentNullException("textReader");
			}
			object obj = JsonReader.Read(textReader);
			if (obj == null)
			{
				return null;
			}
			return ToJsonValue(obj);
		}

		private static IEnumerable<KeyValuePair<string, JsonValue>> ToJsonPairEnumerable(IEnumerable<KeyValuePair<string, object>> kvpc)
		{
			foreach (KeyValuePair<string, object> item in kvpc)
			{
				yield return new KeyValuePair<string, JsonValue>(item.Key, ToJsonValue(item.Value));
			}
		}

		private static IEnumerable<JsonValue> ToJsonValueEnumerable(IEnumerable<object> arr)
		{
			foreach (object item in arr)
			{
				yield return ToJsonValue(item);
			}
		}

		private static JsonValue ToJsonValue(object ret)
		{
			if (ret is IEnumerable<KeyValuePair<string, object>> kvpc)
			{
				return new JsonObject(ToJsonPairEnumerable(kvpc));
			}
			if (ret is IEnumerable<object> arr)
			{
				return new JsonArray(ToJsonValueEnumerable(arr));
			}
			if (ret is bool)
			{
				return new JsonPrimitive((bool)ret);
			}
			if (ret is byte)
			{
				return new JsonPrimitive((byte)ret);
			}
			if (ret is char)
			{
				return new JsonPrimitive((char)ret);
			}
			if (ret is decimal)
			{
				return new JsonPrimitive((decimal)ret);
			}
			if (ret is double)
			{
				return new JsonPrimitive((double)ret);
			}
			if (ret is float)
			{
				return new JsonPrimitive((float)ret);
			}
			if (ret is int)
			{
				return new JsonPrimitive((int)ret);
			}
			if (ret is long)
			{
				return new JsonPrimitive((long)ret);
			}
			if (ret is sbyte)
			{
				return new JsonPrimitive((sbyte)ret);
			}
			if (ret is short)
			{
				return new JsonPrimitive((short)ret);
			}
			if (ret is string)
			{
				return new JsonPrimitive((string)ret);
			}
			if (ret is uint)
			{
				return new JsonPrimitive((uint)ret);
			}
			if (ret is ulong)
			{
				return new JsonPrimitive((ulong)ret);
			}
			if (ret is ushort)
			{
				return new JsonPrimitive((ushort)ret);
			}
			if (ret is DateTime)
			{
				return new JsonPrimitive((DateTime)ret);
			}
			if (ret is DateTimeOffset)
			{
				return new JsonPrimitive((DateTimeOffset)ret);
			}
			if (ret is Guid)
			{
				return new JsonPrimitive((Guid)ret);
			}
			if (ret is TimeSpan)
			{
				return new JsonPrimitive((TimeSpan)ret);
			}
			if (ret is Uri)
			{
				return new JsonPrimitive((Uri)ret);
			}
			throw new NotSupportedException($"Unexpected parser return type: {ret.GetType()}");
		}

		public static JsonValue? Parse(string jsonString)
		{
			if (jsonString == null)
			{
				throw new ArgumentNullException("jsonString");
			}
			return Load(new StringReader(jsonString));
		}

		public virtual bool ContainsKey(string key)
		{
			throw new InvalidOperationException();
		}

		public virtual void Save(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			Save(new StreamWriter(stream));
		}

		public virtual void Save(TextWriter textWriter)
		{
			if (textWriter == null)
			{
				throw new ArgumentNullException("textWriter");
			}
			SaveInternal(textWriter);
		}

		private void SaveInternal(TextWriter w)
		{
			switch (JsonType)
			{
			case JsonType.Object:
			{
				w.Write('{');
				bool flag = false;
				foreach (KeyValuePair<string, JsonValue> item in (JsonObject)this)
				{
					if (flag)
					{
						w.Write(", ");
					}
					w.Write('"');
					w.Write(EscapeString(item.Key));
					w.Write("\": ");
					if (item.Value == null)
					{
						w.Write("null");
					}
					else
					{
						item.Value.SaveInternal(w);
					}
					flag = true;
				}
				w.Write('}');
				break;
			}
			case JsonType.Array:
			{
				w.Write('[');
				bool flag = false;
				foreach (JsonValue item2 in (IEnumerable<JsonValue>)(JsonArray)this)
				{
					if (flag)
					{
						w.Write(", ");
					}
					if (item2 != null)
					{
						item2.SaveInternal(w);
					}
					else
					{
						w.Write("null");
					}
					flag = true;
				}
				w.Write(']');
				break;
			}
			case JsonType.Boolean:
				w.Write(this ? "true" : "false");
				break;
			case JsonType.String:
				w.Write('"');
				w.Write(EscapeString(((JsonPrimitive)this).GetFormattedString()));
				w.Write('"');
				break;
			default:
				w.Write(((JsonPrimitive)this).GetFormattedString());
				break;
			}
		}

		public override string ToString()
		{
			StringWriter stringWriter = new StringWriter();
			Save(stringWriter);
			return stringWriter.ToString();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new InvalidOperationException();
		}

		private bool NeedEscape(string src, int i)
		{
			char c = src[i];
			if (c >= ' ' && c != '"' && c != '\\' && (c < '\ud800' || c > '\udbff' || (i != src.Length - 1 && src[i + 1] >= '\udc00' && src[i + 1] <= '\udfff')) && (c < '\udc00' || c > '\udfff' || (i != 0 && src[i - 1] >= '\ud800' && src[i - 1] <= '\udbff')) && c != '\u2028' && c != '\u2029')
			{
				if (c == '/' && i > 0)
				{
					return src[i - 1] == '<';
				}
				return false;
			}
			return true;
		}

		internal string EscapeString(string src)
		{
			for (int i = 0; i < src.Length; i++)
			{
				if (NeedEscape(src, i))
				{
					StringBuilder stringBuilder = new StringBuilder();
					if (i > 0)
					{
						stringBuilder.Append(src, 0, i);
					}
					return DoEscapeString(stringBuilder, src, i);
				}
			}
			return src;
		}

		private string DoEscapeString(StringBuilder sb, string src, int cur)
		{
			int num = cur;
			for (int i = cur; i < src.Length; i++)
			{
				if (NeedEscape(src, i))
				{
					sb.Append(src, num, i - num);
					switch (src[i])
					{
					case '\b':
						sb.Append("\\b");
						break;
					case '\f':
						sb.Append("\\f");
						break;
					case '\n':
						sb.Append("\\n");
						break;
					case '\r':
						sb.Append("\\r");
						break;
					case '\t':
						sb.Append("\\t");
						break;
					case '"':
						sb.Append("\\\"");
						break;
					case '\\':
						sb.Append("\\\\");
						break;
					case '/':
						sb.Append("\\/");
						break;
					default:
						sb.Append("\\u");
						sb.Append(((int)src[i]).ToString("x04"));
						break;
					}
					num = i + 1;
				}
			}
			sb.Append(src, num, src.Length - num);
			return sb.ToString();
		}

		public static implicit operator JsonValue(bool value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(byte value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(char value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(decimal value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(double value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(float value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(int value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(long value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(sbyte value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(short value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(string value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(uint value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(ulong value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(ushort value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(DateTime value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(DateTimeOffset value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(Guid value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(TimeSpan value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator JsonValue(Uri value)
		{
			return new JsonPrimitive(value);
		}

		public static implicit operator bool(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToBoolean(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator byte(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToByte(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator char(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToChar(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator decimal(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToDecimal(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator double(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToDouble(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator float(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToSingle(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator int(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToInt32(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator long(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToInt64(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator sbyte(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToSByte(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator short(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToInt16(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator string(JsonValue value)
		{
			return (string)((JsonPrimitive)value).Value;
		}

		public static implicit operator uint(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToUInt32(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator ulong(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToUInt64(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator ushort(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return Convert.ToUInt16(((JsonPrimitive)value).Value, NumberFormatInfo.InvariantInfo);
		}

		public static implicit operator DateTime(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return (DateTime)((JsonPrimitive)value).Value;
		}

		public static implicit operator DateTimeOffset(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return (DateTimeOffset)((JsonPrimitive)value).Value;
		}

		public static implicit operator TimeSpan(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return (TimeSpan)((JsonPrimitive)value).Value;
		}

		public static implicit operator Guid(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return (Guid)((JsonPrimitive)value).Value;
		}

		public static implicit operator Uri(JsonValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return (Uri)((JsonPrimitive)value).Value;
		}
	}
}
