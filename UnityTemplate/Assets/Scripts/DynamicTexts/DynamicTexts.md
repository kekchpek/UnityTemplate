# DynamicTexts

Reactive text system built on top of the global **Bindable Value Factory**.

Localized strings can reference gameplay values by id directly in locale files:

```
BUILDING_GAMEPLAY_DESCRIPTION_Quarry=Produces {QuarryStoneIncome} stone when placed next to 3 Mountain cells...
```

When a referenced value or locale changes, the resolved text updates automatically.

---

## Architecture

```
IBindableValueFactory + IBindableValueRegistry (single BindableValueFactory instance)
        │
        ├── QuarryStoneAbility / SawmillWoodAbility → QuarryStoneIncome, SawmillWoodIncome
        ├── BuildingsCountingModel                  → ActiveBuildingsCount_*, BuiltBuildingsCount_*
        │
        ▼
BindableTemplateResolver  ← parses {ValueId} from locale strings
        │
        ▼
ReactiveText (IReactiveText.Text)  ← fully resolved display string
        │
        ▼
UI (TMP_Text / labels)
```

| Layer | Location |
|-------|----------|
| Value factory | `kekchpek.BindableValues` |
| Game value ids | `CellsGame.Game.BindableValues.GameBindableValueIds` |
| Saved value helpers | `BindableValueFactoryExtensions` |
| Text resolution | `kekchpek.DynamicTexts` |
| Game-specific texts | `CellsGame.Game.Buildings.Texts` |

---

## Bindable Value Factory

Shared gameplay values are split into two interfaces:

| Interface | Responsibility | Inject when |
|-----------|----------------|-------------|
| `IBindableValueFactory` | Create and register values | Models, save setup, initialization |
| `IBindableValueRegistry` | Read, format, subscribe | UI, DynamicTexts, consumers |

Both are implemented by `BindableValueFactory` and registered as a single singleton via `BindableValuesSystemInstaller`:

```csharp
Container.Bind(new Type[]
{
    typeof(IBindableValueFactory),
    typeof(IBindableValueRegistry),
}).To<BindableValueFactory>().AsSingle();
```

```csharp
// Creation (models / initialization)
var income = factory.GetOrCreate(registry, GameBindableValueIds.QuarryStoneIncome, BigInteger.Zero);

// Consumption (UI / text)
registry.Get<BigInteger>(GameBindableValueIds.QuarryStoneIncome);
registry.TryGetFormattedValue(GameBindableValueIds.QuarryStoneIncome, out var text);
registry.Subscribe(GameBindableValueIds.QuarryStoneIncome, RefreshUi);
```

**Do not register** UI-only state (selected index, panel visibility, formatting buffers).

---

## Adding a building gameplay description

### Static text (no placeholders)

1. Add locale entry in `EN.txt`
2. Set `gameplayDescriptionKey` in `BuildingsConfig.json`

No code changes needed — `BuildingTextsService` creates a reactive text from the locale key.

### Dynamic text with `{ValueId}` placeholders

**1. Define the value id** in `GameBindableValueIds.cs`:

```csharp
public static BindableValueId HutGoldIncome => new(nameof(HutGoldIncome));
```

**2. Create the bindable** in the ability or model that owns the value:

```csharp
_stoneIncome = bindableValueFactory.GetOrCreate(
    bindableValueRegistry,
    GameBindableValueIds.QuarryStoneIncome,
    BigInteger.One);
```

**3. Set the per-building income** as the initial value (update later if upgrades change it).

**4. Reference it in locale** (`EN.txt`):

```
BUILDING_GAMEPLAY_DESCRIPTION_Hut=Generates {HutGoldIncome} gold while active...
```

**5. Set config key** in `BuildingsConfig.json`:

```json
"gameplayDescriptionKey": "BUILDING_GAMEPLAY_DESCRIPTION_Hut"
```

No template mapping file is required — placeholders are parsed from the locale string.

---

## Using reactive text in UI

Inject `IBuildingTextsService` in view models:

```csharp
var description = _buildingTextsService.GetGameplayDescription(buildingType);
```

Bind resolved text in the view:

```csharp
description.Text.Bind(text => label.text = text);
```

Building select already follows this pattern in `BuildingLayout`.

---

## Save system integration

The save system creates mutables through `IMutableFactory`, configured once when save managers are constructed.  
The save feature has no dependency on bindable values — it only calls `IMutableFactory.Create(valueKey, value)`.

UnityTemplate binds `BindableMutableFactory` as `IMutableFactory` in `UnityTemplateProjectInstaller`; `GameSaveManager` receives it via constructor injection.

```csharp
var activeId = GameBindableValueIds.ActiveBuildingsCount(buildingType);

_activeCounts[buildingType] = _gameSaveManager.GameDataProvider.DeserializeAndCaptureStructValue(
    activeId.Id,
    0);
```

Non-saved values still use the bindable factory directly:

```csharp
bindableValueFactory.GetOrCreate(bindableValueRegistry, GameBindableValueIds.QuarryStoneIncome, BigInteger.Zero);
```

---

## Checklist for a new dynamic value

- [ ] Add id to `GameBindableValueIds`
- [ ] Create via `factory.GetOrCreate(...)` or load via `DeserializeAndCaptureStructValue(id.Id, ...)` for saved values
- [ ] Update value from gameplay/service logic
- [ ] Use `{YourValueId}` in locale strings where needed

---

## File reference

```
BindableValues/
  BindableValueId.cs
  IBindableValueFactory.cs
  IBindableValueRegistry.cs
  IRegisteredBindableValue.cs
  RegisteredBindableValue.cs
  BindableValueFormatters.cs
  BindableValueFactory.cs
  BindableValuesSystemInstaller.cs

DynamicTexts/
  BindableTemplateResolver.cs
  IReactiveText.cs
  ReactiveText.cs
  IReactiveTextService.cs
  ReactiveTextService.cs
  DynamicTextsSystemInstaller.cs
  DynamicTexts.md

UnityTemplate/Game/BindableValues/
  GameBindableValueIds.cs
  BindableValueFactoryExtensions.cs
  BindableMutableFactory.cs

UnityTemplate/Game/Buildings/Texts/
  IBuildingTextsService.cs
  BuildingTextsService.cs
```
