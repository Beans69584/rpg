// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.ConsoleWindowManager.UpdateDisplaySettings(RPG.ConsoleDisplayConfig)")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Current implementation is simplified enough", Scope = "member", Target = "~M:RPG.ProceduralWorldGenerator.DetermineRegionType(System.Int32,System.Int32)~System.String")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.ProceduralWorldGenerator.GenerateLocationName(System.String)~System.String")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.ProceduralWorldGenerator.GenerateLocationDescription(System.String)~System.String")]
[assembly: SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "Needed for brevity in string", Scope = "member", Target = "~M:RPG.Program.ShowOptionsMenuAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.Program.ShowOptionsMenuAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.SaveData.FormatPlayTime(System.TimeSpan)~System.String")]
[assembly: SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Necessary implementation", Scope = "member", Target = "~M:RPG.ConsoleWindowManager.StopRendering")]
