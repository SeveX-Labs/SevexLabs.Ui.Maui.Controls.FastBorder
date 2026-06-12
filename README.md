# FastBorder for .NET MAUI

A lightweight, native-rendered alternative to MAUI `Border`.

`FastBorder` is designed for Android and iOS scenarios where a simple single-content border needs lower layout/rendering overhead than a standard MAUI `Border`.

In the included SampleApp profiling scenario, FastBorder showed up to ~44.5% faster after-layout time in the empty-border comparison. Treat these results as indicative: actual performance depends on the scenario, device, operating system, and build configuration.

## Install

```bash
dotnet add package SevexLabs.Ui.Maui.Controls.FastBorder --version 1.0.2
```

## Register

Register the handler in `MauiProgram.cs`:

```csharp
using SevexLabs.Ui.Maui.Controls.FastBorder.Extensions;

builder.UseSevexLabsFastBorder();
```

## XAML

```xml
xmlns:fastBorder="clr-namespace:SevexLabs.Ui.Maui.Controls.FastBorder;assembly=SevexLabs.Ui.Maui.Controls.FastBorder"
```

`FastBorder` is a lightweight alternative to MAUI `Border`, but it does not use
the same border property names. Use `BorderColor`, `BorderThickness`, and
`CornerRadius`.

Do not use MAUI `Border` properties such as `Stroke`, `StrokeThickness`, or
`StrokeShape` with `FastBorder`.

```xml
<fastBorder:FastBorder
    BorderColor="#4F46E5"
    BorderThickness="1"
    CornerRadius="12"
    Padding="16">
    <Label Text="Native-rendered FastBorder" />
</fastBorder:FastBorder>
```

## Gallery

### Basic

![FastBorder basic example](https://raw.githubusercontent.com/SeveX-Labs/SevexLabs.Ui.Maui.Controls.FastBorder/main/docs/images/fastborder-basic.png)

Simple border with `BorderColor`, `BorderThickness`, `Padding`, and text content.

### Rounded

![FastBorder rounded example](https://raw.githubusercontent.com/SeveX-Labs/SevexLabs.Ui.Maui.Controls.FastBorder/main/docs/images/fastborder-rounded.png)

Rounded card-style border with a visible background and inner content.

### Shadow

![FastBorder shadow example](https://raw.githubusercontent.com/SeveX-Labs/SevexLabs.Ui.Maui.Controls.FastBorder/main/docs/images/fastborder-shadow.png)

FastBorder with visible shadow offset, radius, and opacity.

### Clipping

![FastBorder clipping example](https://raw.githubusercontent.com/SeveX-Labs/SevexLabs.Ui.Maui.Controls.FastBorder/main/docs/images/fastborder-clipping.png)

Colored content clipped by rounded corners.

### Profiling

![Border vs FastBorder profiling view](https://raw.githubusercontent.com/SeveX-Labs/SevexLabs.Ui.Maui.Controls.FastBorder/main/docs/images/fastborder-profiling.png)

SampleApp profiling view for practical, scenario-dependent measurements.

## Local development with ProjectReference

This library is intended to be consumed primarily as a NuGet package.

For local development, debugging, or testing changes before publishing a new package, you can also clone the repository and reference the project directly with a `ProjectReference` from a consuming .NET MAUI app.

This mode is optional and should be treated as a development-only workflow. Normal consumers should use the NuGet package.

### Enable ProjectReference mode locally

To enable local `ProjectReference` mode, create a file named:

```text
Directory.Build.local.props
```

in the same directory as:

```text
Directory.Build.props
```

Do not commit this file. It is meant to contain local machine/developer settings only.

Recommended local configuration:

```xml
<Project>
	<PropertyGroup>
		<UseAsProjectReference>true</UseAsProjectReference>
		<OverrideAndroidSpecificVersion>36.0</OverrideAndroidSpecificVersion>
		<!-- Optional, only if the consuming app requires a specific MacCatalyst platform version. -->
		<!-- <OverrideMacCatalystSpecificVersion>26.0</OverrideMacCatalystSpecificVersion> -->
	</PropertyGroup>
</Project>
```

### What this does

By default, the project uses package-oriented, generic .NET MAUI platform TFMs, for example:

```text
net10.0-ios
net10.0-android
```

When `UseAsProjectReference` is enabled, the project can adjust its target frameworks to match the platform-specific target required by a consuming app.

For example, with:

```xml
<OverrideAndroidSpecificVersion>36.0</OverrideAndroidSpecificVersion>
```

the Android target becomes:

```text
net10.0-android36.0
```

This is useful when a consuming app targets a specific Android platform version and the library is referenced directly as a project instead of as a NuGet package.

If `UseAsProjectReference=true` is set and `OverrideAndroidSpecificVersion` is not provided, the project is configured to fall back to Android `36.0` for ProjectReference mode. Setting the value explicitly is still recommended because it makes the consuming setup easier to read.

### Optional iOS override

If a consuming app requires a specific iOS platform version, use:

```xml
<OverrideIosSpecificVersion>26.0</OverrideIosSpecificVersion>
```

In that case, the iOS target becomes:

```text
net10.0-ios26.0
```

If `OverrideIosSpecificVersion` is not set, the iOS target remains generic:

```text
net10.0-ios
```

### Optional MacCatalyst override

Projects that include MacCatalyst also support:

```xml
<OverrideMacCatalystSpecificVersion>26.0</OverrideMacCatalystSpecificVersion>
```

When set together with `UseAsProjectReference=true`, this changes the MacCatalyst target to:

```text
net10.0-maccatalyst26.0
```

### Important notes

- `Directory.Build.local.props` is for local development only.
- Do not commit `Directory.Build.local.props`.
- Normal NuGet builds and CI builds should run without this local file.
- When the local file is not present, `UseAsProjectReference` defaults to `false`.
- When `UseAsProjectReference` is `false`, the project uses its normal package-oriented target frameworks.
- If the project supports MacCatalyst, `OverrideMacCatalystSpecificVersion` can be used in the same way as the Android and iOS overrides.
- If you switch between package mode and project-reference mode, clean `bin` and `obj` folders before rebuilding.
- Restore and build should be performed in the same mode. If restore runs with local overrides enabled, build should use the same overrides.
- If Rider keeps building against an old Android/iOS target after switching modes, reload all projects. If the problem persists, use **File > Invalidate Caches...** and reopen the solution.

## Packing and testing the NuGet package locally

When creating or testing the NuGet package, make sure the local ProjectReference overrides are disabled. Otherwise the package can be produced with development-specific target frameworks.

Before packing, temporarily rename the local props file if it exists:

```bash
mv Directory.Build.local.props Directory.Build.local.props.disabled
```

Then clean generated folders from the repository root:

```bash
find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

Pack the library by calling `dotnet pack` directly on the library `.csproj`, not on the solution root:

```bash
dotnet pack SevexLabs.Ui.Maui.Controls.FastBorder/SevexLabs.Ui.Maui.Controls.FastBorder.csproj \
  -c Release \
  -o ./local-nuget
```

Packing the concrete library project avoids unintentionally building sample apps, tests, or other projects in the solution.

After packing, you can re-enable your local development settings:

```bash
mv Directory.Build.local.props.disabled Directory.Build.local.props
```

To inspect the generated package contents:

```bash
unzip -l ./local-nuget/SevexLabs.Ui.Maui.Controls.FastBorder.1.0.2.nupkg | grep "lib/"
```

With .NET MAUI/.NET 10, it is normal for the generated `.nupkg` to contain platform-normalized asset folders such as:

```text
lib/net10.0-android36.0/
lib/net10.0-ios26.0/
```

even when the project file declares generic TFMs such as `net10.0-android` or `net10.0-ios`. Those platform versions are resolved by the installed .NET SDK/workloads during build/pack.

To test the package without publishing it, add `./local-nuget` as a local NuGet source in a consuming app and use the normal `PackageReference` workflow. This is the best way to verify the package as a real consumer would use it.
