using System;

namespace NeuroSpeech.EntityAccessControl
{
    /// <summary>
    /// 
    /// </summary>
    public class AppValidationException : Exception
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errors"></param>
        public AppValidationException(AppValidationErrors errors) : base(errors.Message)
        {
            this.Errors = errors;
        }

        /// <summary>
        /// 
        /// </summary>
        public AppValidationErrors Errors { get; }

    }
}
