using System.Collections.Generic;

namespace LanguageServerWithoutUi
{
    public interface IStepService
    {
        IEnumerable<string> GetStepByFragment(string fragment);
        void AddStep(string step);
    }
}