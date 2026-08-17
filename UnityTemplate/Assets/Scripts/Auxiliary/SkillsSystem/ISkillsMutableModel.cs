using System;
using kekchpek.MVVM.Models.GameResources.Prices;
using SkillsSystem.Data;

namespace SkillsSystem 
{
    public interface ISkillsMutableModel : ISkillsModel
    {
        void SetConfig(SkillsConfig config);

        bool AddSkillSource(string skillId, ISkillSource skillSource);

        bool RemoveSkillSource(string skillId, ISkillSource skillSource);

        bool TryGetSkillSource(string skillId, out ISkillSource skillSource);

        void SetSkillPrice(string skillId, IPrice price);

        ReadOnlySpan<ISkillSource> GetSkillSources();
    }
}
