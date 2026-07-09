using UnityEngine;

namespace CosmicRumble.Economy
{
    [CreateAssetMenu(menuName = "CosmicRumble/Economy/AvatarDefinition", fileName = "AvatarDefinition")]
    public class AvatarDefinition : ScriptableObject
    {
        public string avatarId;
        public string displayName;
        public Sprite icon;          // null-safe: UI shows color+letter placeholder until real art exists
        public Color  placeholderColor;
    }
}
