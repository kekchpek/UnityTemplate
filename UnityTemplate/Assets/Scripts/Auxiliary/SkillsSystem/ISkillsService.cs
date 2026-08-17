using System;
using Cysharp.Threading.Tasks;

namespace SkillsSystem 
{
    public interface ISkillsService 
    {

        event Action<string, int> SkillUpgraded;

        UniTask Initialize();


        /// <summary>
        /// Applies skill sources operation to all registered skill sources
        /// and all skill sources, that will be registered in the future.
        /// </summary>
        /// <param name="operation">Operation to apply to all skill sources</param>
        void ApplySkillSourcesOperation(Action<ISkillSource> operation);

        void RegisterSkill(ISkillSource skillSource);

        void ClearSkill(ISkillSource skillSource);

        bool UpgradeSkill(string skillId);

        void SetSkillLevel(string skillId, int level);

    }
}