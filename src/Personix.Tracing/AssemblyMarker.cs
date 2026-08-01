namespace Personix.Tracing;

/// <summary>
/// Anchor type with no members of its own, used to locate this assembly by reflection — for example
/// <c>typeof(AssemblyMarker).Assembly</c> in an <c>ICleanArchitectureAssemblies</c> implementation for
/// <c>Personix.ArchitectureTests</c>.
/// </summary>
public sealed class AssemblyMarker;
