using System.Collections.Generic;

namespace NeuroSpeech.EntityAccessControl
{
    /// <summary>
    /// 
    /// </summary>
    public class AppValidationErrors
    {

        /// <summary>
        /// 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AppValidationError> Errors { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Details { get; set; }

    }
}
