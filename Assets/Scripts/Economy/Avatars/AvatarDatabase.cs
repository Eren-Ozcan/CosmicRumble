using System.Collections.Generic;
using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/AvatarDatabase", fileName = "AvatarDatabase")]
    public class AvatarDatabase : ScriptableObject
    {
        public List<AvatarDefinition> allAvatars = new List<AvatarDefinition>();

        public AvatarDefinition GetById(string id)
        {
            foreach (var a in allAvatars)
                if (a != null && a.avatarId == id) return a;
            return null;
        }
    }
}
