using System.Collections.Generic;

namespace LanguageServerWithoutUi
{
    public class ListStepService : IStepService
    {
        private readonly List<string> _stepList = new List<string>();
        
        public IEnumerable<string> GetStepByFragment(string fragment)
        {
            return _stepList.FindAll(step => step.Contains(fragment));
        }

        public void AddStep(string step)
        {
            if (!_stepList.Contains(step))
            {
                _stepList.Add(step);
            }
        }
    }
}