using System;
using System.Collections.Generic;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Unlock;
using UnityEngine;

namespace NinetyNine.Features.Profile
{
    [Serializable]
    public sealed class ProfileOption
    {
        public string id;
        public UnlockCondition unlock;
    }

    [CreateAssetMenu(menuName = "NinetyNine/Features/Profile Config", fileName = "ProfileFeatureConfig")]
    public sealed class ProfileFeatureConfig : ScriptableObject
    {
        [Tooltip("The first unlocked avatar is applied on a fresh profile.")]
        public List<ProfileOption> avatars = new();

        public List<ProfileOption> titles = new();

        public int freeRenames = 1;
        public Cost renameCost = new("gem", 50);

        public ProfileOption FindAvatar(string id) => avatars.Find(o => o.id == id);

        public ProfileOption FindTitle(string id) => titles.Find(o => o.id == id);
    }
}
