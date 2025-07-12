# Analyzers Extension

The Analyzers extension provides automatic Roslyn analyzer integration infrastructure, allowing developers to seamlessly add any Roslyn analyzers to their projects without manual project file modifications.

## Core Functionality

### Automatic Project File Integration

The `AnalyzersProjectFileGeneration` class automatically enhances generated `.csproj` files to include any Roslyn analyzers placed in the `RoslynAnalyzers` directory:

**Key Features:**
- Automatically scans the `RoslynAnalyzers` directory for analyzer files
- Excludes Unity assemblies (`Unity.*`) to prevent conflicts
- Supports multiple file types for complete analyzer integration
- Zero manual project file configuration required

## Usage

### Setting Up Analyzers

1. **Create the RoslynAnalyzers Directory:**
   ```
   ProjectRoot/
   └── RoslynAnalyzers/
   ```

2. **Add Your Chosen Analyzers:**
   Place any combination of analyzer files in the directory:
   ```
   RoslynAnalyzers/
   ├── YourAnalyzer.dll          # Any Roslyn analyzer
   ├── AnotherAnalyzer.dll       # Multiple analyzers supported
   ├── custom.ruleset            # Rule configuration
   ├── analyzer-config.json      # Analyzer settings
   └── editorconfig              # Additional configuration
   ```

3. **Regenerate Project Files:**
   The analyzers will be automatically included in all non-Unity project files.

### Supported File Types

The system automatically handles different analyzer file types:

**Analyzer Assemblies (`.dll`):**
- Added as `<Analyzer Include="..." />` references
- Provides the core analysis functionality

**Rule Sets (`.ruleset`):**
- Sets the `CodeAnalysisRuleSet` property
- Configures which rules are active and their severity levels

**Configuration Files (`.json`):**
- Added as `<AdditionalFiles Include="..." />` 
- Provides analyzer-specific configuration

## Integration Details

### Project File Modifications
The system automatically adds appropriate MSBuild elements:

```xml
<ItemGroup>
  <Analyzer Include="RoslynAnalyzers/YourAnalyzer.dll" />
  <AdditionalFiles Include="RoslynAnalyzers/config.json" />
</ItemGroup>
<PropertyGroup>
  <CodeAnalysisRuleSet>RoslynAnalyzers/rules.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
```

### Unity Assembly Exclusion
Unity's own assemblies (`Unity.*`) are automatically excluded.