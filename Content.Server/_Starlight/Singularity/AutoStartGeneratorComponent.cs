namespace Content.Server._Starlight.Singularity;

/// <summary>
/// Makes a singularity generator hit its power threshold the moment it is spawned, kicking off the engine
/// spawn animation without needing a particle accelerator. Only meant for the debug generator prototypes.
/// </summary>
[RegisterComponent, Access(typeof(AutoStartGeneratorSystem))]
public sealed partial class AutoStartGeneratorComponent : Component;
