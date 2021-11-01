using System;

namespace NeuroSpeech.EntityAccessControl
{
    public class NoSecurityRulesDefinedException: Exception
    {
        public NoSecurityRulesDefinedException(string message = "No security rule defined")
            : base(message)
        {

        }
    }
}
