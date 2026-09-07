# Shared UI themes

Core provides **Bovine Works** and **The Curator** for BovineLabs samples and opt-in editor tools. Bovine Works is the default. Installing Core does not theme game UI, native Unity windows, or custom inspectors.

In Unity, choose **Edit > Preferences > BovineLabs > Appearance**. Theme selection lives in Preferences; sample panels do not include their own selectors. Changes apply immediately to attached, opted-in panels. The editor stores the choice in `EditorPrefs`; a built player stores its own choice in `PlayerPrefs`.

## Use in a sample

Reference `BovineLabs.Core` in the sample assembly and use the reusable UXML elements:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:bl="BovineLabs.Core.UI">
    <bl:BovineThemeRoot class="my-sample">
        <ui:Label text="Inventory sample" class="bl-title" />
        <ui:Button text="Craft sword" class="bl-button--primary" />
    </bl:BovineThemeRoot>
</ui:UXML>
```

The root loads the shared stylesheet through Core's Resources folder. It preserves normal `VisualElement` layout and picking behavior; declare sample placement, sizing, and pointer routing in the sample's UXML/USS. Keep the existing Unity runtime theme in `PanelSettings` so native controls retain their standard structure and resources.

An existing C#-owned root can opt in with `BovineThemeUtility.Apply(root)`. Repeated calls are safe. Theme bindings subscribe while the root belongs to a panel, unsubscribe when it detaches, and refresh to the latest selection when it reattaches. `BovineThemeUtility.Theme` selects the shared theme programmatically.

## Opt an editor window in later

Existing library windows have not been migrated. A window can adopt the theme explicitly without changing its base class:

```csharp
using BovineLabs.Core.UI;

public void CreateGUI()
{
    BovineThemeUtility.Apply(this.rootVisualElement);
    this.rootVisualElement.AddToClassList(BovineThemeUtility.WindowClass);
    // Build or clone this window's existing UI here.
}
```

`bl-theme-window` opts into the opaque theme background. The plain theme root is transparent. Use the shared Appearance preference for theme selection. Replace window-owned hardcoded decorative colours with theme tokens as that window adopts the theme.

For a `SettingsProvider`, add a new `BovineThemeRoot` child to the activation root and build the page inside that child. Unity reuses the activation root between preference pages, so applying the theme directly to it would also style other providers. Keep all theme classes and stylesheets on the child you own.

A standalone installer such as the BovineLabs package manager must remain usable before Core is installed. Future integration there should be optional and symbol-gated; do not introduce Core as an installer prerequisite just for styling.

## Edit the design in one place

The shared files live under `Packages/com.bovinelabs.core/Resources/BovineLabs/Themes/`:

| File | Responsibility |
| --- | --- |
| `BovineLabs.uss` | Imports the palettes and controls |
| `BovineWorks.uss` | Warm black, bone, vermilion; compact square geometry |
| `Curator.uss` | Slate, brass, plum; slightly softer geometry |
| `Controls.uss` | Scoped native UITK states and reusable presentation classes |

Palette selectors combine `.bl-theme` with `.bl-theme--bovine-works` or `.bl-theme--curator`. You can import the USS and apply these classes yourself when runtime preference binding is unnecessary.

Use semantic tokens in sample-specific USS:

```css
.my-sample {
    background-color: var(--bl-background);
    padding: 16px;
}

.my-sample__feedback {
    color: var(--bl-text-muted);
    border-bottom-color: var(--bl-border);
}
```

| Tokens | Purpose |
| --- | --- |
| `--bl-background`, `--bl-panel`, `--bl-surface`, `--bl-surface-hover` | Layered surfaces and interaction |
| `--bl-text`, `--bl-text-muted` | Primary and secondary text |
| `--bl-border`, `--bl-selection` | Dividers and selected surfaces |
| `--bl-accent`, `--bl-accent-hover`, `--bl-accent-text` | Primary actions; use the paired foreground on filled actions |
| `--bl-secondary-accent` | Brand details and focus |
| `--bl-progress` | A dark progress fill that keeps overlaid light text readable |
| `--bl-success`, `--bl-warning`, `--bl-error` | Status, accompanied by text or icons |
| `--bl-radius`, `--bl-radius-large` | Control and panel corners |
| `--bl-title-size`, `--bl-section-size` | Sample heading sizes |

Reusable classes include `bl-panel`, `bl-title`, `bl-section-title`, `bl-description`, `bl-muted`, and `bl-button--primary`. They supply presentation, not screen hierarchy. Theme selectors use `bl-theme-selector`.

Keep meaningful item categories, team colours, health, and other game data distinct from the brand palette. When authoring specialised controls, use suitably scoped selectors for their primary, selected, disabled, and semantic states so shared control defaults cannot override them.

## App UI and scope

Core's styles have no App UI dependency. Anchor imports a separate App UI alias adapter scoped entirely under `.bl-theme`. It only affects a hierarchy that explicitly opts in. Anchor's application root and Shattered game screens do not opt in automatically.

Keep themed sample hosts separate from actual game UI. Do not add a theme root to a shared game application or globally import a sample stylesheet into a game's theme.

## Sample imports

`Samples~` is source distribution content. Unity Package Manager copies a sample into the consuming project's `Assets/Samples` folder when imported. Reimport or update that copy to receive changed sample layouts. Shared Core palette changes are picked up directly by existing opted-in sample copies.
