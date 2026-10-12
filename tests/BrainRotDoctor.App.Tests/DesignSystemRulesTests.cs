using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using Xunit;

namespace BrainRotDoctor.App.Tests;

/// <summary>
/// The rules of the design system (src/BrainRotDoctor.DesignSystem/README.md),
/// checked on the compiled app so they can't be broken by accident:
/// screens build only from design-system controls and plain layout panels,
/// never style anything themselves, and the design system knows nothing of the app.
/// </summary>
public sealed class DesignSystemRulesTests
{
    private const string DesignSystemAssembly = "BrainRotDoctor.DesignSystem";

    /// <summary>Plain layout containers and images: they arrange, they don't look like anything.</summary>
    private static readonly HashSet<string> LayoutPanels = new()
    {
        "Avalonia.Controls.Grid",
        "Avalonia.Controls.StackPanel",
        "Avalonia.Controls.DockPanel",
        "Avalonia.Controls.WrapPanel",
        "Avalonia.Controls.Panel",
        "Avalonia.Controls.ScrollViewer",
        "Avalonia.Controls.ContentControl",
        "Avalonia.Controls.Image",
    };

    /// <summary>Visual building blocks that only the design system may create.</summary>
    private static readonly string[] StylingBaseTypes =
    {
        "Avalonia.Controls.Control",
        "Avalonia.Controls.Primitives.FlyoutBase",
        "Avalonia.Controls.Documents.TextElement",
        "Avalonia.Media.Brush",
        "Avalonia.Styling.Styles",
        "Avalonia.Styling.StyleBase",
        "Avalonia.Controls.ResourceDictionary",
    };

    private static readonly HashSet<string> StylingTypes = new()
    {
        "Avalonia.Input.Cursor",
        "Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension",
        "Avalonia.Media.FontFamily",
        "Avalonia.Media.BoxShadow",
        "Avalonia.Media.BoxShadows",
        "Avalonia.Media.Brushes",
        "Avalonia.Media.Colors",
        "Avalonia.Media.Color",
        "Avalonia.Media.Geometry",
        "Avalonia.Styling.Setter",
    };

    /// <summary>Properties that change how something looks; only the design system sets them.</summary>
    private static readonly HashSet<string> StyleProperties = new()
    {
        "Background", "Foreground", "BorderBrush", "BorderThickness", "CornerRadius", "BoxShadow",
        "FontSize", "FontWeight", "FontFamily", "FontStyle", "LetterSpacing", "TextDecorations",
        "Opacity", "Cursor", "Resources", "Styles", "Theme", "Classes",
    };

    /// <summary>Interaction plumbing; screens react to controls' own events (Click, Changed…) instead.</summary>
    private static readonly HashSet<string> InteractionMembers = new()
    {
        "TappedEvent", "DoubleTappedEvent",
        "add_Tapped", "add_DoubleTapped", "add_PointerPressed", "add_PointerReleased",
        "add_PointerEntered", "add_PointerExited", "add_PointerMoved",
    };

    [Fact]
    public void Screens_use_only_design_system_controls_and_never_style_anything()
    {
        var violations = new List<string>();
        foreach (TypeDefinition type in ScreenTypes())
        {
            foreach (MethodDefinition method in type.Methods.Where(m => m.HasBody))
            {
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    if (Violation(instruction) is { } problem)
                    {
                        violations.Add($"{type.FullName}.{method.Name}: {problem}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Screens must be built from the design system (see src/BrainRotDoctor.DesignSystem/README.md). " +
            "Add or extend a control there instead of:\n" + string.Join("\n", violations.Distinct()));
    }

    [Fact]
    public void App_controls_derive_from_design_system_bases()
    {
        var violations = new List<string>();
        foreach (TypeDefinition type in ScreenTypes())
        {
            TypeReference? baseType = type.BaseType;
            if (baseType is null || IsInModule(baseType, AppModule.Value))
            {
                continue;
            }

            if (Ancestors(baseType).Any(a => a == "Avalonia.Visual") && baseType.Scope.Name != DesignSystemAssembly)
            {
                violations.Add($"{type.FullName} derives from {baseType.FullName}");
            }
        }

        Assert.True(violations.Count == 0, "Visual types of the app must derive from a design system base (e.g. AppWindow):\n" + string.Join("\n", violations));
    }

    [Fact]
    public void Design_system_knows_nothing_of_the_app()
    {
        string[] references = DesignSystemModule.Value.AssemblyReferences.Select(r => r.Name).ToArray();
        string[] domain = references.Where(r => r.StartsWith("BrainRotDoctor", StringComparison.Ordinal)).ToArray();
        Assert.True(domain.Length == 0, "The design system must stay domain-agnostic but references: " + string.Join(", ", domain));
    }

    [Fact]
    public void Every_design_system_control_is_documented()
    {
        string docs = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BrainRotDoctor.DesignSystem", "docs", "components.md"));
        string[] missing = DesignSystemModule.Value.Types
            .Where(t => t.IsPublic && !t.IsEnum && t.Name != "Gallery")
            .Select(t => t.Name)
            .Where(name => !docs.Contains($"`{name}`", StringComparison.Ordinal))
            .ToArray();
        Assert.True(missing.Length == 0, "Not documented in docs/components.md: " + string.Join(", ", missing));
    }

    // ---------- Inspection ----------

    private static readonly Lazy<ModuleDefinition> AppModule = new(() => Load(typeof(BrainRotDoctor.App.Ui.Loc).Assembly.Location));
    private static readonly Lazy<ModuleDefinition> DesignSystemModule = new(() => Load(typeof(BrainRotDoctor.DesignSystem.Card).Assembly.Location));

    private static ModuleDefinition Load(string path)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(path)!);
        return ModuleDefinition.ReadModule(path, new ReaderParameters { AssemblyResolver = resolver });
    }

    /// <summary>Every app type that builds UI, except the illustration code (scene, product icon) and the DEBUG snapshot tool.</summary>
    private static IEnumerable<TypeDefinition> ScreenTypes()
    {
        foreach (TypeDefinition type in AppModule.Value.GetTypes())
        {
            string name = type.FullName;
            bool exempt = name.StartsWith("BrainRotDoctor.App.Ui.Scene.", StringComparison.Ordinal)
                || name.StartsWith("BrainRotDoctor.App.Ui.ProductIcon", StringComparison.Ordinal)
                || name.StartsWith("BrainRotDoctor.App.Ui.UiSnapshots", StringComparison.Ordinal);
            if (!exempt)
            {
                yield return type;
            }
        }
    }

    private static string? Violation(Instruction instruction)
    {
        if (instruction.Operand is not MemberReference member || member.DeclaringType is not { } declaring)
        {
            return null;
        }

        if (IsInModule(declaring, AppModule.Value) || declaring.Scope.Name == DesignSystemAssembly)
        {
            return null;
        }

        string typeName = declaring.FullName;
        if (!typeName.StartsWith("Avalonia.", StringComparison.Ordinal))
        {
            return null;
        }

        bool isConstructor = member.Name == ".ctor" && instruction.OpCode.Code is Code.Newobj or Code.Call;
        if (isConstructor && !LayoutPanels.Contains(typeName) && IsStyling(declaring))
        {
            return $"creates {typeName}";
        }

        if (StylingTypes.Contains(typeName))
        {
            return $"uses {typeName}.{member.Name}";
        }

        if (member.Name.StartsWith("set_", StringComparison.Ordinal) && StyleProperties.Contains(member.Name[4..]))
        {
            return $"sets {member.Name[4..]}";
        }

        if (member.Name.StartsWith("get_", StringComparison.Ordinal) && member.Name[4..] is "Resources" or "Styles")
        {
            return $"touches {member.Name[4..]}";
        }

        if (member.Name == "set_Item" && typeName == "Avalonia.AvaloniaObject")
        {
            return "binds a property to a resource";
        }

        if (InteractionMembers.Contains(member.Name))
        {
            return $"wires {member.Name.Replace("add_", "")} directly";
        }

        return null;
    }

    private static bool IsStyling(TypeReference type)
    {
        if (StylingTypes.Contains(type.FullName))
        {
            return true;
        }

        List<string> ancestors = Ancestors(type).ToList();
        return ancestors.Any(a => StylingBaseTypes.Contains(a));
    }

    /// <summary>The type and its base types, by full name.</summary>
    private static IEnumerable<string> Ancestors(TypeReference type)
    {
        TypeReference? current = type;
        while (current is not null)
        {
            yield return current.FullName;
            TypeDefinition? definition;
            try
            {
                definition = current.Resolve();
            }
            catch (AssemblyResolutionException)
            {
                yield break;
            }

            current = definition?.BaseType;
        }
    }

    private static bool IsInModule(TypeReference type, ModuleDefinition module) =>
        type.Scope == module || type.Scope.Name == module.Name || type.Scope.Name == module.Assembly.Name.Name;

    private static string RepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "BrainRotDoctor.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new InvalidOperationException("Repository root not found.");
    }
}
