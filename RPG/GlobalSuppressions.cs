// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.UI.ConsoleWindowManager.UpdateDisplaySettings(RPG.Core.ConsoleDisplayConfig)")]
[assembly: SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "Needed for brevity in string", Scope = "member", Target = "~M:RPG.Program.ShowOptionsMenuAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.Program.ShowOptionsMenuAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplifying any further would hurt readability", Scope = "member", Target = "~M:RPG.Save.SaveData.FormatPlayTime(System.TimeSpan)~System.String")]
[assembly: SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Necessary implementation", Scope = "member", Target = "~M:RPG.UI.ConsoleWindowManager.StopRendering")]
[assembly: SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "No need to, they will be caught by _", Scope = "member", Target = "~M:RPG.Utils.PathUtilities.GetApplicationFolder~System.String")]
[assembly: SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "<Pending>", Scope = "member", Target = "~M:RPG.Program.Main~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "<Pending>", Scope = "member", Target = "~M:RPG.Program.StartGameAsync(System.String)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Simplified enough already", Scope = "member", Target = "~M:RPG.World.Generation.ProceduralWorldGenerator.DetermineTerrainType(System.Single,System.Single)~RPG.World.Data.TerrainType")]