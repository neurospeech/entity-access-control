using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EntityAccessControl
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ModelPropertyAttribute: Attribute
    {
    }
}
