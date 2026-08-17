using System;
using AsyncReactAwait.Bindable;
using GameResources.Domain;
using kekchpek.Auxiliary.Collections;
using kekchpek.MVVM.Models.GameResources.Prices;
using SkillsSystem.Data;

namespace SkillsSystem 
{
    public interface ISkillsModel
    {

        ReadOnlySpan<SkillResourceData> GetSkillPrice(string skillId, int level);

        int GetConfigUpgradesCount(string skillId);

        IBindable<int> GetSkillLevel(string skillId);

        IBindable<IPrice> GetSkillPrice(string skillId);

        int GetInitialLevel(string skillId);

        string GetSkillNameKey(string skillId);

        string GetSkillDescriptionKey(string skillId);

        IBindable<string> GetCurrentValue(string skillId);

        IBindable<string> GetNextValue(string skillId);

        IBindable<int> GetMaxLevel(string skillId);

        IBindable<HyperListReadonlyToken<string>> GetDescriptionFormattingArgs(string skillId);

    }
}
