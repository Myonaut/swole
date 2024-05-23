using System;

namespace Swole.Script
{

    [Serializable, Flags]
    public enum PermissionScope
    {
        None = 0, Admin = 1, SceneOnly = 2, ActiveOnly = 4, ExperienceOnly = 8, ObjectOnly = 16, CreateAssets = 32,
        GameplayExperience = ExperienceOnly | CreateAssets
    }

}
